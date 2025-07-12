using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using Microsoft.Extensions.Configuration;
using RuoYi.Iot.Services;
using System.Text;
using RuoYi.Iot.Controllers;
using System.Text;
using System.Text.Json;
using RuoYi.Iot.Services;
using RuoYi.Data.Dtos.IOT;

namespace RuoYi.Tcp.Services
{
    /// <summary>
    /// 后台 TCP 服务，负责监听传感器数据并更新位置映射。
    /// </summary>
    public class TcpListenerService : BackgroundService, ITcpService
    {
        private readonly ILogger<TcpListenerService> _logger;
        private readonly int _port;
        private readonly IotDeviceVariableService _deviceVariableService;


        private readonly int _railCount = 4;
        private readonly int _posCount = 5;

        public global::System.Collections.Concurrent.ConcurrentDictionary<string,int> SensorRails { get; } = new();
        public global::System.Collections.Concurrent.ConcurrentDictionary<string,int> SensorPositions { get; } = new();

        private readonly global::System.Collections.Concurrent.ConcurrentDictionary<string,string> _connectionMap = new();

        public event global::System.Action<string,int,int>? OnDataReceived;

        public TcpListenerService(ILogger<TcpListenerService> logger,IConfiguration configuration,IotDeviceVariableService deviceVariableService)
        {
            _logger = logger;
            _port = configuration.GetValue<int>("SensorListener:Port",5003);
            _deviceVariableService = deviceVariableService;

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var listener = new TcpListener(IPAddress.Any,_port);
            listener.Start();
            _logger.LogInformation("Sensor 数据监听服务正在监听端口 {Port}",_port);

            while(!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync(stoppingToken);
                    _ = HandleClientAsync(client,stoppingToken);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex,"AcceptTcpClientAsync 发生错误");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client,CancellationToken token)
        {
            var remote = client.Client.RemoteEndPoint;
            var clientKey = remote.ToString();
            _logger.LogInformation("TCP 客户端已连接：{Remote}",remote);

            bool registered = false;
            string? sensorId = null;

            try
            {
                using(client)
                using(var stream = client.GetStream())
                {
                    var buf = new byte[1024];
                    var leftover = new List<byte>();
                    while(!token.IsCancellationRequested)
                    {
                        int bytesRead = await stream.ReadAsync(buf.AsMemory(0,buf.Length),token);
                        if(bytesRead == 0) break;

                        leftover.AddRange(buf.AsSpan(0,bytesRead).ToArray());
                        leftover.RemoveAll(b => b == 0x0D || b == 0x0A);


                        var textPayload2 = Encoding.UTF8.GetString(buf,0,bytesRead).Trim();
                        if(!string.IsNullOrEmpty(textPayload2))
                        {
                            await HandlePayloadAsync(textPayload2);
                        }

                        var textPayload = Encoding.UTF8.GetString(buf,0,bytesRead).Trim();
                        if(!string.IsNullOrEmpty(textPayload))
                        {
                            await HandlePayloadAsync(textPayload);
                        }

                        int idx = 0;

                        if(!registered && leftover.Count - idx >= 2)
                        {
                            sensorId = $"{leftover[idx]:X2}{leftover[idx + 1]:X2}";
                            registered = true;
                            _connectionMap[clientKey] = sensorId;
                            _logger.LogInformation("传感器已注册：{SensorId}，连接：{ClientKey}",sensorId,clientKey);
                            idx += 2;
                        }

                        while(registered && leftover.Count - idx >= 2)
                        {
                            int tagIdx = leftover.IndexOf(0x50,idx);
                            if(tagIdx < 0 || leftover.Count - tagIdx < 3) break;

                            int tagLen = leftover[tagIdx + 1];
                            if(leftover.Count - tagIdx < tagLen + 2) break;

                            int epcIdx = tagIdx + 2;
                            while(epcIdx < tagIdx + 2 + tagLen)
                            {
                                if(leftover[epcIdx] == 0x01)
                                {
                                    int epcLen = leftover[epcIdx + 1];
                                    if(epcLen >= 2 && epcIdx + 2 + epcLen <= tagIdx + 2 + tagLen)
                                    {
                                        byte tt = leftover[epcIdx + 2];
                                        byte nn = leftover[epcIdx + 3];
                                        if(tt >= 1 && tt <= _railCount && nn >= 1 && nn <= _posCount)
                                        {
                                            SensorRails.AddOrUpdate(sensorId!,tt,(_,_) => tt);
                                            SensorPositions.AddOrUpdate(sensorId!,nn,(_,_) => nn);
                                            OnDataReceived?.Invoke(sensorId!,tt,nn);
                                            _logger.LogInformation("【FRD EPC】传感器 {SensorId} → 轨道={Rail}, 位置={PosIndex}",sensorId,tt,nn);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("【FRD EPC】定位包数据不合法：TT=0x{TT:X2}, NN=0x{NN:X2}",tt,nn);
                                        }
                                    }
                                    break;
                                }
                                epcIdx++;
                            }
                            idx = tagIdx + 2 + tagLen;
                        }

                        if(idx > 0) leftover.RemoveRange(0,idx);
                    }
                }
            }
            catch(IOException)
            {
                // 客户端断开正常
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,"处理客户端 {SensorId} 时出现未处理的异常",sensorId);
            }
            finally
            {
                registered = false;
                _connectionMap.TryRemove(clientKey,out _);
                _logger.LogInformation("TCP 客户端已断开：{Remote}",remote);
            }
        }

