namespace DeliveryPipeline;

public static class CouriersFile
{
    public static Result<IReadOnlyList<Courier>> Load(string path)
    {
        return JsonFile.Load<CourierData?[]>(path).Bind(data =>
        {
            var couriers = new List<Courier>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < data.Length; i++)
            {
                var item = data[i];
                if (item is null)
                    return Malformed(path, $"Record {i + 1} must be a courier object.");

                var result = Courier.Create(item.Id, item.Name, item.CapacityKg, item.MaxDistanceKm, item.SpeedKmh);
                if (result is Result<Courier>.Failure failure)
                    return Malformed(path, $"Record {i + 1}: {failure.Error.Message}");

                if (result is Result<Courier>.Success success)
                {
                    if (!ids.Add(success.Value.Id))
                        return Malformed(path, $"Duplicate courier ID '{success.Value.Id}'.");

                    couriers.Add(success.Value);
                }
            }

            return Result<IReadOnlyList<Courier>>.Ok(couriers.AsReadOnly());
        });
    }

    private static Result<IReadOnlyList<Courier>> Malformed(string path, string reason) =>
        Result<IReadOnlyList<Courier>>.Fail(new Error("MALFORMED_DATA", $"Invalid couriers file '{path}': {reason}"));

    private sealed record CourierData(string Id, string Name, decimal CapacityKg, decimal MaxDistanceKm, int SpeedKmh);
}
