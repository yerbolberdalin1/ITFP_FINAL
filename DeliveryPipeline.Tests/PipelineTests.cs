using DeliveryPipeline;
using Xunit;

namespace DeliveryPipeline.Tests;

public class PipelineTests
{
    [Fact]
    public void NormalWorkflow_ProducesDocumentedQuotesAndSummary()
    {
        var orders = new List<DeliveryOrder>
        {
            CreateOrder("ORD-01", "Almaty", 10m, 2m, DeliveryMode.Standard),
            CreateOrder("ORD-02", "Astana", 20m, 4m, DeliveryMode.Express)
        };

        var summary = Pipeline.ExecutePipeline(orders, 50m, Policies.StandardPricing);

        Assert.Equal(2, summary.TotalOrders);
        Assert.Equal(2350m, summary.TotalCost);
        Assert.Equal(1175m, summary.AverageCost);
        Assert.Equal("ORD-02", summary.Quotes[0].OrderId);
        Assert.Equal(1400m, summary.Quotes[0].Cost);
        Assert.Equal("ORD-01", summary.Quotes[1].OrderId);
        Assert.Equal(950m, summary.Quotes[1].Cost);
    }

    [Fact]
    public void EmptyDataset_ProducesZeroedSummaryWithoutCrashing()
    {
        var emptyOrders = new List<DeliveryOrder>();

        var summary = Pipeline.ExecutePipeline(emptyOrders, 50m, Policies.StandardPricing);

        Assert.Equal(0, summary.TotalOrders);
        Assert.Equal(0m, summary.TotalCost);
        Assert.Equal(0m, summary.AverageCost);
        Assert.Empty(summary.Quotes);
    }

    [Fact]
    public void CurriedPricing_AppliesConfigurationsCorrectly()
    {
        var order = CreateOrder("ORD-101", "Almaty", 20m, 5m, DeliveryMode.Standard);
        var standard = Policies.CreatePricing(Policies.StandardRules);
        var express = Policies.CreatePricing(Policies.ExpressRules);

        Assert.Equal(1425m, standard(order));
        Assert.Equal(2850m, express(order));
    }

    private static DeliveryOrder CreateOrder(
        string id, string destination, decimal distanceKm, decimal weightKg, DeliveryMode mode)
    {
        return Assert.IsType<OrderResult.Valid>(
            DeliveryOrder.Create(id, destination, distanceKm, weightKg, mode)).Order;
    }
}
