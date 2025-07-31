using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RuoYi.Common.Data;
using RuoYi.Common.Utils;
using RuoYi.Data.Dtos.Iot;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Data.Entities.Iot;
using RuoYi.Framework.DependencyInjection;
using RuoYi.Iot.Repositories;
using RuoYi.Framework.Cache;
using RuoYi.Data;
using Microsoft.Extensions.DependencyInjection;

namespace RuoYi.Iot.Services;

public class IotDeviceVariableService : BaseService<IotDeviceVariable,IotDeviceVariableDto>, ITransient
{
    private readonly IotDeviceVariableRepository _repo;
    private readonly ILogger<IotDeviceVariableService> _logger;
    private readonly IotDeviceVariableHistoryRepository _historyRepo;
    private readonly IotDeviceService _deviceService;
    private readonly IotProductPointService _pointService;
    private readonly ICache _cache;
    private readonly IServiceScopeFactory _scopeFactory;


    public IotDeviceVariableService(ILogger<IotDeviceVariableService> logger,
        IotDeviceVariableRepository repo,
        IotDeviceVariableHistoryRepository historyRepo,
        IotDeviceService deviceService,
         IotProductPointService pointService,
        ICache cache,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _repo = repo;
        _historyRepo = historyRepo;
        _deviceService = deviceService;
        _pointService = pointService;
        _cache = cache;
        _scopeFactory = scopeFactory;

        BaseRepo = repo;
    }

    public async Task<IotDeviceVariable> GetAsync(long id)
    {
        return await base.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IotDeviceVariableDto> GetDtoAsync(long id)
    {
        var dto = new IotDeviceVariableDto { Id = id };
        return await _repo.GetDtoFirstAsync(dto);
    }


    /// <summary>
    /// 确保设备变量与产品点位一致，如有缺失则自动补全
    /// </summary>
    public async Task SyncDeviceVariablesAsync(long deviceId)
    {
        var device = await _deviceService.GetDtoAsync(deviceId);
        if(device == null || !device.ProductId.HasValue)
            return;

        var points = await _pointService.GetCachedListAsync(device.ProductId.Value);

        if(points.Count == 0)
            return;

        var existing = await _repo.GetByDeviceIdAsync(deviceId);
        var existIds = existing.Select(v => v.VariableId).ToHashSet();

        var newVars = new List<IotDeviceVariable>();
        foreach(var p in points)
        {
            if(!existIds.Contains(p.Id))
            {
                newVars.Add(new IotDeviceVariable
                {
                    Id = NextId.Id13(),
                    DeviceId = deviceId,
                    VariableId = p.Id,
                    CurrentValue = p.DefaultValue,
                    Status = "0",
                    DelFlag = "0"
                });
            }
        }

        if(newVars.Count > 0)
        {
            await _repo.InsertBatchAsync(newVars);
            _cache.Remove(CacheConstants.IOT_VAR_MAP_KEY + deviceId);

        }
    }


    /// <summary>
    /// 更新当前值并记录历史
    /// </summary>
    public async Task SaveValueAsync(long deviceId,long variableId,string variableKey,string value)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IotDeviceVariableRepository>();
        var historyRepo = scope.ServiceProvider.GetRequiredService<IotDeviceVariableHistoryRepository>();

        var timestamp = DateTime.Now;
        int affected = await repo.UpdateCurrentValueAsync(deviceId,variableId,value,timestamp);
        if(affected > 0)
        {
            Console.WriteLine($"[调试] 设备{deviceId}的变量{variableId}已更新CurrentValue为{value}，LastUpdateTime为{timestamp}");
        }
        else
        {
            Console.WriteLine($"[警告] 设备{deviceId}的变量{variableId}更新失败，可能没有找到对应记录！");
        }


        var history = new IotDeviceVariableHistory
        {
            Id = IdGenerator.NewId(),
             
            DeviceId = deviceId,  //设备ID
            VariableId = variableId, //变量ID
            VariableKey = variableKey, //变量表示
            Value = value,               //采集值
            Timestamp = timestamp   //采集时间
        };

        bool insertOk = await historyRepo.InsertAsync(history); //插入

        if(insertOk)
        {
            Console.WriteLine($"[调试] 历史变量插入成功，ID={history.Id}");
        }
        else
        {
            Console.WriteLine($"[警告] 历史变量插入失败，ID={history.Id}");
        }

    }

    /// <summary>
    /// 获取指定设备的变量键映射
    /// </summary>
    public async Task<Dictionary<string,IotDeviceVariableDto>> GetVariableMapAsync(long deviceId)
    {
        string cacheKey = CacheConstants.IOT_VAR_MAP_KEY + deviceId;
        var cached = await _cache.GetAsync<Dictionary<string,IotDeviceVariableDto>>(cacheKey);
        if(cached != null && cached.Count > 0)
        {
            // 返回缓存副本，避免多线程环境下修改同一实例
            return new Dictionary<string,IotDeviceVariableDto>(cached);
        }

        var list = await _repo.GetDtoListAsync(new IotDeviceVariableDto { DeviceId = deviceId });
        var dict = list.Where(v => !string.IsNullOrEmpty(v.VariableKey))
                        .ToDictionary(v => v.VariableKey!,v => v);

        if(dict.Count > 0)
        {
            await _cache.SetAsync(cacheKey,dict,60 * 24); // 缓存一天
        }

        return dict;
    }

    /// <summary>
    /// 获取指定设备所有变量的最新值
    /// </summary>
    public async Task<List<IotDeviceVariableDto>> GetLatestListAsync(long deviceId)
    {
        return await _repo.GetDtoListAsync(new IotDeviceVariableDto { DeviceId = deviceId });
    }



    /// <summary>
    /// 移除指定设备的变量映射缓存
    /// </summary>
    public void RemoveCache(long deviceId)
    {
        _cache.Remove(CacheConstants.IOT_VAR_MAP_KEY + deviceId);
    }
}