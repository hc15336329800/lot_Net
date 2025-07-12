using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RuoYi.Common.Data;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Data.Entities.Iot;
using RuoYi.Framework.DependencyInjection;
using RuoYi.Iot.Repositories;

namespace RuoYi.Iot.Services;

public class IotDeviceVariableService : BaseService<IotDeviceVariable,IotDeviceVariableDto>, ITransient
{
    private readonly IotDeviceVariableRepository _repo;
    private readonly ILogger<IotDeviceVariableService> _logger;

    public IotDeviceVariableService(ILogger<IotDeviceVariableService> logger,IotDeviceVariableRepository repo)
    {
        _logger = logger;
        _repo = repo;
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
}