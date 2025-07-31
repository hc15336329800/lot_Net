using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using RuoYi.Data.Entities.Iot;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Iot.Repositories;
using RuoYi.Iot.Services;
using RuoYi.Tcp.Configs;
using RuoYi.Tcp.Services;
using SqlSugar;
using Xunit;

public class TcpServiceTests
{
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

    private static SqlSugarClient CreateDb( )
    {
        var config = new ConnectionConfig
        {
            ConnectionString = $"DataSource={Guid.NewGuid()}.db",
            DbType = DbType.Sqlite,
            InitKeyType = InitKeyType.Attribute,
            IsAutoCloseConnection = true
        };
        var db = new SqlSugarClient(config);
        db.CodeFirst.InitTables<IotDevice,IotProduct>();
        return db;
    }

    private static TcpService CreateService(SqlSugarClient db,IotDevice device,IotProduct product)
    {
        var devRepo = new IotDeviceRepository(new SqlSugarRepository<IotDevice>(db));
        var prodRepo = new IotProductRepository(new SqlSugarRepository<IotProduct>(db));
        db.Insertable(device).ExecuteCommand();
        db.Insertable(product).ExecuteCommand();

        var devSvc = new IotDeviceService(NullLogger<IotDeviceService>.Instance,devRepo) { BaseRepo = devRepo };
        var prodSvc = new IotProductService(NullLogger<IotProductService>.Instance,prodRepo) { BaseRepo = prodRepo };
        var sp = new ServiceCollection().BuildServiceProvider();
        var options = Options.Create(new TcpServerOptions());
        return new TcpService(NullLogger<TcpService>.Instance,sp,devSvc,prodSvc,null!,options);
    }

    private static byte[] BuildExpected(bool ok)
    {
        Span<byte> data = stackalloc byte[6];
        data[0] = 0xE3;
        data[1] = 0x8E;
        data[2] = 0x38;
        data[3] = 0x00;
        data[4] = 0x01;
        data[5] = ok ? (byte)0x06 : (byte)0x33; // success or unknown device
        byte checksum = PacketUtils.CalculateChecksum(data);
        return new byte[] { data[0],data[1],data[2],data[3],data[4],data[5],checksum };
    }

    [Fact]
    public async Task ValidRegistrationGetsSuccessResponse( )
    {
        var db = CreateDb();
        var device = new IotDevice { Id = 1,DeviceName = "d",DeviceStatus = "0",DeviceDn = "dn",CommKey = "k",IotCardNo = "",TcpHost = "h",TcpPort = 1,AutoRegPacket = "reg",OrgId = 1,ProductId = 1,TagCategory = "",ActivateTime = DateTime.Now,Status = "0",DelFlag = "0",Remark = "",CreateBy = "",UpdateBy = "",CreateTime = DateTime.Now,UpdateTime = DateTime.Now };
        var product = new IotProduct { Id = 1,ProductName = "p",OrgId = 1,ProductModel = "m",ProductCode = "c",BrandName = "b",NetworkProtocol = "",AccessProtocol = "0",DataProtocol = "0",IsShared = 0,Description = "",Status = "0",DelFlag = "0",Remark = "",CreateBy = "",UpdateBy = "",CreateTime = DateTime.Now,UpdateTime = DateTime.Now };
        var service = CreateService(db,device,product);

        var listener = new TcpListener(IPAddress.Loopback,0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var cts = new CancellationTokenSource();
        var serverTask = Task.Run(async ( ) =>
        {
            var cli = await listener.AcceptTcpClientAsync();
            var method = typeof(TcpService).GetMethod("HandleClientAsync",System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            await (Task)method.Invoke(service,new object[] { cli,cts.Token });
        });

        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback,port);
        var stream = client.GetStream();
        await stream.WriteAsync(Encoding.UTF8.GetBytes("reg"));
        var resp = new byte[7];
        Assert.True(await ReadExactAsync(stream,resp));
        Assert.Equal(BuildExpected(true),resp);
        cts.Cancel();
        await serverTask;
        listener.Stop();
    }

    [Fact]
    public async Task InvalidRegistrationGetsFailureResponse( )
    {
        var db = CreateDb();
        var device = new IotDevice { Id = 1,DeviceName = "d",DeviceStatus = "0",DeviceDn = "dn",CommKey = "k",IotCardNo = "",TcpHost = "h",TcpPort = 1,AutoRegPacket = "reg",OrgId = 1,ProductId = 1,TagCategory = "",ActivateTime = DateTime.Now,Status = "0",DelFlag = "0",Remark = "",CreateBy = "",UpdateBy = "",CreateTime = DateTime.Now,UpdateTime = DateTime.Now };
        var product = new IotProduct { Id = 1,ProductName = "p",OrgId = 1,ProductModel = "m",ProductCode = "c",BrandName = "b",NetworkProtocol = "",AccessProtocol = "0",DataProtocol = "0",IsShared = 0,Description = "",Status = "0",DelFlag = "0",Remark = "",CreateBy = "",UpdateBy = "",CreateTime = DateTime.Now,UpdateTime = DateTime.Now };
        var service = CreateService(db,device,product);

        var listener = new TcpListener(IPAddress.Loopback,0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var cts = new CancellationTokenSource();
        var serverTask = Task.Run(async ( ) =>
        {
            var cli = await listener.AcceptTcpClientAsync();
            var method = typeof(TcpService).GetMethod("HandleClientAsync",System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            await (Task)method.Invoke(service,new object[] { cli,cts.Token });
        });

        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback,port);
        var stream = client.GetStream();
        await stream.WriteAsync(Encoding.UTF8.GetBytes("bad"));
        var resp = new byte[7];
        Assert.True(await ReadExactAsync(stream,resp));
        Assert.Equal(BuildExpected(false),resp);
        cts.Cancel();
        await serverTask;
        listener.Stop();
    }
}