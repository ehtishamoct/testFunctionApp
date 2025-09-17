#!/bin/bash

# Test script for Azure Function App with Service Bus Queue Trigger
# This script helps test the function locally

echo "Azure Function App Test Script"
echo "==============================="

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK is not installed. Please install .NET 8.0 SDK."
    exit 1
fi

# Check if Azure Functions Core Tools is installed
if ! command -v func &> /dev/null; then
    echo "Warning: Azure Functions Core Tools not found. Install with:"
    echo "npm install -g azure-functions-core-tools@4 --unsafe-perm true"
    echo ""
fi

cd ServiceBusFunctionApp

echo "Building the project..."
dotnet build

if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
else
    echo "❌ Build failed!"
    exit 1
fi

echo ""
echo "Project structure:"
find . -name "*.cs" -o -name "*.json" -o -name "*.csproj" | grep -v bin | grep -v obj | sort

echo ""
echo "Configuration files:"
echo "- host.json: $([ -f host.json ] && echo "✅ Found" || echo "❌ Missing")"
echo "- local.settings.json: $([ -f local.settings.json ] && echo "✅ Found" || echo "❌ Missing")"

echo ""
echo "To test locally:"
echo "1. Update the ServiceBusConnection in local.settings.json"
echo "2. Run 'func start' to start the function locally"
echo "3. Send test messages to your Service Bus queue"

echo ""
echo "To deploy to Azure:"
echo "1. Deploy infrastructure: az deployment group create --template-file ../azure-deploy.json"
echo "2. Deploy function code: func azure functionapp publish <your-function-app-name>"