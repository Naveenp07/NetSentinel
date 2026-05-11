using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetSentinel.Data;
using NetSentinel.Models;

namespace NetSentinel.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        public HomeController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var stats = new DashboardStats
            {
                TotalDevices   = await _db.Devices.CountAsync(),
                OnlineDevices  = await _db.Devices.CountAsync(d => d.IsOnline),
                OfflineDevices = await _db.Devices.CountAsync(d => !d.IsOnline),
                ActiveAlerts   = await _db.Alerts.CountAsync(a => !a.IsAcknowledged),
                LastScan       = (await _db.ScanResults.OrderByDescending(s => s.ScanTime).FirstOrDefaultAsync())?.ScanTime ?? DateTime.MinValue
            };
            return View(stats);
        }

        public async Task<IActionResult> Logs() =>
            View(await _db.LogEntries.OrderByDescending(l => l.Timestamp).Take(200).ToListAsync());

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View();
    }
}
