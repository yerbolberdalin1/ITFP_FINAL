using System.Globalization;
using System.Text;

namespace DeliveryPipeline;

public sealed class CsvReportFile
{
    private readonly Func<string, TextWriter> openWriter;

    public CsvReportFile(Func<string, TextWriter>? openWriter = null)
    {
        this.openWriter = openWriter ?? (path => new StreamWriter(path, false, new UTF8Encoding(false)));
    }

    public Result<string> Save(string path, IReadOnlyList<DispatchRow> rows)
    {
        string? temporaryPath = null;

        try
        {
            string fullPath = Path.GetFullPath(path);
            temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";

            using (var writer = openWriter(temporaryPath))
            {
                writer.WriteLine("OrderId,Destination,Mode,Cost,EstimatedMinutes");

                foreach (var row in rows)
                {
                    writer.WriteLine(string.Join(",",
                        Escape(row.OrderId),
                        Escape(row.Destination),
                        row.Mode.ToString(),
                        row.Cost.ToString("F2", CultureInfo.InvariantCulture),
                        row.EstimatedMinutes.ToString(CultureInfo.InvariantCulture)));
                }
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
            return Result<string>.Ok(fullPath);
        }
        catch (Exception exception) when (IsFileError(exception))
        {
            string message = $"Cannot export report to '{path}': {exception.Message}";

            if (temporaryPath is not null)
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (Exception cleanupError) when (IsFileError(cleanupError))
                {
                    message += $" Temporary file '{temporaryPath}' could not be removed: {cleanupError.Message}";
                }
            }

            return Result<string>.Fail(new Error("EXPORT_ERROR", message));
        }
    }

    private static bool IsFileError(Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException
            or ArgumentException or NotSupportedException;
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";

        return value;
    }
}
