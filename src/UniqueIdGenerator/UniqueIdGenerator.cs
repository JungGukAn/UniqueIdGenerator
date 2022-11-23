using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jung.Utils
{
    public class UniqueIdGenerator
    {
        public struct UniqueId
        {
            public readonly long Value;

            internal UniqueId(long value)
            {
                Value = value;
            }

            public DateTime CreatedAt
            {
                get
                {
                    return BaseCalculateTime.AddSeconds(ExtractIssueSecondsFrom(Value));
                }
            }

            public static implicit operator long(UniqueId uniqueId) => uniqueId.Value;
        }

        private const int GeneratorIdBitCount = 11;
        private const int SequenceBitCount = 20;
        private const int GeneratorIdAndSequenceBitCount = GeneratorIdBitCount + SequenceBitCount;

        public static readonly int MaxGeneratorId = (int)Math.Pow(2, GeneratorIdBitCount);
        public static readonly int MaxSequencePerSeconds = (int)Math.Pow(2, SequenceBitCount);

        protected static readonly DateTime BaseCalculateTime = DateTime.SpecifyKind(new DateTime(2022, 01, 01), DateTimeKind.Utc);

        private readonly int GeneratorId;

        private int _sequence = 0;
        private long _issuedSeconds = 0;

        private static List<UniqueId> CreateUniqueIds(long issueSeconds, int generatorId, int baseSequence, int count)
        {
            return Enumerable
                .Range(1, count)
                .Select(i => CreateUniqueId(issueSeconds, generatorId, baseSequence + i))
                .ToList();
        }

        private static UniqueId CreateUniqueId(long issueSeconds, int generatorId, int sequence)
        {
            var newIdValue = issueSeconds << GeneratorIdAndSequenceBitCount | (long)generatorId << SequenceBitCount | (long)sequence;
            return new UniqueId(newIdValue);
        }

        private static long ExtractIssueSecondsFrom(long uniqueIdValue)
        {
            return uniqueIdValue >> GeneratorIdAndSequenceBitCount;
        }

        private static long GetIssueSeconds(DateTime now)
        {
            var issueSeconds = (long)(now - BaseCalculateTime).TotalSeconds;
            if (issueSeconds > uint.MaxValue)
            {
                throw new InvalidOperationException("issueSeconds exceed 32bit");
            }

            return issueSeconds;
        }

        /// <summary>
        /// GeneratorId must be unique between servers.
        /// </summary>
        /// <param name="generatorId"></param>
        public UniqueIdGenerator(int generatorId)
        {
            if (generatorId <= 0)
            {
                throw new ArgumentOutOfRangeException("generatorId must be larger than zero");
            }

            if (generatorId >= MaxGeneratorId)
            {
                throw new ArgumentOutOfRangeException("generatorId must be lower than 16384");
            }

            GeneratorId = generatorId;
        }

        public UniqueId Issue()
        {
            var uniqueIds = Issue(1);
            return uniqueIds.First();
        }

        public List<UniqueId> Issue(int requestCount)
        {
            if (requestCount <= 0)
            {
                throw new ArgumentException("requestCount must be larger than zero");
            }

            int baseSequence = 0;
            long baseIssuedSeconds = 0;

            lock (this)
            {
                var now = GetCurrentTime();

                long currentIssueSeconds = GetIssueSeconds(now);
                long diffSeconds = currentIssueSeconds - _issuedSeconds;

                if (diffSeconds > 0)
                {
                    _issuedSeconds = currentIssueSeconds;
                    _sequence = 0;
                }

                if (IsIssueAble(requestCount) == false)
                {
                    throw new InvalidOperationException($"Can't create more than {MaxSequencePerSeconds - 1} ids per seconds");
                }

                baseSequence = _sequence;
                baseIssuedSeconds = _issuedSeconds;

                _sequence += requestCount;
            }

            return CreateUniqueIds(baseIssuedSeconds, GeneratorId, baseSequence, requestCount);
        }

        protected virtual DateTime GetCurrentTime()
        {
            return DateTime.UtcNow;
        }

        private bool IsIssueAble(int requestCount)
        {
            int issueAbleCount = MaxSequencePerSeconds - _sequence - 1;
            return requestCount <= issueAbleCount;
        }
    }
}

