using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Mqtt.Tools
{

    /// <summary>
    /// 处理回传的16进制modbus报文数据
    /// </summary>
    public class HexString
    {



        /// <summary>
        /// 将十六进制字符串转换为字节数组
        /// </summary>
        public static byte[] HexStringToByteArray(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }




        /// <summary>
        /// 返回帧：    01 03 36 421C0000 42190000 42120000 41AC0000 42AA0000 3DF5C28F 42CA6666 42CB3333 43FA0000 CRC_L CRC_H
        /// 处理返回帧，只提取并转换 01 03 36 之后的十六进制浮点数：（从第四个字节开始读取，逐步解析每 4 字节的数据）
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
       


        // 每4字节的十六进制字符串转换为浮点数
        public static float HexToFloat(string hex)
        {
            // 将每4字节的十六进制字符串转换为浮点数
            byte[] bytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                bytes[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber);
            }
            Array.Reverse(bytes); // 确保字节顺序符合IEEE754标准
            return BitConverter.ToSingle(bytes, 0);
        }

    }
}
