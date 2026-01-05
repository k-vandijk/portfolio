# Ticker API Dashboard

A .NET 10 web application for tracking and visualizing investment portfolio performance. The dashboard provides real-time portfolio analytics, historical market data visualization, and transaction management capabilities.

## 📋 Overview

The Ticker API Dashboard is an ASP.NET Core MVC application that helps investors track their stock portfolio performance. It integrates with an external Ticker API to fetch market data and stores transaction history in Azure Table Storage. The application features interactive charts, portfolio analytics, and supports multiple display modes including value, profit, and profit percentage views.

## 🏗️ Architecture

The solution follows Clean Architecture principles with clear separation of concerns:

```
├── Dashboard.Web           # ASP.NET Core MVC web application (UI layer)
├── Dashboard.Application   # Business logic and DTOs
├── Dashboard.Domain        # Domain models and core entities
├── Dashboard.Infrastructure # External integrations (API clients, Azure Table Storage)
└── Dashboard.Tests         # Unit tests
```

### Key Components

- **Dashboard.Web**: Controllers, Views, ViewModels, and web-specific configuration
- **Dashboard.Application**: Application services, DTOs, helpers, and interfaces
- **Dashboard.Domain**: Domain models (TransactionEntity) and domain services
- **Dashboard.Infrastructure**: External service implementations (TickerApiService, AzureTableService)
- **Dashboard.Tests**: xUnit test suite

## 🔧 Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Azure Active Directory tenant (for authentication)
- Azure Table Storage account (for transaction storage)
- Access to the Ticker API endpoint

## �� Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/k-vandijk/ticker_api_dashboard.git
cd ticker_api_dashboard
```

### 2. Configure Application Settings

Update `Dashboard.Web/appsettings.json` with your configuration:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "CallbackPath": "/signin-oidc"
  },
  "Secrets": {
    "TickerApiurl": "your-ticker-api-url",
    "TickerApiCode": "your-api-key",
    "TransactionsTableConnectionString": "your-azure-storage-connection-string"
  }
}
```

> **⚠️ Security Note**: Never commit secrets to source control. Use Azure Key Vault, user secrets, or environment variables in production.

### 3. Restore Dependencies

```bash
dotnet restore
```

### 4. Build the Solution

```bash
dotnet build
```

### 5. Run the Application

```bash
cd Dashboard.Web
dotnet run
```

The application will start and display the listening URLs in the console (typically `https://localhost:5001`).

## 🧪 Running Tests

Run all unit tests:

```bash
dotnet test
```

Run tests with coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Run tests for a specific project:

```bash
dotnet test Dashboard.Tests/Dashboard.Tests.csproj
```

## ✨ Key Features

### Portfolio Dashboard
- **Real-time Portfolio Valuation**: View current portfolio worth and performance
- **Multiple Display Modes**:
  - Portfolio value over time
  - Absolute profit/loss in EUR
  - Profit/loss percentage
- **Interactive Charts**: Historical performance visualization with adjustable time ranges
- **Portfolio Breakdown**: Detailed ticker-by-ticker analysis

### Transaction Management
- Add, view, and manage stock transactions
- Track buy/sell history with dates and costs
- Azure Table Storage integration for reliable data persistence

### Market Data Integration
- Real-time price data from external Ticker API
- Historical market data for charting
- Concurrent API calls for optimal performance

### Localization
- Multi-language support (Dutch/English)
- Locale-specific formatting for currency and dates
- Cookie-based culture selection

### Authentication & Security
- Azure AD authentication via OpenID Connect
- Secure user authentication required by default
- Microsoft Identity integration

## 🔑 Configuration

### Central Package Management
The solution uses Central Package Management (CPM) with `Directory.Packages.props` for consistent dependency versions across all projects.

### Build Configuration
Global build properties are defined in `Directory.Build.props`:
- Target Framework: .NET 10
- Nullable reference types enabled
- Implicit usings enabled
- Warnings treated as errors

### Key Dependencies
- **ASP.NET Core**: Web framework
- **Azure.Data.Tables**: Azure Table Storage client
- **Microsoft.Identity.Web**: Azure AD authentication
- **Serilog**: Structured logging
- **xUnit**: Testing framework

## 📁 Project Structure

```
Dashboard.Web/
├── Controllers/           # MVC controllers
│   ├── DashboardController.cs      # Main dashboard logic
│   ├── TransactionsController.cs   # Transaction management
│   ├── InvestmentController.cs     # Investment views
│   └── MarketHistoryController.cs  # Market data endpoints
├── Views/                 # Razor views
├── ViewModels/            # View-specific models
├── wwwroot/              # Static files (CSS, JS, images)
└── Program.cs            # Application entry point

Dashboard.Application/
├── Dtos/                 # Data transfer objects
├── Helpers/              # Utility classes
│   ├── FilterHelper.cs
│   ├── FormattingHelper.cs
│   ├── PeriodHelper.cs
│   └── TransactionMapper.cs
└── Interfaces/           # Service contracts

Dashboard.Domain/
├── Models/               # Domain entities
└── Utils/               # Domain utilities

Dashboard.Infrastructure/
└── Services/            # External service implementations
    ├── TickerApiService.cs
    └── AzureTableService.cs
```

## 🛠️ Development

### Coding Standards
- C# 12 with nullable reference types
- Follow Clean Architecture principles
- Warnings are treated as errors
- Use dependency injection for service management

### Logging
The application uses Serilog for structured logging:
- Console logging enabled
- Log levels configured per namespace
- Request/response logging for debugging

### Localization
- Resources stored in `wwwroot/` directory
- Supported cultures: nl-NL (default), en-US
- Use `IStringLocalizer<SharedResource>` for localized strings

## 📊 Performance Considerations

- Market history API calls are executed concurrently for multiple tickers
- Failed API calls are logged but don't block the entire request
- Caching strategy can be implemented for frequently accessed data
- Consider rate limiting for external API calls

## 🔒 Security

- All routes require authentication by default
- Azure AD integration with OpenID Connect
- Secrets should be stored in Azure Key Vault or environment variables
- HTTPS enforced in non-development environments
- HSTS enabled for production

## 🐛 Troubleshooting

### Common Issues

1. **Authentication Errors**: Verify Azure AD configuration in `appsettings.json`
2. **API Connection Issues**: Check Ticker API URL and access code
3. **Storage Connection Issues**: Verify Azure Table Storage connection string
4. **Build Errors**: Ensure .NET 10 SDK is installed

### Logging
Check console output for detailed error messages and request timings. Logs include:
- API call timings
- Transaction retrieval performance
- Failed ticker API requests

## 📝 License

This project is private. Contact the repository owner for licensing information.

## 👥 Contributing

This is a private repository. Contact the repository owner for contribution guidelines.

## 📞 Support

For issues or questions, please open an issue in the GitHub repository.
