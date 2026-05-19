using Microsoft.AspNetCore.Mvc;

namespace NetSentinel.Controllers
{
    public class PacketsController : Controller
    {
        public IActionResult Index() => View();
    }
}
