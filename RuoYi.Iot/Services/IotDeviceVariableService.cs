using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RuoYi.Common.Data;
using RuoYi.Data.Dtos.Iot;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Data.Entities.Iot;
using RuoYi.Framework.DependencyInjection;
using RuoYi.Iot.Repositories;

namespace RuoYi.Iot.Services;

public class IotDeviceVariableService : BaseService<IotDeviceVariable,IotDeviceVariableDto>, ITransient
{
    private readonly IotDeviceVariableRepository _repo;
    private readonly ILogger<IotDeviceVariableService> _logger;
    private readonly IotDeviceVariableHistoryRepository _historyRepo;


    public IotDeviceVariableService(ILogger<IotDeviceVariableService> logger,IotDeviceVariableRepository repo,IotDeviceVariableHistoryRepository historyRepo)
    {
        _logger = logger;
        _repo = repo;
        _historyRepo = historyRepo;

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
    /// 更新当前值并记录历史
    /// </summary>
    public async Task SaveValueAsync(long deviceId,long variableId,string variableKey,string value)
    {
        var timestamp = DateTime.Now;
        await _repo.UpdateCurrentValueAsync(deviceId,variableId,value,timestamp);

        var history = new IotDeviceVariableHistory
        {
            DeviceId = deviceId,
            VariableId = variableId,
            VariableKey = variableKey,
            Value = value,
            Timestamp = timestamp
        };
        await _historyRepo.InsertAsync(history);


    }

    /// <summary>
    /// 获取指定设备的变量键映射
    /// </summary>
    public async Task<Dictionary<string,IotDeviceVariableDto>> GetVariableMapAsync(long deviceId)
    {
        var list = await _repo.GetDtoListAsync(new IotDeviceVariableDto { DeviceId = deviceId });
        return list.Where(v => !string.IsNullOrEmpty(v.VariableKey))
                   .ToDictionary(v => v.VariableKey!,v => v);
    }
}