# Deployment Guide

This guide provides detailed instructions for deploying the Azure Function App with Service Bus integration to different environments.

## Deployment Options

1. **Automated Deployment** (Recommended for quick setup)
2. **Manual Deployment** (Better for understanding the process)
3. **CI/CD Pipeline** (Best for production)

## Prerequisites

- Azure subscription with appropriate permissions
- Azure CLI installed and configured
- .NET 8.0 SDK
- Git (for cloning the repository)

## Option 1: Automated Deployment

### Quick Deployment Script

The repository includes deployment scripts for both Linux/macOS and Windows:

#### Linux/macOS
```bash
# Clone the repository
git clone <repository-url>
cd testFunctionApp

# Make script executable
chmod +x Scripts/deploy.sh

# Run deployment
./Scripts/deploy.sh
```

#### Windows (PowerShell)
```powershell
# Clone the repository
git clone <repository-url>
cd testFunctionApp

# Run deployment
./Scripts/deploy.ps1
```

### Script Configuration

You can customize the deployment by editing the script variables:

```bash
# In deploy.sh
RESOURCE_GROUP_NAME="rg-servicebus-function"
LOCATION="East US"
PROJECT_NAME="servicebusfunction"
```

```powershell
# In deploy.ps1
param(
    [string]$ResourceGroupName = "rg-servicebus-function",
    [string]$Location = "East US",
    [string]$ProjectName = "servicebusfunction"
)
```

### Custom Parameters

You can also pass parameters to the scripts:

```bash
# Linux/macOS with custom parameters
./Scripts/deploy.sh \
  --resource-group "my-custom-rg" \
  --location "West US 2" \
  --project-name "myapp"
```

```powershell
# Windows with custom parameters
./Scripts/deploy.ps1 \
  -ResourceGroupName "my-custom-rg" \
  -Location "West US 2" \
  -ProjectName "myapp"
```

## Option 2: Manual Deployment

### Step 1: Prepare Azure Environment

1. **Login to Azure**
   ```bash
   az login
   ```

2. **Set Subscription**
   ```bash
   az account set --subscription "your-subscription-id"
   ```

3. **Create Resource Group**
   ```bash
   az group create \
     --name rg-servicebus-function \
     --location "East US"
   ```

### Step 2: Deploy Infrastructure

#### Using Bicep Template (Recommended)

```bash
az deployment group create \
  --resource-group rg-servicebus-function \
  --template-file Infrastructure/Bicep/main.bicep \
  --parameters projectName=servicebusfunction
```

#### Using ARM Template

```bash
az deployment group create \
  --resource-group rg-servicebus-function \
  --template-file Infrastructure/ARM-Templates/azuredeploy.json \
  --parameters projectName=servicebusfunction
```

#### Verify Deployment

```bash
# List all resources in the resource group
az resource list \
  --resource-group rg-servicebus-function \
  --output table
```

### Step 3: Build and Package Function App

1. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

2. **Build Project**
   ```bash
   dotnet build --configuration Release
   ```

3. **Publish Function App**
   ```bash
   dotnet publish \
     --configuration Release \
     --output ./publish
   ```

4. **Create Deployment Package**
   ```bash
   cd publish
   zip -r ../function-app.zip .
   cd ..
   ```

### Step 4: Deploy Function App

1. **Get Function App Name**
   ```bash
   FUNCTION_APP_NAME=$(az functionapp list \
     --resource-group rg-servicebus-function \
     --query "[0].name" \
     --output tsv)
   ```

2. **Deploy Package**
   ```bash
   az functionapp deployment source config-zip \
     --resource-group rg-servicebus-function \
     --name $FUNCTION_APP_NAME \
     --src function-app.zip
   ```

3. **Verify Deployment**
   ```bash
   az functionapp function list \
     --resource-group rg-servicebus-function \
     --name $FUNCTION_APP_NAME
   ```

### Step 5: Configure Application Settings

1. **Get Service Bus Connection String**
   ```bash
   SERVICE_BUS_NAMESPACE=$(az servicebus namespace list \
     --resource-group rg-servicebus-function \
     --query "[0].name" \
     --output tsv)

   CONNECTION_STRING=$(az servicebus namespace authorization-rule keys list \
     --resource-group rg-servicebus-function \
     --namespace-name $SERVICE_BUS_NAMESPACE \
     --name RootManageSharedAccessKey \
     --query primaryConnectionString \
     --output tsv)
   ```

