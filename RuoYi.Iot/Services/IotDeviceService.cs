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

public class IotDeviceService : BaseService<IotDevice,IotDeviceDto>, ITransient
{
    private readonly IotDeviceRepository _repo;
    private readonly ILogger<IotDeviceService> _logger;

    public IotDeviceService(ILogger<IotDeviceService> logger,IotDeviceRepository repo)
    {
        _logger = logger;
        _repo = repo;
        BaseRepo = repo;
    }

    public async Task<IotDevice> GetAsync(long id)
    {
        return await base.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IotDeviceDto> GetDtoAsync(long id)
    {
        var dto = new IotDeviceDto { Id = id };
        return await _repo.GetDtoFirstAsync(dto);
    }
}