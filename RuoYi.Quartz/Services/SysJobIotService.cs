using Microsoft.AspNetCore.Http;
using RuoYi.Common.Data;
using RuoYi.Common.Utils;
using RuoYi.Framework.Interceptors;
using RuoYi.Quartz.Dtos;
using RuoYi.Quartz.Entities;
using RuoYi.Quartz.Repositories;

namespace RuoYi.Quartz.Services;

/// <summary>
///  定时任务IOT扩展表 Service
/// </summary>
public class SysJobIotService : BaseService<SysJobIot,SysJobIotDto>
{
    private readonly SysJobIotRepository _repository;
    private readonly SysJobService _sysJobService;

    public SysJobIotService(SysJobIotRepository repository,SysJobService sysJobService)
    {
        BaseRepo = repository;
        _repository = repository;
        _sysJobService = sysJobService;
    }

    /// <summary>
    /// 分页查询任务，先查扩展表再查主表
    /// </summary>
    public override async Task<SqlSugarPagedList<SysJobIotDto>> GetDtoPagedListAsync(SysJobIotDto dto)
    {
        var jobIds = await _repository.Queryable(dto).Select(d => d.JobId).ToListAsync();
        if(jobIds.Count == 0)
        {
            return new SqlSugarPagedList<SysJobIotDto>
            {
                PageIndex = 0,
                PageSize = 0,
                Total = 0,
                Rows = new List<SysJobIotDto>(),
                Code = StatusCodes.Status200OK,
                HasPrevPages = false,
                HasNextPages = false
            };
        }

        var jobQuery = _sysJobService.BaseRepo.Queryable(dto.Adapt<SysJobDto>())
            .Where(j => jobIds.Contains(j.JobId))
            .Select(j => new SysJobDto
            {
                JobId = j.JobId
            });
        var jobPaged = await _sysJobService.BaseRepo.GetDtoPagedListAsync(jobQuery);

        var pageJobIds = jobPaged.Rows.Select(r => r.JobId).ToList();
        var extList = await _repository.Queryable(new SysJobIotDto())
            .Where(e => pageJobIds.Contains(e.JobId))
                       .Select(e => new SysJobIotDto
                       {
                           JobId = e.JobId
                       })
            .ToListAsync();
        var extDict = extList.ToDictionary(e => e.JobId);

        var rows = jobPaged.Rows.Select(j =>
        {
            var item = j.Adapt<SysJobIotDto>();
            if(extDict.TryGetValue(j.JobId,out var ext))
            {
                item.TargetType = ext.TargetType;
                item.TaskType = ext.TaskType;
                item.DeviceId = ext.DeviceId;
                item.ProductId = ext.ProductId;
                item.SelectPoints = ext.SelectPoints;
                item.TriggerSource = ext.TriggerSource;
                item.Status = ext.Status;
                item.Remark = ext.Remark;
                item.CreateBy = ext.CreateBy;
                item.CreateTime = ext.CreateTime;
                item.UpdateBy = ext.UpdateBy;
                item.UpdateTime = ext.UpdateTime;
            }
            return item;
        }).ToList();

        return new SqlSugarPagedList<SysJobIotDto>
        {
            PageIndex = jobPaged.PageIndex,
            PageSize = jobPaged.PageSize,
            Total = jobPaged.Total,
            Rows = rows,
            Code = jobPaged.Code,
            HasPrevPages = jobPaged.HasPrevPages,
            HasNextPages = jobPaged.HasNextPages
        };

    }
    /// <summary>
    /// 新增任务并创建扩展信息
    /// </summary>
    [Transactional] //增加事务
    public async Task<bool> InsertAsync(SysJobIotDto dto)
    {
        var job = dto.Adapt<SysJobDto>();
        job.JobId = dto.JobId;
        var ok = await _sysJobService.InsertJobAsync(job);

        if(ok)
        {
             return await _repository.InsertAsync(dto);
        }
        return false;
    }

    /// <summary>
    /// 更新任务及扩展信息
    /// </summary>
    public async Task<bool> UpdateAsync(SysJobIotDto dto)
    {
        var rows = await _sysJobService.UpdateJobAsync(dto.Adapt<SysJobDto>());
        if(rows)
        {
            await _repository.UpdateAsync(dto);
        }
        return rows;
    }

    /// <summary>
    /// 删除任务及扩展信息
    /// </summary>
    public async Task DeleteAsync(List<long> jobIds)
    {
        await _sysJobService.DeleteJobByIdsAsync(jobIds);
        await _repository.DeleteAsync(jobIds);
    }

    /// <summary>
    /// 任务调度状态修改  启停
    /// </summary>
    /// <param name="dto">任务对象</param>
    public async Task<bool> ChangeStatusAsync(SysJobIotDto dto)
    {
        return await _sysJobService.ChangeStatusAsync(dto.Adapt<SysJobDto>());
    }

    /// <summary>
    /// 定时任务立即执行一次
    /// </summary>
    /// <param name="dto">任务对象</param>
    public async Task<bool> Run(SysJobIotDto dto)
    {
        return await _sysJobService.Run(dto.Adapt<SysJobDto>());
    }



    /// <summary>
    /// 根据设备ID查询任务列表
    /// </summary>
    public async Task<List<SysJobIotDto>> GetListByDeviceId(long deviceId)
    {
        return await GetListByFilter(new SysJobIotDto { DeviceId = deviceId });
    }

    /// <summary>
    /// 根据产品ID查询任务列表
    /// </summary>
    public async Task<List<SysJobIotDto>> GetListByProductId(long productId)
    {
        return await GetListByFilter(new SysJobIotDto { ProductId = productId });
    }

    private async Task<List<SysJobIotDto>> GetListByFilter(SysJobIotDto dto)
    {
        var jobIds = await _repository.Queryable(dto)
    .Select<long>("job_id") // ★强制用真实列名
    .ToListAsync();

        if(jobIds.Count == 0)
        {
            return new List<SysJobIotDto>();
        }

        var jobList = await _sysJobService.BaseRepo.Queryable(new SysJobDto())
            .Where(j => jobIds.Contains(j.JobId))
                .Select(j => new SysJobDto
                {
                    JobId = j.JobId
                })
            .ToListAsync();

        var extList = await _repository.Queryable(new SysJobIotDto())
            .Where(e => jobIds.Contains(e.JobId))
            .ToListAsync();
        var extDict = extList.ToDictionary(e => e.JobId);

        var rows = jobList.Select(j =>
        {
            var item = j.Adapt<SysJobIotDto>();
            if(extDict.TryGetValue(j.JobId,out var ext))
            {
                item.TargetType = ext.TargetType;
                item.TaskType = ext.TaskType;
                item.DeviceId = ext.DeviceId;
                item.ProductId = ext.ProductId;
                item.SelectPoints = ext.SelectPoints;
                item.TriggerSource = ext.TriggerSource;
                item.Status = ext.Status;
                item.Remark = ext.Remark;
                item.CreateBy = ext.CreateBy;
                item.CreateTime = ext.CreateTime;
                item.UpdateBy = ext.UpdateBy;
                item.UpdateTime = ext.UpdateTime;
            }
            return item;
        }).ToList();

        return rows;
    }

}