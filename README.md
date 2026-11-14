# DeliveryPipeline

Console application for calculating delivery prices, assigning orders to couriers and recording completed deliveries. It continues the DeliveryPipeline from Assignments 5–8.

## Requirements and setup

- .NET SDK 10.0.100 or later in the 10.0 feature bands. Verified with 10.0.301.
- macOS on Apple Silicon and the Xcode command line tools (`clang`). The travel-time library uses the macOS arm64 implementation from Assignment 8.
- NuGet access for the first restore of the xUnit test packages.

Run these commands from the project directory:

```sh
sh native/build.sh
dotnet build DeliveryPipeline.sln -c Release
dotnet test DeliveryPipeline.sln -c Release --no-build
dotnet run --project DeliveryPipeline/DeliveryPipeline.csproj -c Release --no-build
```

The last command opens the menu. `sh run.sh` builds the native library, builds the solution, runs the tests and opens the application.

## Commands

The menu and commands call the same application logic. Examples below assume the working directory contains `sample-data`.

```sh
dotnet run --project DeliveryPipeline -c Release --no-build -- list
dotnet run --project DeliveryPipeline -c Release --no-build -- quote ORD-101 30
dotnet run --project DeliveryPipeline -c Release --no-build -- quotes 30 standard
dotnet run --project DeliveryPipeline -c Release --no-build -- dispatch ORD-101 C-01
dotnet run --project DeliveryPipeline -c Release --no-build -- complete SHP-ORD-101
dotnet run --project DeliveryPipeline -c Release --no-build -- report dispatch-report.csv 30 40 0
dotnet run --project DeliveryPipeline -c Release --no-build -- demo
```

Use `--data <directory>` before the command to select a different directory:

```sh
dotnet run --project DeliveryPipeline -c Release --no-build -- --data ./working-data list
```

That directory must contain `orders.json`, `couriers.json` and `shipments.json`. Copy all three sample files to start a separate session. An empty shipment file contains `[]`; a missing shipment file produces a file error. Dispatch and completion update the selected `shipments.json`. The `demo` command keeps its shipment changes in memory.

`quote <orderId> <capacityKg> [pricing]` calculates a single quote. `quotes <capacityKg> [pricing]` runs the three-stage pipeline for the entire order list. Pricing is `mode`, `standard` or `express`; `mode` is the default.

`report [csvPath] [capacityKg] [speedKmh] [minimumCost]` defaults to `dispatch-report.csv`, 30 kg, 40 km/h and 0 KZT. Paths containing spaces should be quoted. All numeric input uses a decimal point. Commands return exit code 0 on success and 1 on failure.

## Workflows and rules

1. **Calculate quotes.** An order is found explicitly, capacity is checked, and a selected pricing function produces the quote. A missing order, invalid capacity or excess weight returns a failure. The batch version filters eligible orders, calculates prices and creates a sorted summary.
2. **Dispatch an order.** The application finds its order and courier, rejects duplicate dispatch, checks courier range and the combined weight of active shipments, calculates the price and travel time, then saves a new shipment. It reports success only after saving succeeds.
3. **Complete a delivery.** The application finds an existing shipment, checks that the delivery time is not before dispatch, creates a delivered shipment and saves the updated list. Completion before dispatch is rejected. Repeated completion preserves the original delivery time.
4. **Prepare and export reports.** Eligible orders are priced once, travel times are estimated, and two quote reports are calculated. CSV export returns a failure if the target cannot be written. Courier workload is calculated from saved shipment data.

An order can be dispatched once. Completing a shipment frees its weight capacity for other orders. Each order must have a positive distance up to 500 km and a positive weight up to 50 kg. Courier capacity is positive and at most 500 kg, range is positive and at most 500 km, and speed is between 1 and 120 km/h.

Prices in KZT:

| Policy | Base fee | Per km | Per kg |
| --- | ---: | ---: | ---: |
| Standard | 500 | 40 | 25 |
| Express | 1000 | 80 | 50 |
| Priority | 1500 | 120 | 75 |

`mode` selects a policy from the order's mode using pattern matching. Explicit `standard` and `express` apply that policy to every selected order. `Policies.CreatePricing` also accepts custom immutable `PricingRules`.

For the initial sample data, `quote ORD-101 30` returns 1425.00 KZT. `quotes 30` includes three orders and totals 8400.00 KZT. ORD-103 cannot be assigned to C-01 because its 150 km distance exceeds that courier's 100 km range. C-02 can carry that order.

## Reports

- **Eligible order quotes:** capacity and minimum-cost filters, projection into report rows, cost descending and order ID as the tie-breaker. Exported columns are `OrderId,Destination,Mode,Cost,EstimatedMinutes`.
- **Quoted totals by mode:** `GroupBy`, count and total cost, ordered by total cost and mode. It includes all orders eligible for the requested capacity; the minimum-cost filter applies only to the first report. These totals are estimates, not completed-delivery revenue.
- **Courier workload:** all couriers, including idle couriers, with active and completed shipment counts, active weight and revenue from completed shipments. Ordered by active weight and courier ID.

## Code structure

