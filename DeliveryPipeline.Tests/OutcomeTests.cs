using DeliveryPipeline;
using Xunit;

namespace DeliveryPipeline.Tests;

public class OutcomeTests
{
    [Fact]
    public void Result_OkAndFail_HoldExpectedPayloads()
    {
        var ok = Result<int>.Ok(42);
        var fail = Result<int>.Fail(new Error("ERR_CODE", "Error text"));

        Assert.Equal(42, Assert.IsType<Result<int>.Success>(ok).Value);
        Assert.Equal("ERR_CODE", Assert.IsType<Result<int>.Failure>(fail).Error.Code);
    }

    [Fact]
    public void Result_Map_TransformsSuccess()
    {
        var result = Result<int>.Ok(4).Map(v => $"Value: {v * 3}");

        Assert.Equal("Value: 12", Assert.IsType<Result<string>.Success>(result).Value);
    }

    [Fact]
    public void Result_Bind_ShortCircuitsOnFailure()
    {
        var error = new Error("FAIL", "Initial failure");
        var failure = Result<int>.Fail(error);
        bool called = false;

        var bound = failure.Bind(v =>
        {
            called = true;
            return Result<int>.Ok(v + 1);
        });

        Assert.False(called);
        Assert.Same(error, Assert.IsType<Result<int>.Failure>(bound).Error);
    }

    [Fact]
    public void Maybe_ToResult_ConvertsPresenceAndAbsence()
    {
        var missingError = new Error("MISSING", "Value missing");
        var some = Maybe<string>.Some("data").ToResult(missingError);
        var none = Maybe<string>.None.ToResult(missingError);

        Assert.Equal("data", Assert.IsType<Result<string>.Success>(some).Value);
        Assert.Same(missingError, Assert.IsType<Result<string>.Failure>(none).Error);
    }
}
