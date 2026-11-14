namespace DeliveryPipeline;

public static class ShipmentsFile
{
    public static Result<IReadOnlyList<Shipment>> Load(string path,
        IReadOnlyList<DeliveryOrder> orders, IReadOnlyList<Courier> couriers)
    {
        return JsonFile.Load<ShipmentData?[]>(path).Bind(data =>
        {
            var shipments = new List<Shipment>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var orderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < data.Length; i++)
            {
                var item = data[i];
                if (item is null)
                    return Malformed(path, $"Record {i + 1} must be a shipment object.");

                var restored = Shipment.Create(item.Id, item.OrderId, item.CourierId,
                    item.Cost, item.EstimatedMinutes, item.DispatchedAtUtc, item.DeliveredAtUtc);

                if (restored is Result<Shipment>.Failure failure)
                    return Malformed(path, $"Record {i + 1}: {failure.Error.Message}");

                if (restored is Result<Shipment>.Success success)
                {
                    var shipment = success.Value;
                    var order = orders.FirstOrDefault(value => value.Id.Equals(shipment.OrderId, StringComparison.OrdinalIgnoreCase));
                    var courier = couriers.FirstOrDefault(value => value.Id.Equals(shipment.CourierId, StringComparison.OrdinalIgnoreCase));

                    if (order is null || courier is null)
                        return Malformed(path, $"Shipment '{shipment.Id}' references an unknown order or courier.");

                    if (!ids.Add(shipment.Id) || !orderIds.Add(shipment.OrderId))
                        return Malformed(path, $"Duplicate shipment or order in record {i + 1}.");

                    if (order.DistanceKm > courier.MaxDistanceKm)
                        return Malformed(path, $"Shipment '{shipment.Id}' exceeds its courier's range.");

                    shipments.Add(shipment);
                }
            }

            foreach (var courier in couriers)
            {
                var activeIds = shipments
                    .Where(shipment => shipment.Status == ShipmentStatus.InTransit &&
                        shipment.CourierId.Equals(courier.Id, StringComparison.OrdinalIgnoreCase))
                    .Select(shipment => shipment.OrderId)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (orders.Where(order => activeIds.Contains(order.Id)).Sum(order => order.WeightKg) > courier.CapacityKg)
                    return Malformed(path, $"Active shipments exceed courier '{courier.Id}' capacity.");
            }

            return Result<IReadOnlyList<Shipment>>.Ok(shipments.AsReadOnly());
        });
    }

    public static Result<string> Save(string path, IReadOnlyList<Shipment> shipments)
    {
        var data = shipments.Select(shipment => new ShipmentData(shipment.Id, shipment.OrderId,
            shipment.CourierId, shipment.Cost, shipment.EstimatedMinutes,
            shipment.DispatchedAtUtc, shipment.DeliveredAtUtc)).ToArray();

        return JsonFile.Save(path, data);
    }

    private static Result<IReadOnlyList<Shipment>> Malformed(string path, string reason) =>
        Result<IReadOnlyList<Shipment>>.Fail(new Error("MALFORMED_DATA", $"Invalid shipments file '{path}': {reason}"));

    private sealed record ShipmentData(string Id, string OrderId, string CourierId,
        decimal Cost, int EstimatedMinutes, DateTimeOffset DispatchedAtUtc, DateTimeOffset? DeliveredAtUtc);
}
