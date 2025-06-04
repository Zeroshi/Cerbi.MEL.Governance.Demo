# Cerbi.MEL.Governance Demo

A simple console application that demonstrates how to wire up [**Cerbi.MEL.Governance**](https://www.nuget.org/packages/Cerbi.MEL.Governance) into a .NET 8.0 project with Microsoft.Extensions.Logging. It shows:

* How to register and configure the Cerbi governance logger.
* How to define governance rules in JSON.
* How violations are detected and logged as a second console line only when they occur.

---

## 📂 Prerequisites

* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* A terminal or IDE (e.g., Visual Studio, VS Code) capable of building and running .NET console apps.

---

## 🚀 Getting Started

1. **Clone this repository:**

   ```bash
   git clone https://github.com/Zeroshi/Cerbi.MEL.Governance.git
   cd Cerbi.MEL.Governance/Demo
   ```

2. **Restore NuGet packages:**

   ```bash
   dotnet restore
   ```

3. **Create a governance configuration file**
   In the `Demo` folder, create a file named `cerbi_governance.json` with the following contents:

   ```json
   {
     "EnforcementMode": "Strict",
     "LoggingProfiles": {
       "Orders": {
         "RequireTopic": true,
         "AllowedTopics": ["Orders"],
         "FieldSeverities": {
           "userId": "Required",
           "email": "Required",
           "password": "Forbidden"
         },
         "AllowRelax": true
       },
       "Payments": {
         "RequireTopic": true,
         "AllowedTopics": ["Payments"],
         "FieldSeverities": {
           "accountNumber": "Required",
           "amount": "Required"
         },
         "AllowRelax": false
       }
     }
   }
   ```

   > * **Note:**
   >   • This demo version emits an extra JSON payload only when a violation occurs.
   >   • Future releases will continue to emit a second line only on violations; no extra logging when rules pass.

4. **Build and run the demo:**

   ```bash
   dotnet run --project Cerbi.MEL.Governance.Demo.csproj
   ```

---

## 🛠 How It Works

1. **Program.cs**
   The demo’s `Program.cs` uses `Host.CreateDefaultBuilder` to configure logging:

   ```csharp
   Host.CreateDefaultBuilder(args)
       .ConfigureLogging(logging =>
       {
           logging.ClearProviders();

           // 1) Add the built‑in simple console sink:
           logging.AddSimpleConsole(options =>
           {
               options.IncludeScopes   = true;
               options.SingleLine      = true;
               options.TimestampFormat = "HH:mm:ss ";
           });

           // 2) Force‑register the ConsoleLoggerProvider in DI so Cerbi can wrap it:
           logging.Services.AddSingleton<ConsoleLoggerProvider>();

           Console.WriteLine("▶▶ Calling AddCerbiGovernance");

           // 3) Wrap the console sink in Cerbi governance:
           logging.AddCerbiGovernance(opts =>
           {
               opts.Profile    = "Orders";                    // default fallback topic
               opts.ConfigPath = "cerbi_governance.json";
               opts.Enabled    = true;                          // enable runtime verification
           });
       })
       .ConfigureServices(services =>
       {
           services.AddTransient<OrderService>();
           services.AddTransient<PaymentService>();
       })
       .Build();
   ```

2. **OrderService & PaymentService**

   * Each class is decorated with `[CerbiTopic("Orders")]` or `[CerbiTopic("Payments")]`.
   * Methods log structured messages containing fields (e.g., `userId`, `email`, `password`, `accountNumber`, `amount`).
   * Cerbi’s runtime validator inspects each log entry:

     * If a required field is missing or a forbidden field appears, a violation is flagged.
     * Only when a violation is detected does the demo emit a second JSON line with violation details.

3. **Cerbi.MEL.Governance Behavior**
   When `_logger.LogInformation("…", …)` is called, Cerbi.MEL.Governance:

   1. Extracts structured fields from the log state.
   2. Injects a `CerbiTopic` field (from the `[CerbiTopic]` attribute or fallback `opts.Profile`).
   3. Applies governance rules defined in `cerbi_governance.json` via the `RuntimeGovernanceValidator`.
   4. If violations exist, it serializes a JSON payload containing:

      * `GovernanceProfileUsed` (the profile name)
      * `GovernanceViolations` (array of violation codes, e.g., `MissingField:userId`, `ForbiddenField:password`)
      * `GovernanceRelaxed` (always `false` in this demo)
   5. Logs that JSON payload as a second console line.
      If no violations occur, only the original message is emitted.

---

## 🔍 Sample Output

After running the demo, you’ll see output similar to:

```
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

* When a log entry fully complies, you see a JSON line with enforcement details.
* When a violation occurs (missing or forbidden field), you see a JSON line showing the violation list.

---

## 📚 Demo Structure

```
/Demo
 ├─ Program.cs
 ├─ OrderService.cs
 ├─ PaymentService.cs
 ├─ cerbi_governance.json
 ├─ Cerbi.MEL.Governance.Demo.csproj
 └─ README.md          ← (this file)
```

* **Program.cs**: Host configuration, DI, and logging setup.
* **OrderService.cs**: Emits three log scenarios—valid, missing required field, forbidden field.
* **PaymentService.cs**: Emits a payment log scenario (valid).
* **cerbi\_governance.json**: Defines governance rules for `Orders` and `Payments` profiles.

---

## 🎯 Next Steps

* This demo version logs an extra JSON payload only on violations.
* Upcoming enhancements include a `Relax()` helper method for relaxed mode, minimizing manual field injection.
* To extend the demo, add new profiles, add more services, or integrate with other sinks (file, Seq, etc.)—Cerbi.MEL.Governance works with any MEL‑compatible provider.

---

## 🔗 Resources

* **NuGet Package:** [Cerbi.MEL.Governance](https://www.nuget.org/packages/Cerbi.MEL.Governance)
* **Demo & Examples:** [https://github.com/Zeroshi/Cerbi.MEL.Governance](https://github.com/Zeroshi/Cerbi.MEL.Governance)
* **Cerbi Docs:** [https://cerbi.io](https://cerbi.io)
* **Related Libraries:**

  * [Cerbi.Governance.Core](https://www.nuget.org/packages/Cerbi.Governance.Core)
  * [Cerbi.Governance.Runtime](https://www.nuget.org/packages/Cerbi.Governance.Runtime)

> ℹ️ If you encounter any issues or have feedback, please open an issue on the [GitHub repository](https://github.com/Zeroshi/Cerbi.MEL.Governance/issues).
