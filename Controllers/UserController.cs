using Microsoft.AspNetCore.Mvc;

namespace PMEHCRM.Controllers
{
    public class UserController : Controller
    {
        // User Dashboard
        [HttpGet]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Role") != "User")
            {
                return Unauthorized();
            }

            return View(); 
        }
    }
}
