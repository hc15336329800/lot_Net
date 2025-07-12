using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using RuoYi.Iot.Services;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Iot.Controllers;

namespace RuoYi.Mqtt.Services
{
    /// <summary>
    /// MQTT 服务实现，支持连接、订阅、发布功能，并适配 MQTTnet 4.3.7.1207
    /// </summary>
    public class MqttService : IMqttService
    {
        private readonly ILogger<MqttService> _logger; // 日志记录器
        private readonly IMqttClient _mqttClient; // MQTT 客户端实例
        private readonly ConcurrentDictionary<string,Action<string,string>> _topicHandlers; // 主题处理器字典
        private readonly object _connectionLock = new object(); // 连接锁，确保线程安全
        private bool _isConnected; // 当前连接状态,私有字段存储实际的连接状态
        public bool IsConnected => _isConnected; // 只读的公共属性,IsConnected

        private readonly IotDeviceVariableService _deviceVariableService;


        /// <summary>
        /// 全局消息接收事件，用于分发订阅到的消息
        /// </summary>
        public event Action<string,string> OnMessageReceived;

        /// <summary>
        /// 构造函数，初始化 MQTT 客户端和相关配置
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public MqttService(ILogger<MqttService> logger,IotDeviceVariableService deviceVariableService)
        {
            _logger = logger;
            _deviceVariableService = deviceVariableService;
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();
            _topicHandlers = new ConcurrentDictionary<string,Action<string,string>>();

            ConfigureMqttClientCallbacks();
        }

        /// <summary>
        /// 配置 MQTT 客户端的事件回调
        /// </summary>
        private void ConfigureMqttClientCallbacks( )
        {
            // 设置连接成功事件回调
            _mqttClient.ConnectedAsync += async e =>
            {
                _isConnected = true;
                _logger.LogInformation("成功连接到 MQTT 服务器");
            };

            // 设置断开连接事件回调
            _mqttClient.DisconnectedAsync += async e =>
            {
                _isConnected = false;
                _logger.LogWarning("与 MQTT 服务器断开连接，尝试重新连接...");
                await Task.Delay(TimeSpan.FromSeconds(5));
                await ReconnectAsync();
            };

            // 设置消息接收事件回调
            _mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                var topic = e.ApplicationMessage.Topic; // 获取消息主题
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload); // 解码消息内容

                _logger.LogInformation($"收到主题 {topic} 的消息: {payload}");

                // 调用全局事件
                OnMessageReceived?.Invoke(topic,payload);

                await HandlePayloadAsync(payload);


                // 调用主题对应的处理器
                if(_topicHandlers.TryGetValue(topic,out var handler))
                {
                    handler?.Invoke(topic,payload);
                }
            };
        }

        /// <summary>
        /// 连接到 MQTT 服务器
        /// </summary>
        public async Task ConnectToMqttServerAsync(string brokerHost,int brokerPort,string username = null,string password = null,string clientId = null)
        {
            lock(_connectionLock)
            {
                if(_isConnected)
                {
                    _logger.LogInformation("MQTT 客户端已连接，跳过重复连接操作。");
                    return;
                }
            }

            // 如果未传入 clientId，则生成一个默认的客户端 ID
            clientId ??= $"Client_{Guid.NewGuid()}"; // 动态生成客户端 ID

            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(brokerHost,brokerPort)
                .WithClientId(clientId) // 设置客户端 ID
                .WithCleanSession();

            if(!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                optionsBuilder.WithCredentials(username,password);
            }

            var options = optionsBuilder.Build();

            try
            {
                await _mqttClient.ConnectAsync(options);
                _logger.LogInformation($"连接到 MQTT 服务器成功，客户端 ID：{clientId}");
            }
            catch(Exception ex)
            {
                _logger.LogError($"连接到 MQTT 服务器失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 尝试重新连接 MQTT 服务器
        /// </summary>
        private async Task ReconnectAsync( )
        {
            try
            {
                var reconnectOptions = _mqttClient.Options; // 使用原始连接配置
                await _mqttClient.ConnectAsync(reconnectOptions);
                _logger.LogInformation("重新连接到 MQTT 服务器成功");
            }
            catch(Exception ex)
            {
                _logger.LogError($"重新连接到 MQTT 服务器失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 订阅主题并注册消息处理回调
        /// </summary>
        public async Task SubscribeToTopicAsync(string topic,Action<string,string> messageHandler)
        {
            ValidateConnection();

            if(_topicHandlers.ContainsKey(topic))
            {
                _logger.LogWarning($"主题 {topic} 已经被订阅，跳过重复订阅。");
                return;
            }

            _topicHandlers[topic] = messageHandler; // 注册处理回调

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(topic))
                .Build();

            try
            {
                await _mqttClient.SubscribeAsync(subscribeOptions);
                _logger.LogInformation($"成功订阅主题: {topic}");
            }
            catch(Exception ex)
            {
                _logger.LogError($"订阅主题 {topic} 失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 发布消息到指定主题
        /// </summary>
        public async Task PublishMessageAsync(string topic,string message)
        {
            ValidateConnection();

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            try
            {
                await _mqttClient.PublishAsync(applicationMessage);
                _logger.LogInformation($"成功发布消息到主题 {topic}: {message}");
            }
            catch(Exception ex)
            {
                _logger.LogError($"发布消息失败: {ex.Message}");
                throw;
            }
        }

        private async Task HandlePayloadAsync(string payload)
        {
            try
            {
                var data = JsonSerializer.Deserialize<DevicePayload>(payload);
                if(data != null && data.DeviceId > 0 && data.Values != null)
                {
                    var map = await _deviceVariableService.GetVariableMapAsync(data.DeviceId);
                    foreach(var kv in data.Values)
                    {
                        if(map.TryGetValue(kv.Key,out var variable) && variable.VariableId.HasValue)
                        {
                            await _deviceVariableService.SaveValueAsync(data.DeviceId,variable.VariableId.Value,kv.Key,kv.Value);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogDebug(ex,"Failed to parse device payload");
            }
        }


        /// <summary>
        /// 验证客户端连接状态
        /// </summary>
        private void ValidateConnection( )
        {
            if(!_isConnected)
            {
                _logger.LogError("MQTT 客户端未连接，无法执行操作");
                throw new InvalidOperationException("MQTT 客户端未连接");
            }
        }
    }
}
