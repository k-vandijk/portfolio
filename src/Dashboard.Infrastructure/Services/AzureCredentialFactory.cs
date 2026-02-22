using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace Dashboard.Infrastructure.Services;

public static class AzureCredentialFactory
{
    public static TokenCredential GetCredential(IConfiguration config, bool isDevelopment)
    {
        if (isDevelopment)
        {
            var tenantId = config["MicrosoftFoundry:TenantId"];
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new InvalidOperationException(
                    "azure-foundry-tenant-id is required for local development. " +
                    "Add it to user secrets with: dotnet user-secrets set \"MicrosoftFoundry:TenantId\" \"YOUR_TENANT_ID\"");
            }

            return new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
            {
                TenantId = tenantId
            });
        }

        return new DefaultAzureCredential();
    }
}
