using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetSentinel.Data;
using NetSentinel.Hubs;
using NetSentinel.Services;

namespace NetSentinel.Controllers
{
    public class DevicesController : Controller
    {
        private readonly AppDbContext _db;
        public DevicesController(AppDbContext db) { _db = db; }
        public IActionResult Index() => View();
    }

    [ApiController]
    [Route("api/[controller]")]
    public class DevicesApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly NetworkScannerService _scanner;
        private readonly AlertService _alerts;
        private readonly IHubContext<NetworkHub> _hub;

        public DevicesApiController(AppDbContext db, NetworkScannerService scanner, AlertService alerts, IHubContext<NetworkHub> hub)
        { _db = db; _scanner = scanner; _alerts = alerts; _hub = hub; }

        [HttpGet] [Route("/api/devices")]
        public async Task<IActionResult> GetAll() =>
            Ok(await _db.Devices.OrderByDescending(d => d.IsOnline).ThenBy(d => d.IpAddress).ToListAsync());

        [HttpPost] [Route("/api/devices/scan")]
        public async Task<IActionResult> Scan()
        {
            var result = await _scanner.ScanNetworkAsync();
            var newAlerts = await _alerts.RunChecksAsync();
            foreach (var a in newAlerts) await _hub.Clients.All.SendAsync("ReceiveAlert", a);
            await _hub.Clients.All.SendAsync("ReceiveDeviceList", await _db.Devices.ToListAsync());
            return Ok(new { result.DevicesFound, result.OnlineCount, result.NewDevicesFound, result.DurationMs, NewAlerts = newAlerts.Count });
        }

        [HttpPut] [Route("/api/devices/{id}/label")]
        public async Task<IActionResult> SetLabel(int id, [FromBody] string label)
        { var d = await _db.Devices.FindAsync(id); if (d == null) return NotFound(); d.DeviceLabel = label; await _db.SaveChangesAsync(); return Ok(d); }

        [HttpPut] [Route("/api/devices/{id}/trust")]
        public async Task<IActionResult> Trust(int id)
        { var d = await _db.Devices.FindAsync(id); if (d == null) return NotFound(); d.IsKnown = true; await _db.SaveChangesAsync(); return Ok(d); }

        [HttpDelete] [Route("/api/devices/{id}/trust")]
        public async Task<IActionResult> Untrust(int id)
        { var d = await _db.Devices.FindAsync(id); if (d == null) return NotFound(); d.IsKnown = false; await _db.SaveChangesAsync(); return Ok(d); }

        [HttpDelete] [Route("/api/devices/{id}")]
        public async Task<IActionResult> Delete(int id)
        { var d = await _db.Devices.FindAsync(id); if (d == null) return NotFound(); _db.Devices.Remove(d); await _db.SaveChangesAsync(); return NoContent(); }
    }
}
