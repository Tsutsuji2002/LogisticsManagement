using Logistics.Server.Domain.Common;
using Xunit;

namespace Logistics.Server.UnitTests.Domain;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessResult()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult()
    {
        var result = Result.Failure("Invalid operation");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Invalid operation", result.Error);
    }

    [Fact]
    public void Success_Generic_ShouldReturnValue()
    {
        var result = Result.Success(100);

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value);
    }

    [Fact]
    public void Failure_Generic_ShouldThrowOnAccessingValue()
    {
        var result = Result.Failure<string>("Not found");

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
