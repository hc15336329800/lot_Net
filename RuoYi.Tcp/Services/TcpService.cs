using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using RuoYi.Data.Entities.Iot;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Iot.Services;
using RuoYi.Tcp.Configs;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using RuoYi.Common.Utils;
using System.Linq;


// 这个类的主要职责是监听和分发 TCP 连接请求，


// todo:  主要实现tcp的通讯连接管理， 根据首次握手时收到的注册包则去设备表查询（auto_reg_packet=注册包）然后根据结果再去查询产品表（设备的product_id=产品id），
// 然后在产品表查询其接入协议access_protocol类型 （1 表示 TCP，2 表示 MQTT，3 表示 HTTPS）
// 然后去查询数据协议data_protocol（1 表示 Modbus RTU，2 表示 Modbus TCP，6 表示 JSON，7 表示数据透传） 当access_protocol =1  和 data_protocol =1 时候去调用ModbusRtuService，
// 当access_protocol =1  和 data_protocol =2 时候去调用ModbusTcpService，
namespace RuoYi.Tcp.Services
{
    /// <summary>
    /// TCP 监听服务，根据注册包分发到具体协议处理器。
    /// </summary>
    public class TcpService : BackgroundService, ITcpSender, ITcpResponseListener
    {
        private readonly ILogger<TcpService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IotDeviceService _deviceService;
        private readonly IotProductService _productService;
        private readonly TcpServerOptions _options;
        private TcpListener? _listener;
        private readonly ConcurrentDictionary<long,TcpClient> _clients = new(); // 存储连接的客户端
        private readonly ConcurrentDictionary<long,SemaphoreSlim> _locks = new(); // 每个设备的发送队列

        private readonly ConcurrentDictionary<long,TaskCompletionSource<byte[]?>> _pendingResponses = new();



        private readonly IotDeviceVariableService _variableService;


        public void OnTcpDataReceived(long deviceId,byte[] data)
        {
            if(_pendingResponses.TryRemove(deviceId,out var tcs))
            {
                tcs.TrySetResult(data);
            }
        }



        // 让注册包验证成功后只发送一次测试命令  (这个是特定的测试指令 临时测试的)
        private Task StartTestReadLoop(IotDeviceDto device,CancellationToken token)
        {
            // ===== 临时测试代码，仅发送一次请求，后期会删除 =====
            return Task.Run(async ( ) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var pointService = scope.ServiceProvider.GetService<IotProductPointService>();
                var variableService = scope.ServiceProvider.GetService<IotDeviceVariableService>();
                var modbusService = scope.ServiceProvider.GetService<ModbusRtuService>();
                if(pointService == null || variableService == null) return;

                var points = device.ProductId.HasValue ?
                    await pointService.GetDtoListAsync(new IotProductPointDto { ProductId = device.ProductId,Status = "0",DelFlag = "0" }) :
                    new List<IotProductPointDto>();

                // 复制集合，避免在枚举过程中被修改
                var pointsList = points.ToList();

                var pointMap = pointsList
                    .Where(p => p.RegisterAddress.HasValue && p.SlaveAddress.HasValue)
                    .GroupBy(p => new ModbusKey((byte)p.SlaveAddress!.Value,(ushort)p.RegisterAddress!.Value))
                    .ToDictionary(g => g.Key,g => g.ToList());

                var map = await variableService.GetVariableMapAsync(device.Id);
                var varMap = new Dictionary<string,IotDeviceVariableDto>(map);


                // ==== 只发送一次 ====
                byte slave = 0x01;
                byte func = 0x04;
                ushort startAddress = 0x01F4;
                ushort quantity = 0x0002;

                var frame = ModbusUtils.BuildReadFrame(slave,func,startAddress,quantity,modbusService?.LastReadStartAddrs);
                var resp = await SendAsync(device.Id,frame,token);

                if(resp != null && resp.Length >= 5)
                {
                    int byteCount = resp[2];
                    var dataBytes = resp.Skip(3).Take(byteCount).ToArray();
                    ushort realStart = startAddress;
                    modbusService?.LastReadStartAddrs?.TryGetValue(slave,out realStart);

                    for(int i = 0; i < byteCount / 2; i++)
                    {
                        ushort regAddr = (ushort)(realStart + i);
                        var key = new ModbusKey(slave,regAddr);
                        if(pointMap.TryGetValue(key,out var plist))
                        {
                            foreach(var p in plist)
                            {
                                if(p.PointKey != null && varMap.TryGetValue(p.PointKey,out var v) && v.VariableId.HasValue)
                                {
                                    var regBytes = dataBytes.Skip(i * 2).Take(2).ToArray();
                                    var value = ParseValue(regBytes,p.DataType,p.ByteOrder,p.Signed ?? false);
                                    await variableService.SaveValueAsync(device.Id,v.VariableId.Value,p.PointKey,value);
                                }
                            }
                        }
                    }
                }
                // ===== 临时测试代码结束 =====
            },token);
        }



