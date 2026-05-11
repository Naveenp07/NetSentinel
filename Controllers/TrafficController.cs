using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetSentinel.Data;
using NetSentinel.Services;

namespace NetSentinel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrafficController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PacketCaptureService _capture;
        public TrafficController(AppDbContext db, PacketCaptureService capture) { _db = db; _capture = capture; }

        [HttpGet("stats")]    public IActionResult Stats() => Ok(_capture.GetTrafficSummary());
        [HttpGet("logs")]     public IActionResult Logs([FromQuery] int count = 20) => Ok(_capture.GetLiveLogs(count));
        [HttpGet("interfaces")] public IActionResult Interfaces() => Ok(_capture.GetAvailableInterfaces());

        [HttpPost("capture/start")]
        public IActionResult Start([FromQuery] int deviceIndex = 0)
        {
            if (_capture.IsCapturing) return BadRequest("Already capturing.");
            return _capture.StartCapture(deviceIndex)
                ? Ok("Capture started.")
                : StatusCode(500, "Failed. Check Npcap/libpcap and run as Administrator.");
        }

        [HttpPost("capture/stop")]
        public IActionResult Stop() { _capture.StopCapture(); return Ok("Stopped."); }

        [HttpGet("capture/status")]
        public IActionResult Status() => Ok(new { _capture.IsCapturing });

        [HttpGet("history")]
        public async Task<IActionResult> History([FromQuery] int minutes = 10) =>
            Ok(await _db.TrafficRecords
                .Where(t => t.Timestamp >= DateTime.UtcNow.AddMinutes(-minutes))
                .OrderByDescending(t => t.Timestamp).Take(200).ToListAsync());

        [HttpGet("by-protocol")]
        public async Task<IActionResult> ByProtocol() =>
            Ok(await _db.TrafficRecords
                .Where(t => t.Timestamp >= DateTime.UtcNow.AddMinutes(-5))
                .GroupBy(t => t.Protocol)
                .Select(g => new { Protocol = g.Key.ToString(), TotalBytes = g.Sum(t => t.BytesSent), Count = g.Count() })
                .ToListAsync());
    }
}
