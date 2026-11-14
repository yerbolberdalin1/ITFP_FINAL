namespace DeliveryPipeline;

public sealed class DeliveryOrder
{
    public string Id { get; }
    public string Destination { get; }
    public decimal DistanceKm { get; }
    public decimal WeightKg { get; }
    public DeliveryMode Mode { get; }

    private DeliveryOrder(string id, string destination, decimal distanceKm,
        decimal weightKg, DeliveryMode mode)
    {
        Id = id;
        Destination = destination;
        DistanceKm = distanceKm;
        WeightKg = weightKg;
        Mode = mode;
    }

    public static OrderResult Create(string? id, string? destination,
        decimal distanceKm, decimal weightKg, DeliveryMode mode)
    {
        if (string.IsNullOrWhiteSpace(id))
            return new OrderResult.Invalid("Order ID is required.");

        if (string.IsNullOrWhiteSpace(destination))
            return new OrderResult.Invalid("Destination is required.");

        if (distanceKm <= 0 || distanceKm > 500)
            return new OrderResult.Invalid("Distance must be greater than 0 and at most 500 km.");

        if (weightKg <= 0 || weightKg > 50)
            return new OrderResult.Invalid("Weight must be greater than 0 and at most 50 kg.");

        if (!Enum.IsDefined(mode))
            return new OrderResult.Invalid("Unknown delivery mode.");

        return new OrderResult.Valid(
            new DeliveryOrder(id.Trim(), destination.Trim(), distanceKm, weightKg, mode));
    }
}
