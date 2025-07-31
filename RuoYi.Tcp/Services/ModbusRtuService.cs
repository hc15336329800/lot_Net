using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Iot.Services;
using System.Net;
using Microsoft.Extensions.Options;
using RuoYi.Tcp.Configs;


namespace RuoYi.Tcp.Services
{

    /// <summary>
    /// 联合主键：从机地址 + 寄存器地址
    /// </summary>
    public readonly struct ModbusKey : IEquatable<ModbusKey>
    {
        public byte SlaveAddress { get; }
        public ushort RegisterAddress { get; }
        public ModbusKey(byte slave,ushort reg)
        {
            SlaveAddress = slave;
            RegisterAddress = reg;
        }
        public bool Equals(ModbusKey other) => SlaveAddress == other.SlaveAddress && RegisterAddress == other.RegisterAddress;
        public override bool Equals(object? obj) => obj is ModbusKey other && Equals(other);
        public override int GetHashCode( ) => HashCode.Combine(SlaveAddress,RegisterAddress);
        public override string ToString( ) => $"Slave:{SlaveAddress}-Reg:{RegisterAddress}";
    }



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
        private readonly TcpServerOptions _options;
        private readonly ITcpResponseListener? _responseListener;



        private readonly ConcurrentDictionary<long,DeviceConnection> _connections = new();// 存储活动的设备连接

        // 保存最近一次发送的读请求起始寄存器地址，key 为从机地址
        // 但你必须保证所有的包都是你自己主发的，不能直接接收设备主动推送的响应包！,直接接受的话就默认0
        private readonly ConcurrentDictionary<byte,ushort> _lastReadStartAddrs = new();

        /// <summary>
        /// 最近一次读取的起始寄存器地址
        /// </summary>
        public ConcurrentDictionary<byte,ushort> LastReadStartAddrs => _lastReadStartAddrs;


        public ModbusRtuService(ILogger<ModbusRtuService> logger,
            IotDeviceService deviceService,
            IotProductPointService pointService,
           IotDeviceVariableService variableService,
                 IOptions<TcpServerOptions> options,
            ITcpResponseListener? responseListener = null)
        {
            _logger = logger;
            _deviceService = deviceService;
            _pointService = pointService;
            _variableService = variableService;
            _options = options.Value;
            _responseListener = responseListener;
        }

        /// <summary>
        /// 处理 Modbus RTU 客户端连接，在同一个连接上持续接收并响应报文。
        /// TCP 服务接口，当收到信息时候动态解析存库
        /// </summary>
        public async Task HandleClientAsync(TcpClient client,IotDeviceDto device,CancellationToken token)
        {

            //最终作用（这句话的意义）
            //你后面收到Modbus报文时，报文里有寄存器地址（如01、02），
            //你能快速用 pointMap 找到这个地址对应的点位配置（如数据类型、名称等），
            //然后用点位的key，再从 varMap 找到设备变量明细的主键和存储位置，
            //最终将解析的数据存入设备变量表，实现“设备级数据存库”。

            // 加载点位映射和变量映射，便于后续解析后存库
            Dictionary<ModbusKey,List<IotProductPointDto>>? pointMap = null; // 修改类型

            Dictionary<string,IotDeviceVariableDto>? varMap = null; //点位key（pointKey/variableKey）→ 设备变量明细（iot_device_variable）。
            
            
            
            //------------------------------------------------------处理注册包
            
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
                    // 改为：支持一个寄存器+从机可挂多个点位（如1:N情况）

                    // 使用 ToList() 复制集合，避免后续操作时被意外修改
                    var pointsList = points.ToList();

                    pointMap = pointsList
                     .Where(p => p.RegisterAddress.HasValue && p.SlaveAddress.HasValue) //只保留有从机地址和有寄存器地址的点位（因为这些点位才能映射到Modbus物理地址上）。
                     .GroupBy(p => new ModbusKey((byte)p.SlaveAddress!.Value,(ushort)p.RegisterAddress!.Value)) //把所有点位，按“从机地址 + 寄存器地址”分组。
                     .ToDictionary(g => g.Key,g => g.ToList());

                }


