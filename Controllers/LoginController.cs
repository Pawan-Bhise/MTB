using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using PMEHCRM.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using PMEHCRM.Data;
using PMEHCRM.Models;

namespace PMEHCRM.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoginController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: User/Login
        [HttpGet]
        public IActionResult UserLogin()
        {
            return View();
        }

        // POST: User/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UserLogin(UserLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get user from DB
            var user = _context.Users.FirstOrDefault(u => u.Username == model.Username);
            if (user != null)
            {
                var hasher = new PasswordHasher<Login>();
                var result = hasher.VerifyHashedPassword(user, user.Password, model.Password);

                if (result == PasswordVerificationResult.Success)
                {
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("Role", user.Role);

                    return user.Role switch
                    {
                        "Admin" => RedirectToAction("Index", "Admin"),
                        "Supervisor" => RedirectToAction("Index", "Supervisor"),
                        _ => RedirectToAction("Index", "User")
                    };
                }
            }
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        // Logout action
        [HttpGet]
        public IActionResult UserLogout()
        {
            // Clear session data
            HttpContext.Session.Remove("Username");
            HttpContext.Session.Remove("Role");
            HttpContext.Session.Clear();
            return RedirectToAction("UserLogin");
        }
    }
}
