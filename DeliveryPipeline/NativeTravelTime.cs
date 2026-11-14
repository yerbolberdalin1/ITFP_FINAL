using System.Runtime.InteropServices;

namespace DeliveryPipeline;

public sealed class NativeTravelTime
{
    private readonly Func<int, int, int> estimate;

    public NativeTravelTime(Func<int, int, int>? estimate = null)
    {
        this.estimate = estimate ?? EstimatedMinutes;
    }

    public Result<int> Estimate(decimal distanceKm, int speedKmh)
    {
        if (distanceKm <= 0 || distanceKm > 500)
            return Result<int>.Fail(new Error("INVALID_DISTANCE",
                "Travel distance must be greater than 0 and at most 500 km."));

        if (speedKmh < 1 || speedKmh > 120)
            return Result<int>.Fail(new Error("INVALID_SPEED",
                "Travel speed must be between 1 and 120 km/h."));

        int distanceMetres = (int)decimal.Ceiling(distanceKm * 1000);

        try
        {
            int minutes = estimate(distanceMetres, speedKmh);

            if (minutes <= 0)
                return Result<int>.Fail(new Error("NATIVE_CALCULATION_ERROR",
                    "The native estimated_minutes function returned an invalid duration."));

            return Result<int>.Ok(minutes);
        }
        catch (DllNotFoundException)
        {
            return Result<int>.Fail(new Error("NATIVE_LIBRARY_NOT_FOUND",
                "Cannot load libdeliverymath.dylib. Run sh native/build.sh and rebuild the solution."));
        }
        catch (EntryPointNotFoundException)
        {
            return Result<int>.Fail(new Error("NATIVE_ENTRY_POINT_NOT_FOUND",
                "The native library does not export estimated_minutes. Rebuild it from native/deliverymath.c."));
        }
        catch (BadImageFormatException)
        {
            return Result<int>.Fail(new Error("NATIVE_BINARY_ERROR",
                "The native library is invalid or has the wrong architecture. Build it for macOS arm64."));
        }
    }

    [DllImport("deliverymath", EntryPoint = "estimated_minutes",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern int EstimatedMinutes(int distanceMetres, int speedKmh);
}
