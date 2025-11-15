# Cerbi.MEL.Governance Demo

A simple .NET 8.0 console application that demonstrates how to wire up [**Cerbi.MEL.Governance**](https://www.nuget.org/packages/Cerbi.MEL.Governance) into a real `Microsoft.Extensions.Logging` pipeline.

This demo shows:

- How to register and configure the Cerbi governance logger.
- How to define governance rules in JSON.
- How violations are detected and logged as a **second console line only when they occur**.
- How topics (`Orders`, `Payments`) are routed via `[CerbiTopic]`.

It’s intentionally small and focused so you can copy/paste patterns into ASP.NET Core, Worker Services, Azure Functions, or any MEL-based app—even if you’re already using Serilog, NLog, OpenTelemetry, Seq, Loki, ELK/OpenSearch, etc.

---

## 📂 Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)  
- A terminal or IDE (Visual Studio, VS Code, Rider, etc.) that can build and run .NET console apps.

---

## 🚀 Getting Started

1. **Clone the repository**

   ```bash
   git clone https://github.com/Zeroshi/Cerbi.MEL.Governance.git
   cd Cerbi.MEL.Governance/Demo
````

2. **Restore NuGet packages**

   ```bash
   dotnet restore
   ```

3. **Create a governance configuration file**

   In the `Demo` folder, create `cerbi_governance.json` with:

   ```json
   {
     "EnforcementMode": "Strict",
     "LoggingProfiles": {
       "Orders": {
         "RequireTopic": true,
         "AllowedTopics": [ "Orders" ],
         "FieldSeverities": {
           "userId": "Required",
           "email": "Required",
           "password": "Forbidden"
         },
         "AllowRelax": true
       },
       "Payments": {
         "RequireTopic": true,
         "AllowedTopics": [ "Payments" ],
         "FieldSeverities": {
           "accountNumber": "Required",
           "amount": "Required"
         },
         "AllowRelax": false
       }
     }
   }
   ```

   > **Notes for this demo (v1.0.36):**
   >
   > * The **original console log line is always written**.
   > * An extra JSON payload is emitted **only when there is a governance violation**.
   > * The underlying package has **no fluent `Relax()` helper**; this demo does not use Relax at all.
   > * `AllowRelax` is configured for `Orders` but `Relax` is never passed, so `GovernanceRelaxed` remains `false`.

4. **Build and run the demo**

   ```bash
   dotnet run --project Cerbi.MEL.Governance.Demo.csproj
   ```

---

## 🛠 How It Works

### 1. Program.cs – Host & logging setup

The demo uses `Host.CreateDefaultBuilder` to configure MEL and then wraps the console logger with Cerbi governance:

```csharp
Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();

        // 1) Add the built-in simple console sink:
        logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes   = true;
            options.SingleLine      = true;
            options.TimestampFormat = "HH:mm:ss ";
        });

        // 2) Force-register ConsoleLoggerProvider in DI so Cerbi can wrap it:
        logging.Services.AddSingleton<ConsoleLoggerProvider>();

        Console.WriteLine("▶▶ Calling AddCerbiGovernance");

        // 3) Wrap the console sink in Cerbi governance:
        logging.AddCerbiGovernance(opts =>
        {
            opts.Profile    = "Orders";                 // default fallback profile
            opts.ConfigPath = "cerbi_governance.json";  // governance JSON path
            opts.Enabled    = true;                     // enable runtime verification
        });
    })
    .ConfigureServices(services =>
    {
        services.AddTransient<OrderService>();
        services.AddTransient<PaymentService>();
    })
    .Build();
```

The important part is:

* `AddSimpleConsole` provides a normal console sink.
* `AddCerbiGovernance` wraps that sink and applies rules from `cerbi_governance.json` using the runtime validator.

---

### 2. OrderService & PaymentService – Topic-based profiles

* `OrderService` is decorated with `[CerbiTopic("Orders")]`.
* `PaymentService` is decorated with `[CerbiTopic("Payments")]`.

Each service logs structured messages:

* `OrderService` emits:

  * A valid log (all required fields).
  * A log **missing** a required field (`userId`).
  * A log containing a **forbidden** field (`password`).

* `PaymentService` emits:

  * A valid payment log (`accountNumber`, `amount`) under the `Payments` profile.

Cerbi uses:

1. `[CerbiTopic]` to determine which profile (`Orders` / `Payments`) to apply.
2. The JSON profile to enforce `FieldSeverities` and `RequireTopic`.

---

### 3. Cerbi.MEL.Governance behavior in the demo

When `_logger.LogInformation("…", …)` is called:

1. Cerbi extracts structured fields from the logging state.
2. It injects `CerbiTopic` using the `[CerbiTopic]` attribute (or fallback `opts.Profile`).
3. It runs the `RuntimeGovernanceValidator` from `Cerbi.Governance.Runtime` using `cerbi_governance.json`.
4. If the event is compliant, governance metadata is added and logged as JSON (in this demo, you’ll see a second line even for some valid cases).
5. If violations exist, the JSON payload includes:

   * `GovernanceProfileUsed`
   * `GovernanceViolations` (e.g., `MissingField:userId`, `ForbiddenField:password`)
   * `GovernanceRelaxed` (always `false` in this demo)

In all cases:

* The **original human-readable console message** is printed.
* A structured JSON line is printed to show what Cerbi sees and enforces.

---

## 🔍 Sample Output

Running the demo produces output similar to:

```text
▶▶ Starting Demo.Main
▶▶ Calling AddCerbiGovernance
▶▶ Resolving OrderService
[08:12:34 INF] Valid order: abc123 user@example.com

