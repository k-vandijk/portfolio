using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Web.Middleware;
using Web.Services;

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

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient("cached-http-client")
    .AddHttpMessageHandler(sp =>
        new HttpGetCachingHandler(
            sp.GetRequiredService<IMemoryCache>(), 
            absoluteTtl: TimeSpan.FromMinutes(60),
            slidingTtl: TimeSpan.FromMinutes(30)));

builder.Services.AddScoped<IAzureTableService, AzureTableService>();
builder.Services.AddScoped<ITickerApiService, TickerApiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/dashboard"));
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();
