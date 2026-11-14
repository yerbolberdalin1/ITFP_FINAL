using DeliveryPipeline;

namespace DeliveryPipeline.Tests;

public class ShipmentPersistenceTests
{
    [Fact]
    public void DispatchSaveReloadComplete_PreservesTheWholeLifecycle()
    {
        using var files = new TemporaryDirectory();
        var order = TestData.Order();
        var courier = TestData.Courier();
        string path = files.PathFor("shipments.json");
        var sent = TestData.Dispatch(order, courier);

        Assert.IsType<Result<string>.Success>(ShipmentsFile.Save(path, [sent]));
        var active = Assert.IsType<Result<IReadOnlyList<Shipment>>.Success>(
            ShipmentsFile.Load(path, [order], [courier])).Value;
        Assert.Equal(ShipmentStatus.InTransit, Assert.Single(active).Status);

        var completed = Assert.IsType<Result<Shipment>.Success>(
            DeliveryWorkflow.Complete(active, sent.Id, TestData.Start.AddMinutes(30))).Value;
        Assert.IsType<Result<string>.Success>(ShipmentsFile.Save(path, [completed]));

        var loaded = Assert.IsType<Result<IReadOnlyList<Shipment>>.Success>(
            ShipmentsFile.Load(path, [order], [courier])).Value;
        var delivered = Assert.Single(loaded);
        Assert.Equal(ShipmentStatus.Delivered, delivered.Status);
        Assert.Equal(sent.Cost, delivered.Cost);
        Assert.Equal(sent.EstimatedMinutes, delivered.EstimatedMinutes);
        Assert.Equal(TestData.Start.AddMinutes(30), delivered.DeliveredAtUtc);
        Assert.Equal(ShipmentStatus.InTransit, sent.Status);
    }

    [Fact]
    public void Load_MissingShipments_ReturnsFailure()
    {
        using var files = new TemporaryDirectory();
        var result = ShipmentsFile.Load(files.PathFor("missing.json"), [], []);
        Assert.Equal("FILE_NOT_FOUND", Assert.IsType<Result<IReadOnlyList<Shipment>>.Failure>(result).Error.Code);
    }

    [Theory]
    [InlineData("[")]
    [InlineData("[null]")]
    [InlineData("[{\"Id\":\"SHP-1\"}]")]
    [InlineData("[{\"Id\":\"SHP-1\",\"OrderId\":\"missing\",\"CourierId\":\"C-01\",\"Cost\":1425,\"EstimatedMinutes\":30,\"DispatchedAtUtc\":\"2026-11-12T10:00:00Z\",\"DeliveredAtUtc\":null}]")]
    [InlineData("[{\"Id\":\"SHP-1\",\"OrderId\":\"ORD-1\",\"CourierId\":\"C-01\",\"Cost\":1425,\"EstimatedMinutes\":30,\"DispatchedAtUtc\":\"2026-11-12T10:00:00Z\",\"DeliveredAtUtc\":\"2026-11-12T09:59:59Z\"}]")]
    public void Load_InvalidShipments_ReturnsFailureAndPreservesSource(string json)
    {
        using var files = new TemporaryDirectory();
        string path = files.Write("shipments.json", json);

        var result = ShipmentsFile.Load(path, [TestData.Order()], [TestData.Courier()]);

        Assert.Equal("MALFORMED_DATA", Assert.IsType<Result<IReadOnlyList<Shipment>>.Failure>(result).Error.Code);
        Assert.Equal(json, File.ReadAllText(path));
    }

    [Fact]
    public void Load_OverloadedCourier_RejectsCombinedWeight()
    {
        using var files = new TemporaryDirectory();
        var first = TestData.Order(weight: 20);
        var second = TestData.Order("ORD-2", weight: 20);
        var courier = TestData.Courier();
        string path = files.PathFor("shipments.json");
        Assert.IsType<Result<string>.Success>(ShipmentsFile.Save(path,
            [TestData.Dispatch(first, courier), TestData.Dispatch(second, courier)]));

        var result = ShipmentsFile.Load(path, [first, second], [courier]);

        Assert.Equal("MALFORMED_DATA", Assert.IsType<Result<IReadOnlyList<Shipment>>.Failure>(result).Error.Code);
    }

    [Fact]
    public void Load_DuplicateOrder_IsRejected()
    {
        using var files = new TemporaryDirectory();
        var order = TestData.Order();
        var courier = TestData.Courier();
        var shipment = TestData.Dispatch(order, courier);
        string path = files.PathFor("shipments.json");
        Assert.IsType<Result<string>.Success>(ShipmentsFile.Save(path, [shipment, shipment]));

        var result = ShipmentsFile.Load(path, [order], [courier]);

        Assert.Equal("MALFORMED_DATA", Assert.IsType<Result<IReadOnlyList<Shipment>>.Failure>(result).Error.Code);
    }

    [Fact]
    public void Load_EmptyArray_IsAValidInitialState()
    {
        using var files = new TemporaryDirectory();
        var result = ShipmentsFile.Load(files.Write("shipments.json", "[]"), [], []);
        Assert.Empty(Assert.IsType<Result<IReadOnlyList<Shipment>>.Success>(result).Value);
    }

    [Fact]
    public void Save_UnwritableTarget_PreservesExistingData()
    {
        using var files = new TemporaryDirectory();
        string original = files.Write("existing.json", "[]");
        string missingPath = files.PathFor("missing/shipments.json");

        var result = ShipmentsFile.Save(missingPath, [TestData.Dispatch(TestData.Order(), TestData.Courier())]);

        Assert.Equal("FILE_WRITE_ERROR", Assert.IsType<Result<string>.Failure>(result).Error.Code);
        Assert.Equal("[]", File.ReadAllText(original));
        Assert.False(File.Exists(missingPath));
    }

    [Fact]
    public void LoadData_InvalidCouriers_StopsBeforeShipmentsFile()
    {
        using var files = new TemporaryDirectory();
        files.Write("orders.json", "[]");
        files.Write("couriers.json", "[");

        var result = DeliveryData.Load(Path.GetDirectoryName(files.PathFor("orders.json"))!);

        Assert.Equal("MALFORMED_DATA", Assert.IsType<Result<DeliveryData>.Failure>(result).Error.Code);
        Assert.Contains("couriers.json", Assert.IsType<Result<DeliveryData>.Failure>(result).Error.Message);
    }
}
