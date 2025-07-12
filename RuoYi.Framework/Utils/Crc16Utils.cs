using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Framework.Utils
{
    public static class Crc16Utils
    {
        /// <summary>
        /// 计算 Modbus CRC16 校验码
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="offset">偏移</param>
        /// <param name="length">长度</param>
        /// <returns>CRC 值</returns>
        public static ushort ComputeModbus(ReadOnlySpan<byte> data)
        {
            ushort crc = 0xFFFF;
            foreach(var b in data)
            {
                crc ^= b;
                for(int i = 0; i < 8; i++)
                {
                    bool lsb = (crc & 0x0001) != 0;
                    crc >>= 1;
                    if(lsb) crc ^= 0xA001;
                }
            }

            return crc;
        }

        /// <summary>
        /// 计算 Modbus CRC16 校验码
        /// </summary>
        /// <param name="data">数据数组</param>
        /// <param name="offset">起始偏移</param>
        /// <param name="length">计算长度</param>
        /// <returns>CRC 值</returns>
        public static ushort ComputeModbus(byte[] data,int offset,int length)
        {
            return ComputeModbus(data.AsSpan(offset,length));
        }

        /// <summary>
        /// 计算 Modbus CRC16 校验字节
        /// </summary>
        /// <param name="data">数据</param>
        /// <returns>低字节在前，高字节在后</returns>
        public static byte[] ComputeModbusBytes(byte[] data)
        {
            ushort crc = ComputeModbus(data,0,data.Length);
            return new byte[] { (byte)(crc & 0xFF),(byte)((crc >> 8) & 0xFF) };
        }
    }
}
