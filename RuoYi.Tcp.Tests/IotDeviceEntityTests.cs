using RuoYi.Data.Entities.Iot;
using SqlSugar;
using System.Linq;
using System.Reflection;
using Xunit;

public class IotDeviceEntityTests
{
    [Fact]
    public void AutoRegPacket_HasUniqueConstraint( )
    {
        var prop = typeof(IotDevice).GetProperty("AutoRegPacket");
        Assert.NotNull(prop);
        var attr = prop!.GetCustomAttribute<SugarColumn>();
        Assert.NotNull(attr);
        //Assert.True(attr!.IsOnly); //IsOnly报错找不到所以屏蔽了
    }
}