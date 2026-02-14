# Portfolio Insight Dashboard

A .NET 10 Progressive Web App for tracking and visualizing investment portfolio performance with real-time market data analytics. Built with Clean Architecture, Azure Table Storage, and a custom Ticker API.

[![Build Status](https://github.com/k-vandijk/portfolio-insight-dashboard/actions/workflows/continuous-integration.yml/badge.svg)](https://github.com/k-vandijk/portfolio-insight-dashboard/actions/workflows/continuous-integration.yml)
[![Deployment Status](https://github.com/k-vandijk/portfolio-insight-dashboard/actions/workflows/continuous-deployment.yml/badge.svg)](https://github.com/k-vandijk/portfolio-insight-dashboard/actions/workflows/continuous-deployment.yml)

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Quick Start](#quick-start)
- [Development](#development)
- [Repository Structure](#repository-structure)
- [Documentation](#documentation)
- [Contributing](#contributing)

## Overview

Portfolio Insight Dashboard is an ASP.NET Core MVC application for tracking stock portfolio performance through interactive visualizations and real-time market data. Built with Clean Architecture, it integrates with a custom Ticker API and uses Azure Table Storage for persistence.

## Key Features

- 📊 **Portfolio Analytics** - Real-time valuation with profit/loss calculations, interactive charts (1M, 6M, 1Y, ALL), and per-ticker breakdowns
- 💼 **Transaction Management** - Add, view, and delete transactions with filtering by ticker and year
- 📈 **Market Data Integration** - Custom Ticker API with concurrent fetching, smart caching, and historical data
- 🌍 **Localization** - Multi-language support (nl-NL, en-US) with culture-aware formatting
- 🔐 **Security** - Azure AD authentication with enterprise SSO
- 📱 **Progressive Web App** - Installable on iOS/Android with offline support

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20.x](https://nodejs.org/)
- Azure Active Directory tenant
- Azure Table Storage account

### Installation

```bash
# Clone repository
git clone https://github.com/k-vandijk/portfolio-insight-dashboard.git
cd portfolio-insight-dashboard

# Install dependencies
npm install
dotnet restore

# Configure environment variables (see docs/setup.md for details)
export TRANSACTIONS_TABLE_CONNECTION_STRING="your-connection-string"
export TICKER_API_URL="your-api-url"
export TICKER_API_CODE="your-api-code"

# Build and run
npm run sass:build
dotnet build
dotnet run --project src/Dashboard._Web/Dashboard._Web.csproj
```

Application runs at `https://localhost:5001`

**Detailed setup**: See [docs/setup.md](./docs/setup.md)

## Development

### Commands

| Command | Description |
|---|---|
| `npm run dev` | Concurrent SCSS watch + .NET hot reload (recommended) |
| `npm run sass:build` | Compile SCSS → CSS once |
| `npm run sass:watch` | Watch SCSS files for changes |
| `dotnet restore` | Restore NuGet packages |
| `dotnet build` | Build solution |
| `dotnet test` | Run all xUnit tests |

### Workflow

```bash
# Start development with hot reload
npm run dev

# Application runs at https://localhost:5001
# SCSS auto-compiles on save
# .NET hot reload on code changes
```

**Detailed guide**: See [docs/development.md](./docs/development.md)

## Repository Structure

```
src/
  Dashboard._Web/           # Presentation: Controllers, Views, ViewModels, wwwroot
  Dashboard.Application/    # Business Logic: DTOs, Interfaces, Helpers, Mappers
  Dashboard.Domain/         # Core: Entities, Constants
  Dashboard.Infrastructure/ # External Services: Ticker API, Azure Table Storage

tests/
  Dashboard.Tests/          # xUnit tests (Arrange-Act-Assert pattern)

docs/                       # Documentation (detailed guides)
```

**Architecture details**: See [docs/architecture.md](./docs/architecture.md)

## Documentation

### 📚 Complete Documentation

- **[Documentation Index](./docs/README.md)** - Start here for detailed guides
- **[Setup Guide](./docs/setup.md)** - Installation, configuration, troubleshooting
- **[Architecture Guide](./docs/architecture.md)** - Clean Architecture, patterns, design decisions
- **[Development Guide](./docs/development.md)** - Workflows, commands, SCSS, testing
- **[Conventions](./docs/conventions.md)** - Naming, coding standards, best practices
- **[API Reference](./docs/api-reference.md)** - Ticker API endpoints and integration
- **[PWA Guide](./docs/pwa.md)** - Progressive Web App features and installation
- **[CI/CD Guide](./docs/ci-cd.md)** - GitHub Actions workflows and deployment

### 🚀 Quick Links

| I want to... | Documentation |
|---|---|
| Install and run the app | [Setup Guide](./docs/setup.md) |
| Understand the architecture | [Architecture Guide](./docs/architecture.md) |
| Start developing | [Development Guide](./docs/development.md) |
| Follow coding conventions | [Conventions](./docs/conventions.md) |
| Integrate with Ticker API | [API Reference](./docs/api-reference.md) |
| Install as mobile app | [PWA Guide](./docs/pwa.md) |
| Deploy to production | [CI/CD Guide](./docs/ci-cd.md) |

## Contributing

### Standards

- **Language**: Code in English, comments in English
- **Architecture**: Follow Clean Architecture boundaries (see [docs/architecture.md](./docs/architecture.md))
- **Testing**: Add tests for business logic (xUnit, Arrange-Act-Assert)
- **UI**: Bootstrap-first development, minimal custom CSS
- **DRY**: Create reusable components when logic is used 2+ times

### Workflow

1. Create feature branch: `feat-your-feature` or `fix-your-bug`
2. Develop and test locally
3. Run tests: `dotnet test`
4. Create Pull Request to `main`
5. CI validates build and tests
6. After approval and merge, CD deploys automatically

**Detailed conventions**: See [docs/conventions.md](./docs/conventions.md)

---

**Repository**: [github.com/k-vandijk/portfolio-insight-dashboard](https://github.com/k-vandijk/portfolio-insight-dashboard)  
**Author**: Kevin van Dijk  
**Deployment**: Azure Web App  
**License**: MIT
