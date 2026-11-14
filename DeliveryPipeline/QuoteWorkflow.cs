namespace DeliveryPipeline;

public static class QuoteWorkflow
{
    public static Maybe<DeliveryOrder> FindOrder(
        IReadOnlyList<DeliveryOrder> orders, string id)
    {
        foreach (var order in orders)
        {
            if (order.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                return Maybe<DeliveryOrder>.Some(order);
        }

        return Maybe<DeliveryOrder>.None;
    }

    public static Result<DeliveryQuote> CreateQuote(
        IReadOnlyList<DeliveryOrder> orders,
        string id,
        decimal capacity,
        Func<DeliveryOrder, decimal> pricing)
    {
        return FindOrder(orders, id)
            .ToResult(new Error("ORDER_NOT_FOUND", $"Order '{id}' was not found."))
            .Bind(order => CheckVehicle(order, capacity))
            .Map(order => Pipeline.CalculateQuote(order, pricing));
    }

    private static Result<DeliveryOrder> CheckVehicle(DeliveryOrder order, decimal capacity)
    {
        if (capacity <= 0)
            return Result<DeliveryOrder>.Fail(new Error(
                "INVALID_CAPACITY", $"Vehicle capacity must be greater than 0 kg; received {capacity} kg."));

        if (order.WeightKg > capacity)
            return Result<DeliveryOrder>.Fail(new Error(
                "WEIGHT_LIMIT", $"Order '{order.Id}' weighs {order.WeightKg} kg and exceeds the {capacity} kg vehicle limit."));

        return Result<DeliveryOrder>.Ok(order);
    }
}
