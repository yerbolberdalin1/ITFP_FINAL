namespace DeliveryPipeline;

public sealed record PricedOrder(DeliveryOrder Order, decimal Cost);

public sealed record ReportItem(DeliveryOrder Order, decimal Cost, int EstimatedMinutes);

public sealed record DispatchRow(
    string OrderId,
    string Destination,
    DeliveryMode Mode,
    decimal Cost,
    int EstimatedMinutes);

public sealed record ModeTotal(DeliveryMode Mode, int OrderCount, decimal TotalCost);

public sealed record PricingRules(decimal BaseFee, decimal PerKm, decimal PerKg);

public sealed record CourierTotal(string CourierId, string Name, int InTransit,
    int Delivered, decimal ActiveWeightKg, decimal Revenue);