| Requirement | Implementation |
| --- | --- |
| Immutable domain models and factories | `DeliveryOrder.Create`, `Courier.Create`, `Shipment` |
| Three named pure pipeline stages | `Pipeline.FilterEligible`, `CalculateQuotes`, `CreateSummary` |
| Configurable business behaviour | `Policies.CreatePricing`, delegates passed to quote and delivery workflows |
| Meaningful pattern matching | `Policies.ModePricing`, `Shipment.Complete` |
| Safe ordering and immutable transitions | `DeliveryWorkflow.Dispatch`, `DeliveryWorkflow.Complete`, `Shipment.Complete` |
| Explicit domain failures | `QuoteWorkflow`, `DispatchRules`, `Shipment` |
| Real Map and Bind chains | `QuoteWorkflow.CreateQuote`, `DeliveryWorkflow.Dispatch`, `DeliveryData.Load` |
| Missing lookup results | `Maybe<T>`, `QuoteWorkflow.FindOrder`, `FindCourier`, `FindShipment` |
| Custom extension methods | `DeliveryExtensions.ForVehicle`, `ToPricedOrders` |
| LINQ reports and aggregation | `DeliveryReports.Dispatch`, `TotalsByMode`, `CourierWorkload` |
| Lazy processing and snapshot | `ToPricedOrders` uses `yield return`; `ReportWorkflow` materialises with `ToArray` |
| Loading and validating data | `OrdersFile`, `CouriersFile`, `ShipmentsFile`, `DeliveryData.Load` |
| Persistence and export boundaries | `JsonFile.Save`, `ShipmentsFile.Save`, `CsvReportFile.Save` |
| Native time calculation | `NativeTravelTime`, `native/deliverymath.c` |
| Console boundary | `Program.cs` |

Domain calculations do not read the console or access files. The delivery workflow receives its clock value as an argument. The native adapter can receive a replacement calculation function in tests. Transformations create new collections and leave caller-owned collections unchanged.

`Result<T>` carries success or a structured error. `Map` transforms successful values and `Bind` connects operations that can fail. A failure skips subsequent dependent functions. `Maybe<T>` distinguishes a missing lookup from a present value. Exceptions from incorrect API usage remain programming errors; expected domain and file failures use explicit outcomes.

The lazy pricing sequence does no calculation until enumeration. Enumerating it again recalculates prices. `ReportWorkflow` uses `ToArray` before calculating multiple reports so prices are reused. `demo` prints the call count before enumeration, after materialisation and when the snapshot is reused.

JSON loading rejects malformed content, missing required fields, duplicate IDs, invalid domain values, unknown shipment references, impossible delivery dates and overloaded couriers. JSON saves and CSV export write to a temporary file, close it, and then replace the target. A failed write preserves an existing target and attempts to remove the temporary file.

## Tests

The xUnit suite contains 76 test cases. It covers pure calculations, order and courier validation, boundaries, empty inputs, missing lookups, failure propagation, lazy execution, complete workflows, file errors and a real native call.

- `PipelineTests`, `FactoryTests`: calculations, factories and pricing.
- `OutcomeTests`, `DeferredExecutionTests`: explicit outcomes and controlled enumeration.
- `DeliveryWorkflowTests`: successful dispatch and completion, missing prerequisites, capacity, range, duplicate dispatch, repeated completion and native failures.
- `ReportTests`, `CourierReportTests`: filtering, ordering, grouping, empty data and delivered revenue.
- `OrdersFileTests`, `CouriersFileTests`, `ShipmentPersistenceTests`: missing and malformed files, referential validation and persistence round trips.
- `CsvReportFileTests`: escaping, number formatting, writer disposal and failed-write preservation.
- `ReportWorkflowTests`, `NativeTravelTimeTests`: report execution and native integration.

## Limitations and submission

The application assumes a single process writes the shipment file. It does not optimise courier routes or query real traffic; time is estimated from distance and speed. The native library currently supports macOS arm64. Prices are quotes until the courier report counts the completed shipment as revenue.

### Repository and Submission Details
- **Repository URL:** https://github.com/yerbolberdalin1/ITFP_FINAL
- **Branch:** `main`
- **Student / Group:** Berdalin Yerbol, IT-2513 (Astana IT University)
- **Submission Role:** Sole Developer (100% individual implementation)

This directory contains the application, tests, sample data and the reports. The submitted archive matches this repository commit.

## Reports and submission documents

The project reports are prepared and verified in both DOCX and PDF formats:
- **Shared Project Report:** [DOCX](reports/DeliveryPipeline_Project_Report.docx) | [PDF](reports/DeliveryPipeline_Project_Report.pdf) — Documents system architecture, domain models, 4 workflows, requirement mapping, and verification outputs.
- **Individual Contribution Report (Berdalin Yerbol):** [DOCX](reports/Individual_Contribution_Report.docx) | [PDF](reports/Individual_Contribution_Report.pdf) — Complete breakdown of personal ownership (Sole Developer), functional modules authored, 76 automated test cases, resolved defects, and oral defense justifications.

Actual test results and application output are stored in `reports/evidence`: `delivery-tests.trx`, `application-transcript.txt`, `application-runs.json`, `quote-report.csv` and `completed-shipments.json`. `source-sha256.txt` identifies the checked application files; it does not replace the required final Git commit.

The project uses the previous DeliveryPipeline assignments, .NET 10, xUnit, Microsoft.NET.Test.Sdk and coverlet.collector. Dependency versions are recorded in the project files. The native library is built from the included C source.
