using Cerbi;                         // <-- AddCerbiGovernance lives here
using CerbiMelGovernanceDemo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Threading.Tasks;

namespace CerbiMelGovernanceDemo
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("▶▶ Starting Demo.Main");
            Console.WriteLine("▶▶ Before host.Build()");

            using var host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();

                    // 1) Register a console sink:
                    logging.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "HH:mm:ss ";
                    });

                    // 2) Force‐register the ConsoleLoggerProvider in DI so Cerbi can wrap it:
                    logging.Services.AddSingleton<ConsoleLoggerProvider>();

                    Console.WriteLine("▶▶ Calling AddCerbiGovernance");

                    // 3) Now hook in Cerbi governance around that same console sink:
                    logging.AddCerbiGovernance(opts =>
                    {
                        // NOTE: 'DefaultTopic' was renamed to 'Profile' in v1.0.28
                        opts.Profile = "Orders";
                        opts.ConfigPath = "cerbi_governance.json";
                        opts.Enabled = true;
                    });
                })
                .ConfigureServices(services =>
                {
                    // Register your demo services:
                    services.AddTransient<OrderService>();
                    services.AddTransient<PaymentService>();
                })
                .Build();

            Console.WriteLine("▶▶ After host.Build()");
            Console.WriteLine("▶▶ Resolving OrderService");

            var orderSvc = host.Services.GetRequiredService<OrderService>();
            orderSvc.ProcessValid();
            orderSvc.ProcessMissingField();
            orderSvc.ProcessForbidden();

            Console.WriteLine("▶▶ Resolving PaymentService");
            var paymentSvc = host.Services.GetRequiredService<PaymentService>();
            paymentSvc.MakePayment();

            Console.WriteLine("▶▶ Demo finished. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
