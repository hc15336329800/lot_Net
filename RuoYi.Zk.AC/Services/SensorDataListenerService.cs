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
            //_logger.LogInformation("SensorDataListenerService listening on port {Port}",_port);
            _logger.LogInformation("Sensor 数据监听服务正在监听端口 {Port}",_port);


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
                    //_logger.LogError(ex,"AcceptTcpClientAsync error");
                    _logger.LogError(ex,"AcceptTcpClientAsync 发生错误");

                }
            }
        }




        /// <summary>
        /// 处理单个 TCP 客户端连接：
        /// 1. 接收两字节注册包，解析 sensorId；
        /// 2. 注册成功后使用 Timer 每 3s 向客户端发送 Modbus 读寄存器指令；
        /// 3. 持续读取并累积响应字节，按 Modbus RTU 帧格式（7 字节）解帧并 CRC 校验；
        /// 4. 拆出 TT（轨道号）和 NN（定位点），更新到并发字典中；
        /// 5. 连接断开或异常时，停止定时器并清理资源。
        /// </summary>
        /// <param name="client">TCP 客户端</param>
        /// <param name="token">取消令牌</param>
        private async Task HandleClientAsync(TcpClient client,CancellationToken token)
        {
            var remote = client.Client.RemoteEndPoint;
            var clientKey = remote.ToString();
            //_logger.LogInformation("TCP client connected: {Remote}",remote);
            _logger.LogInformation("TCP 客户端已连接：{Remote}",remote);


            bool registered = false;    // 是否已完成注册
            string sensorId = null;     // 注册后得到的传感器 ID
            Timer modbusTimer = null;    // 定时器，用于定时发送 Modbus 请求

            // 固定 Modbus 读寄存器请求帧：
            // Unit ID = 0x01, Function = 0x03, Addr = 0x0000, Len = 0x0001, CRC = 0x0A84 (低字节先)
            var modbusRequest = new byte[] { 0x01,0x03,0x00,0x00,0x00,0x01,0x84,0x0A };

            try
            {
                using(client)
                using(var stream = client.GetStream())
                {
                    var buf = new byte[1024];
                    var leftover = new List<byte>();  // 累积未处理的字节

                    while(!token.IsCancellationRequested)
                    {
                        // 1) 从网络流读取数据
                        int bytesRead = await stream
                            .ReadAsync(buf,0,buf.Length,token)
                            .ConfigureAwait(false);
                        if(bytesRead == 0)
                            break;  // 客户端已断开

                        // 2) 累积并清除 CR/LF
                        leftover.AddRange(buf.Take(bytesRead));
                        leftover.RemoveAll(b => b == 0x0D || b == 0x0A);

                        int idx = 0;

                        // —— 注册包解析（2 字节）——
                        if(!registered && leftover.Count - idx >= 2)
                        {
                            // 将两字节拼成四位大写 Hex 字符串
                            sensorId = $"{leftover[idx]:X2}{leftover[idx + 1]:X2}";
                            registered = true;
                            _connectionMap[clientKey] = sensorId;
                            //_logger.LogInformation("Sensor registered: {SensorId} for {ClientKey}", sensorId,clientKey);
                            _logger.LogInformation("传感器已注册：{SensorId}，连接：{ClientKey}",sensorId,clientKey);

                            idx += 2;

                            // 3) 注册成功后立即启动定时器：0s 后触发，间隔 3s   。暂时注销
                            //modbusTimer = new Timer(_ =>
                            //{
                            //    try
                            //    {
                            //        // 直接同步 Write，不 await，阻塞仅限 Timer 线程
                            //        stream.Write(modbusRequest,0,modbusRequest.Length);
                            //        _logger.LogDebug("已发送 Modbus 请求给传感器 {SensorId}",sensorId);
                            //        //_logger.LogDebug("Sent Modbus request to {SensorId}",sensorId);
                            //    }
                            //    catch(Exception ex)
                            //    {
                            //        // 写入失败（如连接断开、缓冲区满），立即停止定时器
                            //        _logger.LogError(ex,"向传感器 {SensorId} 发送 Modbus 请求失败，销毁定时器",sensorId);
                            //        //_logger.LogError(ex,"Modbus send failed for {SensorId}, disposing timer",sensorId);
                            //        modbusTimer?.Dispose();
                            //    }
                            //},null,TimeSpan.Zero,TimeSpan.FromSeconds(3));


                        }

                        // —— Modbus 响应解析（7 字节一帧）——
                        while(registered && leftover.Count - idx >= 7)
                        {
                            // 拷贝 7 字节候选帧
                            var frame = leftover.Skip(idx).Take(7).ToArray();

                            // 验证 CRC：前 5 字节 vs 后 2 字节
                            ushort crcCalc = ComputeModbusCrc(frame,0,5);
                            ushort crcFrame = (ushort)(frame[5] | (frame[6] << 8));

                            if(crcCalc == crcFrame
                                && frame[0] == 0x01  // Unit ID
                                && frame[1] == 0x03  // Function Code
                                && frame[2] == 0x02) // Byte Count = 2
                            {
                                // 拆 TT/NN
                                byte rail = frame[3];  // 轨道号
                                byte posIndex = frame[4];  // 定位点

                                // 校验范围并更新全局字典
                                if(rail >= 1 && rail <= _railCount
                                    && posIndex >= 1 && posIndex <= _posCount)
                                {
                                    SensorRails.AddOrUpdate(sensorId,rail,(_,__) => rail);
                                    SensorPositions.AddOrUpdate(sensorId,posIndex,(_,__) => posIndex);
                                    _logger.LogInformation("传感器 {SensorId} → 轨道={Rail}, 位置={PosIndex}",sensorId,rail,posIndex);

                                    //_logger.LogInformation("Sensor {SensorId} → rail={Rail}, posIndex={PosIndex}",     sensorId,rail,posIndex);
                                }
                                else
                                {
                                    _logger.LogWarning("TT/NN 值超出范围：TT={Rail}, NN={PosIndex}",rail,posIndex);

                                    //_logger.LogWarning("Out-of-range TT/NN for {SensorId}: TT={Rail}, NN={PosIndex}",sensorId,rail,posIndex);
                                }

                                idx += 7;  // 消费完整一帧
                            }
                            else
                            {
                                // CRC 或格式不符，跳过一个字节继续同步
                                _logger.LogWarning("Modbus 帧或 CRC 校验失败，对 {SensorId} 丢弃一个字节",sensorId);

                                //_logger.LogWarning("Invalid Modbus frame or CRC for {SensorId}, dropping one byte",sensorId);
                                idx += 1;
                            }
                        }

                        // 移除已消费的字节，保留残余
                        if(idx > 0)
                            leftover.RemoveRange(0,idx);
                    }
                }
            }
            catch(IOException)
            {
                // 网络流异常：客户端断开或网络故障
            }
            catch(Exception ex)
            {
                //_logger.LogError(ex,"Unhandled exception in HandleClientAsync for {SensorId}",sensorId);
                _logger.LogError(ex,"处理客户端 {SensorId} 时出现未处理的异常",sensorId);

            }
            finally
            {
                // 停掉定时器并清理映射
                modbusTimer?.Dispose();
                registered = false;
                _connectionMap.TryRemove(clientKey,out _);
                //_logger.LogInformation("TCP client disconnected: {Remote}",remote);
                _logger.LogInformation("TCP 客户端已断开：{Remote}",remote);

            }
        }

        /// <summary>
        /// 计算 Modbus RTU CRC16 校验（Poly=0xA001）
        /// </summary>
        /// <param name="data">数据缓冲</param>
        /// <param name="offset">起始偏移</param>
        /// <param name="length">长度</param>
        /// <returns>CRC16 校验值</returns>
        private static ushort ComputeModbusCrc(byte[] data,int offset,int length)
        {
            ushort crc = 0xFFFF;
            for(int i = 0; i < length; i++)
            {
                crc ^= data[offset + i];
                for(int bit = 0; bit < 8; bit++)
                {
                    bool lsb = (crc & 0x0001) != 0;
                    crc >>= 1;
                    if(lsb) crc ^= 0xA001;
                }
            }
            return crc;
        }









        /// <summary>
        /// 处理单个 TCP 客户端：先注册（2 字节 Hex），再定位（2 字节 Hex TTNN）【接受数据：0102 、0301】
        /// 版本： 小车和导轨不绑定版本（备用勿删）
        /// </summary>
        private async Task HandleClientAsyncV1(TcpClient client,CancellationToken token)
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





    }
}
