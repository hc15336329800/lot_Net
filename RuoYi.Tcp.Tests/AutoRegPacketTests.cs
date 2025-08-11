using System;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Framework.DataEncryption.Extensions;
using RuoYi.Iot.Services;
using Xunit;

public class AutoRegPacketTests
{
    [Fact]
    public void BuildPacket_CreatesFixedLengthString( )
    {
        var dto = new IotDeviceDto
        {
            DeviceDn = "DN001",
            CommKey = "KEY",
            IotCardNo = "CARD"
        };

        var packet = IotDeviceService.BuildAutoRegPacket(dto);
        Assert.Equal(50,packet.Length);
        var expected = "31" +
            "DN001".PadRight(20,'0').Substring(0,20) +
            "KEY".PadRight(8,'0').Substring(0,8) +
            "CARD".PadRight(20,'0').Substring(0,20);
        Assert.Equal(expected,packet);
    }

    [Fact]
    public void BuildPacket_WithEncryption_RoundTrips( )
    {
        var dto = new IotDeviceDto
        {
            DeviceDn = "DN001",
            CommKey = "12345678",
            IotCardNo = "CARD"
        };

        var enc = IotDeviceService.BuildAutoRegPacket(dto,true);
        var bytes = Convert.FromBase64String(enc);
        var plainBytes = bytes.ToAESDecrypt(dto.CommKey!);
        var plain = Encoding.ASCII.GetString(plainBytes);
        var expectedPlain = IotDeviceService.BuildAutoRegPacket(dto);
        Assert.Equal(expectedPlain,plain);
    }
}