namespace DeliveryPipeline;

public static class DeliveryExtensions
{
    public static IEnumerable<DeliveryOrder> ForVehicle(
        this IEnumerable<DeliveryOrder> orders, decimal capacity)
    {
        ArgumentNullException.ThrowIfNull(orders);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        return orders.Where(order => order.WeightKg <= capacity);
    }

    public static IEnumerable<PricedOrder> ToPricedOrders(
        this IEnumerable<DeliveryOrder> orders,
        Func<DeliveryOrder, decimal> pricing)
    {
        ArgumentNullException.ThrowIfNull(orders);
        ArgumentNullException.ThrowIfNull(pricing);

        foreach (var order in orders)
        {
            yield return new PricedOrder(order, pricing(order));
        }
    }
}
