using System.Globalization;
using DeliveryPipeline;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string dataDirectory = Path.Combine(Environment.CurrentDirectory, "sample-data");
string[] command = args;

if (args.Length > 0 && args[0] == "--data")
{
    if (args.Length < 2)
    {
        Console.WriteLine("Usage: --data <directory> [command]");
        return 1;
    }

    dataDirectory = args[1];
    command = args.Skip(2).ToArray();
}

if (command.Length > 0)
    return Execute(dataDirectory, command);

Console.WriteLine("DeliveryPipeline");
Console.WriteLine($"Data: {Path.GetFullPath(dataDirectory)}");

while (true)
{
    Console.WriteLine("\n1. Orders, couriers and shipments\n2. Calculate quote\n3. Dispatch order");
    Console.WriteLine("4. Complete delivery\n5. Reports and CSV\n6. Demonstration\n7. Batch quotes\n0. Exit");
    Console.Write("Choose: ");
    string? choice = Console.ReadLine();
    if (choice is null or "0")
        return 0;

    string[] selected = choice switch
    {
        "1" => ["list"],
        "2" => ["quote", Read("Order ID"), Read("Vehicle capacity (kg)"), Read("Pricing: mode, standard or express", "mode")],
        "3" => ["dispatch", Read("Order ID"), Read("Courier ID"), Read("Pricing: mode, standard or express", "mode")],
        "4" => ["complete", Read("Shipment ID")],
        "5" => ["report", Read("CSV path", "dispatch-report.csv"), Read("Capacity (kg)", "30"), Read("Speed (km/h)", "40"), Read("Minimum quote cost", "0")],
        "6" => ["demo"],
        "7" => ["quotes", Read("Vehicle capacity (kg)", "30"), Read("Pricing: mode, standard or express", "mode")],
        _ => ["help"]
    };

    Execute(dataDirectory, selected);
}

static string Read(string label, string fallback = "")
{
    Console.Write($"{label}{(fallback.Length > 0 ? $" [{fallback}]" : "")}: ");
    string? value = Console.ReadLine();
    return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}

static int Execute(string directory, string[] command)
{
    string action = command[0].ToLowerInvariant();
    if (action is "help" or "--help" or "-h")
    {
        PrintHelp();
        return 0;
    }

    var loaded = DeliveryData.Load(directory);
    if (loaded is Result<DeliveryData>.Failure failure)
        return Print(Result<string>.Fail(failure.Error), value => value);

    var data = ((Result<DeliveryData>.Success)loaded).Value;
    string shipmentsPath = Path.Combine(directory, "shipments.json");

    switch (action)
    {
        case "list" when command.Length == 1:
            PrintData(data);
            return 0;

        case "quote" when command.Length is 3 or 4:
            if (!decimal.TryParse(command[2], out decimal capacity))
                return InputError("Capacity must be a number.");

            return Print(Pricing(command.Length == 4 ? command[3] : "mode")
                .Bind(pricing => QuoteWorkflow.CreateQuote(data.Orders, command[1], capacity, pricing)),
                quote => $"{quote.OrderId} to {quote.Destination}: {quote.Cost:F2} KZT");

        case "quotes" when command.Length is 2 or 3:
            if (!decimal.TryParse(command[1], out decimal batchCapacity) || batchCapacity <= 0)
                return InputError("Vehicle capacity must be a positive number.");

            var batch = Pricing(command.Length == 3 ? command[2] : "mode")
                .Map(pricing => Pipeline.ExecutePipeline(data.Orders, batchCapacity, pricing));
            if (batch is Result<DeliverySummary>.Failure batchFailure)
                return Print(Result<string>.Fail(batchFailure.Error), value => value);

            var summary = ((Result<DeliverySummary>.Success)batch).Value;
            Console.WriteLine($"{summary.TotalOrders} orders | total: {summary.TotalCost:F2} KZT | average: {summary.AverageCost:F2} KZT");
            foreach (var quote in summary.Quotes)
                Console.WriteLine($"{quote.OrderId} | {quote.Destination} | {quote.Cost:F2} KZT");
            return 0;

        case "dispatch" when command.Length is 3 or 4:
            return Print(Pricing(command.Length == 4 ? command[3] : "mode")
                .Bind(pricing => new DeliveryWorkflow().Dispatch(data.Orders, data.Couriers,
                    data.Shipments, command[1], command[2], pricing, DateTimeOffset.UtcNow))
                .Bind(shipment => ShipmentsFile.Save(shipmentsPath,
                    data.Shipments.Append(shipment).ToArray()).Map(_ => shipment)), FormatShipment);

        case "complete" when command.Length == 2:
            return Print(DeliveryWorkflow.Complete(data.Shipments, command[1], DateTimeOffset.UtcNow)
                .Bind(completed => ShipmentsFile.Save(shipmentsPath, data.Shipments
                    .Select(shipment => shipment.Id.Equals(completed.Id, StringComparison.OrdinalIgnoreCase)
                        ? completed : shipment).ToArray()).Map(_ => completed)), FormatShipment);

        case "report" when command.Length <= 5:
            string path = command.Length > 1 ? command[1] : "dispatch-report.csv";
            decimal reportCapacity = 30;
            int speed = 40;
            decimal minimumCost = 0;

            if (command.Length > 2 && !decimal.TryParse(command[2], out reportCapacity) ||
                command.Length > 3 && !int.TryParse(command[3], out speed) ||
                command.Length > 4 && !decimal.TryParse(command[4], out minimumCost))
                return InputError("Capacity, speed and minimum cost must be numbers.");

            var report = new ReportWorkflow().Run(data.Orders, path, reportCapacity, speed,
                Policies.ModePricing, minimumCost);
            if (report is Result<ReportSet>.Failure reportFailure)
                return Print(Result<string>.Fail(reportFailure.Error), value => value);

            PrintReports(((Result<ReportSet>.Success)report).Value, data);
            Console.WriteLine($"\nSaved quote report: {Path.GetFullPath(path)}");
            return 0;

        case "demo" when command.Length == 1:
            return Demonstrate(data);

        default:
            PrintHelp();
            return 1;
    }
}

