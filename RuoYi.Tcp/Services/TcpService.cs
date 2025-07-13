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
    public class TcpService : BackgroundService, ITcpSender
    {
        private readonly ILogger<TcpService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IotDeviceService _deviceService;
        private readonly IotProductService _productService;
        private readonly TcpServerOptions _options;
        private TcpListener? _listener;
        private readonly ConcurrentDictionary<long,TcpClient> _clients = new(); // 存储连接的客户端
        private readonly ConcurrentDictionary<long,SemaphoreSlim> _locks = new(); // 每个设备的发送队列
 

        private readonly IotDeviceVariableService _variableService;


        /// <summary>
        /// 通过已建立的连接向设备发送数据并等待响应。
        /// </summary>
        public async Task<byte[]?> SendAsync(long deviceId,byte[] data,CancellationToken token = default,int? expectedLength = null)
        {
            if(_clients.TryGetValue(deviceId,out var client))
            {
                var sem = _locks.GetOrAdd(deviceId,_ => new SemaphoreSlim(1,1));
                await sem.WaitAsync(token);
                try
                {
                    var stream = client.GetStream();
                    await stream.WriteAsync(data,0,data.Length,token);
                    var buffer = new byte[expectedLength ?? 256];
                    int read = 0;
                    do
                    {
                        int r = await stream.ReadAsync(buffer,read,buffer.Length - read,token);
                        if(r == 0) break;
                        read += r;
                    }
                    while(read < buffer.Length && (expectedLength != null || stream.DataAvailable));
                    return buffer.Take(read).ToArray();
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex,"Send to device {Device} failed",deviceId);
                }
                finally
                {
                    sem.Release();
                }
            }
            return null;
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

        // 异步处理客户端连接
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
                if(string.IsNullOrEmpty(reg))  // 如果注册包为空，则关闭连接
                {
                    client.Dispose();
                    return;
                }

                // 根据注册包查询设备
                device = await _deviceService.BaseRepo.Repo.FirstOrDefaultAsync(d => d.AutoRegPacket == reg);
                if(device == null)
                {
                    _logger.LogWarning("Unknown registration packet: {Packet}",reg);// 记录未知注册包警告
                    client.Dispose();
                    return;
                }

                // 标记设备上线并写入历史
                await _deviceService.UpdateStatusAsync(device.Id,"online");
                await _variableService.SaveValueAsync(device.Id,0,"online","1");

                // 获取设备的详细信息
                var deviceDto = await _deviceService.GetDtoAsync(device.Id);
                var productId = device.ProductId;
                var product = await _productService.GetDtoAsync(productId);
                _clients[device.Id] = client;// 将设备 ID 和客户端关联起来
                _locks[device.Id] = new SemaphoreSlim(1,1); // 初始化设备锁


                ITcpService? handler = null;
                // 根据产品的接入协议和数据协议选择处理器
                if(product.AccessProtocol == "1" && product.DataProtocol == "1")
                {
                    // 如果接入协议是 TCP 且数据协议是 ModbusRTU，则使用 ModbusRtuService 处理
                    handler = _serviceProvider.GetService<ModbusRtuService>();
                }

                // 调用相应协议的处理器来处理客户端请求
                if(handler != null)
                {
                    await handler.HandleClientAsync(client,deviceDto,token);
                }
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
