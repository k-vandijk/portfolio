using Microsoft.Extensions.Caching.Memory;
using Web.MappingProfiles;
using Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient("cached-http-client")
    .AddHttpMessageHandler(sp =>
        new HttpGetCachingHandler(
            sp.GetRequiredService<IMemoryCache>(),
            absoluteTtl: TimeSpan.FromMinutes(60),
            slidingTtl: TimeSpan.FromMinutes(30)));

builder.Services.AddAutoMapper(cfg => 
    cfg.AddProfile<TransactionProfile>());

var app = builder.Build();



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.MapStaticAssets();

app.MapGet("/", () => Results.Redirect("/dashboard"));

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
