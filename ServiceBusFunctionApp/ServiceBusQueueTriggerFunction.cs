using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ServiceBusFunctionApp
{
    public class ServiceBusQueueTriggerFunction
    {
        private readonly ILogger<ServiceBusQueueTriggerFunction> _logger;

        public ServiceBusQueueTriggerFunction(ILogger<ServiceBusQueueTriggerFunction> logger)
        {
            _logger = logger;
        }

        [Function(nameof(ServiceBusQueueTriggerFunction))]
        public async Task Run(
            [ServiceBusTrigger("myqueue", Connection = "ServiceBusConnection")]
            string myQueueItem,
            FunctionContext context)
        {
            _logger.LogInformation("Service Bus queue trigger function processed message: {Message}", myQueueItem);

            try
            {
                // Parse the message if it's JSON
                var messageData = JsonSerializer.Deserialize<Dictionary<string, object>>(myQueueItem);
                if (messageData != null)
                {
                    _logger.LogInformation("Parsed message data: {Data}", JsonSerializer.Serialize(messageData));

                    // Perform your custom task here
                    await ProcessMessage(messageData);
                }
                else
                {
                    _logger.LogWarning("Failed to parse message as JSON, treating as text: {Message}", myQueueItem);
                    await ProcessTextMessage(myQueueItem);
                }

                _logger.LogInformation("Message processing completed successfully");
            }
            catch (JsonException)
            {
                // Handle non-JSON messages
                _logger.LogInformation("Processing non-JSON message: {Message}", myQueueItem);
                await ProcessTextMessage(myQueueItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message}", myQueueItem);
                throw; // Re-throw to trigger retry policy
            }
        }

        private async Task ProcessMessage(Dictionary<string, object> messageData)
        {
            // Simulate some processing work
            await Task.Delay(1000);

            _logger.LogInformation("Custom task executed for structured message with {Count} properties", 
                messageData.Count);

            // Add your custom business logic here
            // For example:
            // - Update database records
            // - Call external APIs
            // - Send notifications
            // - Transform and forward data
        }

        private async Task ProcessTextMessage(string message)
        {
            // Simulate some processing work
            await Task.Delay(500);

            _logger.LogInformation("Custom task executed for text message with length {Length}", 
                message.Length);

            // Add your custom business logic here for simple text messages
        }
    }
}