2. **Update Function App Settings** (if needed)
   ```bash
   az functionapp config appsettings set \
     --resource-group rg-servicebus-function \
     --name $FUNCTION_APP_NAME \
     --settings "ServiceBusConnectionString=$CONNECTION_STRING"
   ```

## Option 3: CI/CD Pipeline

### GitHub Actions

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy Azure Function App

on:
  push:
    branches: [ main ]
  workflow_dispatch:

env:
  AZURE_FUNCTIONAPP_PACKAGE_PATH: '.'
  DOTNET_VERSION: '8.0.x'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
    - name: 'Checkout GitHub Action'
      uses: actions/checkout@v3

    - name: Setup DotNet ${{ env.DOTNET_VERSION }} Environment
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: 'Resolve Project Dependencies Using Dotnet'
      shell: bash
      run: |
        pushd './${{ env.AZURE_FUNCTIONAPP_PACKAGE_PATH }}'
        dotnet build --configuration Release --output ./output
        popd

    - name: 'Login to Azure'
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}

    - name: 'Deploy Infrastructure'
      run: |
        az deployment group create \
          --resource-group ${{ vars.AZURE_RG }} \
          --template-file Infrastructure/Bicep/main.bicep \
          --parameters projectName=${{ vars.PROJECT_NAME }}

    - name: 'Run Azure Functions Action'
      uses: Azure/functions-action@v1
      id: fa
      with:
        app-name: ${{ vars.AZURE_FUNCTIONAPP_NAME }}
        package: '${{ env.AZURE_FUNCTIONAPP_PACKAGE_PATH }}/output'
```

### Azure DevOps

Create `azure-pipelines.yml`:

```yaml
trigger:
- main

variables:
  vmImageName: 'ubuntu-latest'
  workingDirectory: '.'
  functionAppName: 'your-function-app-name'
  resourceGroupName: 'rg-servicebus-function'

