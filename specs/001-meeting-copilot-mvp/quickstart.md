# Quickstart: Meeting Copilot Local Development

**Feature**: 001-meeting-copilot-mvp  
**Date**: 2025-12-06

---

## Prerequisites

### Required Tools

| Tool | Version | Installation |
|------|---------|--------------|
| .NET SDK | 10.0+ | [Download](https://dot.net/download) |
| .NET Aspire workload | Latest | `dotnet workload install aspire` |
| Azure CLI | Latest | `brew install azure-cli` |
| Azure Developer CLI (azd) | Latest | `brew install azd` |
| Docker Desktop | Latest | [Download](https://docker.com/products/docker-desktop) |

### Verify Installation

```bash
# Check .NET version
dotnet --version
# Expected: 10.0.x

# Check Aspire workload
dotnet workload list | grep aspire
# Expected: aspire (installed)

# Check Azure CLI
az --version
# Expected: 2.x.x

# Check azd
azd version

# Check Docker
docker --version
```

---

## Azure Resources (Required)

Meeting Copilot requires these Azure services. For local development, you'll need connection strings/endpoints.

### 1. Azure Speech Services

Create or use existing Azure Speech resource:

```bash
# Create resource group (if needed)
az group create --name rg-meeting-copilot-dev --location eastus

# Create Speech resource
az cognitiveservices account create \
  --name speech-meeting-copilot-dev \
  --resource-group rg-meeting-copilot-dev \
  --kind SpeechServices \
  --sku S0 \
  --location eastus
```

Get the endpoint and key:

```bash
az cognitiveservices account show \
  --name speech-meeting-copilot-dev \
  --resource-group rg-meeting-copilot-dev \
  --query "properties.endpoint"

az cognitiveservices account keys list \
  --name speech-meeting-copilot-dev \
  --resource-group rg-meeting-copilot-dev
```

### 2. Azure AI Foundry (OpenAI)

Create Azure OpenAI resource:

```bash
az cognitiveservices account create \
  --name aoai-meeting-copilot-dev \
  --resource-group rg-meeting-copilot-dev \
  --kind OpenAI \
  --sku S0 \
  --location eastus
```

Deploy required models:

```bash
# GPT-4o for agents
az cognitiveservices account deployment create \
  --name aoai-meeting-copilot-dev \
  --resource-group rg-meeting-copilot-dev \
  --deployment-name gpt-4o \
  --model-name gpt-4o \
  --model-version "2024-08-06" \
  --model-format OpenAI \
  --sku-capacity 10 \
  --sku-name Standard

# Embeddings for vector search
az cognitiveservices account deployment create \
  --name aoai-meeting-copilot-dev \
  --resource-group rg-meeting-copilot-dev \
  --deployment-name text-embedding-3-small \
  --model-name text-embedding-3-small \
  --model-version "1" \
  --model-format OpenAI \
  --sku-capacity 10 \
  --sku-name Standard
```

### 3. Azure Cosmos DB

Create Cosmos DB account with vector search:

```bash
# Create Cosmos DB account
az cosmosdb create \
  --name cosmos-meeting-copilot-dev \
  --resource-group rg-meeting-copilot-dev \
  --locations regionName=eastus failoverPriority=0 \
  --capabilities EnableServerless EnableNoSQLVectorSearch

# Create database
az cosmosdb sql database create \
  --account-name cosmos-meeting-copilot-dev \
  --resource-group rg-meeting-copilot-dev \
  --name meetingcopilot

# Create containers (run for each container)
az cosmosdb sql container create \
  --account-name cosmos-meeting-copilot-dev \
  --resource-group rg-meeting-copilot-dev \
  --database-name meetingcopilot \
  --name meetings \
  --partition-key-path "/meeting_id"

az cosmosdb sql container create \
  --account-name cosmos-meeting-copilot-dev \
  --resource-group rg-meeting-copilot-dev \
  --database-name meetingcopilot \
  --name speakers \
  --partition-key-path "/meeting_id"

az cosmosdb sql container create \
  --account-name cosmos-meeting-copilot-dev \
  --resource-group rg-meeting-copilot-dev \
  --database-name meetingcopilot \
  --name interactions \
  --partition-key-path "/user_id"

az cosmosdb sql container create \
  --account-name cosmos-meeting-copilot-dev \
  --resource-group rg-meeting-copilot-dev \
  --database-name meetingcopilot \
  --name insights \
  --partition-key-path "/user_id"

az cosmosdb sql container create \
  --account-name cosmos-meeting-copilot-dev \
  --resource-group rg-meeting-copilot-dev \
  --database-name meetingcopilot \
  --name session_summaries \
  --partition-key-path "/user_id"
```

---

## Local Configuration

### 1. Clone and Setup

```bash
cd /Users/pakbaz/code/meeting-copilot
git checkout 001-meeting-copilot-mvp
```

### 2. Configure User Secrets

Store sensitive configuration in .NET User Secrets (never in source control):

```bash
# Initialize user secrets
dotnet user-secrets init --project meeting-copilot.csproj

# Set Speech Service credentials
dotnet user-secrets set "Speech:Endpoint" "https://eastus.api.cognitive.microsoft.com/"
dotnet user-secrets set "Speech:Key" "<your-speech-key>"
dotnet user-secrets set "Speech:Region" "eastus"

# Set Azure OpenAI credentials
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://aoai-meeting-copilot-dev.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:Key" "<your-openai-key>"
dotnet user-secrets set "AzureOpenAI:ChatDeployment" "gpt-4o"
dotnet user-secrets set "AzureOpenAI:EmbeddingDeployment" "text-embedding-3-small"

# Set Cosmos DB credentials
dotnet user-secrets set "Cosmos:Endpoint" "https://cosmos-meeting-copilot-dev.documents.azure.com:443/"
dotnet user-secrets set "Cosmos:Key" "<your-cosmos-key>"
dotnet user-secrets set "Cosmos:Database" "meetingcopilot"
```

### 3. Alternative: Use Managed Identity (Recommended for Production)

Instead of keys, authenticate with Azure CLI:

```bash
# Login to Azure
az login

# Set default subscription
az account set --subscription "<your-subscription-id>"
```

Then configure `appsettings.Development.json`:

```json
{
  "Speech": {
    "Endpoint": "https://eastus.api.cognitive.microsoft.com/",
    "Region": "eastus"
  },
  "AzureOpenAI": {
    "Endpoint": "https://aoai-meeting-copilot-dev.openai.azure.com/",
    "ChatDeployment": "gpt-4o",
    "EmbeddingDeployment": "text-embedding-3-small"
  },
  "Cosmos": {
    "Endpoint": "https://cosmos-meeting-copilot-dev.documents.azure.com:443/",
    "Database": "meetingcopilot"
  }
}
```

The app will use `DefaultAzureCredential` which picks up your Azure CLI credentials.

---

## Running Locally

### Option 1: Direct Run (Current State)

Before Aspire projects are added:

```bash
cd /Users/pakbaz/code/meeting-copilot
dotnet run
```

Open browser: `https://localhost:5001`

### Option 2: With Aspire (After MVP Setup)

Once Aspire projects are created:

```bash
# Start all services with Aspire
cd /Users/pakbaz/code/meeting-copilot/MeetingCopilot.AppHost
dotnet run
```

This will:
1. Start the Aspire dashboard at `https://localhost:15000`
2. Start the Blazor web app
3. Start the Agents service
4. Configure service discovery automatically

### Aspire Dashboard

The Aspire dashboard provides:
- Service health status
- Distributed tracing
- Structured logs
- Metrics visualization

---

## First Run Verification

### 1. Check Homepage Loads

Navigate to `https://localhost:5001` and verify:
- [ ] Page loads without errors
- [ ] No console errors in browser DevTools

### 2. Test Microphone Permission

Click "Start Meeting" and verify:
- [ ] Browser prompts for microphone access
- [ ] Microphone dropdown shows available devices

### 3. Test Speech Recognition

Grant microphone permission and speak:
- [ ] Transcription appears in real-time
- [ ] Speaker labels show (Speaker 1, etc.)

### 4. Verify Cosmos DB Connection

Check application logs for:

```text
info: Microsoft.Azure.Cosmos[0]
      CosmosClient created with endpoint: https://cosmos-meeting-copilot-dev.documents.azure.com:443/
```

---

## Troubleshooting

### Speech Service Not Working

1. Check endpoint format (should include region)
2. Verify key is correct
3. Check browser console for WebSocket errors

```bash
# Test Speech service directly
curl -X POST "https://eastus.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language=en-US" \
  -H "Ocp-Apim-Subscription-Key: <your-key>" \
  -H "Content-Type: audio/wav" \
  --data-binary @test.wav
```

### Cosmos DB Connection Errors

1. Verify endpoint URL format
2. Check firewall rules allow your IP
3. Confirm containers exist

```bash
# List containers
az cosmosdb sql container list \
  --account-name cosmos-meeting-copilot-dev \
  --resource-group rg-meeting-copilot-dev \
  --database-name meetingcopilot
```

### Azure OpenAI Rate Limits

If you see 429 errors:
1. Check deployment capacity in Azure Portal
2. Increase TPM (tokens per minute) quota
3. Add retry logic with exponential backoff

---

## Next Steps

After local setup is verified:

1. **Run tests**: `dotnet test`
2. **Build**: `dotnet build`
3. **Deploy preview**: `azd provision --preview`

---

## Project Structure Reference

```text
meeting-copilot/
├── meeting-copilot.csproj       # Main Blazor app
├── Program.cs                   # Entry point
├── appsettings.json            # Non-sensitive config
├── appsettings.Development.json # Dev overrides
├── Agents/                      # Agent implementations
├── Components/                  # Blazor components
├── Data/                        # Data access
├── Services/                    # Business services
├── specs/001-meeting-copilot-mvp/
│   ├── spec.md                 # Feature specification
│   ├── plan.md                 # Implementation plan
│   ├── research.md             # Technology research
│   ├── data-model.md           # Cosmos DB schema
│   ├── quickstart.md           # This file
│   └── contracts/              # API contracts
└── infra/                       # IaC (Bicep) - to be created
```
