using Jung.Utils;
namespace Jung.Tests;

public class UnitTest
{
    [Fact]
    public async Task TestAllHasSameCreatedAtWhenNotExceedMaxSequencePerSecondAsync()
    {
        var uniqueIdGenerator = new UniqueIdGenerator(1);
        var createdTimes = (await uniqueIdGenerator.IssueAsync(UniqueIdGenerator.MaxSequencePerSeconds - 1))
            .Select(uniqueId => uniqueId.CreatedAt).ToHashSet();

        Assert.Single(createdTimes);
    }

    [Fact]
    public async Task TestExistDifferentCreatedAtWhenExceedMaxSequencePerSecondAsync()
    {
        var uniqueIdGenerator = new UniqueIdGenerator(1);
        var uniqueIds = await uniqueIdGenerator.IssueAsync(UniqueIdGenerator.MaxSequencePerSeconds);

        var uniqueIdCountByCreatedAt = uniqueIds.GroupBy(uniqueId => uniqueId.CreatedAt)
            .Select(group => new { CreatedAt = group.Key, Ids = group.ToList() })
            .ToList();

        Assert.True(uniqueIdCountByCreatedAt.Count != 1);
    }

    [Fact]
    public void TestRunSyncWhenRequestCountNotExceedMaxSequencePerSecond()
    {
        var uniqueIdGenerator = new UniqueIdGenerator(1);
        var issueTask = uniqueIdGenerator.IssueAsync(UniqueIdGenerator.MaxSequencePerSeconds - 1);

        Assert.True(issueTask.IsCompleted);
    }

    [Fact]
    public void TestRunAsyncWhenRequestCountExceedMaxSequencePerSecond()
    {
        var uniqueIdGenerator = new UniqueIdGenerator(1);
        var issueTask = uniqueIdGenerator.IssueAsync(UniqueIdGenerator.MaxSequencePerSeconds);

        Assert.False(issueTask.IsCompleted);
    }

    [Fact]
    public void TestRunAsyncWhenSatisfiedMaxSequencePerSecondCondition()
    {
        var uniqueIdGenerator = new UniqueIdGenerator(1);
        var issueTask1 = uniqueIdGenerator.IssueAsync(UniqueIdGenerator.MaxSequencePerSeconds - 1);
        var issueTask2 = uniqueIdGenerator.IssueAsync();

        Assert.True(issueTask1.IsCompleted && issueTask2.IsCompleted == false);
    }

    [Fact]
    public async Task TestRunSyncWhenReleasedMaxSequencePerSecondConditionAsync()
    {
        var uniqueIdGenerator = new UniqueIdGenerator(1);
        var issueTask1 = uniqueIdGenerator.IssueAsync(UniqueIdGenerator.MaxSequencePerSeconds -1);

        await Task.Delay(TimeSpan.FromSeconds(1));

        var issueTask2 = uniqueIdGenerator.IssueAsync();

        Assert.True(issueTask1.IsCompleted && issueTask2.IsCompleted);
    }

    [Fact]
    public void TestRunAsyncWhenCalledAtPastTime()
    {
        var uniqueIdGenerator = new TimeChangeableIdGenerator(1);
        var issueTask1 = uniqueIdGenerator.IssueAsync();
        uniqueIdGenerator.ChangeableTime = DateTime.UtcNow.AddSeconds(-5);
        var issueTask2 = uniqueIdGenerator.IssueAsync();

        Assert.True(issueTask1.IsCompleted && issueTask2.IsCompleted == false);
    }

    [Fact]
    public async Task TestExceptionWhenTimeLimitOverAsync()
    {
        var uniqueIdGenerator = new TimeChangeableIdGenerator(1);
        uniqueIdGenerator.ChangeableTime = uniqueIdGenerator.BaseCalculateTime.AddSeconds((double)uint.MaxValue + 1);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await uniqueIdGenerator.IssueAsync();
        });
    }
}