stages:
- stage: Build
  displayName: Build stage
  jobs:
  - job: Build
    displayName: Build
    pool:
      vmImage: $(vmImageName)
    steps:
    - task: DotNetCoreCLI@2
      displayName: Build
      inputs:
        command: 'build'
        projects: |
          $(workingDirectory)/*.csproj
        arguments: --output $(System.DefaultWorkingDirectory)/publish_output --configuration Release

    - task: ArchiveFiles@2
      displayName: 'Archive files'
      inputs:
        rootFolderOrFile: '$(System.DefaultWorkingDirectory)/publish_output'
        includeRootFolder: false
        archiveType: zip
        archiveFile: $(Build.ArtifactStagingDirectory)/$(Build.BuildId).zip
        replaceExistingArchive: true

    - publish: $(Build.ArtifactStagingDirectory)/$(Build.BuildId).zip
      artifact: drop

- stage: Deploy
  displayName: Deploy stage
  dependsOn: Build
  condition: succeeded()
  jobs:
  - deployment: Deploy
    displayName: Deploy
    environment: 'production'
    pool:
      vmImage: $(vmImageName)
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureFunctionApp@1
            displayName: 'Azure functions app deploy'
            inputs:
              azureSubscription: '$(azureSubscription)'
              appType: functionApp
              appName: $(functionAppName)
              package: '$(Pipeline.Workspace)/drop/$(Build.BuildId).zip'
```

## Environment-Specific Deployments

### Development Environment

```bash
# Use basic SKUs for cost optimization
az deployment group create \
  --resource-group rg-servicebus-function-dev \
  --template-file Infrastructure/Bicep/main.bicep \
  --parameters \
    projectName=servicebusfunction-dev \
    serviceBusSkuName=Basic
```

### Staging Environment

```bash
# Use standard SKUs with better features
az deployment group create \
  --resource-group rg-servicebus-function-staging \
  --template-file Infrastructure/Bicep/main.bicep \
  --parameters \
    projectName=servicebusfunction-staging \
    serviceBusSkuName=Standard
```

### Production Environment

```bash
# Use premium SKUs with high availability
az deployment group create \
  --resource-group rg-servicebus-function-prod \
  --template-file Infrastructure/Bicep/main.bicep \
  --parameters \
    projectName=servicebusfunction-prod \
    serviceBusSkuName=Premium \
    location="East US 2"
```

## Post-Deployment Configuration

### 1. Verify Deployment

```bash
# Check function app status
az functionapp show \
  --resource-group rg-servicebus-function \
  --name $FUNCTION_APP_NAME \
  --query "state"

# List functions
az functionapp function list \
  --resource-group rg-servicebus-function \
  --name $FUNCTION_APP_NAME
```

### 2. Test the Deployment

```bash
# Send a test message
az servicebus queue send \
  --resource-group rg-servicebus-function \
  --namespace-name $SERVICE_BUS_NAMESPACE \
  --name task-queue \
  --body '{
    "TaskId": "deploy-test-123",
    "TaskName": "Deployment Test Task",
    "TaskType": "data-processing",
    "CreatedAt": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'",
    "CreatedBy": "deployment-test",
    "Priority": 1,
    "Parameters": {}
  }'
```

### 3. Monitor Function Execution

```bash
# Get function app logs
az functionapp log tail \
  --resource-group rg-servicebus-function \
  --name $FUNCTION_APP_NAME
```

### 4. Set Up Monitoring

```bash
# Enable Application Insights
az functionapp config appsettings set \
  --resource-group rg-servicebus-function \
  --name $FUNCTION_APP_NAME \
  --settings "APPINSIGHTS_INSTRUMENTATIONKEY=$(az resource show \
    --resource-group rg-servicebus-function \
    --resource-type microsoft.insights/components \
    --name $FUNCTION_APP_NAME-insights \
    --query properties.InstrumentationKey \
    --output tsv)"
```

## Rollback Procedures

### 1. Quick Rollback to Previous Version

```bash
# List deployment history
az functionapp deployment list \
  --resource-group rg-servicebus-function \
  --name $FUNCTION_APP_NAME

# Rollback to specific deployment
az functionapp deployment source sync \
  --resource-group rg-servicebus-function \
  --name $FUNCTION_APP_NAME
```

### 2. Infrastructure Rollback

```bash
# Delete current deployment
az deployment group delete \
  --resource-group rg-servicebus-function \
  --name main

# Redeploy previous version
az deployment group create \
  --resource-group rg-servicebus-function \
  --template-file Infrastructure/Bicep/main.bicep \
  --parameters @previous-parameters.json
```

## Troubleshooting Deployment Issues

### Common Issues

1. **Deployment Fails with Permissions Error**
   ```bash
   # Check your role assignments
   az role assignment list --assignee $(az account show --query user.name --output tsv)
   ```

2. **Function App Won't Start**
   ```bash
   # Check application settings
   az functionapp config appsettings list \
     --resource-group rg-servicebus-function \
     --name $FUNCTION_APP_NAME
   ```

3. **Service Bus Connection Issues**
   ```bash
   # Test Service Bus connectivity
   az servicebus namespace show \
     --resource-group rg-servicebus-function \
     --name $SERVICE_BUS_NAMESPACE \
     --query "status"
   ```

### Diagnostic Commands

```bash
# Get deployment operation details
az deployment group show \
  --resource-group rg-servicebus-function \
  --name main \
  --query "properties.error"

# Check function app logs
az functionapp log download \
  --resource-group rg-servicebus-function \
  --name $FUNCTION_APP_NAME

# Validate ARM template
az deployment group validate \
  --resource-group rg-servicebus-function \
  --template-file Infrastructure/ARM-Templates/azuredeploy.json
```

## Cleanup

### Delete Resources

```bash
# Delete entire resource group (careful!)
az group delete \
  --name rg-servicebus-function \
  --yes \
  --no-wait
```

### Delete Specific Resources

```bash
# Delete function app only
az functionapp delete \
  --resource-group rg-servicebus-function \
  --name $FUNCTION_APP_NAME

# Delete Service Bus namespace
az servicebus namespace delete \
  --resource-group rg-servicebus-function \
  --name $SERVICE_BUS_NAMESPACE
```