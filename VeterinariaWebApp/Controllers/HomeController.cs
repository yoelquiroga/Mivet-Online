using Microsoft.AspNetCore.Mvc;

namespace VeterinariaWebApp.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var isLoggedIn = HttpContext.Session.GetInt32("ClienteId").HasValue
            || HttpContext.Session.GetInt32("VeterinarioId").HasValue
            || HttpContext.Session.GetInt32("RecepcionistaId").HasValue;

        ViewBag.IsLoggedIn = isLoggedIn;
        return View();
    }
}
