namespace DeliveryPipeline;

public static class DeliveryReports
{
    public static IReadOnlyList<CourierTotal> CourierWorkload(IReadOnlyList<DeliveryOrder> orders,
        IReadOnlyList<Courier> couriers, IReadOnlyList<Shipment> shipments)
    {
        var orderWeights = orders.ToDictionary(order => order.Id, order => order.WeightKg,
            StringComparer.OrdinalIgnoreCase);

        return couriers
            .GroupJoin(shipments, courier => courier.Id, shipment => shipment.CourierId,
                (courier, assigned) => new CourierTotal(
                    courier.Id,
                    courier.Name,
                    assigned.Count(shipment => shipment.Status == ShipmentStatus.InTransit),
                    assigned.Count(shipment => shipment.Status == ShipmentStatus.Delivered),
                    assigned.Where(shipment => shipment.Status == ShipmentStatus.InTransit)
                        .Sum(shipment => orderWeights[shipment.OrderId]),
                    assigned.Where(shipment => shipment.Status == ShipmentStatus.Delivered)
                        .Sum(shipment => shipment.Cost)), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(row => row.ActiveWeightKg)
            .ThenBy(row => row.CourierId, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<DispatchRow> Dispatch(
        IEnumerable<ReportItem> items, decimal minimumCost)
    {
        return items
            .Where(item => item.Cost >= minimumCost)
            .Select(item => new DispatchRow(
                item.Order.Id,
                item.Order.Destination,
                item.Order.Mode,
                item.Cost,
                item.EstimatedMinutes))
            .OrderByDescending(row => row.Cost)
            .ThenBy(row => row.OrderId, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<ModeTotal> TotalsByMode(IEnumerable<ReportItem> items)
    {
        return items
            .GroupBy(item => item.Order.Mode)
            .Select(group => new ModeTotal(
                group.Key,
                group.Count(),
                group.Sum(item => item.Cost)))
            .OrderByDescending(row => row.TotalCost)
            .ThenBy(row => row.Mode)
            .ToArray();
    }
}
