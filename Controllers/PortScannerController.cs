using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;

namespace NetSentinel.Controllers
{
    public class PortScannerController : Controller
    {
        public IActionResult Index() => View();
    }

    [ApiController]
    [Route("api/portscanner")]
    public class PortScannerApiController : ControllerBase
    {
        [HttpGet("scan")]
        public async Task<IActionResult> ScanPort([FromQuery] string ip, [FromQuery] int port)
        {
            if (string.IsNullOrWhiteSpace(ip) || port <= 0 || port > 65535)
                return BadRequest("Invalid IP or port.");

            try
            {
                using var tcp = new TcpClient();
                var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(800));
                await tcp.ConnectAsync(ip, port, cts.Token);
                return Ok(new { ip, port, status = "open" });
            }
            catch (OperationCanceledException)
            {
                return Ok(new { ip, port, status = "filtered" });
            }
            catch
            {
                return Ok(new { ip, port, status = "closed" });
            }
        }
    }
}
