using DeliveryPipeline;

namespace DeliveryPipeline.Tests;

public class CouriersFileTests
{
    [Fact]
    public void Load_ValidData_TrimsIdAndReturnsReadOnlyCollection()
    {
        using var files = new TemporaryDirectory();
        string path = files.Write("couriers.json", """
            [{"Id":" C-01 ","Name":" Alibek ","CapacityKg":30,"MaxDistanceKm":100,"SpeedKmh":40}]
            """);

        var couriers = Assert.IsType<Result<IReadOnlyList<Courier>>.Success>(CouriersFile.Load(path)).Value;
        var courier = Assert.Single(couriers);
        Assert.Equal("C-01", courier.Id);
        Assert.Equal("Alibek", courier.Name);
        Assert.True(Assert.IsAssignableFrom<IList<Courier>>(couriers).IsReadOnly);
    }

    [Theory]
    [InlineData("[")]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("[{\"Id\":\"C-01\"}]")]
    [InlineData("[{\"Id\":\"C-01\",\"Name\":\"Alibek\",\"CapacityKg\":0,\"MaxDistanceKm\":100,\"SpeedKmh\":40}]")]
    [InlineData("[{\"Id\":\"C-01\",\"Name\":\"Alibek\",\"CapacityKg\":30,\"MaxDistanceKm\":100,\"SpeedKmh\":40},{\"Id\":\"c-01\",\"Name\":\"Dana\",\"CapacityKg\":30,\"MaxDistanceKm\":100,\"SpeedKmh\":40}]")]
    public void Load_MalformedOrInvalidData_PreservesFile(string json)
    {
        using var files = new TemporaryDirectory();
        string path = files.Write("couriers.json", json);

        Assert.Equal("MALFORMED_DATA", Assert.IsType<Result<IReadOnlyList<Courier>>.Failure>(CouriersFile.Load(path)).Error.Code);
        Assert.Equal(json, File.ReadAllText(path));
    }

    [Fact]
    public void Load_MissingFile_ReturnsFailure()
    {
        using var files = new TemporaryDirectory();
        var result = CouriersFile.Load(files.PathFor("missing.json"));
        Assert.Equal("FILE_NOT_FOUND", Assert.IsType<Result<IReadOnlyList<Courier>>.Failure>(result).Error.Code);
    }

    [Theory]
    [InlineData(0, 100, 40)]
    [InlineData(501, 100, 40)]
    [InlineData(30, 0, 40)]
    [InlineData(30, 501, 40)]
    [InlineData(30, 100, 0)]
    [InlineData(30, 100, 121)]
    public void Create_InvalidCourierRules_ReturnsFailure(int capacity, int range, int speed)
    {
        Assert.IsType<Result<Courier>.Failure>(Courier.Create("C-01", "Alibek", capacity, range, speed));
    }
}
