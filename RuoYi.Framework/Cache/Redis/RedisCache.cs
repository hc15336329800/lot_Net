using Microsoft.Extensions.Caching.Distributed;
using RuoYi.Framework.Cache.Options;
using RuoYi.Framework.JsonSerialization;
using RuoYi.Framework.Logging;
using RuoYi.Framework.Utils;
using StackExchange.Redis;
using System.Text;

namespace RuoYi.Framework.Cache.Redis
{
    /// <summary>
    /// ICache接口的radis实现
    /// </summary>
    public class RedisCache : ICache
    {
        private static readonly DistributedCacheEntryOptions DefaultOptions = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(36500));

        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly IDatabase _database;
        private readonly IServer _server;
        private readonly RedisConfig _redisConfig;

        /// <summary>
        /// 构造函数，初始化RedisCache实例
        /// </summary>
        /// <param name="cache">分布式缓存接口</param>
        /// <param name="multiplexer">Redis连接多路复用器</param>
        public RedisCache(IDistributedCache cache,IConnectionMultiplexer multiplexer)
        {
            _cache = cache;
            _multiplexer = multiplexer;
            _database = _multiplexer.GetDatabase();
            _server = _multiplexer.GetServer(_multiplexer.GetEndPoints()[0]);

            _redisConfig = App.GetConfig<RedisConfig>("CacheConfig:RedisConfig");
        }

        #region String
        /// <summary>
        /// 获取字符串值
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>字符串值</returns>
        public string? GetString(string key)
        {
            return _cache.GetString(key);
        }

        /// <summary>
        /// 设置字符串值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public void SetString(string key,string value)
        {
            _cache.SetString(key,value,DefaultOptions);
        }

