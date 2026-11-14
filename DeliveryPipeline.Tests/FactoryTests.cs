using DeliveryPipeline;
using Xunit;

namespace DeliveryPipeline.Tests;

public class FactoryTests
{
    [Fact]
    public void Create_ValidParameters_ReturnsValidOrder()
    {
        var result = DeliveryOrder.Create("ORD-1", "Almaty", 500m, 50m, DeliveryMode.Express);

        var order = Assert.IsType<OrderResult.Valid>(result).Order;
        Assert.Equal("ORD-1", order.Id);
        Assert.Equal("Almaty", order.Destination);
        Assert.Equal(500m, order.DistanceKm);
        Assert.Equal(50m, order.WeightKg);
        Assert.Equal(DeliveryMode.Express, order.Mode);
    }

    [Fact]
    public void Create_InvalidWeight_ReturnsInvalid()
    {
        var result = DeliveryOrder.Create("ORD-1", "Almaty", 10m, 55m, DeliveryMode.Standard);

        var invalid = Assert.IsType<OrderResult.Invalid>(result);
        Assert.Equal("Weight must be greater than 0 and at most 50 kg.", invalid.Reason);
    }

    [Fact]
    public void Create_InvalidDistance_ReturnsInvalid()
    {
        var result = DeliveryOrder.Create("ORD-1", "Almaty", 0m, 10m, DeliveryMode.Standard);

        var invalid = Assert.IsType<OrderResult.Invalid>(result);
        Assert.Equal("Distance must be greater than 0 and at most 500 km.", invalid.Reason);
    }

    [Fact]
    public void Create_MissingOrEmptyId_ReturnsInvalid()
    {
        var result = DeliveryOrder.Create("", "Almaty", 10m, 5m, DeliveryMode.Standard);

        var invalid = Assert.IsType<OrderResult.Invalid>(result);
        Assert.Equal("Order ID is required.", invalid.Reason);
    }
}
