using DeliveryPipeline;
using Xunit;

namespace DeliveryPipeline.Tests;

public class DeferredExecutionTests
{
    [Fact]
    public void ToPricedOrders_DoesNoPricingUntilEnumerationBegins()
    {
        int calls = 0;
        var orders = Orders();
        var pending = orders.ToPricedOrders(order =>
        {
            calls++;
            return Policies.StandardPricing(order);
        });

        Assert.Equal(0, calls);

        using var iterator = pending.GetEnumerator();
        Assert.Equal(0, calls);
        Assert.True(iterator.MoveNext());
        Assert.Equal(1, calls);
        Assert.Equal("ORD-1", iterator.Current.Order.Id);
    }

    [Fact]
    public void DeferredSequence_RepeatsPricingOnEachEnumeration()
    {
        int calls = 0;
        var pending = Orders().ToPricedOrders(order =>
        {
            calls++;
            return Policies.StandardPricing(order);
        });

        foreach (var _ in pending) { }
        Assert.Equal(2, calls);

        foreach (var _ in pending) { }
        Assert.Equal(4, calls);
    }

    [Fact]
    public void MaterializedSequence_ReusesCachedPrices()
    {
        int calls = 0;
        var snapshot = Orders().ToPricedOrders(order =>
        {
            calls++;
            return Policies.StandardPricing(order);
        }).ToArray();

        Assert.Equal(2, calls);

        foreach (var _ in snapshot) { }
        foreach (var _ in snapshot) { }
        Assert.Equal(2, calls);
    }

    private static IReadOnlyList<DeliveryOrder> Orders()
    {
        return new[]
        {
            Assert.IsType<OrderResult.Valid>(DeliveryOrder.Create("ORD-1", "Almaty", 10m, 2m, DeliveryMode.Standard)).Order,
            Assert.IsType<OrderResult.Valid>(DeliveryOrder.Create("ORD-2", "Astana", 20m, 4m, DeliveryMode.Express)).Order
        };
    }
}
