using DeliveryPipeline;
using Xunit;

namespace DeliveryPipeline.Tests;

public class NativeTravelTimeTests
{
    [Fact]
    public void Estimate_RealNativeCall_ReturnsRoundedUpMinutes()
    {
        var travelTime = new NativeTravelTime();

        var result = travelTime.Estimate(20, 40);

        Assert.Equal(30, Assert.IsType<Result<int>.Success>(result).Value);
    }

    [Fact]
    public void Estimate_RoundsDistanceUpToWholeMetresBeforeCallingNativeCode()
    {
        int receivedMetres = 0;
        int receivedSpeed = 0;
        var travelTime = new NativeTravelTime((metres, speed) =>
        {
            receivedMetres = metres;
            receivedSpeed = speed;
            return 31;
        });

        var result = travelTime.Estimate(20.0001m, 40);

        Assert.Equal(20001, receivedMetres);
        Assert.Equal(40, receivedSpeed);
        Assert.Equal(31, Assert.IsType<Result<int>.Success>(result).Value);
    }

    [Fact]
    public void Estimate_InvalidSpeedOrDistance_ReturnsFailure()
    {
        var travelTime = new NativeTravelTime();

        var resultZeroDistance = travelTime.Estimate(0m, 40);
        var resultZeroSpeed = travelTime.Estimate(20m, 0);

        Assert.IsType<Result<int>.Failure>(resultZeroDistance);
        Assert.IsType<Result<int>.Failure>(resultZeroSpeed);
    }
}
