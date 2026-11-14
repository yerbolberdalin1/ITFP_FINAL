using DeliveryPipeline;
using Xunit;

namespace DeliveryPipeline.Tests;

public class ReportWorkflowTests
{
    [Fact]
    public void Run_NormalInput_ProducesBothReportsAndExportsCsv()
    {
        using var files = new TemporaryFiles();
        string inputPath = files.PathFor("orders.json");
        string outputPath = files.PathFor("dispatch.csv");
        File.WriteAllText(inputPath, """[{"Id":"ORD-1","Destination":"Almaty","DistanceKm":20,"WeightKg":5,"Mode":"Standard"}]""");

        var workflow = new ReportWorkflow();
        var result = workflow.Run(inputPath, outputPath, 30m, 40, Policies.StandardPricing);

        var output = Assert.IsType<Result<ReportSet>.Success>(result).Value;
        Assert.Single(output.Dispatch);
        Assert.Single(output.Totals);
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public void Run_MissingOrderFile_ReturnsFileNotFound()
    {
        using var files = new TemporaryFiles();
        string inputPath = files.PathFor("missing.json");
        string outputPath = files.PathFor("dispatch.csv");

        var workflow = new ReportWorkflow();
        var result = workflow.Run(inputPath, outputPath, 30m, 40, Policies.StandardPricing);

        var error = Assert.IsType<Result<ReportSet>.Failure>(result).Error;
        Assert.Equal("FILE_NOT_FOUND", error.Code);
        Assert.False(File.Exists(outputPath));
    }

    [Fact]
    public void Run_InvalidVehicleCapacity_FailsBeforeProcessing()
    {
        using var files = new TemporaryFiles();
        string inputPath = files.PathFor("orders.json");
        string outputPath = files.PathFor("dispatch.csv");
        File.WriteAllText(inputPath, """[{"Id":"ORD-1","Destination":"Almaty","DistanceKm":20,"WeightKg":5,"Mode":"Standard"}]""");

        var workflow = new ReportWorkflow();
        var result = workflow.Run(inputPath, outputPath, 0m, 40, Policies.StandardPricing);

        var error = Assert.IsType<Result<ReportSet>.Failure>(result).Error;
        Assert.Equal("INVALID_CAPACITY", error.Code);
        Assert.False(File.Exists(outputPath));
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
