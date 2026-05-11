using Microsoft.EntityFrameworkCore;
using NetSentinel.Data;
using NetSentinel.Hubs;
using NetSentinel.Services;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// SQL Server 2019
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(30);
        }));

// App services
builder.Services.AddScoped<NetworkScannerService>();
builder.Services.AddScoped<AlertService>();
builder.Services.AddSingleton<PacketCaptureService>();

var app = builder.Build();

// Auto-create DB and apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        db.Database.Migrate();
        logger.LogInformation("SQL Server database ready.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed. Check SQL Server connection string.");
    }
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<NetworkHub>("/networkHub");

app.Run();
