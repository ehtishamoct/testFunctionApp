using ServiceBusFunctionApp;
using ServiceBusFunctionApp.Utilities;

namespace TestClient;

/// <summary>
/// Test client application to send sample messages to the Service Bus queue
/// This demonstrates how to trigger the Azure Function
/// </summary>
class Program
{
    private static readonly string[] TaskTypes = {
        "data-processing",
        "file-upload", 
        "email-notification",
        "report-generation"
    };

    static async Task Main(string[] args)
    {
        Console.WriteLine("🧪 Service Bus Function Test Client");
        Console.WriteLine("====================================");

        // Read connection string from environment or use default for local testing
        var connectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString") 
            ?? "Endpoint=sb://your-servicebus-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-shared-access-key";
        
        var queueName = Environment.GetEnvironmentVariable("QueueName") ?? "task-queue";

        if (connectionString.Contains("your-servicebus-namespace"))
        {
            Console.WriteLine("⚠️  Please update the Service Bus connection string in local.settings.json or environment variables");
            Console.WriteLine("   Current connection string appears to be a placeholder.");
            Console.WriteLine();
        }

        try
        {
            await using var messageSender = new ServiceBusMessageSender(connectionString, queueName);

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Choose an option:");
                Console.WriteLine("1. Send a single test message");
                Console.WriteLine("2. Send multiple test messages");
                Console.WriteLine("3. Send custom message");
                Console.WriteLine("4. Exit");
                Console.Write("Enter your choice (1-4): ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await SendSingleMessage(messageSender);
                        break;
                    case "2":
                        await SendMultipleMessages(messageSender);
                        break;
                    case "3":
                        await SendCustomMessage(messageSender);
                        break;
                    case "4":
                        Console.WriteLine("👋 Goodbye!");
                        return;
                    default:
                        Console.WriteLine("❌ Invalid choice. Please try again.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("💡 Troubleshooting tips:");
            Console.WriteLine("   - Ensure the Service Bus connection string is correct");
            Console.WriteLine("   - Verify the queue exists in your Service Bus namespace");
            Console.WriteLine("   - Check your network connectivity to Azure");
        }
    }

    private static async Task SendSingleMessage(ServiceBusMessageSender messageSender)
    {
        var taskType = TaskTypes[Random.Shared.Next(TaskTypes.Length)];
        var message = ServiceBusMessageSender.CreateSampleTaskMessage(taskType);

        Console.WriteLine($"📤 Sending message with Task ID: {message.TaskId}");
        Console.WriteLine($"   Task Type: {message.TaskType}");
        Console.WriteLine($"   Task Name: {message.TaskName}");

        await messageSender.SendTaskMessageAsync(message);
        Console.WriteLine("✅ Message sent successfully!");
    }

    private static async Task SendMultipleMessages(ServiceBusMessageSender messageSender)
    {
        Console.Write("Enter number of messages to send (1-10): ");
        if (!int.TryParse(Console.ReadLine(), out int count) || count < 1 || count > 10)
        {
            Console.WriteLine("❌ Invalid number. Please enter a number between 1 and 10.");
            return;
        }

        var messages = new List<TaskMessage>();
        for (int i = 0; i < count; i++)
        {
            var taskType = TaskTypes[Random.Shared.Next(TaskTypes.Length)];
            var message = ServiceBusMessageSender.CreateSampleTaskMessage(taskType);
            message.TaskName = $"Batch task {i + 1}";
            messages.Add(message);
        }

        Console.WriteLine($"📤 Sending {count} messages...");
        await messageSender.SendTaskMessagesAsync(messages);
        
        Console.WriteLine("✅ All messages sent successfully!");
        Console.WriteLine("📋 Messages sent:");
        foreach (var msg in messages)
        {
            Console.WriteLine($"   - {msg.TaskId}: {msg.TaskType} ({msg.TaskName})");
        }
    }

    private static async Task SendCustomMessage(ServiceBusMessageSender messageSender)
    {
        Console.WriteLine("📝 Create custom message:");
        
        Console.Write("Task Name: ");
        var taskName = Console.ReadLine() ?? "Custom Task";
        
        Console.WriteLine("Available task types:");
        for (int i = 0; i < TaskTypes.Length; i++)
        {
            Console.WriteLine($"   {i + 1}. {TaskTypes[i]}");
        }
        Console.Write("Select task type (1-4) or enter custom: ");
        var taskTypeInput = Console.ReadLine();
        
        string taskType;
        if (int.TryParse(taskTypeInput, out int typeIndex) && typeIndex >= 1 && typeIndex <= TaskTypes.Length)
        {
            taskType = TaskTypes[typeIndex - 1];
        }
        else
        {
            taskType = taskTypeInput ?? "custom";
        }

        Console.Write("Priority (1-5, default 3): ");
        if (!int.TryParse(Console.ReadLine(), out int priority) || priority < 1 || priority > 5)
        {
            priority = 3;
        }

        var message = new TaskMessage
        {
            TaskId = Guid.NewGuid().ToString(),
            TaskName = taskName,
            TaskType = taskType,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-client",
            Priority = priority,
            Parameters = new Dictionary<string, object>
            {
                ["customParam"] = "Custom parameter value",
                ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            }
        };

        Console.WriteLine($"📤 Sending custom message:");
        Console.WriteLine($"   Task ID: {message.TaskId}");
        Console.WriteLine($"   Task Type: {message.TaskType}");
        Console.WriteLine($"   Task Name: {message.TaskName}");
        Console.WriteLine($"   Priority: {message.Priority}");

        await messageSender.SendTaskMessageAsync(message);
        Console.WriteLine("✅ Custom message sent successfully!");
    }
}