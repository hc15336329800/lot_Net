using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Mqtt.Tools
{
    public class CRC16Modbus
    {

        // CRC16 校验算法
        public static  byte[] CalculateCRC16(byte[] data)
        {
            ushort crc = 0xFFFF;

            foreach (byte b in data)
            {
                crc ^= b;
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 1) != 0)
                    {
                        crc = (ushort)((crc >> 1) ^ 0xA001);
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }

            return new byte[] { (byte)(crc & 0xFF), (byte)((crc >> 8) & 0xFF) }; // 低字节在前，高字节在后
        }
    }
}
