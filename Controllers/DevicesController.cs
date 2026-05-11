using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetSentinel.Data;
using NetSentinel.Hubs;
using NetSentinel.Services;

namespace NetSentinel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly NetworkScannerService _scanner;
        private readonly AlertService _alerts;
        private readonly IHubContext<NetworkHub> _hub;

        public DevicesController(AppDbContext db, NetworkScannerService scanner, AlertService alerts, IHubContext<NetworkHub> hub)
        { _db = db; _scanner = scanner; _alerts = alerts; _hub = hub; }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _db.Devices.OrderByDescending(d => d.IsOnline).ThenBy(d => d.IpAddress).ToListAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        { var d = await _db.Devices.FindAsync(id); return d == null ? NotFound() : Ok(d); }

        [HttpPost("scan")]
        public async Task<IActionResult> Scan()
        {
            var result = await _scanner.ScanNetworkAsync();
            var newAlerts = await _alerts.RunChecksAsync();
            foreach (var a in newAlerts) await _hub.Clients.All.SendAsync("ReceiveAlert", a);
            await _hub.Clients.All.SendAsync("ReceiveDeviceList", await _db.Devices.ToListAsync());
            return Ok(new { result.DevicesFound, result.OnlineCount, result.NewDevicesFound, result.DurationMs, NewAlerts = newAlerts.Count });
        }

        [HttpPut("{id}/label")]
        public async Task<IActionResult> SetLabel(int id, [FromBody] string label)
        { var d = await _db.Devices.FindAsync(id); if (d == null) return NotFound(); d.DeviceLabel = label; await _db.SaveChangesAsync(); return Ok(d); }

        [HttpPut("{id}/trust")]
        public async Task<IActionResult> Trust(int id)
        { var d = await _db.Devices.FindAsync(id); if (d == null) return NotFound(); d.IsKnown = true; await _db.SaveChangesAsync(); return Ok(d); }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        { var d = await _db.Devices.FindAsync(id); if (d == null) return NotFound(); _db.Devices.Remove(d); await _db.SaveChangesAsync(); return NoContent(); }
    }
}