                //作用：设备上报一个变量 "shidu"，程序解析出来要存库，只要查 varMap["shidu"] 就能拿到 IotDeviceVariableDto，里面有 VariableId 和其它字段。
                //- 目的是不用每次存库都查表，提高性能和一致性。
                if(_variableService != null)
                {
                    var map = await _variableService.GetVariableMapAsync(device.Id);
                    // 创建副本以避免在枚举时集合被修改
                    varMap = new Dictionary<string,IotDeviceVariableDto>(map);
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


            //------------------------------------------------------处理应答信息

            try
            {
                var stream = client.GetStream();
                var buffer = new byte[256]; // 最大包长度，够用即可


                // 注册包不符合规范 ，会直接跳出（目前包字节小于5）
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

                    _responseListener?.OnTcpDataReceived(device.Id,recv);

                    byte slaveAddr = recv[0]; // 设备地址
                    byte func = recv[1];      // 功能码


                    // === 3. 只做功能码 0x03/0x04 响应帧  ,注意此处是解析响应帧格式！！！ ===
                    if((func == 0x03 || func == 0x04) && pointMap != null && varMap != null)
                    {
                        // 根据 Modbus RTU 响应帧格式：[slave][func][byteCount][data][CRC]


                        // 响应帧格式
                        int byteCount = recv[2];
                        if(recv.Length < 3 + byteCount + 2)
                            continue; // 帧长度异常

                        var dataBytes = recv.Skip(3).Take(byteCount).ToArray();

                        _lastReadStartAddrs.TryGetValue(slaveAddr,out ushort startAddr);


                        // 打印当前点位映射和变量映射数量
                        Console.WriteLine($"【调试】pointMap.Count={pointMap?.Count}, varMap.Count={varMap?.Count}");

 
                        // 报文解析循环中查找点位
                        for(int i = 0; i < byteCount / 2; i++)
                        {
                            ushort regAddr = (ushort)(startAddr + i); // 注意点

                            var key = new ModbusKey(slaveAddr,regAddr);

                            if(pointMap.TryGetValue(key,out var pointList))
                            {
                                foreach(var point in pointList)
                                {
                                    if(point.PointKey != null && varMap.TryGetValue(point.PointKey,out var varDto) && varDto.VariableId.HasValue)
                                    {
                                        var singleRegBytes = dataBytes.Skip(i * 2).Take(2).ToArray();
                                        var value = ParseValue(singleRegBytes,point.DataType,point.ByteOrder,point.Signed ?? false);

                                        Console.WriteLine($"【更新当前值并记录历史】设备：{device.DeviceName}，从机：{slaveAddr}，寄存器：{regAddr}，点位：{point.PointKey}，值：{value}");

                                        await _variableService!.SaveValueAsync(device.Id,varDto.VariableId.Value,point.PointKey,value);

                                        string msg = $"【更新当前值并记录历史】设备：{device.DeviceName}，从机：{slaveAddr}，寄存器：{regAddr}，点位：{point.PointKey}，值：{value}";
                                        _logger.LogDebug(msg);
                                        Console.WriteLine(msg);
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine($"【警告】未匹配到点位，从机={slaveAddr}，寄存器={regAddr}");
                            }
                        }
                    }
                    else if(func == 0x06 && pointMap != null && varMap != null)
                    {
                        // 写单寄存器响应
                       //    todo： 0x06功能码解析也需用ModbusKey（不能再用ushort直查）
 

                        ushort addr = (ushort)((recv[2] << 8) | recv[3]);
                        byte slave = recv[0];
                        var key = new ModbusKey(slave,addr);
                        if(pointMap.TryGetValue(key,out var pointList))
                        {
                            foreach(var point in pointList)
                            {
                                if(point.PointKey != null && varMap.TryGetValue(point.PointKey,out var varDto) && varDto.VariableId.HasValue)
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


 



        //=================================================================================================================================

        /// <summary>
        /// 重写的 ExecuteAsync 方法，负责执行后台服务的异步任务。
        /// 它定期轮询设备连接并执行相关操作。
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await LoadDevicesAsync(stoppingToken); // 加载所有设备信息
            while(!stoppingToken.IsCancellationRequested)// 如果没有取消请求，持续处理
            {
                //foreach(var conn in _connections.Values)
                //{
                //    await conn.PollAsync(stoppingToken);// 对每个设备连接进行轮询操作
                //}
                //await Task.Delay(TimeSpan.FromSeconds(1),stoppingToken);// 延迟1秒钟后再继续

                var tasks = _connections.Values.Select(c => c.PollAsync(stoppingToken));
                await Task.WhenAll(tasks);
                await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds),stoppingToken);
            }
        }

        /// <summary>
        /// 加载所有设备，并为每个设备创建设备连接。
        /// </summary>
        private async Task LoadDevicesAsync(CancellationToken token)
        {

            // 获取所有状态正常且未删除的设备列表
            var devices = await _deviceService.GetDtoListAsync(new IotDeviceDto { Status = "0",DelFlag = "0" });
            string[] selfAddresses = { "127.0.0.1","localhost" }; // 本机IP、localhost等
            int serverPort = 5003; // todo: 这里需要appsettings.json中取配置


            foreach(var d in devices)
            {
                // 如果设备没有 TCP 主机或端口信息，则跳过
                if(d.TcpHost == null || d.TcpPort == null) continue;

                // 判断是否等于API端口
                if(selfAddresses.Contains(d.TcpHost) && d.TcpPort == serverPort)
                {
                    _logger.LogError($"【配置错误】设备 {d.DeviceName} 的 tcp_host/tcp_port 指向了本服务器API监听端口({d.TcpHost}:{d.TcpPort})，请勿与服务端口一致，否则会产生死循环和数据异常！");
                    continue; // 跳过该设备，不进行轮询
                }

                // 获取该设备关联的所有产品点
                var points = await _pointService.GetDtoListAsync(new IotProductPointDto { ProductId = d.ProductId,Status = "0",DelFlag = "0" });

                // 为并发安全复制集合
                var pointsList = points.ToList();

                // 获取该设备的变量映射
                var map = await _variableService.GetVariableMapAsync(d.Id);
                var variableMap = new Dictionary<string,IotDeviceVariableDto>(map);


                // 创建设备连接并存储在 _connections 中
                _connections[d.Id] = new DeviceConnection(this,d,pointsList,variableMap,_variableService,_logger);
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
        private static ushort ComputeCrc(byte[] data) => RuoYi.Common.Utils.ModbusUtils.ComputeCrc(data);


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
            private readonly ModbusRtuService _service;

            private readonly IotDeviceDto _device;// 设备信息
            private readonly List<IotProductPointDto> _points;// 设备的产品点列表
            private readonly Dictionary<string,IotDeviceVariableDto> _variableMap;// 设备变量映射
            private readonly IotDeviceVariableService _variableService;// 变量服务
            private readonly ILogger _logger;// 日志记录器
            private TcpClient? _client;// TCP 客户端
            private NetworkStream? _stream;// 网络流

            public DeviceConnection(ModbusRtuService service,IotDeviceDto device,List<IotProductPointDto> points,Dictionary<string,IotDeviceVariableDto> variableMap,IotDeviceVariableService variableService,ILogger logger)
            {
                _service = service;
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
            private   byte[]? BuildReadFrame(IotProductPointDto point)
            {
                if(point.SlaveAddress == null || point.FunctionCode == null || point.RegisterAddress == null) return null;
                ushort qty = (ushort)(point.DataLength ?? 1);// 读取数据长度
                byte slave = (byte)point.SlaveAddress.Value;
                byte func = (byte)point.FunctionCode.Value;

                return RuoYi.Common.Utils.ModbusUtils.BuildReadFrame(slave,func,(ushort)point.RegisterAddress.Value,qty,_service.LastReadStartAddrs);
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
            /// </summary>var pointsList = points.ToList();
            private static ushort ComputeCrc(byte[] data) => RuoYi.Common.Utils.ModbusUtils.ComputeCrc(data);


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
