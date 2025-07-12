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
            .WhereIF(dto.DeviceId.HasValue,d => d.DeviceId == dto.DeviceId);
    }

    public override ISugarQueryable<IotDeviceVariableDto> DtoQueryable(IotDeviceVariableDto dto)
    {
        return Repo.AsQueryable()
            .WhereIF(dto.Id > 0,d => d.Id == dto.Id)
            .WhereIF(dto.DeviceId.HasValue,d => d.DeviceId == dto.DeviceId)
            .Select(d => new IotDeviceVariableDto
            {
                Id = d.Id,
                DeviceId = d.DeviceId,
                VariableId = d.VariableId,
                VariableName = d.VariableName,
                VariableKey = d.VariableKey,
                VariableType = d.VariableType,
                CurrentValue = d.CurrentValue,
                LastUpdateTime = d.LastUpdateTime,
                Status = d.Status,
                DelFlag = d.DelFlag,
                Remark = d.Remark,
                CreateBy = d.CreateBy,
                CreateTime = d.CreateTime,
                UpdateBy = d.UpdateBy,
                UpdateTime = d.UpdateTime
            });
    }

    public async Task<int> UpdateCurrentValueAsync(long deviceId,long variableId,string value,DateTime timestamp)
    {

        return await base.Updateable()
         .SetColumns(d => d.CurrentValue == value)
         .SetColumns(d => d.LastUpdateTime == timestamp)
         .Where(d => d.DeviceId == deviceId && d.VariableId == variableId)
         .ExecuteCommandAsync();

    }
}