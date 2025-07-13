using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RuoYi.Common.Utils
{
    /// <summary>
    /// Modbus 帧与 CRC 计算工具类
    /// </summary>
    public static class ModbusUtils
    {
        /// <summary>
        /// 构建 Modbus 读寄存器帧，同时可记录起始寄存器地址。
        /// </summary>
        public static byte[] BuildReadFrame(byte slave,byte func,ushort startAddress,ushort quantity,ConcurrentDictionary<byte,ushort>? lastReadStartAddrs = null)
        {
            var list = new List<byte>
            {
                slave,
                func,
                (byte)(startAddress >> 8),
                (byte)(startAddress & 0xFF),
                (byte)(quantity >> 8),
                (byte)(quantity & 0xFF)
            };

            if(lastReadStartAddrs != null)
            {
                lastReadStartAddrs[slave] = startAddress;
            }

            ushort crc = ComputeCrc(list.ToArray());
            list.Add((byte)(crc & 0xFF));
            list.Add((byte)(crc >> 8));
            return list.ToArray();
        }

        /// <summary>
        /// 计算 Modbus CRC 校验
        /// </summary>
        public static ushort ComputeCrc(byte[] data)
        {
            ushort crc = 0xFFFF;
            foreach(var b in data)
            {
                crc ^= b;
                for(int i = 0; i < 8; i++)
                {
                    crc = (ushort)((crc & 1) != 0 ? (crc >> 1) ^ 0xA001 : crc >> 1);
                }
            }
            return crc;
        }
    }
}