[08:12:34 INF] {"userId":"abc123","email":"user@example.com","CerbiTopic":"Orders","GovernanceProfileUsed":"Orders","GovernanceEnforced":true,"GovernanceMode":"Strict"}
[08:12:34 INF] Missing userId, only email: user@example.com

[08:12:34 INF] {"email":"user@example.com","CerbiTopic":"Orders","GovernanceViolations":["MissingField:userId"],"GovernanceRelaxed":false,"GovernanceProfileUsed":"Orders"}
[08:12:34 INF] Leaking password: abc123 user@example.com supersecret

[08:12:34 INF] {"userId":"abc123","email":"user@example.com","password":"supersecret","CerbiTopic":"Orders","GovernanceViolations":["ForbiddenField:password"],"GovernanceRelaxed":false,"GovernanceProfileUsed":"Orders"}
▶▶ Resolving PaymentService
[08:12:34 INF] Payment: 9876543210 150.75

[08:12:34 INF] {"accountNumber":"9876543210","amount":150.75,"CerbiTopic":"Payments","GovernanceProfileUsed":"Payments","GovernanceEnforced":true,"GovernanceMode":"Strict"}
▶▶ Demo finished. Press any key to exit.
```

Key points:

* Compliant events include `GovernanceProfileUsed` and enforcement/Mode metadata.
* Violations include `GovernanceViolations` and `GovernanceRelaxed:false`.
* The original line is **never** suppressed.

---

## 📚 Demo Structure

```text
/Demo
 ├─ Program.cs
 ├─ OrderService.cs
 ├─ PaymentService.cs
 ├─ cerbi_governance.json
 ├─ Cerbi.MEL.Governance.Demo.csproj
 └─ README.md   ← this file
```

* **Program.cs** – Host, DI, logging configuration, `AddCerbiGovernance`.
* **OrderService.cs** – Logs valid + violating messages under `Orders`.
* **PaymentService.cs** – Logs valid messages under `Payments`.
* **cerbi_governance.json** – Governance profiles and rules.

---

## 🧩 How This Fits into the Ecosystem

This demo is a thin wrapper around:

* **Cerbi.MEL.Governance**
  MEL integration and logger wrapping.

* **Cerbi.Governance.Core**
  Governance models, schema, and profile contracts.

* **Cerbi.Governance.Runtime**
  Runtime validator that checks logs against profiles.

You can pair the same patterns with:

* Serilog / NLog / Log4Net via MEL adapters.
* OTEL Logging + OTLP exporters into the OpenTelemetry Collector.
* Downstream systems like Seq, Loki, Fluentd/FluentBit, ELK/OpenSearch, Graylog, VictoriaLogs/VictoriaMetrics, or syslog/Journald.

Cerbi governs the log content **before** it hits those systems.

---

## 🎯 Next Steps

* Change profiles in `cerbi_governance.json` and rerun to see different violations.
* Add new services and topics (`[CerbiTopic("Billing")]`, etc.) and wire new profiles.
* Point console output to file or other sinks and watch how governance metadata flows through.
* Use this as a template for wiring Cerbi into a real ASP.NET Core or worker app.

---

## 🔗 Resources

* **NuGet:** [Cerbi.MEL.Governance](https://www.nuget.org/packages/Cerbi.MEL.Governance)
* **Repo & Demo:** [https://github.com/Zeroshi/Cerbi.MEL.Governance](https://github.com/Zeroshi/Cerbi.MEL.Governance)
* **Cerbi Docs & Suite Overview:** [https://cerbi.io](https://cerbi.io)

**Related libraries:**

* [Cerbi.Governance.Core](https://www.nuget.org/packages/Cerbi.Governance.Core)
* [Cerbi.Governance.Runtime](https://www.nuget.org/packages/Cerbi.Governance.Runtime)
