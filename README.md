# Azure Function App with Service Bus Queue Trigger

This repository contains a complete implementation of an Azure Function App that triggers from messages in an Azure Service Bus queue. The function executes various types of tasks upon receiving messages, demonstrating a scalable, event-driven architecture suitable for background processing workloads.

## 🏗️ Architecture Overview

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Client Apps   │───▶│ Service Bus     │───▶│ Azure Function  │
│                 │    │ Queue           │    │ App             │
│ • Web Apps      │    │ • task-queue    │    │ • ProcessTask   │
│ • APIs          │    │ • Dead Letter   │    │ • Task Sim.     │
│ • Background    │    │ • Retry Logic   │    │ • Logging       │
│   Services      │    │                 │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                                       │
                                                       ▼
                                               ┌─────────────────┐
                                               │ Application     │
                                               │ Insights        │
                                               │ • Monitoring    │
                                               │ • Logging       │
                                               │ • Alerts        │
                                               └─────────────────┘
```

## 🚀 Quick Start

### Prerequisites

1. **Azure Subscription** - [Create a free account](https://azure.microsoft.com/free/)
2. **Azure CLI** - [Install Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
3. **.NET 8.0 SDK** - [Download .NET 8.0](https://dotnet.microsoft.com/download)
4. **Visual Studio Code** (recommended) - [Download VS Code](https://code.visualstudio.com/)

### Option 1: Automated Deployment (Recommended)

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd testFunctionApp
   ```

2. **Login to Azure:**
   ```bash
   az login
   ```

3. **Run the deployment script:**
   
   **Linux/macOS:**
   ```bash
   chmod +x Scripts/deploy.sh
   ./Scripts/deploy.sh
   ```
   
   **Windows (PowerShell):**
   ```powershell
   ./Scripts/deploy.ps1
   ```

### Option 2: Manual Step-by-Step Setup

#### Step 1: Create Azure Resources

1. **Create Resource Group:**
   ```bash
   az group create --name rg-servicebus-function --location "East US"
   ```

2. **Deploy infrastructure using Bicep:**
   ```bash
   az deployment group create \
     --resource-group rg-servicebus-function \
     --template-file Infrastructure/Bicep/main.bicep \
     --parameters projectName=servicebusfunction
   ```

#### Step 2: Configure Local Development

