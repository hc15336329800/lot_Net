using RuoYi.Data.Entities;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Mqtt.Model
{
    [SugarTable("mqtt_message_log")] // 指定数据库表名 ,注意大小写一致，不一致就需要映射！
    public class MqttMessageLog : BaseEntity
    {
        [SugarColumn(ColumnName = "id")] // 映射数据库中的 id 列
        public int Id { get; set; }

        [SugarColumn(ColumnName = "topic")] // 映射数据库中的 topic 列
        public string Topic { get; set; }

        [SugarColumn(ColumnName = "message")] // 映射数据库中的 message 列
        public string Message { get; set; }

        [SugarColumn(ColumnName = "receivedTime")] // 映射数据库中的 receivedTime 列
        public DateTime ReceivedTime { get; set; }
    }
}
