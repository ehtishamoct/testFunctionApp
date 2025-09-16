# Azure Function App with Service Bus - PowerShell Deployment Script
# This script deploys the Azure infrastructure and the function app

param(
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "rg-servicebus-function",
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "East US",
    
    [Parameter(Mandatory=$false)]
    [string]$ProjectName = "servicebusfunction",
    
    [Parameter(Mandatory=$false)]
    [string]$SubscriptionId = ""
)

# Colors for output
$Green = "Green"
$Yellow = "Yellow"
$Red = "Red"

function Write-Status {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor $Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor $Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor $Red
}

Write-Host "🚀 Azure Function App with Service Bus Deployment Script" -ForegroundColor $Green
Write-Host "==================================================" -ForegroundColor $Green

# Check if Azure CLI is installed
try {
    $azVersion = az --version 2>$null
    if (-not $azVersion) {
        throw "Azure CLI not found"
    }
} catch {
    Write-Error "Azure CLI is not installed. Please install it first."
    exit 1
}

# Check if user is logged in
try {
    $account = az account show 2>$null | ConvertFrom-Json
    if (-not $account) {
        throw "Not logged in"
    }
} catch {
    Write-Warning "Not logged in to Azure. Please log in first."
    az login
}

# Get subscription ID if not set
if ([string]::IsNullOrEmpty($SubscriptionId)) {
    $SubscriptionId = (az account show --query id --output tsv)
    Write-Status "Using subscription: $SubscriptionId"
}

# Set the subscription
az account set --subscription $SubscriptionId

# Create resource group
Write-Status "Creating resource group: $ResourceGroupName"
az group create --name $ResourceGroupName --location $Location

# Deploy infrastructure using Bicep
Write-Status "Deploying Azure infrastructure..."
$deploymentOutput = az deployment group create `
    --resource-group $ResourceGroupName `
    --template-file "Infrastructure/Bicep/main.bicep" `
    --parameters projectName=$ProjectName `
    --query 'properties.outputs' `
    --output json | ConvertFrom-Json

# Extract outputs
$serviceBusNamespace = $deploymentOutput.serviceBusNamespace.value
$queueName = $deploymentOutput.queueName.value
$functionAppName = $deploymentOutput.functionAppName.value
$serviceBusConnectionString = $deploymentOutput.serviceBusConnectionString.value

Write-Status "Infrastructure deployed successfully!"
Write-Host "Service Bus Namespace: $serviceBusNamespace"
Write-Host "Queue Name: $queueName"
Write-Host "Function App Name: $functionAppName"

# Build and deploy the function app
Write-Status "Building the function app..."
dotnet build --configuration Release

Write-Status "Publishing the function app..."
dotnet publish --configuration Release --output ./publish

Write-Status "Deploying function app to Azure..."
# Create a zip file for deployment
Compress-Archive -Path "./publish/*" -DestinationPath "./function-app.zip" -Force

# Deploy the function app
az functionapp deployment source config-zip `
    --resource-group $ResourceGroupName `
    --name $functionAppName `
    --src function-app.zip

Write-Status "Function app deployed successfully!"

# Clean up
Remove-Item -Path "./function-app.zip" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "./publish" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "🎉 Deployment completed successfully!" -ForegroundColor $Green
Write-Host "==================================================" -ForegroundColor $Green
Write-Host "📋 Deployment Summary:"
Write-Host "  Resource Group: $ResourceGroupName"
Write-Host "  Function App: $functionAppName"
Write-Host "  Service Bus Namespace: $serviceBusNamespace"
Write-Host "  Queue Name: $queueName"
Write-Host ""
Write-Host "🔗 Useful Links:"
Write-Host "  Function App URL: https://$functionAppName.azurewebsites.net"
Write-Host "  Azure Portal: https://portal.azure.com/#@/resource/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName"
Write-Host ""
Write-Host "📝 Next Steps:"
Write-Host "  1. Test the function by sending messages to the Service Bus queue"
Write-Host "  2. Monitor the function logs in Application Insights"
Write-Host "  3. Set up alerts and monitoring as needed"
Write-Host ""
Write-Host "⚠️  Remember to update your local.settings.json with the actual Service Bus connection string:" -ForegroundColor $Yellow
Write-Host "  ServiceBusConnectionString: (hidden for security)"