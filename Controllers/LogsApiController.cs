using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetSentinel.Data;

namespace NetSentinel.Controllers
{
    [ApiController]
    [Route("api/logs")]
    public class LogsApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        public LogsApiController(AppDbContext db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> GetLogs([FromQuery] int count = 500) =>
            Ok(await _db.LogEntries.OrderByDescending(l => l.Timestamp).Take(count).ToListAsync());
    }
}
