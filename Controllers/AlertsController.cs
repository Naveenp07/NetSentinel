using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetSentinel.Data;
using NetSentinel.Services;

namespace NetSentinel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlertsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly AlertService _alertService;

        public AlertsController(AppDbContext db, AlertService alertService)
        {
            _db = db;
            _alertService = alertService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool unacknowledgedOnly = false)
        {
            var query = _db.Alerts.AsQueryable();
            if (unacknowledgedOnly)
                query = query.Where(a => !a.IsAcknowledged);
            var alerts = await query.OrderByDescending(a => a.Timestamp).Take(100).ToListAsync();
            return Ok(alerts);
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var count = await _db.Alerts.CountAsync(a => !a.IsAcknowledged);
            return Ok(new { UnacknowledgedCount = count });
        }

        [HttpPost("{id}/acknowledge")]
        public async Task<IActionResult> Acknowledge(int id)
        {
            var success = await _alertService.AcknowledgeAsync(id);
            return success ? Ok() : NotFound();
        }

        [HttpPost("acknowledge-all")]
        public async Task<IActionResult> AcknowledgeAll()
        {
            var unack = await _db.Alerts.Where(a => !a.IsAcknowledged).ToListAsync();
            foreach (var a in unack) a.IsAcknowledged = true;
            await _db.SaveChangesAsync();
            return Ok(new { Acknowledged = unack.Count });
        }
    }
}
