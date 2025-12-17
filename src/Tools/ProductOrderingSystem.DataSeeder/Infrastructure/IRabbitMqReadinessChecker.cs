namespace ProductOrderingSystem.DataSeeder.Infrastructure;

public interface IRabbitMqReadinessChecker
{
    Task<bool> WaitForQueuesAsync(string[] expectedQueues, TimeSpan timeout);
    Task PurgeQueuesAsync(string[] queueNames);
}
