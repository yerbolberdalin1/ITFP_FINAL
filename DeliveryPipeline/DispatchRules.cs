namespace DeliveryPipeline;

public static class DispatchRules
{
    public static Result<DeliveryOrder> Check(DeliveryOrder order, Courier courier,
        IReadOnlyList<DeliveryOrder> orders, IReadOnlyList<Shipment> shipments)
    {
        if (shipments.Any(shipment => shipment.OrderId.Equals(order.Id, StringComparison.OrdinalIgnoreCase)))
            return Result<DeliveryOrder>.Fail(new Error("ORDER_ALREADY_DISPATCHED", $"Order '{order.Id}' has already been dispatched."));

        if (order.DistanceKm > courier.MaxDistanceKm)
            return Result<DeliveryOrder>.Fail(new Error("OUTSIDE_COURIER_RANGE", $"Order '{order.Id}' exceeds courier '{courier.Id}' range of {courier.MaxDistanceKm} km."));

        var activeOrderIds = shipments
            .Where(shipment => shipment.Status == ShipmentStatus.InTransit &&
                shipment.CourierId.Equals(courier.Id, StringComparison.OrdinalIgnoreCase))
            .Select(shipment => shipment.OrderId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        decimal usedWeight = orders.Where(item => activeOrderIds.Contains(item.Id)).Sum(item => item.WeightKg);

        if (usedWeight + order.WeightKg > courier.CapacityKg)
            return Result<DeliveryOrder>.Fail(new Error("WEIGHT_LIMIT", $"Courier '{courier.Id}' has {courier.CapacityKg - usedWeight} kg available; order requires {order.WeightKg} kg."));

        return Result<DeliveryOrder>.Ok(order);
    }
}
