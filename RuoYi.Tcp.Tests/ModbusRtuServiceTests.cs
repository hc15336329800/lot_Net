using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Tcp.Services;
using Xunit;

public class ModbusRtuServiceTests
{
    private static byte[] BuildWriteSingleRegisterFrame(byte slave,ushort address,ushort value)
    {
        var list = new List<byte> { slave,0x06,(byte)(address >> 8),(byte)(address & 0xFF),(byte)(value >> 8),(byte)(value & 0xFF) };
        ushort crc = ComputeCrc(list.ToArray());
        list.Add((byte)(crc & 0xFF));
        list.Add((byte)(crc >> 8));
        return list.ToArray();
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

    private static async Task<bool> ReadExactAsync(NetworkStream stream,byte[] buffer)
    {
        int read = 0;
        while(read < buffer.Length)
        {
            var r = await stream.ReadAsync(buffer.AsMemory(read,buffer.Length - read));
            if(r == 0) return false;
            read += r;
        }
        return true;
    }

    [Fact]
    public async Task ClientCanSendMultipleFramesAndStayConnected( )
    {
        var service = new ModbusRtuService(NullLogger<ModbusRtuService>.Instance,null!,null!,null!);
        var device = new IotDeviceDto { Id = 1,DeviceName = "dev" };

        var listener = new TcpListener(IPAddress.Loopback,0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        using var cts = new CancellationTokenSource();
        var serverTask = Task.Run(async ( ) =>
        {
            var client = await listener.AcceptTcpClientAsync();
            await service.HandleClientAsync(client,device,cts.Token);
        });

        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback,port);
        var stream = client.GetStream();

        var frame = BuildWriteSingleRegisterFrame(1,1,10);
        await stream.WriteAsync(frame);
        var resp = new byte[8];
        Assert.True(await ReadExactAsync(stream,resp));
        Assert.True(frame.SequenceEqual(resp));

        // send again to verify connection alive
        await stream.WriteAsync(frame);
        resp = new byte[8];
        Assert.True(await ReadExactAsync(stream,resp));
        Assert.True(frame.SequenceEqual(resp));

        Assert.True(client.Connected);
        cts.Cancel();
        await serverTask;
        client.Dispose();
        listener.Stop();
    }
}