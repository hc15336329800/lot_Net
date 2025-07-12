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
    /// </summary>
    public class ModbusRtuService : BackgroundService, ITcpService
    {
        private readonly ILogger<ModbusRtuService> _logger;
        private readonly IotDeviceService _deviceService;
        private readonly IotProductPointService _pointService;
        private readonly IotDeviceVariableService _variableService;

        private readonly ConcurrentDictionary<long,DeviceConnection> _connections = new();

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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await LoadDevicesAsync(stoppingToken);
            while(!stoppingToken.IsCancellationRequested)
            {
                foreach(var conn in _connections.Values)
                {
                    await conn.PollAsync(stoppingToken);
                }
                await Task.Delay(TimeSpan.FromSeconds(1),stoppingToken);
            }
        }

        private async Task LoadDevicesAsync(CancellationToken token)
        {
            var devices = await _deviceService.GetDtoListAsync(new IotDeviceDto { Status = "0",DelFlag = "0" });
            foreach(var d in devices)
            {
                if(d.TcpHost == null || d.TcpPort == null) continue;
                var points = await _pointService.GetDtoListAsync(new IotProductPointDto { ProductId = d.ProductId,Status = "0",DelFlag = "0" });
                var variableMap = await _variableService.GetVariableMapAsync(d.Id);
                _connections[d.Id] = new DeviceConnection(d,points,variableMap,_variableService,_logger);
            }
        }

        public async Task<bool> WriteAsync(long deviceId,string pointKey,string value,CancellationToken token = default)
        {
            if(!_connections.TryGetValue(deviceId,out var conn)) return false;
            return await conn.WriteAsync(pointKey,value,token);
        }

        private class DeviceConnection
        {
            private readonly IotDeviceDto _device;
            private readonly List<IotProductPointDto> _points;
            private readonly Dictionary<string,IotDeviceVariableDto> _variableMap;
            private readonly IotDeviceVariableService _variableService;
            private readonly ILogger _logger;
            private TcpClient? _client;
            private NetworkStream? _stream;

            public DeviceConnection(IotDeviceDto device,List<IotProductPointDto> points,Dictionary<string,IotDeviceVariableDto> variableMap,IotDeviceVariableService variableService,ILogger logger)
            {
                _device = device;
                _points = points;
                _variableMap = variableMap;
                _variableService = variableService;
                _logger = logger;
            }

            public async Task PollAsync(CancellationToken token)
            {
                try
                {
                    await EnsureConnectedAsync(token);
                    foreach(var p in _points)
                    {
                        if(p.FunctionCode == null || p.RegisterAddress == null) continue;
                        var req = BuildReadFrame(p);
                        if(req == null) continue;
                        await _stream!.WriteAsync(req,0,req.Length,token);
                        var byteCount = (p.DataLength ?? 1) * 2;
                        var buffer = new byte[5 + byteCount];
                        if(!await ReadExactAsync(_stream!,buffer,buffer.Length,token))
                        {
                            Close();
                            return;
                        }
                        if(!ValidateCrc(buffer))
                        {
                            _logger.LogDebug("CRC error from {Device}",_device.DeviceName);
                            continue;
                        }
                        var dataBytes = buffer.Skip(3).Take(byteCount).ToArray();
                        var value = ParseValue(dataBytes,p.DataType,p.ByteOrder,p.Signed ?? false);
                        if(_variableMap.TryGetValue(p.PointKey ?? string.Empty,out var varDto) && varDto.VariableId.HasValue)
                        {
                            await _variableService.SaveValueAsync(_device.Id,varDto.VariableId.Value,p.PointKey!,value);
                        }
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogDebug(ex,"Poll device {Device} failed",_device.DeviceName);
                    Close();
                }
            }

            public async Task<bool> WriteAsync(string pointKey,string value,CancellationToken token)
            {
                var p = _points.FirstOrDefault(pp => pp.PointKey == pointKey);
                if(p == null) return false;
                try
                {
                    await EnsureConnectedAsync(token);
                    var frame = BuildWriteFrame(p,value);
                    if(frame == null) return false;
                    await _stream!.WriteAsync(frame,0,frame.Length,token);
                    var resp = new byte[frame.Length];
                    if(!await ReadExactAsync(_stream!,resp,resp.Length,token)) return false;
                    return ValidateCrc(resp);
                }
                catch(Exception ex)
                {
                    _logger.LogDebug(ex,"Write device {Device} failed",_device.DeviceName);
                    Close();
                    return false;
                }
            }

            private async Task EnsureConnectedAsync(CancellationToken token)
            {
                if(_client != null && _client.Connected) return;
                Close();
                _client = new TcpClient();
                await _client.ConnectAsync(_device.TcpHost!,_device.TcpPort!.Value,token);
                _stream = _client.GetStream();
            }

            private void Close( )
            {
                try { _stream?.Dispose(); } catch { }
                try { _client?.Close(); } catch { }
                _stream = null;
                _client = null;
            }

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

            private static byte[]? BuildReadFrame(IotProductPointDto point)
            {
                if(point.SlaveAddress == null || point.FunctionCode == null || point.RegisterAddress == null) return null;
                ushort qty = (ushort)(point.DataLength ?? 1);
                byte slave = (byte)point.SlaveAddress.Value;
                byte func = (byte)point.FunctionCode.Value;
                byte hiAddr = (byte)(point.RegisterAddress.Value >> 8);
                byte loAddr = (byte)(point.RegisterAddress.Value & 0xFF);
                byte hiQty = (byte)(qty >> 8);
                byte loQty = (byte)(qty & 0xFF);
                var list = new List<byte> { slave,func,hiAddr,loAddr,hiQty,loQty };
                ushort crc = ComputeCrc(list.ToArray());
                list.Add((byte)(crc & 0xFF));
                list.Add((byte)(crc >> 8));
                return list.ToArray();
            }

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

            private static bool ValidateCrc(byte[] frame)
            {
                if(frame.Length < 3) return false;
                ushort crcCalc = ComputeCrc(frame.AsSpan(0,frame.Length - 2).ToArray());
                ushort crcFrame = (ushort)(frame[^2] | (frame[^1] << 8));
                return crcCalc == crcFrame;
            }
        }
    }
}
