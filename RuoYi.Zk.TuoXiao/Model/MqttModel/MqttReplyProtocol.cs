using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;



namespace RuoYi.Zk.TuoXiao.Model.MqttModel
{


    public class MqttReplyWrapper
    {
        [JsonPropertyName("rw_prot")]
        public MqttReplyProtocol RwProt { get; set; }
    }


    /// <summary>
    /// 回复数据结构 ReplyProtocol
    /// </summary>
    public class MqttReplyProtocol
    {
        [JsonPropertyName("Ver")]
        public string Ver { get; set; } // 协议版本

        [JsonPropertyName("dir")]
        public string Dir { get; set; } // 数据方向

        [JsonPropertyName("id")]
        public string Id { get; set; } // 唯一 ID

        [JsonPropertyName("w_data")]
        public List<MqttReplyData> WData { get; set; } = new(); // 写入数据
    }


    /// <summary>
    /// 回复数据结构 ReplyProtocol
    /// </summary>
    public class MqttReplyData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } // 点位名称

        [JsonPropertyName("value")]
        public string Value { get; set; } // 写入/读取值

        [JsonPropertyName("err")]
        public string Err { get; set; } // 错误码
    }


 




}
