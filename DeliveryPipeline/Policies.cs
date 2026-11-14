namespace DeliveryPipeline;

public static class Policies
{
    public static readonly PricingRules StandardRules = new(500m, 40m, 25m);
    public static readonly PricingRules ExpressRules = new(1000m, 80m, 50m);
    public static readonly PricingRules PriorityRules = new(1500m, 120m, 75m);

    private static readonly Func<DeliveryOrder, decimal> Standard = CreatePricing(StandardRules);
    private static readonly Func<DeliveryOrder, decimal> Express = CreatePricing(ExpressRules);
    private static readonly Func<DeliveryOrder, decimal> Priority = CreatePricing(PriorityRules);

    public static Func<DeliveryOrder, decimal> CreatePricing(PricingRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        if (rules.BaseFee < 0 || rules.PerKm < 0 || rules.PerKg < 0)
            throw new ArgumentOutOfRangeException(nameof(rules), "Pricing rates cannot be negative.");

        return order => rules.BaseFee + order.DistanceKm * rules.PerKm + order.WeightKg * rules.PerKg;
    }

    public static decimal StandardPricing(DeliveryOrder order) => Standard(order);

    public static decimal ExpressPricing(DeliveryOrder order) => Express(order);

    public static decimal ModePricing(DeliveryOrder order) => order.Mode switch
    {
        DeliveryMode.Standard => Standard(order),
        DeliveryMode.Express => Express(order),
        DeliveryMode.Priority => Priority(order),
        _ => throw new ArgumentOutOfRangeException(nameof(order), "Unknown delivery mode.")
    };
}
