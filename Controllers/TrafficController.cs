using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetSentinel.Data;
using NetSentinel.Services;

namespace NetSentinel.Controllers
{
    public class TrafficController : Controller
    {
        public IActionResult Index() => View();
    }

    [ApiController]
    [Route("api/traffic")]
    public class TrafficApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PacketCaptureService _capture;
        public TrafficApiController(AppDbContext db, PacketCaptureService capture) { _db = db; _capture = capture; }

        [HttpGet("stats")]    public IActionResult Stats() => Ok(_capture.GetTrafficSummary());
        [HttpGet("logs")]     public IActionResult Logs([FromQuery] int count = 20) => Ok(_capture.GetLiveLogs(count));
        [HttpGet("interfaces")] public IActionResult Interfaces() => Ok(_capture.GetAvailableInterfaces());
        [HttpPost("capture/start")] public IActionResult Start([FromQuery] int deviceIndex = 0) =>
            _capture.IsCapturing ? BadRequest("Already capturing.") :
            _capture.StartCapture(deviceIndex) ? Ok("Started.") : StatusCode(500, "Failed. Run as Administrator with Npcap.");
        [HttpPost("capture/stop")] public IActionResult Stop() { _capture.StopCapture(); return Ok(); }
        [HttpGet("capture/status")] public IActionResult Status() => Ok(new { _capture.IsCapturing });
        [HttpGet("history")] public async Task<IActionResult> History([FromQuery] int minutes = 60) =>
            Ok(await _db.TrafficRecords.Where(t => t.Timestamp >= DateTime.UtcNow.AddMinutes(-minutes)).OrderByDescending(t => t.Timestamp).Take(200).ToListAsync());
    }
}
