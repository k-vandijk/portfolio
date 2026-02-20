using Dashboard.Infrastructure;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Localization;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Globalization;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

var keyVaultUri = builder.Configuration["KeyVaultUri"];
if (!builder.Environment.IsDevelopment() && !string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}

// Add services to the container.
// Add Azure AD authentication
builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        options.ResponseType = OpenIdConnectResponseType.Code;
    });

// Require authenticated users by default
builder.Services.AddAuthorization(o => o.FallbackPolicy = o.DefaultPolicy);

builder.Services.AddLocalization(options => options.ResourcesPath = "wwwroot");

builder.Services.AddControllersWithViews().AddViewLocalization();

// Add application services
builder.Services.AddInfrastructure();

var app = builder.Build();

var nlNL = new CultureInfo("nl-NL");
var enUS = new CultureInfo("en-US");

CultureInfo.DefaultThreadCurrentCulture = nlNL;
CultureInfo.DefaultThreadCurrentUICulture = nlNL;

var supportedUI = new[] { nlNL, enUS };
var supportedFormatting = new[] { nlNL };

// Default request culture: format = nl-NL, UI = nl-NL (you can pick en-US if you prefer)
var locOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture: "nl-NL", uiCulture: "nl-NL"),
    SupportedCultures = supportedFormatting,
    SupportedUICultures = supportedUI
};

// Alleen de localization laten bepalen via een cookie
locOptions.RequestCultureProviders = [ new CookieRequestCultureProvider() ];

app.UseRequestLocalization(locOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute("default", "{controller=Dashboard}/{action=Index}/{id?}");

await app.StartAsync();

foreach (var url in app.Urls) Console.WriteLine($"Now listening on: {url}");

// keep the app running
await app.WaitForShutdownAsync();



// Dummy class for localization resources
public sealed class SharedResource { }