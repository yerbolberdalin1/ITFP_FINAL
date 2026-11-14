using DeliveryPipeline;

namespace DeliveryPipeline.Tests;

public class DeliveryWorkflowTests
{
    [Fact]
    public void DispatchThenComplete_ProducesNewStatesAndPreservesInput()
    {
        var order = TestData.Order();
        var courier = TestData.Courier();
        var orders = new List<DeliveryOrder> { order };
        var shipments = new List<Shipment>();
        var workflow = new DeliveryWorkflow(new NativeTravelTime((metres, speed) =>
        {
            Assert.Equal(20000, metres);
            Assert.Equal(40, speed);
            return 30;
        }));

        var sent = Assert.IsType<Result<Shipment>.Success>(workflow.Dispatch(orders, [courier],
            shipments, order.Id, courier.Id, Policies.ModePricing, TestData.Start)).Value;
        var completed = Assert.IsType<Result<Shipment>.Success>(
            DeliveryWorkflow.Complete([sent], sent.Id, TestData.Start.AddMinutes(30))).Value;

        Assert.Equal(1425m, sent.Cost);
        Assert.Equal(30, sent.EstimatedMinutes);
        Assert.Equal(ShipmentStatus.InTransit, sent.Status);
        Assert.Equal(ShipmentStatus.Delivered, completed.Status);
        Assert.Equal(TestData.Start.AddMinutes(30), completed.DeliveredAtUtc);
        Assert.NotSame(sent, completed);
        Assert.Null(sent.DeliveredAtUtc);
        Assert.Empty(shipments);
        Assert.Same(order, Assert.Single(orders));
    }

    [Theory]
    [InlineData("missing", "C-01", "ORDER_NOT_FOUND")]
    [InlineData("ORD-1", "missing", "COURIER_NOT_FOUND")]
    public void MissingLookup_SkipsPricingAndTravelCalculation(string orderId, string courierId, string code)
    {
        int pricingCalls = 0;
        int nativeCalls = 0;
        var workflow = new DeliveryWorkflow(new NativeTravelTime((_, _) => { nativeCalls++; return 30; }));

        var result = workflow.Dispatch([TestData.Order()], [TestData.Courier()], [], orderId, courierId,
            _ => { pricingCalls++; return 100; }, TestData.Start);

        Assert.Equal(code, Assert.IsType<Result<Shipment>.Failure>(result).Error.Code);
        Assert.Equal(0, pricingCalls);
        Assert.Equal(0, nativeCalls);
    }

