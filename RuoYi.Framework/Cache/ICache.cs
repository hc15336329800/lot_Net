namespace RuoYi.Framework.Cache;

public interface ICache
{
    #region String
    /// <summary>
    /// 获取字符串值
    /// </summary>
    /// <param name="key">键</param>
    /// <returns>字符串值</returns>
    string? GetString(string key);


    /// <summary>
    /// 获取字符串值后移除
    /// </summary>
    /// <param name="key">键</param>
    /// <returns>字符串值</returns>
    string? GetStringAndRemove(string key);

    /// <summary>
    /// 设置字符串值
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    void SetString(string key,string value);

    /// <summary>
    /// 设置字符串值并指定过期时间
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="minutes">过期时间（分钟）</param>
    void SetString(string key,string value,long minutes);

    /// <summary>
    /// 移除指定键的值
    /// </summary>
    /// <param name="key">键</param>
    void Remove(string key);




    /// <summary>
    /// 异步获取字符串值
    /// </summary>
    /// <param name="key">键</param>
    /// <returns>字符串值</returns>
    Task<string?> GetStringAsync(string key);

    /// <summary>
    /// 异步设置字符串值
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    Task SetStringAsync(string key,string value);

    /// <summary>
    /// 异步设置字符串值并指定过期时间
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="minutes">过期时间（分钟）</param>
    Task SetStringAsync(string key,string value,long minutes);

    /// <summary>
    /// 异步移除指定键的值
    /// </summary>
    /// <param name="key">键</param>
    Task RemoveAsync(string key);
    #endregion

    #region DB
    /// <summary>
    /// 获取数据库中匹配指定模式的键
    /// </summary>
    /// <param name="pattern">模式</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns>匹配的键集合</returns>
    IEnumerable<string> GetDbKeys(string pattern,int pageSize = 1000);

    /// <summary>
    /// 异步获取数据库信息
    /// </summary>
    /// <param name="args">参数</param>
    /// <returns>数据库信息字典</returns>
    Task<Dictionary<string,string>> GetDbInfoAsync(params object[] args);

    /// <summary>
    /// 异步获取数据库大小
    /// </summary>
    /// <returns>数据库大小</returns>
    Task<long> GetDbSize( );

    /// <summary>
    /// 移除匹配指定模式的键
    /// </summary>
    /// <param name="pattern">模式</param>
    void RemoveByPattern(string pattern);
    #endregion

    #region 泛型
    /// <summary>
    /// 获取指定类型的值
    /// </summary>
    /// <typeparam name="T">值的类型</typeparam>
    /// <param name="key">键</param>
    /// <returns>值</returns>
    T Get<T>(string key);

    /// <summary>
    /// 设置指定类型的值
    /// </summary>
    /// <typeparam name="T">值的类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    void Set<T>(string key,T value);

    /// <summary>
    /// 设置指定类型的值并指定过期时间
    /// </summary>
    /// <typeparam name="T">值的类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="minutes">过期时间（分钟）</param>
    void Set<T>(string key,T value,long minutes);

    /// <summary>
    /// 异步获取指定类型的值
    /// </summary>
    /// <typeparam name="T">值的类型</typeparam>
    /// <param name="key">键</param>
    /// <returns>值</returns>
    Task<T> GetAsync<T>(string key);

    /// <summary>
    /// 异步设置指定类型的值
    /// </summary>
    /// <typeparam name="T">值的类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    Task SetAsync<T>(string key,T value);

    /// <summary>
    /// 异步设置指定类型的值并指定过期时间
    /// </summary>
    /// <typeparam name="T">值的类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="minutes">过期时间（分钟）</param>
    Task SetAsync<T>(string key,T value,long minutes);
    #endregion
}
