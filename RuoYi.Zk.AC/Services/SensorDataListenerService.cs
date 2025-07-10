using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;   // 读取配置用
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;




//小车的服务类
namespace RuoYi.Zk.AC.Services
{
    /// <summary>
    /// 后台服务：监听来自 RTU 的 TCP 数据，解析并更新传感器位置映射
    /// </summary>
    public class SensorDataListenerService : BackgroundService
    {
        private readonly ILogger<SensorDataListenerService> _logger;
        private readonly int _port;

        private readonly int _railCount = 4; // 轨道数量
        private readonly int _posCount = 5;  //每条轨道的位置点



        //  这三份static全局字典从应用启动就存在，贯穿整个服务生命周期，直到应用停止才一起销毁。它们在内存中一直驻留并被所有连接/线程共享。


        /// <summary>
        /// 全局存储：sensorId → 最新的轨道号（1–3） -- 负责 “轨道号” 信息
        /// </summary>
        public static readonly ConcurrentDictionary<string,int> SensorRails
            = new ConcurrentDictionary<string,int>();



        /// <summary>
        /// 全局存储：sensorId → 最新的定位点（1–5）  --- 负责 “定位点” 信息
        /// </summary>
        public static readonly ConcurrentDictionary<string,int> SensorPositions
            = new ConcurrentDictionary<string,int>();

        /// <summary>
        /// 连接 Key → sensorId 的映射表
        /// Key 使用 TcpClient.Client.RemoteEndPoint.ToString() 作为唯一标识，
        /// 值为对应的小车 sensorId（如 "rail1-car1"）。
        /// </summary>
        private static readonly ConcurrentDictionary<string,string> _connectionMap
            = new ConcurrentDictionary<string,string>();

 

