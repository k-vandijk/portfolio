# CI/CD

This document describes the continuous integration and continuous deployment workflows for the Portfolio Insight Dashboard.

## Overview

The project uses **GitHub Actions** for automated CI/CD with two workflows:

| Workflow | Trigger | Purpose | Badge |
|---|---|---|---|
| **Continuous Integration** | Pull requests to `main` | Build and test validation | [![Build Status](https://github.com/k-vandijk/portfolio-insight-dashboard/actions/workflows/continuous-integration.yml/badge.svg)](https://github.com/k-vandijk/portfolio-insight-dashboard/actions/workflows/continuous-integration.yml) |
| **Continuous Deployment** | Push to `main` | Build and deploy to Azure | [![Deployment Status](https://github.com/k-vandijk/portfolio-insight-dashboard/actions/workflows/continuous-deployment.yml/badge.svg)](https://github.com/k-vandijk/portfolio-insight-dashboard/actions/workflows/continuous-deployment.yml) |

## Continuous Integration (CI)

### Workflow File

**Location**: `.github/workflows/continuous-integration.yml`

### Trigger

Runs on **Pull Requests** targeting the `main` branch.

```yaml
on:
  pull_request:
    branches:
      - main
```

### Steps

1. **Checkout Code**
   - Checks out the repository code

2. **Setup .NET**
   - Installs .NET 10 SDK
   - Caches NuGet packages for faster builds

3. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

4. **Build Solution**
   ```bash
   dotnet build --configuration Release --no-restore
   ```

5. **Run Tests**
   ```bash
   dotnet test --no-build --verbosity normal
   ```

### Success Criteria

✅ All steps complete successfully  
✅ Build completes without errors  
✅ All unit tests pass  

### Failure Handling

If any step fails:
- ❌ Pull request shows failed check
- 🔴 Badge turns red
- 📧 Author receives notification
- 🚫 Merge is blocked (if branch protection enabled)

### Example CI Run

```
✅ Checkout code
✅ Setup .NET 10
✅ Restore dependencies (28 packages)
✅ Build solution (0 errors, 0 warnings)
✅ Run tests (15 passed, 0 failed)
✓ CI Passed in 2m 34s
```

## Continuous Deployment (CD)

### Workflow File

**Location**: `.github/workflows/continuous-deployment.yml`

### Trigger

Runs on **Push** to the `main` branch (typically after PR merge).

```yaml
on:
  push:
    branches:
      - main
```

### Steps

#### 1. Checkout Code
Checks out the repository code.

#### 2. Setup Node.js
Installs Node.js 20.x for SCSS compilation.

#### 3. Install npm Dependencies
```bash
npm install
```

Installs:
- Sass compiler
- Bootstrap
- Concurrently

#### 4. Build Frontend Assets
```bash
npm run sass:build
```

Compiles SCSS → CSS:
- Input: `src/Dashboard._Web/wwwroot/scss/main.scss`
- Output: `src/Dashboard._Web/wwwroot/css/site.css`

#### 5. Setup .NET
Installs .NET 10 SDK.

#### 6. Restore .NET Dependencies
```bash
dotnet restore
```

#### 7. Build .NET Solution
```bash
dotnet build --configuration Release --no-restore
```

#### 8. Publish Application
```bash
dotnet publish src/Dashboard._Web/Dashboard._Web.csproj \
  --configuration Release \
  --output ./publish \
  --no-build
```

Creates deployment package in `./publish` directory.

#### 9. Deploy to Azure Web App
```bash
az webapp deploy \
  --resource-group <resource-group> \
  --name as-kvandijk-ticker-api-dashboard \
  --src-path ./publish
```

Deploys the published application to Azure App Service.

### Azure Configuration

**App Service Name**: `as-kvandijk-ticker-api-dashboard`

**Required GitHub Secrets**:

| Secret | Description | Setup |
|---|---|---|
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Azure Web App publish profile | Download from Azure Portal → App Service → Get publish profile |

**Alternative**: Use Azure Service Principal authentication (more secure):

| Secret | Description |
|---|---|
| `AZURE_CLIENT_ID` | Service principal client ID |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |

### Success Criteria

✅ All build steps complete  
✅ Frontend assets compiled  
✅ .NET solution builds successfully  
✅ Application published  
✅ Deployed to Azure without errors  
✅ Application responds at production URL  

### Example CD Run

```
✅ Checkout code
✅ Setup Node.js 20.x
✅ Install npm dependencies (3 packages)
✅ Build SCSS → CSS (site.css: 145 KB)
✅ Setup .NET 10
✅ Restore dependencies (28 packages)
✅ Build solution (0 errors, 0 warnings)
✅ Publish application (32 MB)
✅ Deploy to Azure Web App
✓ CD Passed in 4m 12s
```

## Branching Strategy

### Main Branch

**Branch**: `main`

**Protection**:
- ✅ Require pull request before merging
- ✅ Require status checks to pass (CI)
- ✅ Require branches to be up to date
- ❌ Do not allow force pushes
- ❌ Do not allow deletions

**Purpose**: Production-ready code only. Every commit triggers automatic deployment.

### Feature Branches

**Convention**: `feat-*` or `feature/*`

**Example**: `feat-add-export-function`, `feature/transaction-filters`

**Workflow**:
1. Create branch from `main`: `git checkout -b feat-my-feature`
2. Develop and commit changes
3. Push to GitHub: `git push origin feat-my-feature`
4. Create Pull Request to `main`
5. CI runs automatically
6. Code review and approval
7. Merge to `main` (triggers CD)
8. Delete feature branch

### Bugfix Branches

**Convention**: `fix-*` or `bugfix/*`

**Example**: `fix-date-parsing-bug`, `bugfix/cache-invalidation`

**Workflow**: Same as feature branches.

### Hotfix Branches

**Convention**: `hotfix/*`

**Example**: `hotfix/security-patch`, `hotfix/critical-bug`

**Purpose**: Emergency fixes for production issues.

**Workflow**:
1. Create branch from `main`
2. Make minimal fix
3. Fast-track PR review
4. Merge to `main` (deploys immediately)

## Deployment Process

### Standard Deployment (via PR)

```
1. Create feature branch
2. Develop and commit
3. Push to GitHub
4. Create PR to main
5. CI runs (build + test)
6. Code review
7. Merge PR
8. CD runs automatically
9. Deploy to Azure
10. Verify in production
```

### Time to Production

- **CI Duration**: ~2-3 minutes
- **CD Duration**: ~4-5 minutes
- **Total**: ~6-8 minutes from merge to production

## Environment Variables

### GitHub Secrets

Set in **Repository Settings** → **Secrets and variables** → **Actions**:

| Secret | Required | Description |
|---|---|---|
| `AZURE_WEBAPP_PUBLISH_PROFILE` | ✅ Yes | Azure Web App publish profile |

### Azure App Service Configuration

Set in **Azure Portal** → **App Service** → **Configuration** → **Application settings**:

| Setting | Required | Description |
|---|---|---|
| `TRANSACTIONS_TABLE_CONNECTION_STRING` | ✅ Yes | Azure Table Storage connection |
| `TICKER_API_URL` | ✅ Yes | Ticker API base URL |
| `TICKER_API_CODE` | ✅ Yes | Ticker API authentication code |
| `AzureAd__TenantId` | ✅ Yes | Azure AD tenant ID |
| `AzureAd__ClientId` | ✅ Yes | Azure AD client ID |
| `AzureAd__ClientSecret` | ✅ Yes | Azure AD client secret |

**Best Practice**: Use Azure Key Vault references for secrets:
```
@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/TickerApiCode/)
```

## Monitoring Deployments

### GitHub Actions UI

1. Navigate to **Actions** tab in GitHub
2. View workflow runs
3. Click on a run to see detailed logs
4. Expand each step to see output

### Azure Portal

1. Navigate to **App Service** → **Deployment Center**
2. View deployment history
3. Check deployment logs
4. Verify running version

### Application Insights (Optional)

If configured:
1. Navigate to **Application Insights** in Azure Portal
2. View **Live Metrics** during deployment
3. Check **Failures** for any deployment issues
4. Monitor **Performance** post-deployment

## Rollback Strategy

### Option 1: Revert Commit

```bash
# Revert the problematic commit
git revert <commit-hash>

# Push to main
git push origin main

# CD runs and deploys the reverted version
```

### Option 2: Azure Portal Swap

1. Navigate to **App Service** → **Deployment slots**
2. Swap staging and production slots
3. Instant rollback to previous version

(Requires deployment slots to be configured)

### Option 3: Redeploy Previous Version

1. Navigate to **Deployment Center** in Azure Portal
2. Select previous successful deployment
3. Click **Redeploy**

## Troubleshooting

### CI Failing

**Build Errors**:
1. Pull latest `main` branch
2. Run `dotnet build` locally
3. Fix compilation errors
4. Push fixes and rerun CI

**Test Failures**:
1. Run `dotnet test` locally
2. Fix failing tests
3. Verify all tests pass locally
4. Push fixes and rerun CI

### CD Failing

**SCSS Compilation Error**:
1. Run `npm run sass:build` locally
2. Fix SCSS syntax errors
3. Push fixes

**Deployment Error**:
1. Check GitHub Actions logs
2. Verify Azure credentials are valid
3. Check Azure App Service status
4. Verify publish profile is up to date

### Post-Deployment Issues

**Application Not Starting**:
1. Check Azure App Service logs
2. Verify environment variables are set
3. Check Application Insights for errors
4. Verify database connection strings

**Features Not Working**:
1. Clear browser cache
2. Check Azure App Service logs
3. Verify API keys and secrets
4. Test locally with production configuration

## Best Practices

### Before Merging PR

- ✅ CI passes successfully
- ✅ Code reviewed and approved
- ✅ Feature tested locally
- ✅ No merge conflicts
- ✅ Branch is up to date with `main`

### After Deployment

- ✅ Verify application is accessible
- ✅ Test critical functionality
- ✅ Check for console errors
- ✅ Monitor Application Insights (if available)
- ✅ Announce deployment (if significant changes)

### Secrets Management

- ✅ Never commit secrets to repository
- ✅ Use GitHub Secrets for CI/CD credentials
- ✅ Use Azure Key Vault for application secrets
- ✅ Rotate secrets regularly
- ✅ Revoke compromised secrets immediately

### Performance Optimization

- ✅ Cache NuGet packages (done automatically)
- ✅ Cache npm packages (consider adding)
- ✅ Use incremental builds when possible
- ✅ Minimize published output size
- ✅ Enable Application Insights for monitoring

## Future Enhancements

Potential improvements to CI/CD:

- [ ] **Staging Environment** - Deploy to staging slot first, manual approval for production
- [ ] **Integration Tests** - Add Playwright/Selenium tests to CI
- [ ] **Code Coverage** - Require minimum code coverage threshold
- [ ] **Security Scanning** - Add dependency vulnerability scanning
- [ ] **Performance Testing** - Automated performance regression tests
- [ ] **Deployment Notifications** - Slack/Teams notifications on deployment
- [ ] **Blue-Green Deployment** - Zero-downtime deployments using slots
- [ ] **Canary Releases** - Gradual rollout to subset of users

---

[← Back to Documentation Index](./README.md)