1. **Update local.settings.json:**
   
   Get your Service Bus connection string from the Azure portal:
   ```bash
   az servicebus namespace authorization-rule keys list \
     --resource-group rg-servicebus-function \
     --namespace-name <your-servicebus-namespace> \
     --name RootManageSharedAccessKey
   ```

   Update `local.settings.json`:
   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
       "ServiceBusConnectionString": "<your-actual-connection-string>",
       "QueueName": "task-queue"
     }
   }
   ```

#### Step 3: Build and Test Locally

1. **Build the project:**
   ```bash
   dotnet build
   ```

2. **Run the function locally (if you have Azure Functions Core Tools):**
   ```bash
   func start
   ```

3. **Test with the test client:**
   ```bash
   cd TestClient
   dotnet run
   ```

#### Step 4: Deploy to Azure

1. **Build and publish:**
   ```bash
   dotnet publish --configuration Release --output ./publish
   ```

2. **Create deployment package:**
   ```bash
   cd publish
   zip -r ../function-app.zip .
   cd ..
   ```

3. **Deploy to Azure:**
   ```bash
   az functionapp deployment source config-zip \
     --resource-group rg-servicebus-function \
     --name <your-function-app-name> \
     --src function-app.zip
   ```

## 📁 Project Structure

```
├── ServiceBusFunctionApp.csproj         # Main project file
├── Program.cs                           # Function app entry point
├── host.json                           # Function host configuration
├── local.settings.json                 # Local development settings
├── ServiceBusQueueFunction.cs          # Main function implementation
├── Utilities/
│   └── ServiceBusMessageSender.cs     # Utility for sending test messages
├── TestClient/
│   ├── Program.cs                      # Test client application
│   └── TestClient.csproj              # Test client project file
├── Infrastructure/
│   ├── ARM-Templates/
│   │   └── azuredeploy.json           # ARM template for infrastructure
│   └── Bicep/
│       └── main.bicep                 # Bicep template for infrastructure
├── Scripts/
│   ├── deploy.sh                      # Linux/macOS deployment script
│   └── deploy.ps1                     # Windows PowerShell deployment script
└── README.md                          # This file
```

## 🔧 Function Implementation Details

### Service Bus Queue Function

The main function (`ServiceBusQueueFunction.cs`) includes:

- **Service Bus Trigger**: Automatically processes messages from the queue
- **Task Simulation**: Demonstrates different types of background tasks
- **Error Handling**: Comprehensive error handling with logging
- **Message Deserialization**: JSON-based message processing
- **Logging**: Structured logging with Application Insights integration

### Supported Task Types

The function can process different types of tasks:

1. **Data Processing** - Simulates batch data processing operations
2. **File Upload** - Simulates file upload and processing tasks
3. **Email Notification** - Simulates sending email notifications
4. **Report Generation** - Simulates report generation tasks
5. **Generic Tasks** - Fallback for custom task types

### Message Format

Messages should be JSON formatted with the following structure:

```json
{
  "TaskId": "12345678-1234-5678-9012-123456789abc",
  "TaskName": "Process Customer Data",
  "TaskType": "data-processing",
  "CreatedAt": "2024-01-15T10:30:00Z",
  "CreatedBy": "user@example.com",
  "Priority": 3,
  "Parameters": {
    "inputPath": "/data/input",
    "outputPath": "/data/output",
    "timeout": 300
  }
}
```

## 🧪 Testing

### Using the Test Client

The included test client (`TestClient/Program.cs`) provides an interactive way to send test messages:

1. **Run the test client:**
   ```bash
   cd TestClient
   dotnet run
   ```

2. **Choose from available options:**
   - Send a single test message
   - Send multiple test messages
   - Send custom message
   - Exit

### Manual Testing with Azure Service Bus Explorer

1. **Use Azure Portal:**
   - Navigate to your Service Bus namespace
   - Select the queue
   - Use "Service Bus Explorer" to send test messages

2. **Use Azure CLI:**
   ```bash
   az servicebus queue send \
     --resource-group rg-servicebus-function \
     --namespace-name <your-namespace> \
     --name task-queue \
     --body '{"TaskId":"test-123","TaskName":"Test Task","TaskType":"data-processing","CreatedAt":"2024-01-15T10:30:00Z","CreatedBy":"test","Priority":1,"Parameters":{}}'
   ```

## 📊 Monitoring and Logging

### Application Insights Integration

The function app is configured with Application Insights for comprehensive monitoring:

1. **Function Execution Logs**: Track function invocations and performance
2. **Custom Telemetry**: Business logic metrics and custom events
3. **Error Tracking**: Automatic exception capture and analysis
4. **Performance Monitoring**: Response times and throughput metrics

### Accessing Logs

1. **Azure Portal:**
   - Navigate to your Function App
   - Go to "Application Insights" → "Logs"
   
2. **Sample Queries:**
   ```kusto
   // Function executions in the last hour
   requests
   | where timestamp > ago(1h)
   | where cloud_RoleName == "your-function-app-name"
   
   // Error analysis
   exceptions
   | where timestamp > ago(24h)
   | summarize count() by type, outerMessage
   
   // Custom task processing metrics
   traces
   | where message contains "Task execution completed"
   | summarize count() by bin(timestamp, 1h)
   ```

### Setting Up Alerts

Create alerts for important metrics:

1. **Failed Function Executions**
2. **High Error Rate**
3. **Message Processing Delays**
4. **Queue Length Thresholds**

## 🔒 Security Considerations

### Connection Strings

- Use **Azure Key Vault** for production connection strings
- Never commit connection strings to source control
- Use **Managed Identity** when possible

### Access Control

- Configure appropriate **RBAC roles** for Service Bus access
- Use **Azure AD authentication** instead of connection strings where possible
- Implement **network restrictions** and **private endpoints** for enhanced security

### Example with Managed Identity

```csharp
// Update Program.cs to use Managed Identity
services.Configure<ServiceBusClientOptions>(options =>
{
    options.TransportType = ServiceBusTransportType.AmqpTcp;
});

// Use DefaultAzureCredential instead of connection string
var credential = new DefaultAzureCredential();
var client = new ServiceBusClient("your-namespace.servicebus.windows.net", credential);
```

## 🚀 Production Deployment

### CI/CD Pipeline

Consider implementing a CI/CD pipeline using:

1. **GitHub Actions**
2. **Azure DevOps**
3. **Azure Resource Manager (ARM) templates**
4. **Infrastructure as Code (IaC)**

### Scaling Considerations

1. **Function App Plan**: Consider Premium or Dedicated plans for production
2. **Service Bus Tiers**: Standard or Premium for production workloads
3. **Concurrent Executions**: Configure based on your throughput requirements
4. **Dead Letter Queue**: Implement proper dead letter handling

### Environment Configuration

Use different configurations for different environments:

- **Development**: Basic SKUs, local storage emulator
- **Staging**: Standard SKUs, separate namespaces
- **Production**: Premium SKUs, high availability, monitoring

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Troubleshooting

### Common Issues

1. **Connection String Issues**
   - Verify the Service Bus connection string is correct
   - Ensure the queue exists in the namespace
   - Check access permissions

2. **Function Not Triggering**
   - Verify the queue name matches the configuration
   - Check if messages are in the dead letter queue
   - Review Application Insights logs for errors

3. **Local Development Issues**
   - Install Azure Storage Emulator or use Azure Storage
   - Ensure all NuGet packages are restored
   - Check local.settings.json configuration

### Getting Help

- Review [Azure Functions documentation](https://docs.microsoft.com/azure/azure-functions/)
- Check [Azure Service Bus documentation](https://docs.microsoft.com/azure/service-bus-messaging/)
- Open an issue in this repository
- Contact Azure Support for Azure-specific issues

## 🔗 Additional Resources

- [Azure Functions Best Practices](https://docs.microsoft.com/azure/azure-functions/functions-best-practices)
- [Service Bus Messaging Patterns](https://docs.microsoft.com/azure/service-bus-messaging/service-bus-messaging-overview)
- [Application Insights for Azure Functions](https://docs.microsoft.com/azure/azure-functions/functions-monitoring)
- [Azure Function App Security](https://docs.microsoft.com/azure/azure-functions/security-concepts)