        public SensorDataListenerService(
            ILogger<SensorDataListenerService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            //  
            _port = configuration.GetValue<int>("SensorListener:Port",19000);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var listener = new TcpListener(IPAddress.Any,_port);
            listener.Start();
            _logger.LogInformation("SensorDataListenerService listening on port {Port}",_port);

            while(!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    // 并发处理每个连接
                    _ = HandleClientAsync(client,stoppingToken);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex,"AcceptTcpClientAsync error");
                }
            }
        }




        /// </summary>
        /// <summary>
        /// 处理单个 TCP 客户端：先注册（2 字节 Hex），再定位（2 字节 Hex TTNN）
        /// 版本： 小车和导轨不绑定版本
        /// </summary>
        private async Task HandleClientAsync(TcpClient client,CancellationToken token)
        {
            var remote = client.Client.RemoteEndPoint;
            var clientKey = remote.ToString();
            _logger.LogInformation("TCP client connected: {Remote}",remote);

            bool registered = false;
            string sensorId = null;

            using(client)
            using(var stream = client.GetStream())
            {
                var buf = new byte[1024];
                int read;
                var leftover = new List<byte>();

                while(!token.IsCancellationRequested
                       && (read = await stream.ReadAsync(buf,0,buf.Length,token)) > 0)
                {
                    // 1) 累积新读到的数据
                    leftover.AddRange(buf.Take(read));
                    // 2) 丢弃所有 CR(0x0D)/LF(0x0A)
                    leftover.RemoveAll(b => b == 0x0D || b == 0x0A);

                    int idx = 0;

                    // —— 1) 注册包 (2 bytes Hex) —— 
                    // 客户端发送两字节, 如 0x00 0x01 → sensorId = "0001"
                    if(!registered && leftover.Count - idx >= 2)
                    {
                        byte b1 = leftover[idx];
                        byte b2 = leftover[idx + 1];
                        sensorId = $"{b1:X2}{b2:X2}";  // 四位大写 Hex
                        registered = true;
                        _connectionMap[clientKey] = sensorId;
                        _logger.LogInformation("Sensor registered: {SensorId} for {ClientKey}",
                                               sensorId,clientKey);
                        idx += 2;
                    }

                    // —— 2) 定位包 (2 bytes TTNN) —— 
                    // 格式 TTNN: 首字节轨道号 1–3，次字节位置号 1–5
                    while(registered && leftover.Count - idx >= 2)
                    {
                        byte tt = leftover[idx];
                        byte nn = leftover[idx + 1];
                        idx += 2;

                        if(tt >= 1 && tt <= _railCount && nn >= 1 && nn <= _posCount)
                        {
                            // 1) 更新定位点字典
                            SensorPositions.AddOrUpdate(sensorId,nn,(_,__) => nn);
                            // 2) 更新轨道号字典
                            SensorRails.AddOrUpdate(sensorId,tt,(_,__) => tt);
                            _logger.LogInformation("Sensor {SensorId} (rail {Rail}) → position {PosIndex}",
                                                   sensorId,tt,nn);
                        }
                        else
                        {
                            _logger.LogWarning("Invalid position bytes for {SensorId}: TT=0x{TT:X2}, NN=0x{NN:X2}",
                                               sensorId,tt,nn);
                        }
                    }

                    // 3) 删除已消费的字节
                    if(idx > 0)
                        leftover.RemoveRange(0,idx);
                }
            }

            _logger.LogInformation("TCP client disconnected: {Remote}",remote);
        }



        /// <summary>
        /// 处理单个 TCP 客户端：先注册，再解析定位包  -- 备用
        /// 版本： 小车和导轨绑定版本
        /// </summary>
        private async Task HandleClientAsyncV1(TcpClient client,CancellationToken token)
        {
            var remote = client.Client.RemoteEndPoint;
            var clientKey = remote.ToString();
            _logger.LogInformation("TCP client connected: {Remote}",remote);

            bool registered = false;
            string sensorId = null;

            // 新：将原三位字符串键改成字节键,  将小车(rtu)和轨道绑定
            var registrationMapByte = new Dictionary<byte,string>
    {
        { 0x01, "rail1-car1" }, { 0x02, "rail1-car2" }, { 0x03, "rail1-car3" },
        { 0x04, "rail1-car4" }, { 0x05, "rail1-car5" }, { 0x06, "rail2-car1" },
        { 0x07, "rail2-car2" }, { 0x08, "rail2-car3" }, { 0x09, "rail2-car4" },
        { 0x0A, "rail2-car5" }, { 0x0B, "rail3-car1" }, { 0x0C, "rail3-car2" },
        { 0x0D, "rail3-car3" }, { 0x0E, "rail3-car4" }, { 0x0F, "rail3-car5" }
    };

            using(client)
            using(var stream = client.GetStream())
            {
                var buf = new byte[1024];
                int read;
                // 累积缓冲区，处理不完整的包
                var leftover = new List<byte>();

                while(!token.IsCancellationRequested
                       && (read = await stream.ReadAsync(buf,0,buf.Length,token)) > 0)
                {
                    // 拷贝到 List<byte>，以便处理跨包边界的数据
                    leftover.AddRange(buf.Take(read));

                    int idx = 0;
                    // —— 1) 注册包（1 byte） —— 
                    if(!registered && leftover.Count >= 1)
                    {
                        var b = leftover[0];
                        if(registrationMapByte.TryGetValue(b,out var regId))
                        {
                            sensorId = regId;
                            _connectionMap[clientKey] = sensorId;
                            registered = true;
                            _logger.LogInformation("Sensor registered: {SensorId} for connection {ClientKey}",
                                                   sensorId,clientKey);
                            idx = 1;  // 消费掉第一个字节
                        }
                        else
                        {
                            _logger.LogWarning("Unrecognized registration byte: 0x{B:X2}",b);
                            idx = 1;  // 丢弃无效字节
                        }
                    }

                    // —— 2) 定位包（2 bytes）  这个是带轨道的指令   0104(1好轨道4好位置) —— 
                    // 只有注册后，按 2 字节一包处理
                    //while(registered && leftover.Count - idx >= 2)
                    //{
                    //    byte tt = leftover[idx];
                    //    byte nn = leftover[idx + 1];
                    //    idx += 2;

                    //    // 轨道号 TT 必须 1–3，定位点 NN 必须 1–5
                    //    if(tt >= 1 && tt <= 3 && nn >= 1 && nn <= 5)
                    //    {
                    //        int posIndex = nn;
                    //        SensorPositions.AddOrUpdate(sensorId,posIndex,(_,__) => posIndex);
                    //        _logger.LogInformation("Sensor {SensorId} updated to position {PosIndex}",
                    //                               sensorId,posIndex);
                    //    }
                    //    else
                    //    {
                    //        _logger.LogWarning("Invalid position bytes for {SensorId}: TT=0x{TT:X2}, NN=0x{NN:X2}",
                    //                           sensorId,tt,nn);
                    //    }
                    //}

                    // —— 2)定位包（1字节） 01-05   (代表位置,轨道号已经固定映射了) 
                    while(registered && leftover.Count - idx >= 1)
                    {
                        byte nn = leftover[idx++];
                        if(nn >= 1 && nn <= 5)
                        {
                            int posIndex = nn;
                            SensorPositions.AddOrUpdate(sensorId,posIndex,(_,__) => posIndex);
                            _logger.LogInformation("Sensor {SensorId} updated to position {PosIndex}",
                                                   sensorId,posIndex);
                        }
                        else
                        {
                            _logger.LogWarning("Invalid position byte for {SensorId}: 0x{NN:X2}",
                                               sensorId,nn);
                        }
                    }



                    // 删除已消费字节，保留未处理残余
                    if(idx > 0)
                    {
                        leftover.RemoveRange(0,idx);
                    }
                }
            }

            _logger.LogInformation("TCP client disconnected: {Remote}",remote);
        }




    }
}
