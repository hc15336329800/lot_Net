using RuoYi.Common.Data;
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
    /// 新增任务并创建扩展信息
    /// </summary>
    public async Task<bool> InsertAsync(SysJobIotDto dto)
    {
        var ok = await _sysJobService.InsertJobAsync(dto);
        if(ok)
        {
            dto.JobId = dto.JobId;
            return await _repository.InsertAsync(dto);
        }
        return false;
    }

    /// <summary>
    /// 更新任务及扩展信息
    /// </summary>
    public async Task<bool> UpdateAsync(SysJobIotDto dto)
    {
        var rows = await _sysJobService.UpdateJobAsync(dto);
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
        return await _sysJobService.ChangeStatusAsync(dto);
    }

    /// <summary>
    /// 定时任务立即执行一次
    /// </summary>
    /// <param name="dto">任务对象</param>
    public async Task<bool> Run(SysJobIotDto dto)
    {
        return await _sysJobService.Run(dto);
    }


    /// <summary>
    /// 根据设备ID查询任务列表
    /// </summary>
    public async Task<List<SysJobDto>> GetJobsByDeviceId(long deviceId)
    {
        var jobIds = await _repository.Queryable(new SysJobIotDto { DeviceId = deviceId })
            .Select(d => d.JobId)
            .ToListAsync();

        if(!jobIds.Any())
        {
            return new List<SysJobDto>();
        }

        var query = _sysJobService.BaseRepo.DtoQueryable(new SysJobDto());
        query = query.Where(j => jobIds.Contains(j.JobId));
        return await query.ToListAsync();
    }

    /// <summary>
    /// 根据产品ID查询任务列表
    /// </summary>
    public async Task<List<SysJobDto>> GetJobsByProductId(long productId)
    {
        var jobIds = await _repository.Queryable(new SysJobIotDto { productId = productId })
            .Select(d => d.JobId)
            .ToListAsync();

        if(!jobIds.Any())
        {
            return new List<SysJobDto>();
        }

        var query = _sysJobService.BaseRepo.DtoQueryable(new SysJobDto());
        query = query.Where(j => jobIds.Contains(j.JobId));
        return await query.ToListAsync();
    }
}