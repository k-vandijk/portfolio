# Portfolio

ASP.NET Core MVC PWA for tracking and visualizing investment portfolio performance with real-time market data.

[![Build Status](https://github.com/k-vandijk/portfolio-insight-dashboard/actions/workflows/continuous-integration.yml/badge.svg)](https://github.com/k-vandijk/portfolio-insight-dashboard/actions/workflows/continuous-integration.yml)
[![Deployment Status](https://github.com/k-vandijk/portfolio-insight-dashboard/actions/workflows/continuous-deployment.yml/badge.svg)](https://github.com/k-vandijk/portfolio-insight-dashboard/actions/workflows/continuous-deployment.yml)

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 10, C# 12, ASP.NET Core MVC |
| Auth | Azure AD (OpenID Connect) via Microsoft.Identity.Web |
| Storage | Azure Table Storage (`Azure.Data.Tables`) |
| Secrets | Azure Key Vault (production) / user secrets (development) |
| Frontend | Bootstrap 5.3, SCSS, Chart.js, DataTables, jQuery |
| Push | Web Push API (VAPID) via `WebPush` NuGet package |
| CI/CD | GitHub Actions — Ubuntu runners, .NET 10 |

## Setup

**Prerequisites:** .NET 10 SDK, Node.js 20.x, Azure AD tenant, Azure Table Storage account.

```bash
git clone https://github.com/k-vandijk/portfolio-insight-dashboard.git
cd portfolio-insight-dashboard
npm install
npm run sass:build
dotnet restore && dotnet build
dotnet run --project src/Kvandijk.Portfolio._Web/Kvandijk.Portfolio._Web.csproj
```

App starts at `https://localhost:61277`.

## Appsettings / Keyvault Secrets

| Variable | Description |
|---|---|
| `AzureAd--CallbackPath` | Azure AD callback path |
| `AzureAd--ClientId` | Azure AD client ID |
| `AzureAd--ClientSecret` | Azure AD client secret |
| `AzureAd--Instance` | Azure AD instance |
| `AzureAd--TenantId` | Azure AD tenant ID |
| `ConnectionStrings--StorageAccount` | Azure Table Storage connection string |
| `KeyVaultUri` | Azure Key Vault URI (non-dev only) |
| `TickerApi--Code` | Ticker API authentication key |
| `TickerApi--Url` | Base URL for the Ticker API |
| `Vapid--PrivateKey` | VAPID private key for Web Push |
| `Vapid--PublicKey` | VAPID public key for Web Push |
| `Vapid--Subject` | VAPID subject (mailto: or URL) |

Production secrets are loaded from Azure Key Vault. For local development, use `dotnet user-secrets`.

## Ticker API Reference

### GET `/market-history`

```http
GET {TickerApi--Url}/market-history?ticker={ticker}&period={period}&interval={interval}
```

| Parameter | Type | Required | Example |
|---|---|---|---|
| `ticker` | string | Yes | `AAPL`, `MSFT` |
| `period` | string | No | `1mo`, `1y`, `max` |
| `interval` | string | No | `1d`, `1wk`, `1mo` |

**Response:**

```json
{
  "ticker": "AAPL",
  "currency": "USD",
  "history": [
    { "ticker": "AAPL", "date": "2024-01-15", "open": 185.50, "close": 187.25 }
  ]
}
```

Caching: sliding 1 min / absolute 5 min. Returns `null` on failure; errors are logged.

## Development

```bash
npm run dev                 # SCSS watch + dotnet hot reload (concurrent)
npm run sass:build          # Compile SCSS once
dotnet build                # Build solution
dotnet test                 # Run all xUnit tests
```

## CI/CD

| Workflow | Trigger | Action |
|---|---|---|
| `continuous-integration.yml` | PR to `main` | Build + test |
| `continuous-deployment.yml` | Push to `main` | Build, publish, deploy to `as-kvandijk-portfolio-dashboard` |

**Branching:** `feat-*` / `fix-*` branches → PR → merge to `main` → auto-deploy.
