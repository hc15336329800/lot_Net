using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Iot.Services;

namespace RuoYi.Tcp.Services
{
    /// <summary>
    /// ModbusRtu 数据协议 （默认协议） 
    /// 该类处理 Modbus RTU 通信，它是默认协议。
    /// </summary>
    public class ModbusRtuService : BackgroundService, ITcpService
    {
        private readonly ILogger<ModbusRtuService> _logger; // 用于记录服务的日志
        private readonly IotDeviceService _deviceService;// 用于与 IoT 设备交互的服务
        private readonly IotProductPointService _pointService;// 用于与 IoT 产品点交互的服务
        private readonly IotDeviceVariableService _variableService;// 用于与设备变量交互的服务

        private readonly ConcurrentDictionary<long,DeviceConnection> _connections = new();// 存储活动的设备连接

        public ModbusRtuService(ILogger<ModbusRtuService> logger,
            IotDeviceService deviceService,
            IotProductPointService pointService,
            IotDeviceVariableService variableService)
        {
            _logger = logger;
            _deviceService = deviceService;
            _pointService = pointService;
            _variableService = variableService;
        }

        /// <summary>
        /// 处理 Modbus RTU 客户端连接，在同一个连接上持续接收并响应报文。
        /// </summary>
        public async Task HandleClientAsync(TcpClient client,IotDeviceDto device,CancellationToken token)
        {
            // 加载点位映射和变量映射，便于后续解析后存库
            Dictionary<ushort,IotProductPointDto>? pointMap = null;
            Dictionary<string,IotDeviceVariableDto>? varMap = null;
            try
            {
                if(_pointService != null && device.ProductId.HasValue)
                {
                    var points = await _pointService.GetDtoListAsync(new IotProductPointDto
                    {
                        ProductId = device.ProductId,
                        Status = "0",
                        DelFlag = "0"
                    });
                    // 只保留第一个重复的寄存器点位
                    pointMap = points
                        .Where(p => p.RegisterAddress.HasValue)
                        .GroupBy(p => (ushort)p.RegisterAddress!.Value)
                        .ToDictionary(g => g.Key,g => g.First());

                }

                if(_variableService != null)
                {
                    varMap = await _variableService.GetVariableMapAsync(device.Id);
                 }

                // 【调试】输出变量映射表内容和数量，判断是否为空
                if(varMap == null || varMap.Count == 0)
                {
                    Console.WriteLine($"【调试警告】设备 {device.DeviceName} 的变量映射 varMap 为空，请检查变量配置或GetVariableMapAsync实现！");
                }
                else
                {
                    Console.WriteLine($"【调试】设备 {device.DeviceName} 的变量映射 varMap.Count={varMap.Count}，keys=" + string.Join(",",varMap.Keys));
                }
            }
            catch(Exception ex)
            {
                _logger.LogDebug(ex,"Failed to load mapping for device {Device}",device.DeviceName);
                Console.WriteLine($"【异常】加载设备映射关系失败：{device.DeviceName}，异常信息：{ex.Message}");
            }

            try
            {
                var stream = client.GetStream();
                var buffer = new byte[256]; // 最大包长度，够用即可

                while(!token.IsCancellationRequested)
                {
                    // === 1. 读取一包（建议按8或最大帧长读取，也可以自适应读取再判断包长） ===
                    int readLen = await stream.ReadAsync(buffer,0,buffer.Length,token);
                    if(readLen < 5) // Modbus最短帧为5字节
                        break;

                    var recv = buffer.Take(readLen).ToArray();

                    // === 2. 校验CRC，仅处理合法包 ===
                    if(!ValidateCrc(recv))
                    {
                        _logger.LogDebug("CRC校验失败，设备：{Device}",device.DeviceName);
                        Console.WriteLine($"【警告】CRC校验失败，设备：{device.DeviceName}，数据：" +
                            BitConverter.ToString(recv).Replace("-"," "));
                        continue;
                    }

                    byte slaveAddr = recv[0]; // 设备地址
                    byte func = recv[1];      // 功能码

                    // === 3. 根据不同功能码处理 ===
                    if((func == 0x03 || func == 0x04) && pointMap != null && varMap != null)
                    {
                        // 读保持/输入寄存器响应
                        byte byteCount = recv[2]; // 数据区长度（字节数）
                        if(recv.Length < 3 + byteCount + 2)
                             continue; // 帧长度异常

                        var dataBytes = recv.Skip(3).Take(byteCount).ToArray();

                        // 打印当前点位映射和变量映射数量
                        Console.WriteLine($"【调试】pointMap.Count={pointMap?.Count}, varMap.Count={varMap?.Count}");


                        // 由于一次请求可能读多个寄存器，每2字节一个寄存器
                        for(int i = 0; i < byteCount / 2; i++)
                        {
                            ushort regAddr = 0;
                            // 通常可以通过查询请求时的寄存器起始地址来定位真实点位，这里举例假设连续映射
                            // 你可自定义寄存器基址，这里用i叠加
                            var basePoint = pointMap.Values.OrderBy(p => p.RegisterAddress).FirstOrDefault();
                            if(basePoint != null)
                                regAddr = (ushort)(basePoint.RegisterAddress!.Value + i);

                            // ★ 1. 显示当前要解析的寄存器地址
                            Console.WriteLine($"【调试】正在解析寄存器地址：{regAddr}");


                            if(pointMap.TryGetValue(regAddr,out var point) && point.PointKey != null
                                && varMap.TryGetValue(point.PointKey,out var varDto) && varDto.VariableId.HasValue)
                            {
                                // 拿2字节数据
                                var singleRegBytes = dataBytes.Skip(i * 2).Take(2).ToArray();
                                var value = ParseValue(singleRegBytes,point.DataType,point.ByteOrder,point.Signed ?? false);

                                // ★ 2. 存库前打印准备写入信息
                                Console.WriteLine($"【准备存库】设备：{device.DeviceName}，功能码：{func:X2}，寄存器地址：{regAddr}，点位：{point.PointKey}，值：{value}");


                                // 存库
                                await _variableService!.SaveValueAsync(device.Id,varDto.VariableId.Value,point.PointKey,value);

                                // 日志记录
                                string msg = $"【存库成功】设备：{device.DeviceName}，功能码：{func:X2}，寄存器地址：{regAddr}，点位：{point.PointKey}，值：{value}";
                                _logger.LogDebug(msg);
                                Console.WriteLine(msg);


                            }
                            else
                            {
                                // ★ 3. 没查到映射时提示
                                Console.WriteLine($"【警告】未匹配到点位或变量，寄存器地址={regAddr}");
                            }
                        }
                    }
                    else if(func == 0x06 && pointMap != null && varMap != null)
                    {
                        // 写单寄存器响应
                        ushort addr = (ushort)((recv[2] << 8) | recv[3]);
                        if(pointMap.TryGetValue(addr,out var point) && point.PointKey != null
                            && varMap.TryGetValue(point.PointKey,out var varDto) && varDto.VariableId.HasValue)
                        {
                            var dataBytes = new[] { recv[4],recv[5] };
                            var value = ParseValue(dataBytes,point.DataType,point.ByteOrder,point.Signed ?? false);

                            // 存库
                            await _variableService!.SaveValueAsync(device.Id,varDto.VariableId.Value,point.PointKey,value);

                            // 日志记录
                            string msg = $"【写入成功】设备：{device.DeviceName}，功能码：{func:X2}，寄存器地址：{addr}，点位：{point.PointKey}，值：{value}";
                            _logger.LogDebug(msg);
                            Console.WriteLine(msg);
                        }
                    }
                    // 可扩展更多功能码解析

                    // === 4. 回复或继续循环，保持连接 ===
                    // 若需要回包可在此发送，如需要透传可直接Write
                    // 将请求原样回写，保持连接
                    // 日志记录
                    Console.WriteLine("time:" + DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fffffff")
                        + " 收到Modbus包：" + BitConverter.ToString(recv).Replace("-"," "));


                    await stream.WriteAsync(recv,0,recv.Length,token);

                }
            }
            catch(Exception ex)
            {
                _logger.LogDebug(ex,"Error processing Modbus RTU client for device {Device}",device.DeviceName);
                Console.WriteLine($"【异常】处理Modbus RTU客户端异常，设备：{device.DeviceName}，异常信息：{ex.Message}");

            }
            finally
            {
                try { client.Dispose(); } catch { }
                Console.WriteLine($"【断开连接】设备：{device.DeviceName} 已断开。");

            }
        }



