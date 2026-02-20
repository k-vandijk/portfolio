# Portfolio Insight Dashboard

A .NET 10 Progressive Web App for tracking and visualizing investment portfolio performance with real-time market data analytics.

[![Build Status](https://github.com/k-vandijk/portfolio-insight-dashboard/actions/workflows/continuous-integration.yml/badge.svg)](https://github.com/k-vandijk/portfolio-insight-dashboard/actions/workflows/continuous-integration.yml)
[![Deployment Status](https://github.com/k-vandijk/portfolio-insight-dashboard/actions/workflows/continuous-deployment.yml/badge.svg)](https://github.com/k-vandijk/portfolio-insight-dashboard/actions/workflows/continuous-deployment.yml)

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Technology Stack](#technology-stack)
- [Installation & Setup](#installation--setup)
- [Ticker API Reference](#ticker-api-reference)
- [Progressive Web App (PWA)](#progressive-web-app-pwa)
- [Environment Variables](#environment-variables)
- [Development](#development)
- [CI/CD](#cicd)

## Overview

Portfolio Insight Dashboard is an ASP.NET Core MVC application that enables investors to track stock portfolio performance through interactive visualizations and real-time market data. Built with Clean Architecture principles, the application integrates with a custom Ticker API for market data via Yahoo Finance and uses Azure Table Storage for transaction persistence.

## Key Features

### 📊 Portfolio Analytics
- **Real-time Valuation**: Current portfolio worth with profit/loss calculations
- **Multiple Display Modes**: Switch between absolute values, profit amounts, and profit percentages
- **Interactive Charts**: Line charts with adjustable time ranges (1M, 6M, 1Y, ALL)
- **Ticker Breakdown**: Detailed per-ticker analysis with investment percentages

### 💼 Transaction Management
- Add, view, and delete stock transactions with transaction costs
- Historical transaction tracking with Azure Table Storage persistence
- Transaction filtering by ticker and year
- DataTables integration for advanced table functionality

### 📈 Market Data Integration
- **Custom Ticker API**: Proprietary API supplying market data via Yahoo Finance
- **Concurrent Fetching**: Parallel API calls for multiple tickers
- **Smart Caching**: Memory caching with sliding and absolute expiration
- **Historical Data**: Comprehensive market history with configurable periods

### 🌍 Localization & UX
- Multi-language support (nl-NL default, en-US)
- Currency and date formatting per locale
- Responsive Bootstrap 5 design
- Dark theme with Fluent Design-inspired colors
- Skeleton loaders and loading indicators

### 🔐 Security
- Azure AD authentication (OpenID Connect)
- Enterprise SSO integration
- HTTPS enforcement and HSTS
- Secure secret management

## Technology Stack

**Backend:**
- .NET 10 (C# 12) with ASP.NET Core MVC
- Azure.Data.Tables for Table Storage
- Microsoft.Identity.Web for Azure AD

**Frontend:**
- Bootstrap 5.3.3 (primary UI framework)
- SCSS with modular design tokens
- Chart.js for data visualization
- DataTables 2.3.4 for table rendering
- jQuery, Flatpickr, SweetAlert2, Font Awesome

**Architecture:**
- Clean Architecture (Web → Application → Domain → Infrastructure)
- Dependency Injection
- Repository Pattern
- DTO/ViewModel separation

## Installation & Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20.x](https://nodejs.org/) (for SCSS compilation)
- Azure Active Directory tenant
- Azure Table Storage account
- Ticker API or a similar market data provider

### Setup Steps

**1. Clone the Repository**

```bash
git clone https://github.com/k-vandijk/portfolio-insight-dashboard.git
cd portfolio-insight-dashboard
```

**2. Install npm Dependencies**

```bash
npm install
```

**3. Build Frontend Assets**

```bash
npm run sass:build
```

**4. Log into Azure CLI**

```bash
az login
```

**5. Restore and Build**

```bash
dotnet restore
dotnet build
```

**6. Run the Application**

```bash
cd src/Dashboard._Web
dotnet run
```

The application starts at `https://localhost:61277`

## Ticker API Reference

The Ticker API is a custom-built API that provides real-time and historical market data for any stock ticker using Yahoo Finance as the data source. The dashboard integrates with this API to fetch market information.

### Base Configuration

- **Base URL**: Configured via `TICKER_API_URL` environment variable
- **Authentication**: API code passed via `TICKER_API_CODE` environment variable
- **Caching**: Memory caching with sliding (5 min) and absolute (15 min) expiration

### Endpoint: Get Market History

Fetches historical price data for a specific ticker symbol.

**HTTP Request:**
```http
GET {TICKER_API_URL}/market-history?ticker={ticker}&period={period}&interval={interval}
```

**Request Parameters:**

| Parameter  | Type   | Required | Description                                              | Example Values        |
|------------|--------|----------|----------------------------------------------------------|-----------------------|
| `ticker`   | string | Yes      | Stock ticker symbol                                      | `AAPL`, `GOOGL`, `MSFT` |
| `period`   | string | No       | Time period for historical data                          | `1mo`, `1y`, `max`    |
| `interval` | string | No       | Data interval (default: `1d`)                            | `1d`, `1wk`, `1mo`    |

**Response Structure:**

```json
{
  "ticker": "AAPL",
  "currency": "USD",
  "history": [
    {
      "ticker": "AAPL",
      "date": "2024-01-15",
      "open": 185.50,
      "close": 187.25
    },
    {
      "ticker": "AAPL",
      "date": "2024-01-16",
      "open": 187.30,
      "close": 189.10
    }
  ]
}
```

**Response Fields:**

- `ticker` (string): The stock ticker symbol
- `currency` (string): Currency code (e.g., USD, EUR)
- `history` (array): Array of historical data points
  - `ticker` (string): Stock ticker symbol
  - `date` (string): Date in ISO format (YYYY-MM-DD)
  - `open` (decimal): Opening price
  - `close` (decimal): Closing price

**Usage Example (C#):**

```csharp
// Injected via ITickerApiService
var response = await _tickerApiService.GetMarketHistoryResponseAsync(
    ticker: "AAPL",
    period: "1y",
    interval: "1d"
);

if (response?.History != null)
{
    foreach (var dataPoint in response.History)
    {
        Console.WriteLine($"{dataPoint.Date}: {dataPoint.Close}");
    }
}
```

**Error Handling:**

- Returns `null` if the API call fails
- Errors are logged via Serilog but don't block the application
- Failed ticker requests are logged with details for debugging

**Performance Features:**

- Concurrent API calls for multiple tickers using `Task.WhenAll`
- Memory caching reduces redundant API calls
- Request timing logged for performance monitoring

## Progressive Web App (PWA)

Portfolio Insight Dashboard is installable as a Progressive Web App, providing an app-like experience with offline capabilities.

### PWA Features

- ✅ **Installable**: Add to home screen on iOS and Android
- ✅ **Standalone Mode**: Runs without browser UI
- ✅ **Offline Support**: Static assets cached for offline access
- ✅ **Fast Performance**: Service worker caching for instant loads
- ✅ **Responsive Design**: Optimized for mobile and desktop

### Installing on Mobile

#### iOS (iPhone/iPad)

1. Open Safari and navigate to the dashboard URL
2. Tap the **Share** button (square with arrow pointing up)
3. Scroll down and tap **Add to Home Screen**
4. Customize the name if desired
5. Tap **Add** in the top right corner
6. The app icon appears on your home screen

#### Android

1. Open Chrome and navigate to the dashboard URL
2. Tap the **three-dot menu** in the top right
3. Select **Add to Home screen** or **Install app**
4. Confirm the installation in the dialog
5. The app icon appears in your app drawer

### PWA Configuration

The PWA is configured via `wwwroot/manifest.json`:

```json
{
  "name": "Ticker API Dashboard",
  "short_name": "Ticker Dashboard",
  "description": "Track and visualize investment portfolio performance",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#1560BD",
  "icons": [
    {
      "src": "/icon-192x192.png",
      "sizes": "192x192",
      "type": "image/png",
      "purpose": "any maskable"
    },
    {
      "src": "/icon-512x512.png",
      "sizes": "512x512",
      "type": "image/png",
      "purpose": "any maskable"
    }
  ]
}
```

### Service Worker Strategy

The service worker (`wwwroot/service-worker.js` v3) implements a smart caching strategy:

- **Cache-First** for static assets (CSS, JS, images, fonts) - instant loading
- **Network-First** for HTML and API endpoints - fresh data with offline fallback
- Automatic versioning and cache cleanup
- Skips authentication and extension requests

## Environment Variables

### Required Variables

| Variable                              | Description                                    | Example                                      |
|---------------------------------------|------------------------------------------------|----------------------------------------------|
| `TRANSACTIONS_TABLE_CONNECTION_STRING` | Azure Table Storage connection string          | `DefaultEndpointsProtocol=https;Account...`  |
| `TICKER_API_URL`                      | Base URL for the Ticker API                    | `https://api.example.com/ticker`             |
| `TICKER_API_CODE`                     | Authentication code/key for Ticker API         | `your-api-key-here`                          |

### Azure AD Configuration

Configure in `appsettings.json` or via environment variables:

```bash
export AzureAd__TenantId="your-tenant-id"
export AzureAd__ClientId="your-client-id"
export AzureAd__ClientSecret="your-client-secret"
```

### Configuration Methods

- **Environment Variables**: Direct system environment variables
- **appsettings.json**: Application configuration file (non-sensitive only)
- **User Secrets**: For local development (`dotnet user-secrets`)
- **Azure Key Vault**: Recommended for production secrets

## Development

### Development Commands

```bash
# Frontend
npm run sass:build          # Build SCSS → CSS once
npm run sass:watch          # Watch SCSS files for changes
npm run dotnet:watch        # .NET hot reload
npm run dev                 # Concurrent SCSS watch + dotnet watch

# Backend
dotnet restore              # Restore NuGet packages
dotnet build                # Build solution
dotnet test                 # Run all tests
dotnet test --collect:"XPlat Code Coverage"  # Tests with coverage
```

### Architecture Overview

```
src/
├── Dashboard._Web/           # Presentation Layer (MVC)
├── Dashboard.Application/    # Business Logic Layer
├── Dashboard.Domain/         # Domain Layer
└── Dashboard.Infrastructure/ # Infrastructure Layer (External services)

tests/
└── Dashboard.Tests/          # xUnit test suite
```

### Coding Standards

- C# 12 with nullable reference types
- Clean Architecture principles
- Warnings treated as errors
- Dependency Injection for all services
- Bootstrap-first UI development
- DRY principle: create reusable components

### Project Structure

- **Controllers**: MVC controllers for each feature area
- **Views**: Razor views with partial views and components
- **ViewModels**: View-specific data models
- **Dtos**: Data transfer objects for API communication
- **Services**: External integrations (Ticker API, Azure Storage)
- **wwwroot**: Static assets (compiled CSS, JS, icons, PWA files)

## CI/CD

### Continuous Integration

**Workflow**: `.github/workflows/continuous-integration.yml`

- Triggers on Pull Requests to `main` branch
- Builds the solution
- Runs all unit tests
- Validates code quality
- Caches dependencies for faster builds

### Continuous Deployment

**Workflow**: `.github/workflows/continuous-deployment.yml`

- Triggers on push to `main` branch
- Builds frontend assets (SCSS → CSS)
- Restores .NET dependencies
- Publishes release build
- Deploys to Azure Web App (`as-kvandijk-ticker-api-dashboard`)

### Branching Strategy

- **`main`**: Production branch, automatically deployed to Azure
- **`feat-*` / `fix-*`**: Feature and bugfix branches

**Workflow:**
1. Create feature branch from `main`
2. Develop and test changes
3. Create PR to merge into `main`
4. Merge to `main` triggers automatic deployment

---

**Repository**: [github.com/k-vandijk/portfolio-insight-dashboard](https://github.com/k-vandijk/portfolio-insight-dashboard)  
**Author**: Kevin van Dijk  
**Deployment**: Azure Web App
