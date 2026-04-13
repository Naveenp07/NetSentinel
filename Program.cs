using Microsoft.EntityFrameworkCore;
using NetSentinel.Data;
using NetSentinel.Hubs;
using NetSentinel.Services;
 
var builder = WebApplication.CreateBuilder(args);
 
// JSON must be camelCase so JavaScript fetch calls can read properties
builder.Services.AddControllersWithViews()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.WriteIndented = false;
    });
 
builder.Services.AddSignalR();
 
// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=netsentinel.db"));
 
// Services
builder.Services.AddScoped<NetworkScannerService>();
builder.Services.AddScoped<AlertService>();
// PacketCaptureService holds a long-lived capture device handle, must be Singleton
builder.Services.AddSingleton<PacketCaptureService>();
 
var app = builder.Build();
 
// Ensure DB exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}
 
if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");
 
app.UseStaticFiles();
app.UseRouting();
 
// IMPORTANT: MapControllers for API + MapControllerRoute for MVC views
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
 
app.MapHub<NetworkHub>("/networkHub");
 
app.Run();