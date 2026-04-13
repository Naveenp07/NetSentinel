using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetSentinel.Data;
using NetSentinel.Models;
using NetSentinel.Services;

namespace NetSentinel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrafficController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PacketCaptureService _captureService;

        public TrafficController(AppDbContext db, PacketCaptureService captureService)
        {
            _db = db;
            _captureService = captureService;
        }

        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            var summary = _captureService.GetTrafficSummary();
            return Ok(summary);
        }

        [HttpGet("logs")]
        public IActionResult GetLiveLogs([FromQuery] int count = 20)
        {
            var logs = _captureService.GetLiveLogs(count);
            return Ok(logs);
        }

        [HttpGet("interfaces")]
        public IActionResult GetInterfaces()
        {
            return Ok(_captureService.GetAvailableInterfaces());
        }

        [HttpPost("capture/start")]
        public IActionResult StartCapture([FromQuery] int deviceIndex = 0)
        {
            if (_captureService.IsCapturing)
                return BadRequest("Capture already running.");
            var started = _captureService.StartCapture(deviceIndex);
            return started ? Ok("Capture started.") : StatusCode(500, "Failed to start capture. Check Npcap/libpcap and run as admin.");
        }

        [HttpPost("capture/stop")]
        public IActionResult StopCapture()
        {
            _captureService.StopCapture();
            return Ok("Capture stopped.");
        }

        [HttpGet("capture/status")]
        public IActionResult CaptureStatus()
        {
            return Ok(new { IsCapturing = _captureService.IsCapturing });
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int minutes = 10)
        {
            var since = DateTime.UtcNow.AddMinutes(-minutes);
            var records = await _db.TrafficRecords
                .Where(t => t.Timestamp >= since)
                .OrderByDescending(t => t.Timestamp)
                .Take(200)
                .ToListAsync();
            return Ok(records);
        }

        [HttpGet("by-protocol")]
        public async Task<IActionResult> GetByProtocol()
        {
            var since = DateTime.UtcNow.AddMinutes(-5);
            var summary = await _db.TrafficRecords
                .Where(t => t.Timestamp >= since)
                .GroupBy(t => t.Protocol)
                .Select(g => new
                {
                    Protocol = g.Key.ToString(),
                    TotalBytes = g.Sum(t => t.BytesSent),
                    PacketCount = g.Count()
                })
                .ToListAsync();
            return Ok(summary);
        }
    }
}
