using DeliveryPipeline;

namespace DeliveryPipeline.Tests;

internal static class TestData
{
    public static readonly DateTimeOffset Start = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

    public static DeliveryOrder Order(string id = "ORD-1", decimal weight = 5, decimal distance = 20,
        DeliveryMode mode = DeliveryMode.Standard) => Assert.IsType<OrderResult.Valid>(
            DeliveryOrder.Create(id, "Almaty", distance, weight, mode)).Order;

    public static Courier Courier(string id = "C-01", decimal capacity = 30, decimal range = 100) =>
        Assert.IsType<Result<Courier>.Success>(
            DeliveryPipeline.Courier.Create(id, "Alibek", capacity, range, 40)).Value;

    public static Shipment Dispatch(DeliveryOrder order, Courier courier)
    {
        var workflow = new DeliveryWorkflow(new NativeTravelTime((_, _) => 30));
        return Assert.IsType<Result<Shipment>.Success>(workflow.Dispatch(
            [order], [courier], [], order.Id, courier.Id, Policies.ModePricing, Start)).Value;
    }
}

internal sealed class TemporaryDirectory : IDisposable
{
    private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory("delivery-final-");

    public string PathFor(string filename) => Path.Combine(directory.FullName, filename);

    public string Write(string filename, string content)
    {
        string path = PathFor(filename);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose() => directory.Delete(true);
}