    [Fact]
    public void OutsideRange_SkipsPricing()
    {
        var order = TestData.Order(distance: 101);
        int calls = 0;
        var result = new DeliveryWorkflow().Dispatch([order], [TestData.Courier()], [],
            order.Id, "C-01", _ => { calls++; return 100; }, TestData.Start);

        Assert.Equal("OUTSIDE_COURIER_RANGE", Assert.IsType<Result<Shipment>.Failure>(result).Error.Code);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Capacity_AccountsForOtherOrdersAndIsReleasedAfterCompletion()
    {
        var first = TestData.Order(weight: 20);
        var second = TestData.Order("ORD-2", weight: 15);
        var courier = TestData.Courier();
        var active = TestData.Dispatch(first, courier);
        var workflow = new DeliveryWorkflow(new NativeTravelTime((_, _) => 30));

        var rejected = workflow.Dispatch([first, second], [courier], [active], second.Id,
            courier.Id, Policies.ModePricing, TestData.Start);
        Assert.Equal("WEIGHT_LIMIT", Assert.IsType<Result<Shipment>.Failure>(rejected).Error.Code);

        var delivered = Assert.IsType<Result<Shipment>.Success>(active.Complete(TestData.Start.AddMinutes(30))).Value;
        var allowed = workflow.Dispatch([first, second], [courier], [delivered], second.Id,
            courier.Id, Policies.ModePricing, TestData.Start.AddMinutes(31));
        Assert.IsType<Result<Shipment>.Success>(allowed);
        Assert.Equal(ShipmentStatus.InTransit, active.Status);
    }

    [Fact]
    public void ExactCapacityAndRange_AreAccepted()
    {
        var order = TestData.Order(weight: 30, distance: 100);
        Assert.IsType<Result<DeliveryOrder>.Success>(
            DispatchRules.Check(order, TestData.Courier(), [order], []));
    }

    [Fact]
    public void DuplicateDispatch_IsRejectedForActiveAndDeliveredOrders()
    {
        var order = TestData.Order();
        var courier = TestData.Courier();
        var active = TestData.Dispatch(order, courier);
        var delivered = Assert.IsType<Result<Shipment>.Success>(active.Complete(TestData.Start.AddHours(1))).Value;

        foreach (var shipment in new[] { active, delivered })
        {
            var result = DispatchRules.Check(order, courier, [order], [shipment]);
            Assert.Equal("ORDER_ALREADY_DISPATCHED", Assert.IsType<Result<DeliveryOrder>.Failure>(result).Error.Code);
        }
    }

    [Fact]
    public void CompletionBeforeDispatch_IsRejected()
    {
        var result = DeliveryWorkflow.Complete([], "SHP-ORD-1", TestData.Start);
        Assert.Equal("SHIPMENT_NOT_FOUND", Assert.IsType<Result<Shipment>.Failure>(result).Error.Code);
    }

    [Fact]
    public void CompletionBeforeStart_PreservesActiveShipment()
    {
        var active = TestData.Dispatch(TestData.Order(), TestData.Courier());
        var result = active.Complete(TestData.Start.AddMinutes(-1));

        Assert.Equal("INVALID_DELIVERY_TIME", Assert.IsType<Result<Shipment>.Failure>(result).Error.Code);
        Assert.Equal(ShipmentStatus.InTransit, active.Status);
        Assert.Null(active.DeliveredAtUtc);
    }

    [Fact]
    public void RepeatedCompletion_PreservesOriginalDeliveryTime()
    {
        var active = TestData.Dispatch(TestData.Order(), TestData.Courier());
        var delivered = Assert.IsType<Result<Shipment>.Success>(active.Complete(TestData.Start.AddMinutes(30))).Value;
        var repeated = Assert.IsType<Result<Shipment>.Success>(delivered.Complete(TestData.Start.AddHours(2))).Value;

        Assert.Same(delivered, repeated);
        Assert.Equal(TestData.Start.AddMinutes(30), repeated.DeliveredAtUtc);
    }

    [Fact]
    public void NativeFailure_PropagatesWithoutCreatingShipment()
    {
        var workflow = new DeliveryWorkflow(new NativeTravelTime((_, _) => throw new DllNotFoundException()));
        var result = workflow.Dispatch([TestData.Order()], [TestData.Courier()], [],
            "ORD-1", "C-01", Policies.ModePricing, TestData.Start);

        Assert.Equal("NATIVE_LIBRARY_NOT_FOUND", Assert.IsType<Result<Shipment>.Failure>(result).Error.Code);
    }

    [Theory]
    [InlineData(DeliveryMode.Standard, 1425)]
    [InlineData(DeliveryMode.Express, 2850)]
    [InlineData(DeliveryMode.Priority, 4275)]
    public void ModePricing_UsesModeSpecificRules(DeliveryMode mode, int expected)
    {
        Assert.Equal(expected, Policies.ModePricing(TestData.Order(mode: mode)));
    }

    [Fact]
    public void CustomPricing_ChangesTheDispatchedCost()
    {
        var workflow = new DeliveryWorkflow(new NativeTravelTime((_, _) => 30));
        var policy = Policies.CreatePricing(new PricingRules(100, 2, 3));
        var result = workflow.Dispatch([TestData.Order()], [TestData.Courier()], [],
            "ORD-1", "C-01", policy, TestData.Start);

        Assert.Equal(155m, Assert.IsType<Result<Shipment>.Success>(result).Value.Cost);
    }
}
