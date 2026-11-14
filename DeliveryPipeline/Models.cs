namespace DeliveryPipeline;

public enum DeliveryMode
{
    Standard,
    Express,
    Priority
}

public sealed record DeliveryQuote(
    string OrderId,
    string Destination,
    decimal Cost,
    DeliveryMode Mode
);

public sealed record DeliverySummary(
    int TotalOrders,
    decimal TotalCost,
    decimal AverageCost,
    IReadOnlyList<DeliveryQuote> Quotes
);
