using Microsoft.AspNetCore.Mvc;

namespace NetSentinel.Controllers
{
    public class SettingsController : Controller
    {
        public IActionResult Index() => View();
    }
}