static Result<Func<DeliveryOrder, decimal>> Pricing(string name)
{
    Func<DeliveryOrder, decimal>? policy = name.ToLowerInvariant() switch
    {
        "mode" => Policies.ModePricing,
        "standard" => Policies.StandardPricing,
        "express" => Policies.ExpressPricing,
        _ => null
    };

    return policy is null
        ? Result<Func<DeliveryOrder, decimal>>.Fail(new Error("INVALID_PRICING", "Pricing must be mode, standard or express."))
        : Result<Func<DeliveryOrder, decimal>>.Ok(policy);
}

static void PrintData(DeliveryData data)
{
    Console.WriteLine("\nOrders:");
    foreach (var order in data.Orders)
        Console.WriteLine($"{order.Id} | {order.Destination} | {order.DistanceKm} km | {order.WeightKg} kg | {order.Mode}");

    Console.WriteLine("\nCouriers:");
    foreach (var courier in data.Couriers)
        Console.WriteLine($"{courier.Id} | {courier.Name} | {courier.CapacityKg} kg | {courier.MaxDistanceKm} km | {courier.SpeedKmh} km/h");

    Console.WriteLine("\nShipments:");
    foreach (var shipment in data.Shipments)
        Console.WriteLine(FormatShipment(shipment));
    if (data.Shipments.Count == 0)
        Console.WriteLine("No shipments yet.");

    PrintWorkload(data);
}

static void PrintWorkload(DeliveryData data)
{
    Console.WriteLine("\nCourier workload and delivered revenue:");
    foreach (var row in DeliveryReports.CourierWorkload(data.Orders, data.Couriers, data.Shipments))
        Console.WriteLine($"{row.CourierId} | {row.Name} | active: {row.InTransit} | delivered: {row.Delivered} | {row.ActiveWeightKg} kg | {row.Revenue:F2} KZT");
}

static void PrintReports(ReportSet reports, DeliveryData data)
{
    Console.WriteLine("\nEligible order quotes:");
    foreach (var row in reports.Dispatch)
        Console.WriteLine($"{row.OrderId} | {row.Destination} | {row.Mode} | {row.Cost:F2} KZT | {row.EstimatedMinutes} min");

    Console.WriteLine("\nQuoted totals by mode (all eligible orders):");
    foreach (var row in reports.Totals)
        Console.WriteLine($"{row.Mode} | {row.OrderCount} orders | {row.TotalCost:F2} KZT");

    PrintWorkload(data);
}