        /// <summary>
        /// 重写的 ExecuteAsync 方法，负责执行后台服务的异步任务。
        /// 它定期轮询设备连接并执行相关操作。
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await LoadDevicesAsync(stoppingToken); // 加载所有设备信息
            while(!stoppingToken.IsCancellationRequested)// 如果没有取消请求，持续处理
            {
                foreach(var conn in _connections.Values)
                {
                    await conn.PollAsync(stoppingToken);// 对每个设备连接进行轮询操作
                }
                await Task.Delay(TimeSpan.FromSeconds(1),stoppingToken);// 延迟1秒钟后再继续
            }
        }

        /// <summary>
        /// 加载所有设备，并为每个设备创建设备连接。
        /// </summary>
        private async Task LoadDevicesAsync(CancellationToken token)
        {
            // 获取所有状态正常且未删除的设备列表

            var devices = await _deviceService.GetDtoListAsync(new IotDeviceDto { Status = "0",DelFlag = "0" });
            foreach(var d in devices)
            {
                // 如果设备没有 TCP 主机或端口信息，则跳过
                if(d.TcpHost == null || d.TcpPort == null) continue;
                // 获取该设备关联的所有产品点
                var points = await _pointService.GetDtoListAsync(new IotProductPointDto { ProductId = d.ProductId,Status = "0",DelFlag = "0" });
                // 获取该设备的变量映射
                var variableMap = await _variableService.GetVariableMapAsync(d.Id);
                // 创建设备连接并存储在 _connections 中
                _connections[d.Id] = new DeviceConnection(d,points,variableMap,_variableService,_logger);
            }
        }


