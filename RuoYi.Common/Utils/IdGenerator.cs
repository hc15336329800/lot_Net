using System;

namespace RuoYi.Common.Utils
{

    /// <summary>
    /// 简单版雪花ID生成器（Snowflake-like），用于生成全局唯一的长整型ID。
    /// 支持高并发下毫秒级自增，适合分布式环境。
    /// 16-19 位
    /// </summary>
    public static class IdGenerator
    {
        // 线程锁，保证并发下ID生成安全
        private static readonly object _lock = new();

        // 记录上一次生成ID时的时间戳（毫秒）
        private static long _lastTimestamp;

        // 同一毫秒内的自增序号
        private static long _sequence;

        // 序号位数（12位，可支持每毫秒最多生成4096个ID）
        private const int SequenceBits = 12;

        // 序号掩码（4095，确保序号不会溢出12位）
        private const long SequenceMask = -1L ^ (-1L << SequenceBits);

        /// <summary>
        /// 生成新的全局唯一ID
        /// </summary>
        /// <returns>返回全局唯一的long型ID（毫秒时间戳左移+序号）</returns>
        public static long NewId( )
        {
            lock(_lock) // 保证多线程下ID生成不重复
            {
                // 获取当前时间戳（毫秒）
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                if(timestamp == _lastTimestamp)
                {
                    // 如果和上次一样（同一毫秒），序号自增
                    _sequence = (_sequence + 1) & SequenceMask;
                    // 如果同一毫秒内生成数量超过4096，则等到下一个毫秒
                    if(_sequence == 0)
                    {
                        while(timestamp <= _lastTimestamp)
                        {
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                    }
                }
                else
                {
                    // 跨毫秒，序号重置为0
                    _sequence = 0;
                }

                // 更新最近一次的时间戳
                _lastTimestamp = timestamp;

                // 返回：毫秒时间戳左移12位 + 序号（确保全局唯一且递增）
                return (timestamp << SequenceBits) | _sequence;
            }
        }
    }
}