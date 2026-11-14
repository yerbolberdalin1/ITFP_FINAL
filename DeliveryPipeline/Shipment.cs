namespace DeliveryPipeline;

public enum ShipmentStatus
{
    InTransit,
    Delivered
}

public sealed class Shipment
{
    public string Id { get; }
    public string OrderId { get; }
    public string CourierId { get; }
    public decimal Cost { get; }
    public int EstimatedMinutes { get; }
    public DateTimeOffset DispatchedAtUtc { get; }
    public DateTimeOffset? DeliveredAtUtc { get; }
    public ShipmentStatus Status => DeliveredAtUtc is null ? ShipmentStatus.InTransit : ShipmentStatus.Delivered;

    private Shipment(string id, string orderId, string courierId, decimal cost,
        int estimatedMinutes, DateTimeOffset dispatchedAtUtc, DateTimeOffset? deliveredAtUtc)
    {
        Id = id;
        OrderId = orderId;
        CourierId = courierId;
        Cost = cost;
        EstimatedMinutes = estimatedMinutes;
        DispatchedAtUtc = dispatchedAtUtc.ToUniversalTime();
        DeliveredAtUtc = deliveredAtUtc?.ToUniversalTime();
    }

    internal static Result<Shipment> Create(string? id, string? orderId, string? courierId,
        decimal cost, int estimatedMinutes, DateTimeOffset dispatchedAtUtc,
        DateTimeOffset? deliveredAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(courierId))
            return Result<Shipment>.Fail(new Error("INVALID_SHIPMENT", "Shipment, order and courier IDs are required."));

        if (cost < 0 || estimatedMinutes <= 0 || dispatchedAtUtc == default)
            return Result<Shipment>.Fail(new Error("INVALID_SHIPMENT", "Shipment cost, duration or dispatch time is invalid."));

        if (deliveredAtUtc is not null && deliveredAtUtc < dispatchedAtUtc)
            return Result<Shipment>.Fail(new Error("INVALID_DELIVERY_TIME", "Delivery time cannot be earlier than dispatch time."));

        return Result<Shipment>.Ok(new Shipment(id.Trim(), orderId.Trim(), courierId.Trim(),
            cost, estimatedMinutes, dispatchedAtUtc, deliveredAtUtc));
    }

    public Result<Shipment> Complete(DateTimeOffset deliveredAtUtc)
    {
        return Status switch
        {
            ShipmentStatus.Delivered => Result<Shipment>.Ok(this),
            ShipmentStatus.InTransit => Create(Id, OrderId, CourierId, Cost,
                EstimatedMinutes, DispatchedAtUtc, deliveredAtUtc),
            _ => Result<Shipment>.Fail(new Error("INVALID_STATE", "Shipment cannot be completed in its current state."))
        };
    }
}
