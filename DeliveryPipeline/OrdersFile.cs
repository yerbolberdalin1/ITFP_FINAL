using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeliveryPipeline;

public static class OrdersFile
{
    private static readonly JsonSerializerOptions Options = new()
    {
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        Converters = { new JsonStringEnumConverter<DeliveryMode>(allowIntegerValues: false) }
    };

    public static Result<IReadOnlyList<DeliveryOrder>> Load(string path)
    {
        try
        {
            var data = JsonSerializer.Deserialize<OrderData?[]>(File.ReadAllText(path), Options);
            if (data is null)
                return Malformed(path, "The root value must be an array of orders.");

            var orders = new List<DeliveryOrder>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < data.Length; i++)
            {
                var item = data[i];
                if (item is null)
                    return Malformed(path, $"Record {i + 1} must be an order object.");

                switch (DeliveryOrder.Create(item.Id, item.Destination,
                    item.DistanceKm, item.WeightKg, item.Mode))
                {
                    case OrderResult.Invalid invalid:
                        return Malformed(path, $"Record {i + 1}: {invalid.Reason}");
                    case OrderResult.Valid valid:
                        if (!ids.Add(valid.Order.Id))
                            return Malformed(path,
                                $"Record {i + 1}: duplicate order ID '{valid.Order.Id}'.");

                        orders.Add(valid.Order);
                        break;
                }
            }

            return Result<IReadOnlyList<DeliveryOrder>>.Ok(orders.AsReadOnly());
        }
        catch (FileNotFoundException)
        {
            return Missing(path);
        }
        catch (DirectoryNotFoundException)
        {
            return Missing(path);
        }
        catch (JsonException exception)
        {
            return Malformed(path, $"Invalid JSON at {exception.Path ?? "$"}: {exception.Message}");
        }
        catch (UnauthorizedAccessException)
        {
            return Result<IReadOnlyList<DeliveryOrder>>.Fail(
                new Error("FILE_READ_ERROR", $"Access to '{path}' is denied."));
        }
        catch (IOException exception)
        {
            return Result<IReadOnlyList<DeliveryOrder>>.Fail(
                new Error("FILE_READ_ERROR", $"Cannot read '{path}': {exception.Message}"));
        }
    }

    private static Result<IReadOnlyList<DeliveryOrder>> Missing(string path)
    {
        return Result<IReadOnlyList<DeliveryOrder>>.Fail(
            new Error("FILE_NOT_FOUND", $"Orders file '{path}' was not found."));
    }

    private static Result<IReadOnlyList<DeliveryOrder>> Malformed(string path, string reason)
    {
        return Result<IReadOnlyList<DeliveryOrder>>.Fail(
            new Error("MALFORMED_DATA", $"Invalid orders file '{path}': {reason}"));
    }

    private sealed record OrderData(
        string Id,
        string Destination,
        decimal DistanceKm,
        decimal WeightKg,
        DeliveryMode Mode);
}
