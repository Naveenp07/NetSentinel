using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetSentinel.Data;
using NetSentinel.Hubs;
using NetSentinel.Models;
using NetSentinel.Services;

namespace NetSentinel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly NetworkScannerService _scanner;
        private readonly AlertService _alertService;
        private readonly IHubContext<NetworkHub> _hub;

        public DevicesController(AppDbContext db, NetworkScannerService scanner,
            AlertService alertService, IHubContext<NetworkHub> hub)
        {
            _db = db;
            _scanner = scanner;
            _alertService = alertService;
            _hub = hub;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var devices = await _db.Devices.OrderByDescending(d => d.IsOnline)
                                           .ThenBy(d => d.IpAddress)
                                           .ToListAsync();
            return Ok(devices);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var device = await _db.Devices.FindAsync(id);
            return device == null ? NotFound() : Ok(device);
        }

        [HttpPost("scan")]
        public async Task<IActionResult> Scan()
        {
            var result = await _scanner.ScanNetworkAsync();

            // Run alert checks after scan
            var alerts = await _alertService.RunChecksAsync();
            foreach (var alert in alerts)
                await _hub.Clients.All.SendAsync("ReceiveAlert", alert);

            // Push updated device list
            var devices = await _db.Devices.ToListAsync();
            await _hub.Clients.All.SendAsync("ReceiveDeviceList", devices);

            return Ok(new
            {
                result.DevicesFound,
                result.OnlineCount,
                result.NewDevicesFound,
                result.DurationMs,
                NewAlerts = alerts.Count
            });
        }

        [HttpPut("{id}/label")]
        public async Task<IActionResult> SetLabel(int id, [FromBody] string label)
        {
            var device = await _db.Devices.FindAsync(id);
            if (device == null) return NotFound();
            device.DeviceLabel = label;
            await _db.SaveChangesAsync();
            return Ok(device);
        }

        [HttpPut("{id}/trust")]
        public async Task<IActionResult> MarkKnown(int id)
        {
            var device = await _db.Devices.FindAsync(id);
            if (device == null) return NotFound();
            device.IsKnown = true;
            await _db.SaveChangesAsync();
            return Ok(device);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var device = await _db.Devices.FindAsync(id);
            if (device == null) return NotFound();
            _db.Devices.Remove(device);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
