using RuoYi.Common.Data;
using RuoYi.Quartz.Dtos;
using RuoYi.Quartz.Entities;
using SqlSugar;

namespace RuoYi.Quartz.Repositories;

public class SysJobIotRepository : BaseRepository<SysJobIot,SysJobIotDto>
{
    public SysJobIotRepository(ISqlSugarRepository<SysJobIot> repo)
    {
        Repo = repo;
    }

    public override ISugarQueryable<SysJobIot> Queryable(SysJobIotDto dto)
    {
        return Repo.AsQueryable()
 .WhereIF(dto.JobId > 0,d => d.JobId == dto.JobId)
            .WhereIF(!string.IsNullOrEmpty(dto.TargetType),d => d.TargetType == dto.TargetType)
            .WhereIF(!string.IsNullOrEmpty(dto.TaskType),d => d.TaskType == dto.TaskType)
            .WhereIF(dto.DeviceId > 0,d => d.DeviceId == dto.DeviceId)
            .WhereIF(dto.ProductId > 0,d => d.ProductId == dto.ProductId)
            .WhereIF(!string.IsNullOrEmpty(dto.TriggerSource),d => d.TriggerSource == dto.TriggerSource);
    }

    public override ISugarQueryable<SysJobIotDto> DtoQueryable(SysJobIotDto dto)
    {
        return Repo.AsQueryable()
  .WhereIF(dto.JobId > 0,d => d.JobId == dto.JobId)
            .WhereIF(!string.IsNullOrEmpty(dto.TargetType),d => d.TargetType == dto.TargetType)
            .WhereIF(!string.IsNullOrEmpty(dto.TaskType),d => d.TaskType == dto.TaskType)
            .WhereIF(dto.DeviceId > 0,d => d.DeviceId == dto.DeviceId)
            .WhereIF(dto.ProductId > 0,d => d.ProductId == dto.ProductId)
            .WhereIF(!string.IsNullOrEmpty(dto.TriggerSource),d => d.TriggerSource == dto.TriggerSource).Select(d => new SysJobIotDto
            {
                JobId = d.JobId,
                TargetType = d.TargetType,
                TaskType = d.TaskType,
                DeviceId = d.DeviceId,
                ProductId = d.ProductId,

                SelectPoints = d.SelectPoints,
                TriggerSource = d.TriggerSource,
                Star = SqlFunc.ToInt32(d.Status),
                Remark = d.Remark,
                CreateBy = d.CreateBy,
                CreateTime = d.CreateTime,
                UpdateBy = d.UpdateBy,
                UpdateTime = d.UpdateTime
            });
    }
}