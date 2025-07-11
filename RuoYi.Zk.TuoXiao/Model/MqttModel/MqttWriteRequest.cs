using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static RuoYi.Zk.TuoXiao.Controllers.DataViewController;

/// <summary>
/// 请求数据结构 WriteRequest
/// </summary>
namespace RuoYi.Zk.TuoXiao.Model.MqttModel
{

    /// <summary>
    /// 请求数据结构 WriteRequest
    /// </summary>
    public class MqttWriteRequest
    {
        public string? Topic { get; set; } // 目标主题
        public List<MqttWriteData> WData { get; set; } = new List<MqttWriteData>();
    }


    /// <summary>
    /// 请求数据结构 WriteRequest
    /// </summary>
    public class MqttWriteData
    {
        [JsonPropertyName("name")] //指定序列化时的 JSON 字段名为小写
        public string Name { get; set; } // 点位名称

        [JsonPropertyName("value")] //指定序列化时的 JSON 字段名为小写
        public string Value { get; set; } // 写入的值
    }

}
