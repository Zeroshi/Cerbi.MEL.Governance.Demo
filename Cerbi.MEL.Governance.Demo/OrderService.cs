using Microsoft.Extensions.Logging;
using Cerbi;    // <-- This is where [CerbiTopic] and AddCerbiGovernance(…) live

namespace CerbiMelGovernanceDemo
{
    [CerbiTopic("Orders")]
    public class OrderService
    {
        private readonly ILogger<OrderService> _logger;

        public OrderService(ILogger<OrderService> logger) => _logger = logger;

        public void ProcessValid()
        {
            // These two fields (userId + email) exactly match the “Orders” profile
            _logger.LogInformation("Valid order: {userId} {email}", "abc123", "user@example.com");
        }

        public void ProcessMissingField()
        {
            // Missing userId → triggers “MissingField:userId”
            _logger.LogInformation("Missing userId, only email: {email}", "user@example.com");
        }

        public void ProcessForbidden()
        {
            // “password” is explicitly forbidden in “Orders” profile → → triggers “ForbiddenField:password”
            _logger.LogInformation(
                "Leaking password: {userId} {email} {password}",
                "abc123", "user@example.com", "supersecret"
            );
        }
    }
}
