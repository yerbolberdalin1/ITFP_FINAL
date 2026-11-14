using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeliveryPipeline;

internal static class JsonFile
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public static Result<T> Load<T>(string path) where T : notnull
    {
        try
        {
            var data = JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
            return data is null
                ? Result<T>.Fail(new Error("MALFORMED_DATA", $"File '{path}' cannot contain null."))
                : Result<T>.Ok(data);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return Result<T>.Fail(new Error("FILE_NOT_FOUND", $"Data file '{path}' was not found."));
        }
        catch (JsonException exception)
        {
            return Result<T>.Fail(new Error("MALFORMED_DATA", $"Invalid JSON in '{path}': {exception.Message}"));
        }
        catch (Exception exception) when (IsFileError(exception))
        {
            return Result<T>.Fail(new Error("FILE_READ_ERROR", $"Cannot read '{path}': {exception.Message}"));
        }
    }

    public static Result<string> Save<T>(string path, T data) where T : notnull
    {
        string? temporaryPath = null;

        try
        {
            string fullPath = Path.GetFullPath(path);
            temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(data, Options));
            File.Move(temporaryPath, fullPath, overwrite: true);
            return Result<string>.Ok(fullPath);
        }
        catch (Exception exception) when (IsFileError(exception))
        {
            string message = $"Cannot save '{path}': {exception.Message}";

            if (temporaryPath is not null)
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (Exception cleanupError) when (IsFileError(cleanupError))
                {
                    message += $" Cannot remove temporary file '{temporaryPath}': {cleanupError.Message}";
                }
            }

            return Result<string>.Fail(new Error("FILE_WRITE_ERROR", message));
        }
    }

    private static bool IsFileError(Exception exception) => exception is IOException
        or UnauthorizedAccessException or ArgumentException or NotSupportedException;
}
