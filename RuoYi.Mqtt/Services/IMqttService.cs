using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Mqtt.Services
{

        /// <summary>
        /// MQTT 服务接口定义
        /// </summary>
        public interface IMqttService
        {
        /// <summary>
        /// 连接到 MQTT 服务器
        /// </summary>
        /// <param name="brokerHost">MQTT Broker 主机地址</param>
        /// <param name="brokerPort">MQTT Broker 端口号</param>
        /// <param name="username">MQTT 用户名</param>
        /// <param name="password">MQTT 密码</param>
        /// <param name="clientId">MQTT 客户端 ID（可选）</param>
        Task ConnectToMqttServerAsync(string brokerHost,int brokerPort,string username = null,string password = null,string clientId = null);

        /// <summary>
        /// 订阅主题
        /// </summary>
        /// <param name="topic">要订阅的主题</param>
        /// <param name="messageHandler">消息处理回调方法</param>
        Task SubscribeToTopicAsync(string topic,Action<string,string> messageHandler);

            /// <summary>
            /// 发布消息到指定主题
            /// </summary>
            /// <param name="topic">目标主题</param>
            /// <param name="message">消息内容</param>
        Task PublishMessageAsync(string topic,string message);

            /// <summary>
            /// 全局消息接收事件，用于分发订阅到的消息
            /// </summary>
         event Action<string,string> OnMessageReceived;

        // 链接状态
        bool IsConnected { get; }

    }
    
}
