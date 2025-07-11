using System.Text.Json;
using Mapster;
using Microsoft.Extensions.Logging;
using RuoYi.Common.Data;
using RuoYi.Data;
using RuoYi.Framework;
using RuoYi.Framework.Cache;
using RuoYi.Framework.DependencyInjection;
using RuoYi.Zk.TuoXiao.Dtos;
using RuoYi.Zk.TuoXiao.Entities;
using RuoYi.Zk.TuoXiao.Repositories;
using StackExchange.Redis;

namespace RuoYi.Zk.TuoXiao.Services
{
    /// <summary>
    ///  工厂设备_传感器大屏数据 Service
    ///  author ruoyi.net
    ///  date   2024-10-26 16:11:03
    /// </summary>
    public class TxSensorsDataviewService : BaseService<TxSensorsDataview, TxSensorsDataviewDto007>, ITransient
    {



        private readonly ILogger<TxSensorsDataviewService> _logger;
        private readonly TxSensorsDataviewRepository _txSensorsDataviewRepository;
        private readonly ICache _cache;

        public TxSensorsDataviewService(ICache cache,ILogger<TxSensorsDataviewService> logger,
            TxSensorsDataviewRepository txSensorsDataviewRepository)
        {
            BaseRepo = txSensorsDataviewRepository;
            _cache = cache;

            _logger = logger;
            _txSensorsDataviewRepository = txSensorsDataviewRepository;
        }


        /// <summary>
        /// 将 TxSensorsDataviewDto 对象存储到 Redis
        /// </summary>
        /// <param name="topic">消息来源的主题</param>
        /// <param name="data">解析后的 TxSensorsDataviewDto 对象</param>
        public async Task StoreDtoInRedis(string topic,TxSensorsDataviewDto data)
        {
            if(string.IsNullOrEmpty(topic))
            {
                topic = "ReplyTopicAutoData";
            }

            try
            {
                // 序列化对象为 JSON 字符串
                string cacheEntry = JsonSerializer.Serialize(data);

                // 设置 Redis 缓存键
                string cacheKey = GetCacheKey(topic);

                // 将数据存入 Redis 并设置过期时间（例如 10 分钟）
                long expireMinutes = 60; // 缓存过期时间可根据需求调整
                await _cache.SetStringAsync(cacheKey,cacheEntry,expireMinutes);

                // 日志记录
                _logger.LogInformation($"数据已成功存入 Redis，键: {cacheKey}, 过期时间: {expireMinutes} 分钟");
            }
            catch(Exception ex)
            {
                _logger.LogError($"存储到 Redis 时发生错误: {ex.Message}",ex);
            }
        }



        /// <summary>
        /// 从 Redis 获取最新的传感器数据
        /// </summary>
        /// <param name="topic">Redis 键的主题部分</param>
        /// <returns>传感器数据对象</returns>
        public TxSensorsDataviewDto007? GetLatestSensorData(string topic)
        {
            if(string.IsNullOrEmpty(topic))
            {
                topic = "ReplyTopicAutoData";
            }

            try
            {
                // 从 Redis 中获取数据并移除
                string? configValue = _cache.GetString(GetCacheKey(topic));

                // 检查是否为空
                if(string.IsNullOrEmpty(configValue))
                {
                    _logger.LogWarning($"Redis 中未找到传感器数据，主题: {topic}");
                    return null;
                }

                // 反序列化为对象
                var dto = JsonSerializer.Deserialize<TxSensorsDataviewDto007>(configValue);

                if(dto == null)
                {
                    _logger.LogError($"从 Redis 中反序列化数据失败，主题: {topic}, 数据: {configValue}");
                    return null;
                }

                // 成功返回数据
                _logger.LogInformation($"成功从 Redis 中获取传感器数据，主题: {topic}");
                return dto;
            }
            catch(Exception ex)
            {
                // 记录异常日志
                _logger.LogError($"获取 Redis 数据时发生错误，主题: {topic}, 错误信息: {ex.Message}");
                throw;
            }
        }



        /// <summary>
        /// 从 Redis 获取字符串值后移除
        /// </summary>
        /// <param name="topic">Redis 键的主题部分</param>
        /// <returns>传感器数据对象</returns>
        public bool MoveLatestSensorData(string topic)
        {
            if(string.IsNullOrEmpty(topic))
            {
                topic = "ReplyTopicAutoData";
            }

            try
            {
                // 从 Redis 中获取数据并移除
                string? configValue = _cache.GetStringAndRemove(GetCacheKey(topic));

                // 检查是否为空
                if(string.IsNullOrEmpty(configValue))
                {
                     return false;
                }
                else
                {
                    return true;
                }
 
            }
            catch(Exception ex)
            {
                // 记录异常日志
                throw;
            }
        }




        /// <summary>
        /// 查询 工厂设备_传感器大屏数据 详情
        /// </summary>
        public async Task<TxSensorsDataview> GetAsync(long id)
        {
            var entity = await base.FirstOrDefaultAsync(e => e.Id == id);
            return entity;
        }

        /// <summary>
        /// 查询 工厂设备_传感器大屏数据 详情
        /// </summary>
        public async Task<TxSensorsDataviewDto007> GetDtoAsync(long id)
        {
            var entity = await base.FirstOrDefaultAsync(e => e.Id == id);
            var dto = entity.Adapt<TxSensorsDataviewDto007>();
            // TODO 填充关联表数据
            return dto;
        }

        /// <summary>
        /// 私有方法，生成 Redis 键
        /// </summary>
        /// <param name="configKey"></param>
        /// <returns></returns>
        private static string GetCacheKey(string configKey)
        {
            return CacheConstants.TX_DataView_Info + configKey;
        }

    }
}