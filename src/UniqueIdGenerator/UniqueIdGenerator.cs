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

        private struct IdIssueResult
        {
            public bool IsCompleted => RemainingCount == 0;

            public readonly List<UniqueId>? Ids;
            public readonly int RemainingCount;
            public readonly long WaitSeconds;

            public IdIssueResult(List<UniqueId>? ids, int remainingCount, long waitSeconds)
            {
                Ids = ids;
                RemainingCount = remainingCount;
                WaitSeconds = waitSeconds;
            }
        }

        public const int MaxGeneratorId = 16384; // 2^14
        public const int MaxSequencePerSeconds = 131072; // 2^17
        protected static readonly DateTime BaseCalculateTime = DateTime.SpecifyKind(new DateTime(2022, 01, 01), DateTimeKind.Utc);

        private readonly int GeneratorId;

        private int _sequence = 0;
        private long _issuedSeconds = 0;

        private static UniqueId CreateUniqueId(long issueSeconds, int generatorId, int sequence)
        {
            var newIdValue = issueSeconds << 31 | (long)generatorId << 17 | (long)sequence;
            return new UniqueId(newIdValue);
        }

        private static long ExtractIssueSecondsFrom(long uniqueIdValue)
        {
            return uniqueIdValue >> 31;
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

        public async Task<UniqueId> IssueAsync()
        {
            return (await IssueAsync(1)).First();
        }

        public async Task<List<UniqueId>> IssueAsync(int requestCount)
        {
            if (requestCount <= 0)
            {
                throw new ArgumentException("requestCount must be larger than zero");
            }

            var ids = new List<UniqueId>(requestCount);

            var issueResult = IssueNewIds(requestCount);
            if (issueResult.Ids != null)
            {
                ids.AddRange(issueResult.Ids);
            }

            while (issueResult.IsCompleted == false)
            {
                await Task.Delay(TimeSpan.FromSeconds(issueResult.WaitSeconds));

                issueResult = IssueNewIds(issueResult.RemainingCount);
                if (issueResult.Ids != null)
                {
                    ids.AddRange(issueResult.Ids);
                }
            }

            return ids;
        }

        /// <summary>
        /// Time synchronization can make server time to past again. 
        /// So, This prevents above situation.
        /// </summary>
        /// <param name="requestCount"></param>
        /// <returns></returns>
        private IdIssueResult IssueNewIds(int requestCount)
        {
            lock (this)
            {
                var now = GetCurrentTime();

                long currentIssueSeconds = GetIssueSeconds(now);
                long diffSeconds = currentIssueSeconds - _issuedSeconds;

                if (diffSeconds < 0)
                {
                    return CreateIdIssueResult(requestCount, 0, -diffSeconds, currentIssueSeconds);
                }

                if (diffSeconds > 0)
                {
                    _issuedSeconds = currentIssueSeconds;
                    _sequence = 0;
                }

                int issueCount = GetIssueCount(requestCount);
                bool isCompleted = requestCount == issueCount;
                long waitSeconds = isCompleted ? 0 : 1;

                var issueResult = CreateIdIssueResult(requestCount, issueCount, waitSeconds, currentIssueSeconds);

                _sequence += issueCount;
                return issueResult;
            }
        }

        protected virtual DateTime GetCurrentTime()
        {
            return DateTime.UtcNow;
        }

        private IdIssueResult CreateIdIssueResult(int requestCount, int issueCount, long waitSeconds, long issueSeconds)
        {
            List<UniqueId>? ids = null;

            if (issueCount > 0)
            {
                ids = Enumerable
                    .Range(1, issueCount)
                    .Select(i => CreateUniqueId(issueSeconds, GeneratorId, _sequence + i))
                    .ToList();
            }

            var result = new IdIssueResult(ids, requestCount - issueCount, waitSeconds);
            return result;
        }

        private int GetIssueCount(int requestCount)
        {
            int issueAbleCount = MaxSequencePerSeconds - _sequence - 1;
            return Math.Min(issueAbleCount, requestCount);
        }
    }
}

