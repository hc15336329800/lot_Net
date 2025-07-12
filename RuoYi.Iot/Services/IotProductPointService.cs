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

public class IotProductPointService : BaseService<IotProductPoint,IotProductPointDto>, ITransient
{
    private readonly IotProductPointRepository _repo;
    private readonly ILogger<IotProductPointService> _logger;

    public IotProductPointService(ILogger<IotProductPointService> logger,IotProductPointRepository repo)
    {
        _logger = logger;
        _repo = repo;
        BaseRepo = repo;
    }

    public async Task<IotProductPoint> GetAsync(long id)
    {
        return await base.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IotProductPointDto> GetDtoAsync(long id)
    {
        var dto = new IotProductPointDto { Id = id };
        return await _repo.GetDtoFirstAsync(dto);
    }
}