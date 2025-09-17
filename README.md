# Azure Function App with Service Bus Queue Trigger

This repository contains an Azure Function App that triggers from messages in an Azure Service Bus queue. The function processes incoming messages and executes custom tasks based on the message content.

## Features

- **Service Bus Queue Trigger**: Automatically processes messages from an Azure Service Bus queue
- **JSON and Text Message Support**: Handles both structured JSON messages and plain text messages
- **Comprehensive Logging**: Uses Application Insights for detailed telemetry and logging
- **Error Handling**: Includes retry policies and error handling mechanisms
- **Configurable Processing**: Easy to customize for different business logic requirements

## Project Structure

```
ServiceBusFunctionApp/
├── ServiceBusQueueTriggerFunction.cs  # Main function with Service Bus trigger
├── Program.cs                         # Function app host configuration
├── ServiceBusFunctionApp.csproj      # Project file with dependencies
├── host.json                         # Function app runtime configuration
├── host.production.json              # Production-specific configuration
├── local.settings.json               # Local development settings
└── azure-deploy.json                 # ARM template for Azure deployment
```

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- Azure subscription with Service Bus namespace
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/) (for deployment)

## Setup Instructions

### 1. Azure Service Bus Setup

1. **Create Service Bus Namespace**:
   ```bash
   az servicebus namespace create \
     --resource-group <your-resource-group> \
     --name <your-servicebus-namespace> \
     --location <your-location> \
     --sku Standard
   ```

2. **Create Queue**:
   ```bash
   az servicebus queue create \
     --resource-group <your-resource-group> \
     --namespace-name <your-servicebus-namespace> \
     --name myqueue \
     --max-size 1024
   ```

3. **Get Connection String**:
   ```bash
   az servicebus namespace authorization-rule keys list \
     --resource-group <your-resource-group> \
     --namespace-name <your-servicebus-namespace> \
     --name RootManageSharedAccessKey
   ```

### 2. Local Development Setup

1. **Clone and navigate to the project**:
   ```bash
   git clone <repository-url>
   cd testFunctionApp/ServiceBusFunctionApp
   ```

2. **Install dependencies**:
   ```bash
   dotnet restore
   ```

3. **Update local.settings.json**:
   Replace the `ServiceBusConnection` value with your actual Service Bus connection string:
   ```json
   {
     "Values": {
       "ServiceBusConnection": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key"
     }
   }
   ```

4. **Build the project**:
   ```bash
   dotnet build
   ```

5. **Run locally**:
   ```bash
   func start
   ```

### 3. Testing the Function

**Send a JSON message to the queue**:
```bash
# Using Azure CLI
az servicebus queue message send \
  --resource-group <your-resource-group> \
  --namespace-name <your-servicebus-namespace> \
  --queue-name myqueue \
  --body '{"id": "123", "name": "Test Message", "timestamp": "2024-01-01T12:00:00Z"}'
```

**Send a text message to the queue**:
```bash
az servicebus queue message send \
  --resource-group <your-resource-group> \
  --namespace-name <your-servicebus-namespace> \
  --queue-name myqueue \
  --body "Simple text message for processing"
```

## Deployment to Azure

### Option 1: Using ARM Template (Recommended)

1. **Deploy infrastructure**:
   ```bash
   az deployment group create \
     --resource-group <your-resource-group> \
     --template-file azure-deploy.json \
     --parameters functionAppName=<your-function-app-name> \
                  serviceBusNamespaceName=<your-servicebus-namespace>
   ```

2. **Deploy function code**:
   ```bash
   func azure functionapp publish <your-function-app-name>
   ```

### Option 2: Manual Deployment

1. **Create Function App**:
   ```bash
   az functionapp create \
     --resource-group <your-resource-group> \
     --consumption-plan-location <your-location> \
     --runtime dotnet-isolated \
     --runtime-version 8.0 \
     --functions-version 4 \
     --name <your-function-app-name> \
     --storage-account <your-storage-account>
   ```

2. **Configure Application Settings**:
   ```bash
   az functionapp config appsettings set \
     --name <your-function-app-name> \
     --resource-group <your-resource-group> \
     --settings "ServiceBusConnection=<your-connection-string>"
   ```

3. **Deploy function code**:
   ```bash
   func azure functionapp publish <your-function-app-name>
   ```

## Configuration

### Environment Variables

| Setting | Description | Required |
|---------|-------------|----------|
| `ServiceBusConnection` | Service Bus connection string | Yes |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Application Insights connection string | No |
| `AzureWebJobsStorage` | Storage account connection string | Yes |

### host.json Configuration

The `host.json` file contains important configuration for Service Bus processing:

- **prefetchCount**: Number of messages to prefetch (default: 100)
- **maxConcurrentCalls**: Maximum concurrent message processing (default: 32)
- **autoComplete**: Automatically complete messages (default: true)
- **maxAutoRenewDuration**: Maximum auto-renewal duration (default: 5 minutes)

## Customization

### Adding Custom Business Logic

Edit the `ProcessMessage` and `ProcessTextMessage` methods in `ServiceBusQueueTriggerFunction.cs` to implement your specific business requirements:

```csharp
private async Task ProcessMessage(Dictionary<string, object> messageData)
{
    // Add your custom business logic here
    // Examples:
    // - Update database records
    // - Call external APIs
    // - Send notifications
    // - Transform and forward data
    
    _logger.LogInformation("Processing message with ID: {Id}", 
        messageData.GetValueOrDefault("id", "unknown"));
}
```

### Error Handling and Retry Policies

The function includes built-in error handling and retry mechanisms. Failed messages will be retried according to the retry policy defined in `host.json`.

## Monitoring and Troubleshooting

1. **View logs in Azure Portal**:
   - Navigate to your Function App
   - Go to Functions → ServiceBusQueueTriggerFunction → Monitor

2. **Application Insights**:
   - View detailed telemetry and performance metrics
   - Set up alerts for errors or performance issues

3. **Service Bus Metrics**:
   - Monitor queue length and processing rates
   - Check for dead letter messages

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.