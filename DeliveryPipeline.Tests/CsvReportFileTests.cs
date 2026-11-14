using System.Globalization;
using DeliveryPipeline;
using Xunit;

namespace DeliveryPipeline.Tests;

public class CsvReportFileTests
{
    [Fact]
    public void Save_EscapesCsvFieldsAndUsesInvariantMoneyFormatting()
    {
        using var files = new TemporaryFiles();
        string path = files.PathFor("dispatch.csv");
        var rows = new[]
        {
            new DispatchRow("ORD-1", "North, \"Dock\" Gate", DeliveryMode.Standard, 1425.5m, 30)
        };
        var previousCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

            Assert.IsType<Result<string>.Success>(new CsvReportFile().Save(path, rows));

            string content = File.ReadAllText(path);
            Assert.Contains("OrderId,Destination,Mode,Cost,EstimatedMinutes", content);
            Assert.Contains("1425.50", content);
            Assert.Contains("\"North, \"\"Dock\"\" Gate\"", content);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void Save_Success_DisposesWriterAndCreatesFile()
    {
        using var files = new TemporaryFiles();
        string path = files.PathFor("dispatch.csv");
        var rows = new[]
        {
            new DispatchRow("ORD-1", "Almaty", DeliveryMode.Standard, 1425m, 30)
        };

        var result = new CsvReportFile().Save(path, rows);

        Assert.IsType<Result<string>.Success>(result);
        Assert.True(File.Exists(path));
        using var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.NotNull(stream);
    }

    [Fact]
    public void Save_InvalidDirectory_ReturnsExportError()
    {
        string invalidPath = "/non_existent_folder_999/dispatch.csv";
        var rows = new[]
        {
            new DispatchRow("ORD-1", "Almaty", DeliveryMode.Standard, 1425m, 30)
        };

        var result = new CsvReportFile().Save(invalidPath, rows);

        var error = Assert.IsType<Result<string>.Failure>(result).Error;
        Assert.Equal("EXPORT_ERROR", error.Code);
    }

    [Fact]
    public void Save_WriterFailure_PreservesExistingReportAndRemovesTemporaryFile()
    {
        using var files = new TemporaryDirectory();
        string path = files.Write("dispatch.csv", "previous report");
        var reportFile = new CsvReportFile(temporaryPath => new FailingWriter(temporaryPath));

        var result = reportFile.Save(path,
            [new DispatchRow("ORD-1", "Almaty", DeliveryMode.Standard, 1425, 30)]);

        Assert.Equal("EXPORT_ERROR", Assert.IsType<Result<string>.Failure>(result).Error.Code);
        Assert.Equal("previous report", File.ReadAllText(path));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp"));
    }

    private sealed class FailingWriter(string path) : StreamWriter(path)
    {
        public override void WriteLine(string? value) => throw new IOException("Simulated write failure.");
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
