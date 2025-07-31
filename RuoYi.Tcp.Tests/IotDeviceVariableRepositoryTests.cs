using System;
using System.Threading.Tasks;
using RuoYi.Data.Entities.Iot;
using RuoYi.Iot.Repositories;
using SqlSugar;
using Xunit;

public class IotDeviceVariableRepositoryTests
{
    [Fact]
    public async Task UpdateCurrentValueAsync_NoDuplicate_DoesNotThrow( )
    {
        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = $"DataSource={Guid.NewGuid()}.db",
            DbType = DbType.Sqlite,
            InitKeyType = InitKeyType.Attribute,
            IsAutoCloseConnection = true
        });
        db.CodeFirst.InitTables<IotDeviceVariable>();
        var repo = new IotDeviceVariableRepository(new SqlSugarRepository<IotDeviceVariable>(db));
        db.Insertable(new IotDeviceVariable
        {
            Id = 1,
            DeviceId = 1,
            VariableId = 1,
            Status = "0",
            DelFlag = "0",
            CurrentValue = "",
            LastUpdateTime = DateTime.Now
        }).ExecuteCommand();

        var result = await repo.UpdateCurrentValueAsync(1,1,"v",DateTime.Now);
        Assert.Equal(1,result);
    }

    [Fact]
    public async Task Insert_Duplicate_RespectsUniqueIndex( )
    {
        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = $"DataSource={Guid.NewGuid()}.db",
            DbType = DbType.Sqlite,
            InitKeyType = InitKeyType.Attribute,
            IsAutoCloseConnection = true
        });
        db.CodeFirst.InitTables<IotDeviceVariable>();
        db.Ado.ExecuteCommand("CREATE UNIQUE INDEX idx_device_variable_unique ON iot_device_variable(device_id,variable_id);");
        db.Insertable(new IotDeviceVariable
        {
            Id = 1,
            DeviceId = 1,
            VariableId = 1,
            Status = "0",
            DelFlag = "0"
        }).ExecuteCommand();

        await Assert.ThrowsAsync<Exception>(async ( ) =>
        {
            await db.Insertable(new IotDeviceVariable
            {
                Id = 2,
                DeviceId = 1,
                VariableId = 1,
                Status = "0",
                DelFlag = "0"
            }).ExecuteCommandAsync();
        });
    }
}