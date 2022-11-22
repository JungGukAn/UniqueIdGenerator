using Jung.Utils;
namespace Jung.Tests;

public class UnitTest
{
    [Fact]
    public void TestThrowErrorWhenRequestCountExceedMaxSequencePerSecond()
    {
        var uniqueIdGenerator = new UniqueIdGenerator(1);

        Assert.Throws<InvalidOperationException>(() =>
        {
            var uniqueIds = uniqueIdGenerator.Issue(UniqueIdGenerator.MaxSequencePerSeconds);
        });
    }

    [Fact]
    public void TestRunWhenNotExceedMaxSequencePerSecond()
    {
        var uniqueIdGenerator = new TimeChangeableIdGenerator(1);
        uniqueIdGenerator.ChangeableTime = DateTime.UtcNow;

        var uniqueIds = uniqueIdGenerator.Issue(UniqueIdGenerator.MaxSequencePerSeconds - 3);
        var unqiueIds2 = uniqueIdGenerator.Issue(2);
    }

    [Fact]
    public void TestThrowErrorWhenExceedMaxSequencePerSecond()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            var uniqueIdGenerator = new TimeChangeableIdGenerator(1);
            uniqueIdGenerator.ChangeableTime = DateTime.UtcNow;

            var uniqueIds = uniqueIdGenerator.Issue(UniqueIdGenerator.MaxSequencePerSeconds - 1);
            var uniqueId = uniqueIdGenerator.Issue();
        });
    }


    [Fact]
    public async Task TestRunWhenInitializedSequencePerSecondAsync()
    {
        var uniqueIdGenerator = new UniqueIdGenerator(1);
        var uniqueIds = uniqueIdGenerator.Issue(UniqueIdGenerator.MaxSequencePerSeconds - 1);

        await Task.Delay(TimeSpan.FromSeconds(1));

        var uniqueId = uniqueIdGenerator.Issue();
    }

    [Fact]
    public void TestThrowErrorWhenTimeLimitOver()
    {
        var uniqueIdGenerator = new TimeChangeableIdGenerator(1);
        uniqueIdGenerator.ChangeableTime = uniqueIdGenerator.BaseCalculateTime.AddSeconds((double)uint.MaxValue + 1);

        Assert.Throws<InvalidOperationException>(() =>
        {
            uniqueIdGenerator.Issue();
        });
    }
}