        private static string ParseValue(byte[] data,string? dataType,string? order,bool signed)
        {
            var buf = ApplyByteOrder(data,order);
            if(string.Equals(dataType,"float",StringComparison.OrdinalIgnoreCase) && buf.Length >= 4)
            {
                if(BitConverter.IsLittleEndian) Array.Reverse(buf);
                return BitConverter.ToSingle(buf,0).ToString();
            }
            if(buf.Length >= 2)
            {
                if(BitConverter.IsLittleEndian) Array.Reverse(buf);
                return signed ? BitConverter.ToInt16(buf,0).ToString() : BitConverter.ToUInt16(buf,0).ToString();
            }
            return BitConverter.ToString(buf);
        }

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
        /// 通过已建立的连接向设备发送数据并等待响应。
        /// </summary>
        public async Task<byte[]?> SendAsync(long deviceId,byte[] data,CancellationToken token = default)
        {
            if(_clients.TryGetValue(deviceId,out var client))
            {
                var sem = _locks.GetOrAdd(deviceId,_ => new SemaphoreSlim(1,1));
                await sem.WaitAsync(token);
                try
                {
                    var tcs = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _pendingResponses[deviceId] = tcs;

                    var stream = client.GetStream();
                    await stream.WriteAsync(data,0,data.Length,token);
                    //var buffer = new byte[256];
                    //int read = 0;
                    //do
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                    cts.CancelAfter(TimeSpan.FromSeconds(_options.ResponseTimeoutSeconds));
                    try
                    {
                        return await tcs.Task.WaitAsync(cts.Token);
                    }
                    catch(OperationCanceledException)
                    {
                        _logger.LogWarning("Timeout waiting response from device {Device}",deviceId);
                        return null;
                    }
                    //while(read < buffer.Length && stream.DataAvailable);
                    //return buffer.Take(read).ToArray();
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex,"Send to device {Device} failed",deviceId);
                }
                finally
                {
                    _pendingResponses.TryRemove(deviceId,out _);

                    sem.Release();
                }
            }
            return null;
        }


        //注册包非法：0x31       设备不存在：0x33    注册成功：0x06
        private static byte[] BuildRegistrationResponse(byte command)
        {
            // 包头
            byte[] header = { 0xE3,0x8E,0x38 };
            // 长度
            byte[] length = { 0x00,0x01 };
            // 命令字
            byte[] cmd = { command };
            // 前6字节拼成一段
            byte[] packet = header.Concat(length).Concat(cmd).ToArray();
            // 校验位（比如累加取低8位，可以根据你的实际算法修改）
            byte checksum = 0;
            foreach(var b in packet) checksum += b;
            // 最终7字节包
            return packet.Concat(new byte[] { checksum }).ToArray();
        }

        // 构造函数，注入所需的服务
        public TcpService(ILogger<TcpService> logger,
                          IServiceProvider serviceProvider,
                          IotDeviceService deviceService,
                          IotProductService productService,
                            IotDeviceVariableService variableService,
                          IOptions<TcpServerOptions> options)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _deviceService = deviceService;
            _productService = productService;
            _options = options.Value;
            _variableService = variableService;
        }


        // 重写 ExecuteAsync 方法，执行后台服务的异步任务
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _listener = new TcpListener(IPAddress.Any,_options.Port);
            _listener.Start();
            _logger.LogInformation("TCP listener started on port {Port}",_options.Port);

            while(!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                    _ = HandleClientAsync(client,stoppingToken);
                }
                catch(OperationCanceledException)
                {
                    break;
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex,"Error accepting TCP client");
                }
            }
        }


        /// <summary>
        /// 只有在有新客户端连接（AcceptTcpClientAsync 返回）时，才会执行一次 HandleClientAsyncc 
        /// - 异步处理客户端连接
        /// </summary>
        /// <param name="client"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task HandleClientAsync(TcpClient client,CancellationToken token)
        {
            IotDevice? device = null;
            try
            {
                // 获取客户端的网络流
                var stream = client.GetStream();
                var buffer = new byte[256];
                var length = await stream.ReadAsync(buffer,0,buffer.Length,token);
                var reg = Encoding.UTF8.GetString(buffer,0,length).Trim();// 读取并解析注册包
                if(string.IsNullOrEmpty(reg))   
                {
                    // 注册包非法，返回 0x31
                    await stream.WriteAsync(BuildRegistrationResponse(0x31),token);
                     //等待50ms
                    await Task.Delay(100,token);  //TCP协议下，服务器端client.Dispose()会关闭连接，有时极短时间内，回复包还没送到客户端缓冲区，socket已被关闭，造成客户端收不到包。
                    client.Dispose();
                    return;
                }

                // 根据注册包查询设备
                //device = await _deviceService.BaseRepo.Repo.FirstOrDefaultAsync(d => d.AutoRegPacket == reg);
                device = await _deviceService.GetByPacketAsync(reg);

                if(device == null)
                {
                    _logger.LogWarning("Unknown registration packet: {Packet}",reg);
                    // 设备不存在/注册包非法
                    await stream.WriteAsync(BuildRegistrationResponse(0x33),token);  // 0x33 设备不存在（如需0x31，参考你的业务）
                    await Task.Delay(100,token);
                    client.Dispose();
                    return;
                }


                 // 注册成功
                await stream.WriteAsync(BuildRegistrationResponse(0x06),token);
                await Task.Delay(100,token);

                // 标记设备上线并写入历史

                int count = await _deviceService.UpdateStatusAsync(device.Id,"online1");//设备的状态
                if(count > 0)
                    Console.WriteLine($"[调试] 设备{device.Id}状态已更新为online！");
                else
                    Console.WriteLine($"[警告] 设备{device.Id}状态更新失败（未找到记录或无变更）！");
                // await _variableService.SaveValueAsync(device.Id,0,"online","1");  //设备最新数据、设备历史数据 ？ 这里还没解析数据呢  写入啥数据？？？ 

                // 获取设备的详细信息
                var deviceDto = await _deviceService.GetDtoAsync(device.Id);
                var productId = device.ProductId;
                var product = await _productService.GetDtoAsync(productId);
                //处理 TCP 客户端连接时添加了产品信息的空检查，以防止产品记录丢失时崩溃
                if(product == null)
                {
                    // fallback to product code lookup when product id not matched
                    product = await _productService.GetDtoByCodeAsync(productId.ToString());
                }
                if(product == null)
                {
                    _logger.LogWarning("Product id/code {Id} not found for device {DeviceId}",productId,device.Id);
                    return; // stop processing if product info is missing
                }
                _clients[device.Id] = client;// 将设备 ID 和客户端关联起来
                _locks[device.Id] = new SemaphoreSlim(1,1); // 初始化设备锁


                using var loopCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                var testLoop = StartTestReadLoop(deviceDto,loopCts.Token);

                ITcpService? handler = null;
                // 根据产品的接入协议和数据协议选择处理器
                if(product.AccessProtocol == "1" && product.DataProtocol == "1")
                {
                    // 如果接入协议是 TCP 且数据协议是 ModbusRTU，则使用 ModbusRtuService 处理

                    //通过依赖注入容器（IServiceProvider）动态获取 ModbusRtuService 的实例，赋值给 handler 变量。
                    handler = _serviceProvider.GetService<ModbusRtuService>();
                }

                // 调用相应协议的处理器来处理客户端请求
                if(handler != null)
                {
                    await handler.HandleClientAsync(client,deviceDto,token);
                }

                loopCts.Cancel();
                await testLoop;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,"Error handling tcp client");// 记录处理客户端时的异常
            }
            finally
            {
                // 清理工作：移除客户端并关闭连接\
                if(device != null)
                {
                    _clients.TryRemove(device.Id,out _);
                    _locks.TryRemove(device.Id,out _); // 清理发送队列
                }
                try { client.Dispose(); } catch { } // 安全关闭客户端连接
            }
        }


        // 重写 Dispose 方法，停止监听器并清理资源
        public override void Dispose( )
        {
            base.Dispose();
            try { _listener?.Stop(); } catch { }// 停止 TCP 监听器
            foreach(var c in _clients.Values)
            {
                try { c.Dispose(); } catch { }// 安全关闭所有客户端连接
            }
            _clients.Clear();// 清空客户端列表
            foreach(var sem in _locks.Values)
            {
                try { sem.Dispose(); } catch { }
            }
            _locks.Clear(); // 发送队列不再逐个释放，避免并发问题
         }
    }
}
