﻿using System;
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
        int affected = await _repo.UpdateCurrentValueAsync(deviceId,variableId,value,timestamp);
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
        bool insertOk = await _historyRepo.InsertAsync(history); //插入
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
        var list = await _repo.GetDtoListAsync(new IotDeviceVariableDto { DeviceId = deviceId });
        return list.Where(v => !string.IsNullOrEmpty(v.VariableKey))
                   .ToDictionary(v => v.VariableKey!,v => v);
    }

    /// <summary>
    /// 获取指定设备所有变量的最新值
    /// </summary>
    public async Task<List<IotDeviceVariableDto>> GetLatestListAsync(long deviceId)
    {
        return await _repo.GetDtoListAsync(new IotDeviceVariableDto { DeviceId = deviceId });
    }
}