using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;

namespace ProductOrderingSystem.DataSeeder.Infrastructure;

public class RabbitMqReadinessChecker : IRabbitMqReadinessChecker
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RabbitMqReadinessChecker> _logger;
    private readonly IConfiguration _configuration;

    public RabbitMqReadinessChecker(
        HttpClient httpClient,
        ILogger<RabbitMqReadinessChecker> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> WaitForQueuesAsync(string[] expectedQueues, TimeSpan timeout)
    {
        var managementUrl = _configuration.GetConnectionString("messaging-management");
        if (string.IsNullOrEmpty(managementUrl))
        {
            _logger.LogWarning("⚠️  messaging-management connection string not found. Skipping queue readiness check.");
            return false;
        }

        var started = DateTime.UtcNow;
        var checkInterval = TimeSpan.FromSeconds(2);
        
        _logger.LogInformation("🔍 Checking RabbitMQ for consumer queues: {Queues}", string.Join(", ", expectedQueues));

        while (DateTime.UtcNow - started < timeout)
        {
            try
            {
                // Set up basic auth for RabbitMQ Management API
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes("guest:guest"));
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

                var response = await _httpClient.GetAsync($"{managementUrl}/api/queues");
                
                if (response.IsSuccessStatusCode)
                {
                    var queues = await response.Content.ReadFromJsonAsync<List<QueueInfo>>();
                    if (queues != null)
                    {
                        var existingQueueNames = queues.Select(q => q.Name).ToHashSet();
                        var allQueuesExist = expectedQueues.All(eq => 
                            existingQueueNames.Any(qn => qn.Contains(eq, StringComparison.OrdinalIgnoreCase)));

                        if (allQueuesExist)
                        {
                            var withConsumers = queues
                                .Where(q => expectedQueues.Any(eq => q.Name.Contains(eq, StringComparison.OrdinalIgnoreCase)))
                                .Where(q => q.Consumers > 0)
                                .ToList();

                            if (withConsumers.Count >= expectedQueues.Length)
                            {
                                _logger.LogInformation("✅ All expected queues found with active consumers:");
                                foreach (var queue in withConsumers)
                                {
                                    _logger.LogInformation("   • {QueueName} ({Consumers} consumers)", 
                                        queue.Name, queue.Consumers);
                                }
                                return true;
                            }
                            else
                            {
                                _logger.LogDebug("Queues exist but waiting for consumers to connect... ({Found}/{Expected})", 
                                    withConsumers.Count, expectedQueues.Length);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("RabbitMQ readiness check failed: {Message}", ex.Message);
            }

            await Task.Delay(checkInterval);
        }

        _logger.LogWarning("⏱️  Timeout after {Seconds}s waiting for consumer queues", timeout.TotalSeconds);
        return false;
    }

    public async Task PurgeQueuesAsync(string[] queueNames)
    {
        var managementUrl = _configuration.GetConnectionString("messaging-management");
        if (string.IsNullOrEmpty(managementUrl))
        {
            _logger.LogWarning("⚠️  messaging-management connection string not found. Cannot purge queues.");
            return;
        }

        _logger.LogInformation("🧹 Purging old messages from RabbitMQ queues...");

        try
        {
            // Set up basic auth for RabbitMQ Management API
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes("guest:guest"));
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            // Get all queues
            var response = await _httpClient.GetAsync($"{managementUrl}/api/queues");
            if (response.IsSuccessStatusCode)
            {
                var queues = await response.Content.ReadFromJsonAsync<List<QueueInfo>>();
                if (queues != null)
                {
                    foreach (var queueName in queueNames)
                    {
                        var matchingQueues = queues.Where(q => q.Name.Contains(queueName)).ToList();
                        foreach (var queue in matchingQueues)
                        {
                            // Purge the queue using DELETE request to /api/queues/{vhost}/{name}/contents
                            var purgeUrl = $"{managementUrl}/api/queues/%2F/{Uri.EscapeDataString(queue.Name)}/contents";
                            var purgeResponse = await _httpClient.DeleteAsync(purgeUrl);
                            
                            if (purgeResponse.IsSuccessStatusCode)
                            {
                                _logger.LogInformation("  ✓ Purged queue: {Queue}", queue.Name);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to purge RabbitMQ queues");
        }
    }

    private class QueueInfo
    {
        public string Name { get; set; } = string.Empty;
        public int Consumers { get; set; }    }
}