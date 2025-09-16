# Azure Service Bus Setup Guide

This guide walks you through setting up Azure Service Bus for use with the Function App.

## Prerequisites

- Azure subscription
- Azure CLI installed and configured
- Appropriate permissions to create Azure resources

## Step 1: Create Service Bus Namespace

### Using Azure Portal

1. **Navigate to Azure Portal**
   - Go to [portal.azure.com](https://portal.azure.com)
   - Click "Create a resource"

2. **Search for Service Bus**
   - Type "Service Bus" in the search box
   - Select "Service Bus" from the results

3. **Configure the Namespace**
   - **Subscription**: Select your Azure subscription
   - **Resource Group**: Create new or select existing
   - **Namespace Name**: Choose a unique name (e.g., `your-app-servicebus`)
   - **Location**: Select the same region as your Function App
   - **Pricing Tier**: 
     - **Basic**: For development/testing
     - **Standard**: For production (supports topics/subscriptions)
     - **Premium**: For high-performance scenarios

4. **Review and Create**
   - Click "Review + create"
   - Click "Create"

### Using Azure CLI

```bash
# Set variables
RESOURCE_GROUP="rg-servicebus-function"
NAMESPACE_NAME="your-app-servicebus"
LOCATION="East US"

# Create resource group (if it doesn't exist)
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create Service Bus namespace
az servicebus namespace create \
  --resource-group $RESOURCE_GROUP \
  --name $NAMESPACE_NAME \
  --location $LOCATION \
  --sku Standard
```

## Step 2: Create the Queue

### Using Azure Portal

1. **Navigate to your Service Bus Namespace**
   - Find your namespace in the Azure portal
   - Click on the namespace name

2. **Create Queue**
   - In the left menu, click "Queues"
   - Click "+ Queue"
   - **Name**: `task-queue`
   - **Max Queue Size**: 1 GB (or as needed)
   - **Message time to live**: Default (14 days)
   - **Lock duration**: 1 minute
   - **Enable duplicate detection**: No (unless needed)
   - **Enable dead lettering**: Yes (recommended)
   - **Enable sessions**: No (unless needed)
   - **Enable partitioning**: No (unless needed)

3. **Create the Queue**
   - Click "Create"

### Using Azure CLI

```bash
# Create the queue
az servicebus queue create \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $NAMESPACE_NAME \
  --name task-queue \
  --max-size 1024 \
  --default-message-time-to-live P14D \
  --lock-duration PT1M \
  --max-delivery-count 5 \
  --enable-dead-lettering-on-message-expiration true
```

## Step 3: Get Connection String

### Using Azure Portal

1. **Navigate to Shared Access Policies**
   - In your Service Bus namespace
   - Click "Shared access policies" in the left menu
   - Click "RootManageSharedAccessKey"

2. **Copy Connection String**
   - Copy the "Primary Connection String"
   - This is what you'll use in your Function App configuration

### Using Azure CLI

```bash
# Get the connection string
az servicebus namespace authorization-rule keys list \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $NAMESPACE_NAME \
  --name RootManageSharedAccessKey \
  --query primaryConnectionString \
  --output tsv
```

## Step 4: Configure Security (Production)

### Create Custom Access Policy

Instead of using the root access key, create a custom policy with minimal permissions:

```bash
# Create a custom access policy for the function app
az servicebus namespace authorization-rule create \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $NAMESPACE_NAME \
  --name FunctionAppPolicy \
  --rights Listen

# Get the connection string for the custom policy
az servicebus namespace authorization-rule keys list \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $NAMESPACE_NAME \
  --name FunctionAppPolicy \
  --query primaryConnectionString \
  --output tsv
```

### Using Managed Identity (Recommended for Production)

1. **Enable Managed Identity for Function App**
   ```bash
   az functionapp identity assign \
     --name your-function-app-name \
     --resource-group $RESOURCE_GROUP
   ```

2. **Grant Permissions to Service Bus**
   ```bash
   # Get the Function App's managed identity
   FUNCTION_APP_IDENTITY=$(az functionapp identity show \
     --name your-function-app-name \
     --resource-group $RESOURCE_GROUP \
     --query principalId \
     --output tsv)

   # Assign Service Bus Data Receiver role
   az role assignment create \
     --assignee $FUNCTION_APP_IDENTITY \
     --role "Azure Service Bus Data Receiver" \
     --scope "/subscriptions/your-subscription-id/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.ServiceBus/namespaces/$NAMESPACE_NAME"
   ```

3. **Update Function App Configuration**
   - Remove the connection string
   - Set `ServiceBusConnection__fullyQualifiedNamespace` to your namespace FQDN
   - Example: `your-namespace.servicebus.windows.net`

## Step 5: Testing the Setup

### Send Test Message using Azure CLI

```bash
# Send a test message to the queue
az servicebus queue send \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $NAMESPACE_NAME \
  --name task-queue \
  --body '{
    "TaskId": "test-123",
    "TaskName": "Test Task",
    "TaskType": "data-processing",
    "CreatedAt": "2024-01-15T10:30:00Z",
    "CreatedBy": "test",
    "Priority": 1,
    "Parameters": {}
  }'
```

### Using Service Bus Explorer

1. **Navigate to your Queue**
   - In the Azure portal, go to your Service Bus namespace
   - Click on your queue name

2. **Use Service Bus Explorer**
   - Click "Service Bus Explorer"
   - Click "Send Messages"
   - Enter your test message in JSON format
   - Click "Send"

## Queue Configuration Options

### Message Properties

- **Time to Live**: How long messages stay in the queue
- **Lock Duration**: How long a message is locked when being processed
- **Max Delivery Count**: How many times to retry failed messages
- **Dead Letter Queue**: Where failed messages go after max retries

### Performance Settings

- **Partitioning**: Improves throughput for high-volume scenarios
- **Duplicate Detection**: Prevents duplicate messages
- **Sessions**: Enables ordered message processing

### Example Production Configuration

```bash
az servicebus queue create \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $NAMESPACE_NAME \
  --name task-queue \
  --max-size 5120 \
  --default-message-time-to-live P7D \
  --lock-duration PT5M \
  --max-delivery-count 3 \
  --enable-dead-lettering-on-message-expiration true \
  --enable-duplicate-detection true \
  --duplicate-detection-history-time-window PT10M
```

## Monitoring and Alerts

### Set up Monitoring

1. **Enable Diagnostic Settings**
   ```bash
   # Create Log Analytics workspace (if needed)
   az monitor log-analytics workspace create \
     --resource-group $RESOURCE_GROUP \
     --workspace-name servicebus-logs

   # Enable diagnostic settings
   az monitor diagnostic-settings create \
     --name servicebus-diagnostics \
     --resource "/subscriptions/your-subscription-id/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.ServiceBus/namespaces/$NAMESPACE_NAME" \
     --logs '[{"category":"OperationalLogs","enabled":true}]' \
     --metrics '[{"category":"AllMetrics","enabled":true}]' \
     --workspace servicebus-logs
   ```

### Create Alerts

```bash
# Alert for high queue length
az monitor metrics alert create \
  --name "High Queue Length" \
  --resource-group $RESOURCE_GROUP \
  --scopes "/subscriptions/your-subscription-id/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.ServiceBus/namespaces/$NAMESPACE_NAME" \
  --condition "avg ActiveMessages > 100" \
  --description "Alert when queue has more than 100 active messages"
```

## Troubleshooting

### Common Issues

1. **Connection String Not Working**
   - Verify the connection string is complete and correct
   - Check if the namespace name is correct
   - Ensure the access key hasn't been regenerated

2. **Queue Not Found**
   - Verify the queue name matches exactly
   - Check if the queue exists in the correct namespace

3. **Access Denied**
   - Verify the access policy has the correct permissions
   - Check if using Managed Identity, ensure roles are assigned correctly

4. **Messages Not Being Processed**
   - Check if messages are in the dead letter queue
   - Verify the Function App is running and deployed correctly
   - Check Application Insights logs for errors

### Useful Azure CLI Commands

```bash
# List all queues in namespace
az servicebus queue list \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $NAMESPACE_NAME

# Get queue details
az servicebus queue show \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $NAMESPACE_NAME \
  --name task-queue

# Get queue message count
az servicebus queue show \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $NAMESPACE_NAME \
  --name task-queue \
  --query "countDetails.activeMessageCount"

# Peek messages (without removing them)
az servicebus queue peek \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $NAMESPACE_NAME \
  --name task-queue \
  --max-count 10
```