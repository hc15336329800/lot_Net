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

public class IotProductService : BaseService<IotProduct,IotProductDto>, ITransient
{
    private readonly IotProductRepository _repo;
    private readonly ILogger<IotProductService> _logger;

    public IotProductService(ILogger<IotProductService> logger,IotProductRepository repo)
    {
        _logger = logger;
        _repo = repo;
        BaseRepo = repo;
    }

    public async Task<IotProduct> GetAsync(long id)
    {
        return await base.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IotProductDto> GetDtoAsync(long id)
    {
        var dto = new IotProductDto { Id = id };
        return await _repo.GetDtoFirstAsync(dto);
    }


    public async Task<IotProductDto?> GetDtoByCodeAsync(string code)
    {
        var dto = new IotProductDto { ProductCode = code };
        return await _repo.GetDtoFirstAsync(dto);
    }


}