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
using RuoYi.Framework.Cache;
using RuoYi.Data;

namespace RuoYi.Iot.Services;

public class IotProductPointService : BaseService<IotProductPoint,IotProductPointDto>, ITransient
{
    private readonly IotProductPointRepository _repo;
    private readonly ILogger<IotProductPointService> _logger;
    private readonly ICache _cache;


    public IotProductPointService(ILogger<IotProductPointService> logger,IotProductPointRepository repo,ICache cache)
    {
        _logger = logger;
        _repo = repo;
        _cache = cache;

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

    /// <summary>
    /// 移除指定产品的点位缓存
    /// </summary>
    public void RemoveCache(long productId)
    {
        _cache.Remove(CacheConstants.IOT_POINT_MAP_KEY + productId);
    }

    /// <summary>
    /// 获取指定产品的点位列表，优先从缓存中读取
    /// </summary>
    public async Task<List<IotProductPointDto>> GetCachedListAsync(long productId)
    {
        string cacheKey = CacheConstants.IOT_POINT_MAP_KEY + productId;
        var list = await _cache.GetAsync<List<IotProductPointDto>>(cacheKey);
        if(list != null && list.Count > 0)
        {
            return list;
        }

        list = await _repo.GetDtoListAsync(new IotProductPointDto
        {
            ProductId = productId,
            Status = "0",
            DelFlag = "0"
        });

        if(list.Count > 0)
        {
            await _cache.SetAsync(cacheKey,list,60 * 24); // 缓存一天
        }

        return list;
    }

}