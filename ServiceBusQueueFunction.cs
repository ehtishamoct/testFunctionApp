using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ServiceBusFunctionApp;

public class ServiceBusQueueFunction
{
    private readonly ILogger<ServiceBusQueueFunction> _logger;

    public ServiceBusQueueFunction(ILogger<ServiceBusQueueFunction> logger)
    {
        _logger = logger;
    }

    [Function("ProcessTaskMessage")]
    public async Task Run(
        [ServiceBusTrigger("task-queue", Connection = "ServiceBusConnectionString")]
        string message,
        FunctionContext context)
    {
        _logger.LogInformation("Service Bus queue trigger function processed message: {Message}", message);

        try
        {
            // Deserialize the message to extract task details
            var taskMessage = JsonSerializer.Deserialize<TaskMessage>(message);
            
            if (taskMessage == null)
            {
                _logger.LogWarning("Received null or invalid task message");
                return;
            }

            _logger.LogInformation("Processing task: {TaskId} - {TaskName}", taskMessage.TaskId, taskMessage.TaskName);

            // Simulate task execution
            await ExecuteTask(taskMessage);

            _logger.LogInformation("Successfully completed task: {TaskId}", taskMessage.TaskId);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize task message: {Message}", message);
            throw; // Re-throw to move message to dead letter queue
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing task message: {Message}", message);
            throw; // Re-throw to move message to dead letter queue
        }
    }

    private async Task ExecuteTask(TaskMessage taskMessage)
    {
        _logger.LogInformation("Starting execution of task: {TaskId} - {TaskName}", taskMessage.TaskId, taskMessage.TaskName);

        // Simulate different types of tasks based on task type
        switch (taskMessage.TaskType.ToLowerInvariant())
        {
            case "data-processing":
                await SimulateDataProcessing(taskMessage);
                break;
            case "file-upload":
                await SimulateFileUpload(taskMessage);
                break;
            case "email-notification":
                await SimulateEmailNotification(taskMessage);
                break;
            case "report-generation":
                await SimulateReportGeneration(taskMessage);
                break;
            default:
                await SimulateGenericTask(taskMessage);
                break;
        }

        _logger.LogInformation("Task execution completed: {TaskId}", taskMessage.TaskId);
    }

    private async Task SimulateDataProcessing(TaskMessage taskMessage)
    {
        _logger.LogInformation("Processing data for task: {TaskId}", taskMessage.TaskId);
        
        // Simulate data processing work
        await Task.Delay(TimeSpan.FromSeconds(2)); // Simulate processing time
        
        var recordsProcessed = Random.Shared.Next(100, 1000);
        _logger.LogInformation("Data processing completed. Records processed: {RecordsProcessed}", recordsProcessed);
    }

    private async Task SimulateFileUpload(TaskMessage taskMessage)
    {
        _logger.LogInformation("Uploading file for task: {TaskId}", taskMessage.TaskId);
        
        // Simulate file upload work
        await Task.Delay(TimeSpan.FromSeconds(3)); // Simulate upload time
        
        var fileSizeMB = Random.Shared.Next(1, 100);
        _logger.LogInformation("File upload completed. File size: {FileSizeMB} MB", fileSizeMB);
    }

    private async Task SimulateEmailNotification(TaskMessage taskMessage)
    {
        _logger.LogInformation("Sending email notification for task: {TaskId}", taskMessage.TaskId);
        
        // Simulate email sending
        await Task.Delay(TimeSpan.FromMilliseconds(500)); // Simulate email send time
        
        _logger.LogInformation("Email notification sent successfully");
    }

    private async Task SimulateReportGeneration(TaskMessage taskMessage)
    {
        _logger.LogInformation("Generating report for task: {TaskId}", taskMessage.TaskId);
        
        // Simulate report generation
        await Task.Delay(TimeSpan.FromSeconds(5)); // Simulate report generation time
        
        var pageCount = Random.Shared.Next(10, 100);
        _logger.LogInformation("Report generation completed. Pages generated: {PageCount}", pageCount);
    }

    private async Task SimulateGenericTask(TaskMessage taskMessage)
    {
        _logger.LogInformation("Executing generic task: {TaskId}", taskMessage.TaskId);
        
        // Simulate generic work
        await Task.Delay(TimeSpan.FromSeconds(1)); // Simulate work time
        
        _logger.LogInformation("Generic task completed successfully");
    }
}

public class TaskMessage
{
    public string TaskId { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public int Priority { get; set; } = 0;
}