        /// <summary>
        /// 向指定设备的指定点写入数据。
        /// </summary>
        public async Task<bool> WriteAsync(long deviceId,string pointKey,string value,CancellationToken token = default)
        {
            // 查找设备连接
            if(!_connections.TryGetValue(deviceId,out var conn)) return false;
            // 调用设备连接的写入方法
            return await conn.WriteAsync(pointKey,value,token);
        }

        /// <summary>
        /// 从网络流中精确读取指定长度的数据。
        /// </summary>
        private static async Task<bool> ReadExactAsync(NetworkStream stream,byte[] buffer,int length,CancellationToken token)
        {
            int read = 0;
            while(read < length)
            {
                var r = await stream.ReadAsync(buffer,read,length - read,token);
                if(r == 0) return false;
                read += r;
            }
            return true;
        }

        /// <summary>
        /// 解析返回的值，根据数据类型和字节顺序处理。
        /// </summary>
        private static string ParseValue(byte[] data,string? dataType,string? order,bool signed)
        {
            var buf = ApplyByteOrder(data,order);
            if(string.Equals(dataType,"float",StringComparison.OrdinalIgnoreCase) && buf.Length >= 4)
            {
                if(BitConverter.IsLittleEndian) Array.Reverse(buf);
                return BitConverter.ToSingle(buf).ToString();
            }
            if(buf.Length >= 2)
            {
                if(BitConverter.IsLittleEndian) Array.Reverse(buf);
                return signed ? BitConverter.ToInt16(buf,0).ToString() : BitConverter.ToUInt16(buf,0).ToString();
            }
            return BitConverter.ToString(buf);
        }

