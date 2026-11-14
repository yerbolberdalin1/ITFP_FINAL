using DeliveryPipeline;
using Xunit;

namespace DeliveryPipeline.Tests;

public class ReportTests
{
    [Fact]
    public void ForVehicle_ExcludesOrdersAboveCapacity()
    {
        var orders = SampleOrders();

        var eligible = orders.ForVehicle(30m).ToList();

        Assert.Equal(3, eligible.Count);
        Assert.DoesNotContain(eligible, o => o.WeightKg > 30m);
    }

    [Fact]
    public void Dispatch_OrdersByDescendingCostThenId()
    {
        var items = new[]
        {
            new ReportItem(CreateOrder("ORD-101", "Almaty", 20m, 5m, DeliveryMode.Standard), 1425m, 30),
            new ReportItem(CreateOrder("ORD-102", "Astana", 45m, 12.5m, DeliveryMode.Express), 2612.5m, 68),
            new ReportItem(CreateOrder("ORD-104", "Karaganda", 30m, 2m, DeliveryMode.Standard), 1750m, 45)
        };

        var rows = DeliveryReports.Dispatch(items, 0m);

        Assert.Equal(3, rows.Count);
        Assert.Equal("ORD-102", rows[0].OrderId);
        Assert.Equal("ORD-104", rows[1].OrderId);
        Assert.Equal("ORD-101", rows[2].OrderId);
    }

    [Fact]
    public void Dispatch_AppliesMinimumCostFilter()
    {
        var items = new[]
        {
            new ReportItem(CreateOrder("ORD-101", "Almaty", 20m, 5m, DeliveryMode.Standard), 1425m, 30),
            new ReportItem(CreateOrder("ORD-102", "Astana", 45m, 12.5m, DeliveryMode.Express), 2612.5m, 68)
        };

        var rows = DeliveryReports.Dispatch(items, 2000m);

        Assert.Single(rows);
        Assert.Equal("ORD-102", rows[0].OrderId);
    }

    [Fact]
    public void TotalsByMode_GroupsAndSumsCorrectly()
    {
        var items = new[]
        {
            new ReportItem(CreateOrder("ORD-101", "Almaty", 20m, 5m, DeliveryMode.Standard), 1425m, 30),
            new ReportItem(CreateOrder("ORD-102", "Astana", 45m, 12.5m, DeliveryMode.Express), 2612.5m, 68),
            new ReportItem(CreateOrder("ORD-104", "Karaganda", 30m, 2m, DeliveryMode.Standard), 1750m, 45)
        };

        var totals = DeliveryReports.TotalsByMode(items);

        Assert.Equal(2, totals.Count);
        Assert.Equal(DeliveryMode.Standard, totals[0].Mode);
        Assert.Equal(2, totals[0].OrderCount);
        Assert.Equal(3175m, totals[0].TotalCost);
        Assert.Equal(DeliveryMode.Express, totals[1].Mode);
        Assert.Equal(1, totals[1].OrderCount);
        Assert.Equal(2612.50m, totals[1].TotalCost);
    }

    private static DeliveryOrder CreateOrder(string id, string dest, decimal dist, decimal weight, DeliveryMode mode)
    {
        return Assert.IsType<OrderResult.Valid>(DeliveryOrder.Create(id, dest, dist, weight, mode)).Order;
    }

    private static IReadOnlyList<DeliveryOrder> SampleOrders()
    {
        return new[]
        {
            CreateOrder("ORD-101", "Almaty", 20m, 5m, DeliveryMode.Standard),
            CreateOrder("ORD-102", "Astana", 45m, 12.5m, DeliveryMode.Express),
            CreateOrder("ORD-103", "Shymkent", 80m, 50m, DeliveryMode.Standard),
            CreateOrder("ORD-104", "Karaganda", 30m, 2m, DeliveryMode.Standard)
        };
    }
}
