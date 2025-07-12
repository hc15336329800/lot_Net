using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// 这个类的主要职责是监听以及分发调用！

// todo:  主要实现tcp的通讯连接管理， 根据首次握手时收到的注册包则去设备表查询（auto_reg_packet=注册包）然后根据结果再去查询产品表（设备的product_id=产品id），
// 然后在产品表查询其接入协议access_protocol类型 （1 表示 TCP，2 表示 MQTT，3 表示 HTTPS）
// 然后去查询数据协议data_protocol（1 表示 Modbus RTU，2 表示 Modbus TCP，6 表示 JSON，7 表示数据透传） 当access_protocol =1  和 data_protocol =1 时候去调用ModbusRtuService，
// 当access_protocol =1  和 data_protocol =2 时候去调用ModbusTcpService，
namespace RuoYi.Tcp.Services
{
    public class TcpService
    {
    }
}
