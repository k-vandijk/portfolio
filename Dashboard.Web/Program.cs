using Dashboard.Application;
using Dashboard.Domain;
using Dashboard.Infrastructure;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Add Azure AD authentication
builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.SaveTokens = true;
    });

// Require authenticated users by default
builder.Services.AddAuthorization(o => o.FallbackPolicy = o.DefaultPolicy);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add application services
builder.Services.AddApplication();
builder.Services.AddDomain();
builder.Services.AddInfrastructure();

var app = builder.Build();

var culture = new System.Globalization.CultureInfo("nl-NL");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;

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

//app.MapGet("/", () => Results.Redirect("/dashboard"));
app.MapControllerRoute("default", "{controller=Dashboard}/{action=Index}/{id?}");

await app.StartAsync();

foreach (var a in app.Urls) Log.Information("Now listening on: {BaseUrl}", a);

// keep the app running
await app.WaitForShutdownAsync();