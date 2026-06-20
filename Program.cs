using Microsoft.EntityFrameworkCore;
using LeadCapture.Data;
using LeadCapture.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var connUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var dbConnectionString = "Data Source=leads.db";
Action<DbContextOptionsBuilder>? dbConfig = null;

if (!string.IsNullOrEmpty(connUrl))
{
    try
    {
        var uri = new Uri(connUrl);
        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var userInfo = uri.UserInfo.Split(':');
            var password = userInfo.Length > 1 ? userInfo[1] : "";
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');
            dbConnectionString = $"Host={uri.Host};Port={port};Database={database};Username={userInfo[0]};Password={password};SSL Mode=Require;Trust Server Certificate=true;Timeout=5";
            dbConfig = options => options.UseNpgsql(dbConnectionString);
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[DB] Failed to parse DATABASE_URL: {ex.Message}");
    }
}

// If Npgsql is configured, try it; fall back to SQLite if it fails
if (dbConfig != null)
{
    // Register Npgsql first
    builder.Services.AddDbContext<AppDbContext>(dbConfig);
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(dbConnectionString));
}

builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", config =>
    {
        config.Cookie.Name = "LeadCapture.Auth";
        config.LoginPath = "/Admin/Login";
        config.AccessDeniedPath = "/Admin/Login";
    });

var app = builder.Build();

try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        Console.Error.WriteLine("[DB] EnsureCreated succeeded");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[DB] EnsureCreated failed: {ex}");
}

app.UseStaticFiles();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapGet("/debug", (HttpContext ctx) =>
{
    var lines = new List<string>
    {
        $"Time: {DateTime.UtcNow:O}",
        $"Env: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}",
        $"HasDbUrl: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_URL"))}"
    };
    return string.Join("\n", lines);
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<LeadHub>("/hub/leads");

app.Run();
