namespace DeliveryPipeline;

public static class Pipeline
{
    public static DeliveryQuote CalculateQuote(
        DeliveryOrder? order,
        Func<DeliveryOrder, decimal> pricingPolicy)
    {
        if (order is null)
            throw new InvalidOperationException("A validated order is required before calculating a quote.");

        ArgumentNullException.ThrowIfNull(pricingPolicy);

        decimal cost = pricingPolicy(order);
        return new DeliveryQuote(order.Id, order.Destination, cost, order.Mode);
    }

    public static IReadOnlyList<DeliveryOrder> FilterEligible(
        IReadOnlyList<DeliveryOrder> orders, 
        decimal maxAllowedWeight)
    {
        ArgumentNullException.ThrowIfNull(orders);

        var eligible = new List<DeliveryOrder>();
        foreach (var order in orders)
        {
            if (order.WeightKg <= maxAllowedWeight)
            {
                eligible.Add(order);
            }
        }
        return eligible.AsReadOnly();
    }

    public static IReadOnlyList<DeliveryQuote> CalculateQuotes(
        IReadOnlyList<DeliveryOrder> orders, 
        Func<DeliveryOrder, decimal> pricingPolicy)
    {
        ArgumentNullException.ThrowIfNull(orders);
        ArgumentNullException.ThrowIfNull(pricingPolicy);

        var quotes = new List<DeliveryQuote>(orders.Count);
        foreach (var order in orders)
        {
            quotes.Add(CalculateQuote(order, pricingPolicy));
        }
        return quotes.AsReadOnly();
    }

    public static DeliverySummary CreateSummary(IReadOnlyList<DeliveryQuote> quotes)
    {
        ArgumentNullException.ThrowIfNull(quotes);

        if (quotes.Count == 0)
        {
            return new DeliverySummary(0, 0m, 0m, Array.Empty<DeliveryQuote>());
        }

        decimal totalCost = 0m;
        foreach (var quote in quotes)
        {
            totalCost += quote.Cost;
        }

        decimal averageCost = Math.Round(totalCost / quotes.Count, 2);
        var sortedQuotes = quotes.OrderByDescending(q => q.Cost)
            .ThenBy(q => q.OrderId, StringComparer.Ordinal).ToList();

        return new DeliverySummary(quotes.Count, totalCost, averageCost, sortedQuotes.AsReadOnly());
    }

    public static DeliverySummary ExecutePipeline(
        IReadOnlyList<DeliveryOrder> orders,
        decimal maxAllowedWeight,
        Func<DeliveryOrder, decimal> pricingPolicy)
    {
        var stage1 = FilterEligible(orders, maxAllowedWeight);
        var stage2 = CalculateQuotes(stage1, pricingPolicy);
        var stage3 = CreateSummary(stage2);

        return stage3;
    }
}
