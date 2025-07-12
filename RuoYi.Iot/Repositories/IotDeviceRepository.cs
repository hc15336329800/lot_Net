using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RuoYi.Common.Data;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Data.Entities.Iot;
using SqlSugar;

  
namespace RuoYi.Iot.Repositories;

public class IotDeviceRepository : BaseRepository<IotDevice,IotDeviceDto>
{
    public IotDeviceRepository(ISqlSugarRepository<IotDevice> repo)
    {
        Repo = repo;
    }

    public override ISugarQueryable<IotDevice> Queryable(IotDeviceDto dto)
    {
        return Repo.AsQueryable()
            .WhereIF(dto.Id > 0,d => d.Id == dto.Id)
            .WhereIF(!string.IsNullOrEmpty(dto.DeviceName),d => d.DeviceName.Contains(dto.DeviceName!));
    }

    public override ISugarQueryable<IotDeviceDto> DtoQueryable(IotDeviceDto dto)
    {
        return Repo.AsQueryable()
            .WhereIF(dto.Id > 0,d => d.Id == dto.Id)
            .WhereIF(!string.IsNullOrEmpty(dto.DeviceName),d => d.DeviceName.Contains(dto.DeviceName!))
            .Select(d => new IotDeviceDto
            {
                Id = d.Id,
                DeviceName = d.DeviceName,
                DeviceStatus = d.DeviceStatus,
                DeviceDn = d.DeviceDn,
                CommKey = d.CommKey,
                IotCardNo = d.IotCardNo,
                TcpHost = d.TcpHost,
                TcpPort = d.TcpPort,
                AutoRegPacket = d.AutoRegPacket,
                OrgId = d.OrgId,
                ProductId = d.ProductId,
                TagCategory = d.TagCategory,
                ActivateTime = d.ActivateTime,
                Status = d.Status,
                DelFlag = d.DelFlag,
                Remark = d.Remark,
                CreateBy = d.CreateBy,
                CreateTime = d.CreateTime,
                UpdateBy = d.UpdateBy,
                UpdateTime = d.UpdateTime
            });
    }

    public async Task<int> UpdateStatusAsync(long id,string status)
    {
        return await base.Updateable()
            .SetColumns(d => d.DeviceStatus == status)
            .Where(d => d.Id == id)
            .ExecuteCommandAsync();
    }
}