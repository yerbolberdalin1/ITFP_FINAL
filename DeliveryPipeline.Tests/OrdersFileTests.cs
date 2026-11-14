using DeliveryPipeline;
using Xunit;

namespace DeliveryPipeline.Tests;

public class OrdersFileTests
{
    [Fact]
    public void Load_ValidFile_ReturnsOrders()
    {
        using var files = new TemporaryFiles();
        string path = files.PathFor("orders.json");
        File.WriteAllText(path, """[{"Id":"ORD-1","Destination":"Almaty","DistanceKm":10,"WeightKg":2,"Mode":"Standard"}]""");

        var result = OrdersFile.Load(path);

        var orders = Assert.IsType<Result<IReadOnlyList<DeliveryOrder>>.Success>(result).Value;
        Assert.Single(orders);
        Assert.Equal("ORD-1", orders[0].Id);
    }

    [Fact]
    public void Load_MissingFile_ReturnsFileNotFound()
    {
        using var files = new TemporaryFiles();
        string path = files.PathFor("nonexistent.json");

        var result = OrdersFile.Load(path);

        var error = Assert.IsType<Result<IReadOnlyList<DeliveryOrder>>.Failure>(result).Error;
        Assert.Equal("FILE_NOT_FOUND", error.Code);
    }

    [Fact]
    public void Load_MalformedJson_ReturnsMalformedData()
    {
        using var files = new TemporaryFiles();
        string path = files.PathFor("broken.json");
        File.WriteAllText(path, "{ broken json }");

        var result = OrdersFile.Load(path);

        var error = Assert.IsType<Result<IReadOnlyList<DeliveryOrder>>.Failure>(result).Error;
        Assert.Equal("MALFORMED_DATA", error.Code);
    }

    private sealed class TemporaryFiles : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "DeliveryPipelineTests_" + Guid.NewGuid());

        public TemporaryFiles() => Directory.CreateDirectory(_dir);

        public string PathFor(string filename) => Path.Combine(_dir, filename);

        public void Dispose()
        {
            if (Directory.Exists(_dir))
            {
                try { Directory.Delete(_dir, true); } catch { }
            }
        }
    }
}
