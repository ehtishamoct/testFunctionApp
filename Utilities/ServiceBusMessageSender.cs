using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace ServiceBusFunctionApp.Utilities;

/// <summary>
/// Utility class for sending test messages to the Service Bus queue
/// This class is useful for testing the function locally or for sending messages programmatically
/// </summary>
public class ServiceBusMessageSender
{
    private readonly ServiceBusClient _client;
    private readonly string _queueName;

    public ServiceBusMessageSender(string connectionString, string queueName)
    {
        _client = new ServiceBusClient(connectionString);
        _queueName = queueName;
    }

    /// <summary>
    /// Sends a task message to the Service Bus queue
    /// </summary>
    /// <param name="taskMessage">The task message to send</param>
    /// <returns>Task representing the async operation</returns>
    public async Task SendTaskMessageAsync(TaskMessage taskMessage)
    {
        var sender = _client.CreateSender(_queueName);
        
        try
        {
            var messageBody = JsonSerializer.Serialize(taskMessage);
            var message = new ServiceBusMessage(messageBody)
            {
                MessageId = taskMessage.TaskId,
                Subject = taskMessage.TaskType,
                ContentType = "application/json"
            };

            // Add custom properties for routing or filtering
            message.ApplicationProperties.Add("TaskType", taskMessage.TaskType);
            message.ApplicationProperties.Add("Priority", taskMessage.Priority);
            message.ApplicationProperties.Add("CreatedBy", taskMessage.CreatedBy);

            await sender.SendMessageAsync(message);
            Console.WriteLine($"Message sent successfully: {taskMessage.TaskId}");
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }

    /// <summary>
    /// Sends multiple task messages to the Service Bus queue as a batch
    /// </summary>
    /// <param name="taskMessages">Collection of task messages to send</param>
    /// <returns>Task representing the async operation</returns>
    public async Task SendTaskMessagesAsync(IEnumerable<TaskMessage> taskMessages)
    {
        var sender = _client.CreateSender(_queueName);
        
        try
        {
            using var messageBatch = await sender.CreateMessageBatchAsync();

            foreach (var taskMessage in taskMessages)
            {
                var messageBody = JsonSerializer.Serialize(taskMessage);
                var message = new ServiceBusMessage(messageBody)
                {
                    MessageId = taskMessage.TaskId,
                    Subject = taskMessage.TaskType,
                    ContentType = "application/json"
                };

                message.ApplicationProperties.Add("TaskType", taskMessage.TaskType);
                message.ApplicationProperties.Add("Priority", taskMessage.Priority);
                message.ApplicationProperties.Add("CreatedBy", taskMessage.CreatedBy);

                if (!messageBatch.TryAddMessage(message))
                {
                    throw new InvalidOperationException($"Message {taskMessage.TaskId} is too large for the batch.");
                }
            }

            await sender.SendMessagesAsync(messageBatch);
            Console.WriteLine($"Batch of {taskMessages.Count()} messages sent successfully");
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a sample task message for testing purposes
    /// </summary>
    /// <param name="taskType">Type of task to create</param>
    /// <returns>A sample TaskMessage</returns>
    public static TaskMessage CreateSampleTaskMessage(string taskType = "data-processing")
    {
        return new TaskMessage
        {
            TaskId = Guid.NewGuid().ToString(),
            TaskName = $"Sample {taskType} task",
            TaskType = taskType,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            Priority = Random.Shared.Next(1, 5),
            Parameters = new Dictionary<string, object>
            {
                ["inputPath"] = "/data/input",
                ["outputPath"] = "/data/output",
                ["timeout"] = 300
            }
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            await _client.DisposeAsync();
        }
    }
}