# Setup Guide

This guide walks you through the complete installation and configuration process for the Portfolio Insight Dashboard.

## Prerequisites

Before you begin, ensure you have the following installed:

- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** - Required for building and running the application
- **[Node.js 20.x](https://nodejs.org/)** - Required for SCSS compilation
- **Azure Active Directory tenant** - For authentication
- **Azure Table Storage account** - For transaction persistence
- **Ticker API access** - Or a similar market data provider

## Installation Steps

### 1. Clone the Repository

```bash
git clone https://github.com/k-vandijk/portfolio-insight-dashboard.git
cd portfolio-dashboard
```

### 2. Install npm Dependencies

```bash
npm install
```

This installs:
- Bootstrap 5.3.3
- Sass compiler
- Concurrently (for running multiple dev commands)

### 3. Configure Environment Variables

Set the following environment variables or update `src/Dashboard._Web/appsettings.json`:

#### Required Variables

| Variable | Description | Example |
|---|---|---|
| `TRANSACTIONS_TABLE_CONNECTION_STRING` | Azure Table Storage connection string | `DefaultEndpointsProtocol=https;AccountName=...` |
| `TICKER_API_URL` | Base URL for the Ticker API | `https://api.example.com/ticker` |
| `TICKER_API_CODE` | Authentication code/key for Ticker API | `your-api-key-here` |

**Setting Environment Variables:**

**Windows (PowerShell):**
```powershell
$env:TRANSACTIONS_TABLE_CONNECTION_STRING="your-connection-string"
$env:TICKER_API_URL="your-ticker-api-url"
$env:TICKER_API_CODE="your-api-authentication-code"
```

**Linux/macOS:**
```bash
export TRANSACTIONS_TABLE_CONNECTION_STRING="your-connection-string"
export TICKER_API_URL="your-ticker-api-url"
export TICKER_API_CODE="your-api-authentication-code"
```

### 4. Configure Azure AD Authentication

Update `src/Dashboard._Web/appsettings.json` with your Azure AD credentials:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "CallbackPath": "/signin-oidc"
  }
}
```

Alternatively, use environment variables:

```bash
export AzureAd__TenantId="your-tenant-id"
export AzureAd__ClientId="your-client-id"
export AzureAd__ClientSecret="your-client-secret"
```

> **⚠️ Security Warning**: Never commit secrets to source control. Use Azure Key Vault, User Secrets, or environment variables in production.

### 5. Build Frontend Assets

Compile SCSS to CSS:

```bash
npm run sass:build
```

### 6. Restore and Build .NET Solution

```bash
dotnet restore
dotnet build
```

### 7. Run the Application

**Option A: Production Mode**
```bash
cd src/Dashboard._Web
dotnet run
```

**Option B: Development Mode with Hot Reload**
```bash
npm run dev
```

This runs both SCSS watch and dotnet watch concurrently.

The application starts at `https://localhost:5001`

## Configuration Methods

### Development: User Secrets

For local development, use .NET User Secrets to store sensitive configuration:

```bash
cd src/Dashboard._Web

# Set each secret
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
dotnet user-secrets set "TICKER_API_CODE" "your-api-code"
dotnet user-secrets set "TRANSACTIONS_TABLE_CONNECTION_STRING" "your-connection-string"
```

### Production: Azure Key Vault

For production deployments, use Azure Key Vault:

1. Create an Azure Key Vault
2. Add secrets to the Key Vault
3. Configure your Azure Web App to access the Key Vault
4. Reference secrets using the Key Vault configuration provider

See [Azure Key Vault documentation](https://learn.microsoft.com/en-us/azure/key-vault/) for details.

## Azure Table Storage Setup

### 1. Create Storage Account

1. Navigate to [Azure Portal](https://portal.azure.com)
2. Create a new Storage Account
3. Note the connection string from "Access keys"

### 2. Table Configuration

The application automatically creates the required table (`Transactions`) on first run. The table name is configured in `src/Dashboard.Domain/Utils/StaticDetails.cs`.

**Table Schema:**
- **PartitionKey**: Fixed value (`UserId` from `StaticDetails`)
- **RowKey**: Transaction GUID
- **Columns**: Ticker, Date, Amount, Price, TransactionCosts

## Ticker API Configuration

The Ticker API provides real-time and historical market data via Yahoo Finance.

### Configuration

Set the base URL and authentication code:

```bash
export TICKER_API_URL="https://your-ticker-api.azurewebsites.net"
export TICKER_API_CODE="your-api-authentication-code"
```

### Caching Configuration

Cache durations are configured in `src/Dashboard.Domain/Utils/StaticDetails.cs`:

```csharp
public static int CacheSlidingExpirationMinutes = 10;
public static int CacheAbsoluteExpirationMinutes = 60;
```

Adjust these values based on your API rate limits and data freshness requirements.

## Troubleshooting

### Build Failures

**Issue**: `dotnet build` fails with package restore errors

**Solution**: Clear NuGet cache and restore
```bash
dotnet nuget locals all --clear
dotnet restore
dotnet build
```

### SCSS Compilation Errors

**Issue**: `npm run sass:build` fails

**Solution**: Ensure Node.js 20+ is installed and dependencies are up to date
```bash
node --version
npm install
npm run sass:build
```

### Authentication Issues

**Issue**: Azure AD authentication fails

**Solution**: Verify:
1. Azure AD app registration is configured correctly
2. Redirect URIs include `https://localhost:5001/signin-oidc`
3. Client secret is valid and not expired
4. Tenant ID and Client ID are correct

### Table Storage Connection Errors

**Issue**: Cannot connect to Azure Table Storage

**Solution**: Verify:
1. Connection string is correctly formatted
2. Storage account exists and is accessible
3. Firewall rules allow your IP address
4. SAS token (if used) has not expired

## Next Steps

- **Learn the architecture**: [Architecture Guide](./architecture.md)
- **Start developing**: [Development Guide](./development.md)
- **Follow conventions**: [Conventions](./conventions.md)
- **Deploy to Azure**: [CI/CD Guide](./ci-cd.md)

---

[← Back to Documentation Index](./README.md)
