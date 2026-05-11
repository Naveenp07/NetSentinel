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
        private readonly AlertService _svc;
        public AlertsController(AppDbContext db, AlertService svc) { _db = db; _svc = svc; }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool unacknowledgedOnly = false)
        {
            var q = _db.Alerts.AsQueryable();
            if (unacknowledgedOnly) q = q.Where(a => !a.IsAcknowledged);
            return Ok(await q.OrderByDescending(a => a.Timestamp).Take(100).ToListAsync());
        }

        [HttpGet("count")]
        public async Task<IActionResult> Count() =>
            Ok(new { UnacknowledgedCount = await _db.Alerts.CountAsync(a => !a.IsAcknowledged) });

        [HttpPost("{id}/acknowledge")]
        public async Task<IActionResult> Ack(int id) => await _svc.AcknowledgeAsync(id) ? Ok() : NotFound();

        [HttpPost("acknowledge-all")]
        public async Task<IActionResult> AckAll()
        {
            var list = await _db.Alerts.Where(a => !a.IsAcknowledged).ToListAsync();
            list.ForEach(a => a.IsAcknowledged = true);
            await _db.SaveChangesAsync();
            return Ok(new { Acknowledged = list.Count });
        }
    }
}
