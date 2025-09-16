#!/bin/bash

# Azure Function App with Service Bus - Deployment Script
# This script deploys the Azure infrastructure and the function app

set -e

# Configuration
RESOURCE_GROUP_NAME="rg-servicebus-function"
LOCATION="East US"
PROJECT_NAME="servicebusfunction"
SUBSCRIPTION_ID=""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}🚀 Azure Function App with Service Bus Deployment Script${NC}"
echo "=================================================="

# Function to print colored output
print_status() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    print_error "Azure CLI is not installed. Please install it first."
    exit 1
fi

# Check if user is logged in
if ! az account show &> /dev/null; then
    print_warning "Not logged in to Azure. Please log in first."
    az login
fi

# Get subscription ID if not set
if [ -z "$SUBSCRIPTION_ID" ]; then
    SUBSCRIPTION_ID=$(az account show --query id --output tsv)
    print_status "Using subscription: $SUBSCRIPTION_ID"
fi

# Set the subscription
az account set --subscription "$SUBSCRIPTION_ID"

# Create resource group
print_status "Creating resource group: $RESOURCE_GROUP_NAME"
az group create \
    --name "$RESOURCE_GROUP_NAME" \
    --location "$LOCATION"

# Deploy infrastructure using Bicep
print_status "Deploying Azure infrastructure..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --template-file "Infrastructure/Bicep/main.bicep" \
    --parameters projectName="$PROJECT_NAME" \
    --query 'properties.outputs' \
    --output json)

# Extract outputs
SERVICE_BUS_NAMESPACE=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.serviceBusNamespace.value')
QUEUE_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.queueName.value')
FUNCTION_APP_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.functionAppName.value')
SERVICE_BUS_CONNECTION_STRING=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.serviceBusConnectionString.value')

print_status "Infrastructure deployed successfully!"
echo "Service Bus Namespace: $SERVICE_BUS_NAMESPACE"
echo "Queue Name: $QUEUE_NAME"
echo "Function App Name: $FUNCTION_APP_NAME"

# Build and deploy the function app
print_status "Building the function app..."
dotnet build --configuration Release

print_status "Publishing the function app..."
dotnet publish --configuration Release --output ./publish

print_status "Deploying function app to Azure..."
# Create a zip file for deployment
cd publish
zip -r ../function-app.zip .
cd ..

# Deploy the function app
az functionapp deployment source config-zip \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --name "$FUNCTION_APP_NAME" \
    --src function-app.zip

print_status "Function app deployed successfully!"

# Clean up
rm -f function-app.zip
rm -rf publish

echo ""
echo -e "${GREEN}🎉 Deployment completed successfully!${NC}"
echo "=================================================="
echo "📋 Deployment Summary:"
echo "  Resource Group: $RESOURCE_GROUP_NAME"
echo "  Function App: $FUNCTION_APP_NAME"
echo "  Service Bus Namespace: $SERVICE_BUS_NAMESPACE"
echo "  Queue Name: $QUEUE_NAME"
echo ""
echo "🔗 Useful Links:"
echo "  Function App URL: https://$FUNCTION_APP_NAME.azurewebsites.net"
echo "  Azure Portal: https://portal.azure.com/#@/resource/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP_NAME"
echo ""
echo "📝 Next Steps:"
echo "  1. Test the function by sending messages to the Service Bus queue"
echo "  2. Monitor the function logs in Application Insights"
echo "  3. Set up alerts and monitoring as needed"
echo ""
echo "⚠️  Remember to update your local.settings.json with the actual Service Bus connection string:"
echo "  ServiceBusConnectionString: (hidden for security)"