        /// <summary>
        /// 根据字节顺序应用字节序
        /// </summary>
        private static byte[] ApplyByteOrder(byte[] bytes,string? order)
        {
            if(string.IsNullOrEmpty(order) || order.Equals("ABCD",StringComparison.OrdinalIgnoreCase) || bytes.Length < 4)
                return bytes;
            return order.ToUpper() switch
            {
                "DCBA" => bytes.Reverse().ToArray(),
                "BADC" => new[] { bytes[1],bytes[0],bytes[3],bytes[2] },
                "CDAB" => new[] { bytes[2],bytes[3],bytes[0],bytes[1] },
                _ => bytes
            };
        }

        /// <summary>
        /// 计算 CRC 校验码
        /// </summary>
        private static ushort ComputeCrc(byte[] data)
        {
            ushort crc = 0xFFFF;
            foreach(var b in data)
            {
                crc ^= b;
                for(int i = 0; i < 8; i++)
                {
                    if((crc & 1) != 0) crc = (ushort)((crc >> 1) ^ 0xA001);
                    else crc >>= 1;
                }
            }
            return crc;
        }

        /// <summary>
        /// 验证 CRC 校验
        /// </summary>
        private static bool ValidateCrc(byte[] frame)
        {
            if(frame.Length < 3) return false;
            ushort crcCalc = ComputeCrc(frame.AsSpan(0,frame.Length - 2).ToArray());
            ushort crcFrame = (ushort)(frame[^2] | (frame[^1] << 8));
            return crcCalc == crcFrame;
        }


        /// <summary>
        /// 设备连接类，负责与设备建立连接并进行数据读写。
        /// </summary>
        private class DeviceConnection
        {
            private readonly IotDeviceDto _device;// 设备信息
            private readonly List<IotProductPointDto> _points;// 设备的产品点列表
            private readonly Dictionary<string,IotDeviceVariableDto> _variableMap;// 设备变量映射
            private readonly IotDeviceVariableService _variableService;// 变量服务
            private readonly ILogger _logger;// 日志记录器
            private TcpClient? _client;// TCP 客户端
            private NetworkStream? _stream;// 网络流

            public DeviceConnection(IotDeviceDto device,List<IotProductPointDto> points,Dictionary<string,IotDeviceVariableDto> variableMap,IotDeviceVariableService variableService,ILogger logger)
            {
                _device = device;
                _points = points;
                _variableMap = variableMap;
                _variableService = variableService;
                _logger = logger;
            }

