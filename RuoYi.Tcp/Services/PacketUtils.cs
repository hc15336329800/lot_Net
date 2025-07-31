using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Tcp.Services
{
    public static class PacketUtils
    {
        /// <summary>
        /// Calculate a simple checksum byte as the sum of all bytes modulo 256.
        /// </summary>
        public static byte CalculateChecksum(ReadOnlySpan<byte> data)
        {
            int sum = 0;
            foreach(var b in data)
                sum += b;
            return (byte)(sum & 0xFF);
        }
    }
}
