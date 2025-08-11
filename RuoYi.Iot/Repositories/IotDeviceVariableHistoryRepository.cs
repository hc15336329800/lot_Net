using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RuoYi.Data.Dtos.Iot;
using RuoYi.Data.Entities.Iot;


namespace RuoYi.Iot.Repositories
{
    public class IotDeviceVariableHistoryRepository : BaseRepository<IotDeviceVariableHistory,IotDeviceVariableHistoryDto>
    {
        public IotDeviceVariableHistoryRepository(ISqlSugarRepository<IotDeviceVariableHistory> repo)
        {
            Repo = repo;
        }

        public override ISugarQueryable<IotDeviceVariableHistory> Queryable(IotDeviceVariableHistoryDto dto)
        {
            return Repo.AsQueryable()
                //.WhereIF(dto.Id > 0,d => d.Id == dto.Id)
                .WhereIF(dto.DeviceId.HasValue,d => d.DeviceId == dto.DeviceId)
                .WhereIF(dto.VariableId.HasValue,d => d.VariableId == dto.VariableId);
        }

        public override ISugarQueryable<IotDeviceVariableHistoryDto> DtoQueryable(IotDeviceVariableHistoryDto dto)
        {
            return Repo.AsQueryable()
                //.WhereIF(dto.Id > 0,d => d.Id == dto.Id)
                .WhereIF(dto.DeviceId.HasValue,d => d.DeviceId == dto.DeviceId)
                .WhereIF(dto.VariableId.HasValue,d => d.VariableId == dto.VariableId)
                .Select(d => new IotDeviceVariableHistoryDto
                {
                    //Id = d.Id,
                    DeviceId = d.DeviceId,
                    VariableId = d.VariableId,
                    VariableKey = d.VariableKey,
                    Value = d.Value,
                    Timestamp = d.Timestamp,
                    Status = d.Status,
                    DelFlag = d.DelFlag,
                    Remark = d.Remark,
                    CreateBy = d.CreateBy,
                    CreateTime = d.CreateTime,
                    UpdateBy = d.UpdateBy,
                    UpdateTime = d.UpdateTime
                });
        }
    }
}