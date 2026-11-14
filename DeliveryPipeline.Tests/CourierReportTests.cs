using DeliveryPipeline;

namespace DeliveryPipeline.Tests;

public class CourierReportTests
{
    [Fact]
    public void Workload_SeparatesActiveWeightFromDeliveredRevenueAndIncludesIdleCouriers()
    {
        var first = TestData.Order(weight: 10);
        var second = TestData.Order("ORD-2", weight: 5);
        var courier = TestData.Courier();
        var idle = TestData.Courier("C-02");
        var active = TestData.Dispatch(first, courier);
        var completed = Assert.IsType<Result<Shipment>.Success>(
            TestData.Dispatch(second, courier).Complete(TestData.Start.AddMinutes(30))).Value;

        var rows = DeliveryReports.CourierWorkload([first, second], [idle, courier], [active, completed]);

        Assert.Equal(2, rows.Count);
        Assert.Equal("C-01", rows[0].CourierId);
        Assert.Equal(1, rows[0].InTransit);
        Assert.Equal(1, rows[0].Delivered);
        Assert.Equal(10m, rows[0].ActiveWeightKg);
        Assert.Equal(1425m, rows[0].Revenue);
        Assert.Equal(0, rows[1].InTransit);
        Assert.Equal(0m, rows[1].Revenue);
    }

    [Fact]
    public void Workload_EqualWeightsAreOrderedByCourierId()
    {
        var rows = DeliveryReports.CourierWorkload([], [TestData.Courier("C-02"), TestData.Courier()], []);
        Assert.Equal(new[] { "C-01", "C-02" }, rows.Select(row => row.CourierId));
    }

    [Fact]
    public void Reports_EmptyInput_ReturnsEmptyReports()
    {
        Assert.Empty(DeliveryReports.Dispatch([], 0));
        Assert.Empty(DeliveryReports.TotalsByMode([]));
        Assert.Empty(DeliveryReports.CourierWorkload([], [], []));
    }

    [Fact]
    public void Dispatch_EqualCostsAreOrderedByOrderId()
    {
        var items = new[]
        {
            new ReportItem(TestData.Order("ORD-2"), 1000, 30),
            new ReportItem(TestData.Order(), 1000, 30)
        };

        Assert.Equal(new[] { "ORD-1", "ORD-2" },
            DeliveryReports.Dispatch(items, 1000).Select(row => row.OrderId));
    }
}
