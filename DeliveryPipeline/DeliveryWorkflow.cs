namespace DeliveryPipeline;

public sealed class DeliveryWorkflow
{
    private readonly NativeTravelTime travelTime;

    public DeliveryWorkflow(NativeTravelTime? travelTime = null)
    {
        this.travelTime = travelTime ?? new NativeTravelTime();
    }

    public Result<Shipment> Dispatch(IReadOnlyList<DeliveryOrder> orders,
        IReadOnlyList<Courier> couriers, IReadOnlyList<Shipment> shipments,
        string orderId, string courierId, Func<DeliveryOrder, decimal> pricing,
        DateTimeOffset dispatchedAtUtc)
    {
        return QuoteWorkflow.FindOrder(orders, orderId)
            .ToResult(new Error("ORDER_NOT_FOUND", $"Order '{orderId}' was not found."))
            .Bind(order => FindCourier(couriers, courierId)
                .ToResult(new Error("COURIER_NOT_FOUND", $"Courier '{courierId}' was not found."))
                .Bind(courier => DispatchRules.Check(order, courier, orders, shipments)
                    .Map(validOrder => new PricedOrder(validOrder, pricing(validOrder)))
                    .Bind(priced => travelTime.Estimate(priced.Order.DistanceKm, courier.SpeedKmh)
                        .Bind(minutes => Shipment.Create($"SHP-{order.Id}", order.Id,
                            courier.Id, priced.Cost, minutes, dispatchedAtUtc)))));
    }

    public static Result<Shipment> Complete(IReadOnlyList<Shipment> shipments,
        string shipmentId, DateTimeOffset deliveredAtUtc)
    {
        return FindShipment(shipments, shipmentId)
            .ToResult(new Error("SHIPMENT_NOT_FOUND", $"Shipment '{shipmentId}' was not found. Dispatch the order first."))
            .Bind(shipment => shipment.Complete(deliveredAtUtc));
    }

    public static Maybe<Courier> FindCourier(IReadOnlyList<Courier> couriers, string id)
    {
        var courier = couriers.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        return courier is null ? Maybe<Courier>.None : Maybe<Courier>.Some(courier);
    }

    public static Maybe<Shipment> FindShipment(IReadOnlyList<Shipment> shipments, string id)
    {
        var shipment = shipments.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        return shipment is null ? Maybe<Shipment>.None : Maybe<Shipment>.Some(shipment);
    }
}
