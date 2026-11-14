namespace DeliveryPipeline;

public sealed class Courier
{
    public string Id { get; }
    public string Name { get; }
    public decimal CapacityKg { get; }
    public decimal MaxDistanceKm { get; }
    public int SpeedKmh { get; }

    private Courier(string id, string name, decimal capacityKg, decimal maxDistanceKm, int speedKmh)
    {
        Id = id;
        Name = name;
        CapacityKg = capacityKg;
        MaxDistanceKm = maxDistanceKm;
        SpeedKmh = speedKmh;
    }

    public static Result<Courier> Create(string? id, string? name,
        decimal capacityKg, decimal maxDistanceKm, int speedKmh)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
            return Result<Courier>.Fail(new Error("INVALID_COURIER", "Courier ID and name are required."));

        if (capacityKg <= 0 || capacityKg > 500)
            return Result<Courier>.Fail(new Error("INVALID_CAPACITY", "Courier capacity must be greater than 0 and at most 500 kg."));

        if (maxDistanceKm <= 0 || maxDistanceKm > 500)
            return Result<Courier>.Fail(new Error("INVALID_DISTANCE", "Courier range must be greater than 0 and at most 500 km."));

        if (speedKmh < 1 || speedKmh > 120)
            return Result<Courier>.Fail(new Error("INVALID_SPEED", "Courier speed must be between 1 and 120 km/h."));

        return Result<Courier>.Ok(new Courier(id.Trim(), name.Trim(), capacityKg, maxDistanceKm, speedKmh));
    }
}
