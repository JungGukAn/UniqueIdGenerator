using System;
namespace Jung.Utils
{
    public class TimeChangeableIdGenerator: UniqueIdGenerator
    {
        public DateTime ChangeableTime { get; set; }
        public readonly new DateTime BaseCalculateTime;

        public TimeChangeableIdGenerator(int generatorId): base(generatorId)
        {
            ChangeableTime = DateTime.UtcNow;
            BaseCalculateTime = UniqueIdGenerator.BaseCalculateTime;
        }

        protected override DateTime GetCurrentTime()
        {
            return ChangeableTime;
        }
    }
}