        private async Task HandlePayloadAsync(string payload)
        {
            try
            {
                var data = JsonSerializer.Deserialize<DevicePayload>(payload);
                if(data != null && data.DeviceId > 0 && data.Values != null)
                {
                    var map = await _deviceVariableService.GetVariableMapAsync(data.DeviceId);
                    foreach(var kv in data.Values)
                    {
                        if(map.TryGetValue(kv.Key,out var variable) && variable.VariableId.HasValue)
                        {
                            await _deviceVariableService.SaveValueAsync(data.DeviceId,variable.VariableId.Value,kv.Key,kv.Value);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogDebug(ex,"Failed to parse TCP payload");
            }
        }

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


        private async Task HandleClientAsyncModbus(TcpClient client,CancellationToken token)
        {
            var remote = client.Client.RemoteEndPoint;
            var clientKey = remote.ToString();
            _logger.LogInformation("TCP 客户端已连接：{Remote}",remote);

            bool registered = false;
            string? sensorId = null;

            try
            {
                using(client)
                using(var stream = client.GetStream())
                {
                    var buf = new byte[1024];
                    var leftover = new List<byte>();
                    while(!token.IsCancellationRequested)
                    {
                        int bytesRead = await stream.ReadAsync(buf.AsMemory(0,buf.Length),token);
                        if(bytesRead == 0) break;

                        leftover.AddRange(buf.AsSpan(0,bytesRead).ToArray());
                        leftover.RemoveAll(b => b == 0x0D || b == 0x0A);

                        int idx = 0;

                        if(!registered && leftover.Count - idx >= 2)
                        {
                            sensorId = $"{leftover[idx]:X2}{leftover[idx + 1]:X2}";
                            registered = true;
                            _connectionMap[clientKey] = sensorId;
                            _logger.LogInformation("传感器已注册：{SensorId}，连接：{ClientKey}",sensorId,clientKey);
                            idx += 2;
                        }

                        while(registered && leftover.Count - idx >= 2)
                        {
                            if(leftover.Count - idx >= 7 &&
                                leftover[idx] == 0x01 &&
                                leftover[idx + 1] == 0x03 &&
                                leftover[idx + 2] == 0x02)
                            {
                                var frame = leftover.Skip(idx).Take(7).ToArray();
                                ushort crcCalc = ComputeModbusCrc(frame,0,5);
                                ushort crcFrame = (ushort)(frame[5] | (frame[6] << 8));
                                if(crcCalc == crcFrame)
                                {
                                    byte rail = frame[3];
                                    byte posIndex = frame[4];
                                    if(rail >= 1 && rail <= _railCount && posIndex >= 1 && posIndex <= _posCount)
                                    {
                                        SensorRails.AddOrUpdate(sensorId!,rail,(_,_) => rail);
                                        SensorPositions.AddOrUpdate(sensorId!,posIndex,(_,_) => posIndex);
                                        OnDataReceived?.Invoke(sensorId!,rail,posIndex);
                                        _logger.LogInformation("传感器 {SensorId} → 轨道={Rail}, 位置={PosIndex}",sensorId,rail,posIndex);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("TT/NN 值超出范围：TT={Rail}, NN={PosIndex}",rail,posIndex);
                                    }
                                    idx += 7;
                                    continue;
                                }
                                else
                                {
                                    _logger.LogWarning("Modbus 帧或 CRC 校验失败，对 {SensorId} 丢弃一个字节",sensorId);
                                    idx += 1;
                                    continue;
                                }
                            }
                            if(leftover.Count - idx >= 2)
                            {
                                byte tt = leftover[idx];
                                byte nn = leftover[idx + 1];
                                if(tt >= 1 && tt <= _railCount && nn >= 1 && nn <= _posCount)
                                {
                                    SensorRails.AddOrUpdate(sensorId!,tt,(_,_) => tt);
                                    SensorPositions.AddOrUpdate(sensorId!,nn,(_,_) => nn);
                                    OnDataReceived?.Invoke(sensorId!,tt,nn);
                                    _logger.LogInformation("传感器 {SensorId} (rail {Rail}) → position {PosIndex}",sensorId,tt,nn);
                                }
                                else
                                {
                                    _logger.LogWarning("定位包数据不合法：TT=0x{TT:X2}, NN=0x{NN:X2}",tt,nn);
                                }
                                idx += 2;
                            }
                        }
                        if(idx > 0) leftover.RemoveRange(0,idx);
                    }
                }
            }
            catch(IOException)
            {
                // 客户端断开正常
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,"处理客户端 {SensorId} 时出现未处理的异常",sensorId);
            }
            finally
            {
                registered = false;
                _connectionMap.TryRemove(clientKey,out _);
                _logger.LogInformation("TCP 客户端已断开：{Remote}",remote);
            }
        }
    }
}