static int Demonstrate(DeliveryData data)
{
    if (data.Orders.Count == 0 || data.Couriers.Count == 0)
        return InputError("The demonstration needs at least one order and one courier.");

    var pair = data.Orders.SelectMany(order => data.Couriers,
            (order, courier) => new { Order = order, Courier = courier })
        .FirstOrDefault(item => item.Order.WeightKg <= item.Courier.CapacityKg &&
            item.Order.DistanceKm <= item.Courier.MaxDistanceKm);

    if (pair is null)
        return InputError("No courier can carry the sample orders.");

    Console.WriteLine("Demonstration uses a separate shipment list in memory.");
    var summary = Pipeline.ExecutePipeline(data.Orders, pair.Courier.CapacityKg, Policies.ModePricing);
    Console.WriteLine($"Pipeline: {summary.TotalOrders} eligible orders, {summary.TotalCost:F2} KZT total.");

    Console.WriteLine("\nQuote:");
    Print(QuoteWorkflow.CreateQuote(data.Orders, pair.Order.Id, pair.Courier.CapacityKg, Policies.ModePricing),
        quote => $"{quote.OrderId}: {quote.Cost:F2} KZT");

    Console.WriteLine("\nComplete before dispatch:");
    Print(DeliveryWorkflow.Complete([], $"SHP-{pair.Order.Id}", DateTimeOffset.UtcNow), FormatShipment);

    var workflow = new DeliveryWorkflow();
    var sent = workflow.Dispatch(data.Orders, data.Couriers, [], pair.Order.Id,
        pair.Courier.Id, Policies.ModePricing, DateTimeOffset.UtcNow);
    Console.WriteLine("\nDispatch:");
    if (Print(sent, FormatShipment) != 0)
        return 1;

    var shipment = ((Result<Shipment>.Success)sent).Value;
    Console.WriteLine("\nDispatch again:");
    Print(workflow.Dispatch(data.Orders, data.Couriers, [shipment], pair.Order.Id,
        pair.Courier.Id, Policies.ModePricing, DateTimeOffset.UtcNow), FormatShipment);

    Console.WriteLine("\nComplete delivery:");
    if (Print(DeliveryWorkflow.Complete([shipment], shipment.Id, DateTimeOffset.UtcNow), FormatShipment) != 0)
        return 1;

    int pricingCalls = 0;
    var deferred = data.Orders.ForVehicle(pair.Courier.CapacityKg).ToPricedOrders(order =>
    {
        pricingCalls++;
        return Policies.ModePricing(order);
    });

    Console.WriteLine($"\nBefore enumeration: {pricingCalls} pricing calls.");
    var snapshot = deferred.ToArray();
    Console.WriteLine($"After ToArray: {pricingCalls} pricing calls for {snapshot.Length} orders.");
    Console.WriteLine($"Reused snapshot total: {snapshot.Sum(item => item.Cost):F2}; pricing calls: {pricingCalls}.");
    return 0;
}

static string FormatShipment(Shipment shipment) =>
    $"{shipment.Id} | {shipment.OrderId} | {shipment.CourierId} | {shipment.Status} | {shipment.Cost:F2} KZT | {shipment.EstimatedMinutes} min";

static int Print<T>(Result<T> result, Func<T, string> format) where T : notnull
{
    switch (result)
    {
        case Result<T>.Success success:
            Console.WriteLine(format(success.Value));
            return 0;
        case Result<T>.Failure failure:
            Console.WriteLine($"{failure.Error.Code}: {failure.Error.Message}");
            return 1;
        default:
            throw new InvalidOperationException("Unknown result case.");
    }
}

static int InputError(string message)
{
    Console.WriteLine($"INVALID_INPUT: {message}");
    return 1;
}

static void PrintHelp()
{
    Console.WriteLine("Commands:");
    Console.WriteLine("  list");
    Console.WriteLine("  quote <orderId> <capacityKg> [mode|standard|express]");
    Console.WriteLine("  quotes <capacityKg> [mode|standard|express]");
    Console.WriteLine("  dispatch <orderId> <courierId> [mode|standard|express]");
    Console.WriteLine("  complete <shipmentId>");
    Console.WriteLine("  report [csvPath] [capacityKg] [speedKmh] [minimumCost]");
    Console.WriteLine("  demo");
    Console.WriteLine("Use --data <directory> before a command to select another data directory.");
}
