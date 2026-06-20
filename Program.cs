using Microsoft.EntityFrameworkCore;
using LeadCapture.Data;
using LeadCapture.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var connUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(connUrl))
{
    var uri = new Uri(connUrl);
    var userInfo = uri.UserInfo.Split(':');
    var connString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connString));
}
else
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
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
