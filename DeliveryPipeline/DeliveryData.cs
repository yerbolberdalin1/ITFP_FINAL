namespace DeliveryPipeline;

public sealed record DeliveryData(
    IReadOnlyList<DeliveryOrder> Orders,
    IReadOnlyList<Courier> Couriers,
    IReadOnlyList<Shipment> Shipments)
{
    public static Result<DeliveryData> Load(string directory)
    {
        return OrdersFile.Load(Path.Combine(directory, "orders.json"))
            .Bind(orders => CouriersFile.Load(Path.Combine(directory, "couriers.json"))
                .Bind(couriers => ShipmentsFile.Load(Path.Combine(directory, "shipments.json"), orders, couriers)
                    .Map(shipments => new DeliveryData(orders, couriers, shipments))));
    }
}
