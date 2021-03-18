# ShortSnowFlake 
![MIT](https://img.shields.io/github/license/yrz1994/ShortSnowFlake)
[![Nuget](https://img.shields.io/nuget/v/ShortSnowFlake)](https://www.nuget.org/packages/ShortSnowFlake)

```C#
    /**
     * 调整雪花算法位数分布--牺牲单位时间吞吐量换取较短的ID长度（15位）
     * 
     * 分布调整方案：
     * 1.时间戳位由41位的毫秒级时间戳调整为32位秒级时间戳
     * 2.10位WokerID调整为2位，由最多支持1024台机器分布式部署改为最多支持4台机器分布式部署
     * 3.自增序列调整为14位，每秒支持生成16,384个ID
     * 
     * 影响：
     * 调整后ID生成理论上由原先的1024*4096=4,194,304（个/毫秒）缩减至4*16384=65,536(个/秒)
     * 
     * |--------|--------|--------|--------|--------|--------|--------|--------|    64bit
     * |--------|--------|********|********|********|********|--------|--------|    32bit 秒级时间戳
     * |--------|--------|--------|--------|--------|--------|**------|--------|    2bit  机器位，支持4台机器部署
     * |--------|--------|--------|--------|--------|--------|--******|********|    14bit 自增序列
     *
     * 由于机器位与自增序列占用16位，所以时间戳将左移16位（相当于乘以2的16次方），据此还可以根据时间戳反向计算ID生成范围
     **/
    public class ShortIdWorker
    {
        public const long Twepoch = 0L;

        public const int WorkerIdBits = 2;
        public const int SequenceBits = 14;

        public const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
        public const long SequenceMask = -1L ^ (-1L << SequenceBits);

        public const int WorkerIdShift = SequenceBits;
        public const int TimestampLeftShift = SequenceBits + WorkerIdBits;

        private long _sequence = 0L;
        private long _lastTimestamp = -1L;

        private readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public long WorkerId { get; private set; }
        public long Sequence
        {
            get { return _sequence; }
        }

        public ShortIdWorker(long workerId)
        {
            if (workerId > MaxWorkerId || workerId < 0)
            {
                throw new ArgumentException(string.Format("workerId can't be less than 0 and can't be greater than {0}", MaxWorkerId));
            }
            WorkerId = workerId;
        }

        readonly object _lock = new Object();

        public long NextId()
        {
            lock (_lock)
            {
                var timestamp = TimeGen();
                if (timestamp < _lastTimestamp)
                {
                    throw new Exception($"Clock is moving backwards. Refusing to generate id for {_lastTimestamp - timestamp} seconds");
                }
                if (_lastTimestamp == timestamp)
                {
                    _sequence = (_sequence + 1) & SequenceMask;
                    if (_sequence == 0)
                    {
                        timestamp = TilNextMillis(_lastTimestamp);
                    }
                }
                else
                {
                    _sequence = 0;
                }
                _lastTimestamp = timestamp;
                return ((timestamp - Twepoch) << TimestampLeftShift) | (WorkerId << WorkerIdShift) | _sequence;
            }
        }

        public long TranslateTimespan(long timespan)
        {
            return ((timespan - Twepoch) << TimestampLeftShift);
        }

        private long TilNextMillis(long lastTimestamp)
        {
            var timestamp = TimeGen();
            while (timestamp <= lastTimestamp)
            {
                timestamp = TimeGen();
            }
            return timestamp;
        }

        private long TimeGen()
        {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalSeconds;
        }
    }
```
