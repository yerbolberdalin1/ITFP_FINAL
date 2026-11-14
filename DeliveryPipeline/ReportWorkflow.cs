namespace DeliveryPipeline;

public sealed record ReportSet(
    IReadOnlyList<DispatchRow> Dispatch,
    IReadOnlyList<ModeTotal> Totals);

public sealed class ReportWorkflow
{
    private readonly NativeTravelTime travelTime;
    private readonly CsvReportFile reportFile;

    public ReportWorkflow(NativeTravelTime? travelTime = null, CsvReportFile? reportFile = null)
    {
        this.travelTime = travelTime ?? new NativeTravelTime();
        this.reportFile = reportFile ?? new CsvReportFile();
    }

    public Result<ReportSet> Run(string inputPath, string outputPath,
        decimal capacity, int speedKmh, Func<DeliveryOrder, decimal> pricing,
        decimal minimumCost = 0)
    {
        return OrdersFile.Load(inputPath)
            .Bind(orders => Run(orders, outputPath, capacity, speedKmh, pricing, minimumCost));
    }

    public Result<ReportSet> Run(IReadOnlyList<DeliveryOrder> orders, string outputPath,
        decimal capacity, int speedKmh, Func<DeliveryOrder, decimal> pricing,
        decimal minimumCost = 0)
    {
        return Create(orders, capacity, speedKmh, pricing, minimumCost)
            .Bind(reports => reportFile.Save(outputPath, reports.Dispatch).Map(_ => reports));
    }

    private Result<ReportSet> Create(IReadOnlyList<DeliveryOrder> orders,
        decimal capacity, int speedKmh, Func<DeliveryOrder, decimal> pricing,
        decimal minimumCost)
    {
        if (capacity <= 0)
            return Result<ReportSet>.Fail(new Error(
                "INVALID_CAPACITY", "Vehicle capacity must be greater than 0 kg."));

        if (speedKmh < 1 || speedKmh > 120)
            return Result<ReportSet>.Fail(new Error(
                "INVALID_SPEED", "Travel speed must be between 1 and 120 km/h."));

        if (minimumCost < 0)
            return Result<ReportSet>.Fail(new Error(
                "INVALID_REPORT_FILTER", "Minimum report cost cannot be negative."));

        var pricedOrders = orders.ForVehicle(capacity).ToPricedOrders(pricing).ToArray();
        var items = new List<ReportItem>(pricedOrders.Length);

        foreach (var priced in pricedOrders)
        {
            var estimate = travelTime.Estimate(priced.Order.DistanceKm, speedKmh);

            if (estimate is Result<int>.Failure failure)
                return Result<ReportSet>.Fail(new Error(failure.Error.Code,
                    $"Order '{priced.Order.Id}': {failure.Error.Message}"));

            if (estimate is Result<int>.Success success)
                items.Add(new ReportItem(priced.Order, priced.Cost, success.Value));
        }

        var reports = new ReportSet(
            DeliveryReports.Dispatch(items, minimumCost),
            DeliveryReports.TotalsByMode(items));

        return Result<ReportSet>.Ok(reports);
    }
}
