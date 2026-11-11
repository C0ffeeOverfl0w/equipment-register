namespace Equipment.UnitTests;

public sealed class ResultTests
{
    [Fact]
    public void FailureCannotExposeValue()
    {
        var result = Result.Failure<int>(new Error("test", "failure"));
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void FailureRequiresAnError()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));
        Assert.Throws<ArgumentException>(() => Result.Failure<int>(Error.None));
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
    }

    [Fact]
    public void NullDoesNotBecomeSuccessfulResult()
    {
        Result<string> result = (string?)null;
        Assert.Equal(Error.NullValue, result.Error);
        Assert.Throws<ArgumentNullException>(() => Result.Success<string>(null!));
    }
}
