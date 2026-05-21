using System.Text.Json;

using CommunicationAPI.Models;

using Confluent.Kafka;

using Microsoft.EntityFrameworkCore;


namespace CommunicationAPI
{
    public class BackgroundMonitor : BackgroundService
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly IServiceScopeFactory _scopeFactory;
        private const string TopicName = "task-events";

        public BackgroundMonitor(IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;

            var config = new ConsumerConfig
            {
                // Pulls dynamically from environment string overrides first, falls back to internal cluster URL
                BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "kafka-service:9092",
                GroupId = "communication-service-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false // Manual commit handling ensures data integrity
            };

            _consumer = new ConsumerBuilder<string, string>(config).Build();
        }

        protected override System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Run the consumer polling loop on a dedicated background thread execution lane
            return System.Threading.Tasks.Task.Run(() => StartMessageConsumerLoop(stoppingToken), stoppingToken);
        }

        private async System.Threading.Tasks.Task StartMessageConsumerLoop(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(TopicName);
            Console.WriteLine($"[Kafka Consumer] Worker attached. Subscribed to topic: {TopicName}");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // Block thread up to 100ms waiting for incoming broker records
                        var consumeResult = _consumer.Consume(stoppingToken);
                        if (consumeResult == null) continue;

                        // Retrieve the published Task Id string from the Kafka Message Key
                        if (int.TryParse(consumeResult.Message.Key, out int taskId))
                        {
                            Console.WriteLine($"[Kafka Consumer] Event received for Task ID: {taskId}. Spawning database worker scope...");

                            // Create a transient scope to safely resolve the Scoped DbContext
                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var dbContext = scope.ServiceProvider.GetRequiredService<TaskDbContext>();

                                // Query database for the target task record matching the event key
                                var taskRecord = await dbContext.Tasks.FirstOrDefaultAsync(t => t.id == taskId, stoppingToken);

                                if (taskRecord != null)
                                {
                                    // Update the target description column per requirements
                                    taskRecord.description = "Message received. Task completed";

                                    await dbContext.SaveChangesAsync(stoppingToken);
                                    Console.WriteLine($"[Database Update] Successfully updated task description for Task ID: {taskId}");
                                }
                                else
                                {
                                    Console.WriteLine($"[Database Warning] Task ID {taskId} was not found in the database layer.");
                                }
                            }
                        }

                        // Commit offset to Kafka only after successful message ingestion and database persistence
                        _consumer.Commit(consumeResult);
                    }
                    catch (ConsumeException ex)
                    {
                        Console.WriteLine($"[Kafka Consumer Error] Broker synchronization discrepancy: {ex.Error.Reason}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Worker Exception] Processing failure: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Safe handling when pod scaling down or restarting
            }
            finally
            {
                _consumer.Close();
                Console.WriteLine("[Kafka Consumer] Connection closed cleanly.");
            }
        }
    }
}