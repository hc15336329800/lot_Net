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
            .WhereIF(dto.JobId > 0,d => d.JobId == dto.JobId);
    }

    public override ISugarQueryable<SysJobIotDto> DtoQueryable(SysJobIotDto dto)
    {
        return Repo.AsQueryable()
            .WhereIF(dto.JobId > 0,d => d.JobId == dto.JobId)
            .Select(d => new SysJobIotDto
            {
                JobId = d.JobId,
                TargetType = d.TargetType,
                TaskType = d.TaskType,
                DeviceId = d.DeviceId,
                SelectPoints = d.SelectPoints,
                TriggerSource = d.TriggerSource,
                Status = d.Status,
                Remark = d.Remark,
                CreateBy = d.CreateBy,
                CreateTime = d.CreateTime,
                UpdateBy = d.UpdateBy,
                UpdateTime = d.UpdateTime
            });
    }
}