            /// <summary>
            /// 对设备进行轮询操作，定期读取设备数据。
            /// </summary>
            public async Task PollAsync(CancellationToken token)
            {
                try
                {
                    await EnsureConnectedAsync(token); // 确保连接已建立
                    foreach(var p in _points)// 遍历设备点
                    {
                        if(p.FunctionCode == null || p.RegisterAddress == null) continue;
                        var req = BuildReadFrame(p);// 构建读取请求帧
                        if(req == null) continue;
                        await _stream!.WriteAsync(req,0,req.Length,token);// 发送请求帧
                        var byteCount = (p.DataLength ?? 1) * 2;// 数据字节数
                        var buffer = new byte[5 + byteCount];
                        if(!await ReadExactAsync(_stream!,buffer,buffer.Length,token))
                        {
                            Close();// 读取失败时关闭连接
                            return;
                        }
                        if(!ValidateCrc(buffer))// 验证 CRC 校验
                        {
                            _logger.LogDebug("CRC error from {Device}",_device.DeviceName);
                            continue;
                        }
                        var dataBytes = buffer.Skip(3).Take(byteCount).ToArray();// 提取数据
                        var value = ParseValue(dataBytes,p.DataType,p.ByteOrder,p.Signed ?? false); // 解析数据值
                        if(_variableMap.TryGetValue(p.PointKey ?? string.Empty,out var varDto) && varDto.VariableId.HasValue)
                        {
                            // 保存数据值
                            await _variableService.SaveValueAsync(_device.Id,varDto.VariableId.Value,p.PointKey!,value);
                        }
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogDebug(ex,"Poll device {Device} failed",_device.DeviceName); ; // 记录异常
                    Close();// 关闭连接
                }
            }


            /// <summary>
            /// 向指定点写入数据值。
            /// </summary>
            public async Task<bool> WriteAsync(string pointKey,string value,CancellationToken token)
            {
                var p = _points.FirstOrDefault(pp => pp.PointKey == pointKey);// 查找指定点
                if(p == null) return false;
                try
                {
                    await EnsureConnectedAsync(token);// 确保连接已建立
                    var frame = BuildWriteFrame(p,value); // 构建写入请求帧
                    if(frame == null) return false;
                    await _stream!.WriteAsync(frame,0,frame.Length,token);// 发送写入帧
                    var resp = new byte[frame.Length];
                    if(!await ReadExactAsync(_stream!,resp,resp.Length,token)) return false;
                    return ValidateCrc(resp); // 验证写入响应的 CRC 校验
                }
                catch(Exception ex)
                {
                    _logger.LogDebug(ex,"Write device {Device} failed",_device.DeviceName);
                    Close(); // 关闭连接
                    return false;
                }
            }


            /// <summary>
            /// 确保与设备建立连接，如果没有连接则重新建立连接。
            /// </summary>
            private async Task EnsureConnectedAsync(CancellationToken token)
            {
                if(_client != null && _client.Connected) return;// 如果已连接则返回
                Close();// 关闭旧连接
                _client = new TcpClient();// 创建新连接
                await _client.ConnectAsync(_device.TcpHost!,_device.TcpPort!.Value,token); // 连接设备
                _stream = _client.GetStream();// 获取网络流
            }

            /// <summary>
            /// 关闭设备连接并清理资源。
            /// </summary>
            private void Close( )
            {
                try { _stream?.Dispose(); } catch { }// 释放流资源
                try { _client?.Close(); } catch { }// 关闭客户端连接
                _stream = null;
                _client = null;
            }


            /// <summary>
            /// 从网络流中精确读取指定长度的数据。
            /// </summary>
            private static async Task<bool> ReadExactAsync(NetworkStream stream,byte[] buffer,int length,CancellationToken token)
            {
                int read = 0;
                while(read < length)
                {
                    var r = await stream.ReadAsync(buffer,read,length - read,token);
                    if(r == 0) return false; // 如果读取失败，则返回 false
                    read += r;
                }
                return true;
            }



            /// <summary>
            /// 构建读取数据请求帧。
            /// </summary>
            private static byte[]? BuildReadFrame(IotProductPointDto point)
            {
                if(point.SlaveAddress == null || point.FunctionCode == null || point.RegisterAddress == null) return null;
                ushort qty = (ushort)(point.DataLength ?? 1);// 读取数据长度
                byte slave = (byte)point.SlaveAddress.Value;
                byte func = (byte)point.FunctionCode.Value;
                byte hiAddr = (byte)(point.RegisterAddress.Value >> 8);
                byte loAddr = (byte)(point.RegisterAddress.Value & 0xFF);
                byte hiQty = (byte)(qty >> 8);
                byte loQty = (byte)(qty & 0xFF);
                var list = new List<byte> { slave,func,hiAddr,loAddr,hiQty,loQty };
                ushort crc = ComputeCrc(list.ToArray());// 计算 CRC 校验码
                list.Add((byte)(crc & 0xFF));
                list.Add((byte)(crc >> 8));
                return list.ToArray(); // 返回请求帧
            }



            /// <summary>
            /// 构建写入数据请求帧。
            /// </summary>
            private static byte[]? BuildWriteFrame(IotProductPointDto point,string value)
            {
                if(point.SlaveAddress == null || point.FunctionCode == null || point.RegisterAddress == null) return null;
                byte slave = (byte)point.SlaveAddress.Value;
                byte func = (byte)point.FunctionCode.Value;
                ushort addr = (ushort)point.RegisterAddress.Value;
                byte[] data;
                try
                {
                    data = BuildDataBytes(value,point);
                }
                catch
                {
                    return null;
                }
                var frame = new List<byte> { slave,func,(byte)(addr >> 8),(byte)(addr & 0xFF) };
                if(func == 0x10)
                {
                    ushort qty = (ushort)(point.DataLength ?? (data.Length / 2));
                    frame.Add((byte)(qty >> 8));
                    frame.Add((byte)(qty & 0xFF));
                    frame.Add((byte)data.Length);
                }
                frame.AddRange(data);
                ushort crc = ComputeCrc(frame.ToArray());
                frame.Add((byte)(crc & 0xFF));
                frame.Add((byte)(crc >> 8));
                return frame.ToArray();
            }



            /// <summary>
            /// 构建数据字节，支持简单的 UInt16/Int16/Float32 转换。
            /// </summary>
            private static byte[] BuildDataBytes(string value,IotProductPointDto point)
            {
                // only simple UInt16/Int16/Float32 conversions handled
                var order = point.ByteOrder;
                if(string.Equals(point.DataType,"float",StringComparison.OrdinalIgnoreCase))
                {
                    float v = float.Parse(value);
                    var bytes = BitConverter.GetBytes(v);
                    if(BitConverter.IsLittleEndian) Array.Reverse(bytes);
                    return ApplyByteOrder(bytes,order);
                }
                else
                {
                    short v = short.Parse(value);
                    var bytes = BitConverter.GetBytes(v);
                    if(BitConverter.IsLittleEndian) Array.Reverse(bytes);
                    return ApplyByteOrder(bytes,order);
                }
            }



            /// <summary>
            /// 解析返回的值，根据数据类型和字节顺序处理。
            /// </summary>
            private static string ParseValue(byte[] data,string? dataType,string? order,bool signed)
            {
                var buf = ApplyByteOrder(data,order);
                if(string.Equals(dataType,"float",StringComparison.OrdinalIgnoreCase) && buf.Length >= 4)
                {
                    if(BitConverter.IsLittleEndian) Array.Reverse(buf);
                    return BitConverter.ToSingle(buf).ToString();
                }
                if(buf.Length >= 2)
                {
                    if(BitConverter.IsLittleEndian) Array.Reverse(buf);
                    return signed ? BitConverter.ToInt16(buf,0).ToString() : BitConverter.ToUInt16(buf,0).ToString();
                }
                return BitConverter.ToString(buf);
            }


            /// <summary>
            /// 根据字节顺序应用字节序
            /// </summary>
            private static byte[] ApplyByteOrder(byte[] bytes,string? order)
            {
                if(string.IsNullOrEmpty(order) || order.Equals("ABCD",StringComparison.OrdinalIgnoreCase) || bytes.Length < 4)
                    return bytes;
                return order.ToUpper() switch
                {
                    "DCBA" => bytes.Reverse().ToArray(),
                    "BADC" => new[] { bytes[1],bytes[0],bytes[3],bytes[2] },
                    "CDAB" => new[] { bytes[2],bytes[3],bytes[0],bytes[1] },
                    _ => bytes
                };
            }


            /// <summary>
            /// 计算 CRC 校验码
            /// </summary>
            private static ushort ComputeCrc(byte[] data)
            {
                ushort crc = 0xFFFF;
                foreach(var b in data)
                {
                    crc ^= b;
                    for(int i = 0; i < 8; i++)
                    {
                        if((crc & 1) != 0) crc = (ushort)((crc >> 1) ^ 0xA001);
                        else crc >>= 1;
                    }
                }
                return crc;
            }

            /// <summary>
            /// 验证 CRC 校验
            /// </summary>
            private static bool ValidateCrc(byte[] frame)
            {
                if(frame.Length < 3) return false;
                ushort crcCalc = ComputeCrc(frame.AsSpan(0,frame.Length - 2).ToArray());
                ushort crcFrame = (ushort)(frame[^2] | (frame[^1] << 8));
                return crcCalc == crcFrame;// 验证 CRC 校验码是否匹配
            }
        }
    }
}
