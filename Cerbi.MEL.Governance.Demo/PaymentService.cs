using Microsoft.Extensions.Logging;
using Cerbi;  // <-- Must have this or AddCerbiGovernance won't compile

namespace CerbiMelGovernanceDemo
{
    [CerbiTopic("Payments")]
    public class PaymentService
    {
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(ILogger<PaymentService> logger) => _logger = logger;

        public void MakePayment()
        {
            // These two fields (accountNumber + amount) exactly match the “Payments” profile
            _logger.LogInformation("Payment: {accountNumber} {amount}", "9876543210", 150.75m);
        }
    }
}
