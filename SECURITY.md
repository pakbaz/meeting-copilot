# Security Configuration Guide

This document outlines the secure credential management implementation following Microsoft Azure best practices.

## 🔒 Security Architecture

This application uses a **layered security approach** for credential management:

1. **Azure Managed Identity** (Production) - Most Secure
2. **Azure Key Vault** (Production) - Centralized Secret Management  
3. **User Secrets** (Development) - Local Secure Storage
4. **Environment Variables** (CI/CD) - Pipeline Integration

## 📁 Configuration Sources (Priority Order)

The application attempts authentication in this secure order:

### 1. Managed Identity (Recommended for Production)
- **What**: Uses Azure's built-in identity for resource-to-resource authentication
- **When**: Azure App Service, Container Apps, Functions, AKS
- **Security**: No stored credentials, automatically managed by Azure
- **Setup**: Assign "Cognitive Services Speech User" role to the Managed Identity

### 2. Azure Key Vault (Production)
- **What**: Centralized secret management service
- **When**: Production environments, shared secrets across teams
- **Configuration**:
  ```json
  {
    "KeyVaultName": "your-keyvault-name"
  }
  ```
- **Secret Name**: `AzureSpeech--SubscriptionKey` (note the double dash)

### 3. User Secrets (Development)
- **What**: Local encrypted storage for development secrets
- **When**: Local development only
- **Setup**: Already configured via `dotnet user-secrets`
- **Location**: `%APPDATA%\Microsoft\UserSecrets\{user-secrets-id}\secrets.json`

### 4. Environment Variables (CI/CD)
- **What**: OS-level environment variables
- **When**: CI/CD pipelines, container deployments
- **Variable Name**: `AZURE_SPEECH_KEY`
- **Setup**: Set in deployment pipeline or container configuration

## 🚀 Deployment Configurations

### Local Development
```bash
# Option 1: Use User Secrets (Recommended)
dotnet user-secrets set "AzureSpeech:SubscriptionKey" "your-key-here"

# Option 2: Environment Variable
$env:AZURE_SPEECH_KEY = "your-key-here"
```

### Azure App Service
```bash
# Set as Application Settings (automatically become environment variables)
az webapp config appsettings set --name your-app --resource-group your-rg --settings AZURE_SPEECH_KEY="your-key"

# OR: Use Managed Identity (Recommended)
# 1. Enable system-assigned managed identity
# 2. Assign "Cognitive Services Speech User" role
```

### Azure Container Apps / AKS
```yaml
# Option 1: Managed Identity (Recommended)
apiVersion: v1
kind: Pod
metadata:
  labels:
    azure.workload.identity/use: "true"
spec:
  serviceAccountName: workload-identity-sa

# Option 2: Key Vault Secret Store CSI Driver
apiVersion: v1
kind: SecretProviderClass
spec:
  secretObjects:
  - secretName: speech-secret
    data:
    - objectName: azure-speech-key
      key: AZURE_SPEECH_KEY
```

### Azure Key Vault Setup
```bash
# 1. Create Key Vault
az keyvault create --name your-keyvault --resource-group your-rg

# 2. Add secret
az keyvault secret set --vault-name your-keyvault --name "AzureSpeech--SubscriptionKey" --value "your-key"

# 3. Grant access to your app's Managed Identity
az keyvault set-policy --name your-keyvault --object-id YOUR_MANAGED_IDENTITY_OBJECT_ID --secret-permissions get
```

## 🔐 Security Best Practices Implemented

✅ **No secrets in source code** - All credentials are externalized  
✅ **Least privilege access** - Each environment uses appropriate auth method  
✅ **Secret rotation ready** - Configuration supports dynamic secret updates  
✅ **Environment separation** - Different secrets for dev/staging/production  
✅ **Audit trail** - Key Vault provides access logging  
✅ **Encryption at rest** - All storage mechanisms encrypt secrets  
✅ **Transport security** - HTTPS-only configuration  

## 🚨 Security Warnings

❌ **Never commit secrets to git**  
❌ **Don't use production secrets in development**  
❌ **Don't share user secrets across team members**  
❌ **Don't use environment variables for highly sensitive data in shared environments**  

## 🔄 Secret Rotation

For automated secret rotation:
1. Update secret in Azure Key Vault
2. Application automatically picks up new value (if Key Vault integration enabled)
3. Or restart application to reload environment variables

## 📞 Troubleshooting

### Common Issues:
1. **401 Authentication Error**: Check role assignments and secret values
2. **Secret not found**: Verify configuration priority and secret names
3. **Access denied**: Confirm Managed Identity has proper Key Vault permissions

### Debug Steps:
```bash
# Check current user context
az account show

# Verify role assignments
az role assignment list --assignee YOUR_USER_ID --scope RESOURCE_SCOPE

# Test Key Vault access
az keyvault secret show --name "AzureSpeech--SubscriptionKey" --vault-name your-vault
```