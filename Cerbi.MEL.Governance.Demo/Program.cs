using Cerbi;                  // for AddCerbiGovernance, CerbiTopicAttribute
using CerbiMelGovernanceDemo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using YourDemoNamespace;     // for OrderService, PaymentService

namespace YourDemoNamespace
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
                    // Remove all built‐in providers:
                    logging.ClearProviders();

                    // Now register exactly one console + wrap it in Cerbi:
                    logging.AddCerbiGovernance(opts =>
                    {
                        opts.Profile = "Orders";               // fallback if no [CerbiTopic]
                        opts.ConfigPath = "cerbi_governance.json";
                        opts.Enabled = true;
                    });
                })
                .ConfigureServices(services =>
                {
                    // Register your demo services that have [CerbiTopic]:
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
