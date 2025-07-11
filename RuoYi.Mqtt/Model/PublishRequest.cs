using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Mqtt.Model
{
    /// <summary>
    /// 发布请求模型
    /// </summary>
    public class PublishRequest
    {
        /// <summary>
        /// 发布的主题
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// 发布的消息内容
        /// </summary>
        public string Message { get; set; }
    }
}
