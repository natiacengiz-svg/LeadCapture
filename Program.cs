using Microsoft.EntityFrameworkCore;
using LeadCapture.Data;
using LeadCapture.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var connUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(connUrl))
{
    try
    {
        var uri = new Uri(connUrl);
        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var userInfo = uri.UserInfo.Split(':');
            var password = userInfo.Length > 1 ? userInfo[1] : "";
            var connString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={password};SSL Mode=Require;Trust Server Certificate=true;Timeout=30";
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connString));
        }
    }
    catch { }
}

if (!builder.Services.Any(s => s.ServiceType == typeof(AppDbContext)))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite("Data Source=leads.db"));
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<LeadHub>("/hub/leads");

app.Run();