        /// <summary>
        /// 设置字符串值并指定过期时间
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="minutes">过期时间（分钟）</param>
        public void SetString(string key,string value,long minutes)
        {
            var options = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(minutes));
            _cache.SetString(key,value,options);
        }

        /// <summary>
        /// 移除指定键的值
        /// </summary>
        /// <param name="key">键</param>
        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        /// <summary>
        /// 异步获取字符串值
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>字符串值</returns>
        public async Task<string?> GetStringAsync(string key)
        {
            return await _cache.GetStringAsync(key,default);
        }

        /// <summary>
        /// 异步设置字符串值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public async Task SetStringAsync(string key,string value)
        {
            await _cache.SetStringAsync(key,value,DefaultOptions,default);
        }

        /// <summary>
        /// 异步设置字符串值并指定过期时间
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="minutes">过期时间（分钟）</param>
        public async Task SetStringAsync(string key,string value,long minutes)
        {
            var options = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(minutes));
            await _cache.SetStringAsync(key,value,options,default);
        }

        /// <summary>
        /// 异步移除指定键的值
        /// </summary>
        /// <param name="key">键</param>
        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }
        #endregion

        #region DB(IConnectionMultiplexer)

        /// <summary>
        /// 获取数据库中匹配指定模式的键
        /// </summary>
        /// <param name="pattern">模式</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>匹配的键集合</returns>
        public IEnumerable<string> GetDbKeys(string pattern,int pageSize = 1000)
        {
            var keys = GetKeys(pattern,pageSize);
            return ToStringKeys(keys);
        }

        /// <summary>
        /// 按名字删除缓存
        /// </summary>
        /// <param name="cacheName">缓存名称</param>
        public void RemoveByPattern(string cacheName)
        {
            var redisKeys = GetKeys(cacheName);
            Remove(redisKeys);
        }

        /// <summary>
        /// 异步获取数据库信息
        /// </summary>
        /// <param name="args">参数</param>
        /// <returns>数据库信息字典</returns>
        public async Task<Dictionary<string,string>> GetDbInfoAsync(params object[] args)
        {
            var info = (await ExecuteAsync("INFO",args)).ToString();

            return string.IsNullOrEmpty(info)
                ? new Dictionary<string,string>()
                : ParseInfo(info);
        }

        /// <summary>
        /// 当前db中 key数量
        /// </summary>
        /// <returns>数据库大小</returns>
        public async Task<long> GetDbSize( )
        {
            var dbsize = (await ExecuteAsync("dbsize")).ToString();
            return Convert.ToInt64(dbsize);
        }

        #region private

        /// <summary>
        /// 获取匹配指定模式的键
        /// </summary>
        /// <param name="pattern">模式</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>匹配的键集合</returns>
        private IEnumerable<RedisKey> GetKeys(string pattern,int pageSize = 1000)
        {
            if(!pattern.StartsWith('*'))
            {
                pattern = _redisConfig.InstanceName + pattern;
            }
            return _server.Keys(pattern: pattern,pageSize: pageSize);
        }

        /// <summary>
        /// 将RedisKey集合转换为字符串集合
        /// </summary>
        /// <param name="keys">RedisKey集合</param>
        /// <returns>字符串集合</returns>
        private IEnumerable<string> ToStringKeys(IEnumerable<RedisKey> keys)
        {
            return keys.Select(k =>
            {
                var key = k.ToString();
                // 去掉开头的 ruoyi_net:
                return key.StartsWith(_redisConfig.InstanceName) ? StringUtils.StripStart(key,_redisConfig.InstanceName) : key;
            });
        }

        /// <summary>
        /// 移除指定的键集合
        /// </summary>
        /// <param name="keys">键集合</param>
        /// <returns>移除的键数量</returns>
        private long Remove(IEnumerable<RedisKey> keys)
        {
            return _database.KeyDelete(keys.ToArray());
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="command">命令</param>
        /// <param name="args">参数</param>
        /// <returns>命令执行结果</returns>
        private async Task<RedisResult> ExecuteAsync(string command,params object[] args)
        {
            return await _database.ExecuteAsync(command,args);
        }

        /// <summary>
        /// 解析Redis INFO命令的结果
        /// </summary>
        /// <param name="info">INFO命令的结果</param>
        /// <returns>解析后的信息字典</returns>
        private Dictionary<string,string> ParseInfo(string info)
        {
            // 调用ParseCategorizedInfo以减少重复代码
            var data = ParseCategorizedInfo(info);

            // 返回Info Key和Info value的字典
            var result = new Dictionary<string,string>();

            for(var i = 0; i < data.Length; i++)
            {
                var x = data[i];

                result.Add(x.Key,x.InfoValue);
            }

            return result;
        }

        /// <summary>
        /// 解析分类信息
        /// </summary>
        /// <param name="info">分类信息字符串</param>
        /// <returns>解析后的信息详情数组</returns>
        private InfoDetail[] ParseCategorizedInfo(string info)
        {
            var data = new List<InfoDetail>();
            var category = string.Empty;

            var lines = info.Split(new[] { Environment.NewLine },StringSplitOptions.RemoveEmptyEntries);

            foreach(var line in lines.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if(line[0] == '#')
                {
                    category = line.Replace("#",string.Empty).Trim();
                    continue;
                }

                var idx = line.IndexOf(':');

                if(idx > 0)
                {
                    var key = line.Substring(0,idx);
                    var infoValue = line.Substring(idx + 1).Trim();

                    data.Add(new(category,key,infoValue));
                }
            }

            return data.ToArray();
        }
        #endregion

        #endregion

        #region 泛型
        /// <summary>
        /// 获取指定类型的值
        /// </summary>
        /// <typeparam name="T">值的类型</typeparam>
        /// <param name="key">键</param>
        /// <returns>值</returns>
        public T Get<T>(string key)
        {
            try
            {
                var valueString = _cache.GetString(key);

                if(string.IsNullOrEmpty(valueString))
                {
                    return default!;
                }
                return JSON.Deserialize<T>(valueString);
            }
            catch(Exception e)
            {
                Log.Error("RedisCache Get error",e);
                return default!;
            }
        }

        /// <summary>
        /// 设置指定类型的值
        /// </summary>
        /// <typeparam name="T">值的类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public void Set<T>(string key,T value)
        {
            _cache.SetString(key,JSON.Serialize(value!),DefaultOptions);
        }

        /// <summary>
        /// 设置指定类型的值并指定过期时间
        /// </summary>
        /// <typeparam name="T">值的类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="minutes">过期时间（分钟）</param>
        public void Set<T>(string key,T value,long minutes)
        {
            var options = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(minutes));
            _cache.SetString(key,JSON.Serialize(value!),options);
        }

        /// <summary>
        /// 异步获取指定类型的值
        /// </summary>
        /// <typeparam name="T">值的类型</typeparam>
        /// <param name="key">键</param>
        /// <returns>值</returns>
        public async Task<T> GetAsync<T>(string key)
        {
            try
            {
                var valueString = await _cache.GetStringAsync(key,default);
                if(string.IsNullOrEmpty(valueString))
                {
                    return default!;
                }

                return JSON.Deserialize<T>(valueString);
            }
            catch(Exception e)
            {
                Log.Error("RedisCache GetAsync error",e);
                return default!;
            }
        }

        /// <summary>
        /// 异步设置指定类型的值
        /// </summary>
        /// <typeparam name="T">值的类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public async Task SetAsync<T>(string key,T value)
        {
            await _cache.SetStringAsync(key,JSON.Serialize(value!),DefaultOptions,default);
        }

        /// <summary>
        /// 异步设置指定类型的值并指定过期时间
        /// </summary>
        /// <typeparam name="T">值的类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="minutes">过期时间（分钟）</param>
        public async Task SetAsync<T>(string key,T value,long minutes)
        {
            var options = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(minutes));
            await _cache.SetStringAsync(key,JSON.Serialize(value!),options,default);
        }

        /// <summary>
        /// 获取字符串值后移除
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>字符串值</returns>
        public string? GetStringAndRemove(string key)
        {
            var value = _cache.GetString(key);
            if(value != null)
            {
                _cache.Remove(key);
            }
            return value;
        }
    }
    #endregion
}
