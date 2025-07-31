using RuoYi.Common.Data;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Data.Entities.Iot;
using SqlSugar;

namespace RuoYi.Iot.Repositories;

public class IotDeviceVariableRepository : BaseRepository<IotDeviceVariable,IotDeviceVariableDto>
{
    public IotDeviceVariableRepository(ISqlSugarRepository<IotDeviceVariable> repo)
    {
        Repo = repo;
    }

    public override ISugarQueryable<IotDeviceVariable> Queryable(IotDeviceVariableDto dto)
    {
        return Repo.AsQueryable()
            .WhereIF(dto.Id > 0,d => d.Id == dto.Id)
                        .Where(d => d.Status == "0" && d.DelFlag == "0")  // 无论如何都加

            .WhereIF(dto.DeviceId.HasValue,d => d.DeviceId == dto.DeviceId);
    }

    public override ISugarQueryable<IotDeviceVariableDto> DtoQueryable(IotDeviceVariableDto dto)
    {
        return Repo.AsQueryable()
            .LeftJoin<IotProductPoint>((dv,pp) => dv.VariableId == pp.Id)
            .WhereIF(dto.Id > 0,(dv,pp) => dv.Id == dto.Id)
            .WhereIF(dto.DeviceId.HasValue,(dv,pp) => dv.DeviceId == dto.DeviceId)
            .Where((dv,pp) => dv.Status == "0" && dv.DelFlag == "0") // 无论如何都加
            .Select((dv,pp) => new IotDeviceVariableDto
            {
                Id = dv.Id,
                DeviceId = dv.DeviceId,
                VariableId = dv.VariableId,
                VariableName = pp.PointName,
                VariableKey = pp.PointKey,
                VariableType = pp.VariableType,
                CurrentValue = dv.CurrentValue,
                LastUpdateTime = dv.LastUpdateTime,
                Status = dv.Status,
                DelFlag = dv.DelFlag,
                Remark = dv.Remark,
                CreateBy = dv.CreateBy,
                CreateTime = dv.CreateTime,
                UpdateBy = dv.UpdateBy,
                UpdateTime = dv.UpdateTime
            });
    }

    public async Task<int> UpdateCurrentValueAsync(long deviceId,long variableId,string value,DateTime timestamp)
    {

        var id = await Repo.AsQueryable()
           .Where(d => d.DeviceId == deviceId && d.VariableId == variableId)
           .Select(d => d.Id)
           .FirstAsync();

        if(id == 0)
        {
            RuoYi.Framework.Logging.Log.Warning("Device variable not found for device {0} variable {1}",deviceId,variableId);
            return 0;
        }

        return await base.Updateable()
            .SetColumns(d => d.CurrentValue == value)
            .SetColumns(d => d.LastUpdateTime == timestamp)
            .Where(d => d.Id == id)
            .ExecuteCommandAsync();

    }


    public async Task<List<IotDeviceVariable>> GetByDeviceIdAsync(long deviceId)
    {
        return await Repo.AsQueryable().Where(d => d.DeviceId == deviceId).ToListAsync();
    }


    public async Task<IotDeviceVariable?> GetByDeviceIdAndKeyAsync(long deviceId,string variableKey)
    {
        return await Repo.AsQueryable()
            .LeftJoin<IotProductPoint>((dv,pp) => dv.VariableId == pp.Id)
            .Where((dv,pp) => dv.DeviceId == deviceId && pp.PointKey == variableKey)
            .Select((dv,pp) => dv)
            .FirstAsync();
    }

}