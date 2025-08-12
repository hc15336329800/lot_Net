using RuoYi.Framework.Attributes;
using StackExchange.Redis;
using System.Text.Json;
using RuoYi.Mqtt.Model;

[Task("RyTaskMqttMes")]
public class RyTaskMqttMes
{
    // 无参构造函数，保证反射可以正常实例化
    //public RyTaskMqttMes()
    //{
    //    // 通过日志记录，确认无参构造函数被调用
    //    RuoYi.Framework.Logging.Log.Information("通过无参构造函数创建 RyTaskMqttMes 实例");
    //}


    // 处理 Redis 消息并批量插入数据库的方法,一直循环
    public async Task RyNoParams()
    {
        // 从 DI 容器获取 logger 和 redis 实例
        var logger = App.GetService<ILogger<RyTaskMqttMes>>();
        var redis = App.GetService<IConnectionMultiplexer>();
        var db = redis.GetDatabase();

        logger.LogInformation("定时任务开始批量处理 Redis 消息");

        var cacheKey = "mqtt_messages_list"; // Redis List 的键
        var batchSize = 100; // 每次处理的批量大小
        var messages = new List<MqttMessageLog>();

        try
        {
            // 批量从 Redis 列表中弹出消息
            for (int i = 0; i < batchSize; i++)
            {
                var cacheEntry = db.ListRightPop(cacheKey); // 从 Redis 列表右侧弹出
                if (cacheEntry.IsNullOrEmpty)
                {
                    // 如果 Redis 中没有消息，直接退出循环
                    //logger.LogInformation("Redis 列表中没有新的消息需要处理，任务退出。");
                    break;
                }

                // 反序列化消息
                var mqttMessage = JsonSerializer.Deserialize<MqttMessageLog>(cacheEntry);
                if (mqttMessage != null)
                {
                    messages.Add(mqttMessage);
                }
            }

            // 如果消息列表为空，直接退出
            if (messages.Count == 0)
            {
                logger.LogInformation("没有消息数据，任务退出。");
                // 千万不要使用返回，不然定时器会停止！！！！！！！！！具体我也不明白
                // return  
                
            }
            else
            {
                // 从 DI 容器获取 SqlSugarClient 实例
                var sqlSugarClient = App.GetService<ISqlSugarClient>();

                // 手动实例化 SqlSugarRepository
                var repository = new SqlSugarRepository<MqttMessageLog>(sqlSugarClient);

                // 批量插入数据库
                await repository.InsertAsync(messages);  // 插入单个或多个实体，支持异步操作。
                logger.LogInformation($"批量插入 {messages.Count} 条消息到 MySQL");

            }

            
        }
        catch (Exception ex)
        {
            logger.LogError($"批量处理消息时发生错误: {ex.Message}", ex);
        }
    }
}
