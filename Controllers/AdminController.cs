using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PMEHCRM.Models;
using PMEHCRM.Data;

namespace PMEHCRM.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Admin Dashboard
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("UserLogin", "Login");
            }
            var recordsCount = _context.TicketManagementRecords.AsQueryable().Count();
            ViewBag.RecordsCount = recordsCount;
            var now = DateTime.Now;
            var openTicketsCount = _context.TicketManagementRecords.Where(x => x.TicketStatus != null && x.TicketStatus == "Open").ToList().Count();
            var solvedTicketCount = _context.TicketManagementRecords.Where(x => x.TicketStatus != null && x.TicketStatus == "Close").ToList().Count();
            var pendingTicketCount = _context.TicketManagementRecords.Where(x => x.TicketStatus != null && x.TicketStatus == "Pending").ToList().Count();
            var overDueTicketsCount = _context.TicketManagementRecords
               .Where(x => x.TicketStatus != null && (x.TicketStatus == "Open" || x.TicketStatus == "Reopen" || x.TicketStatus == "Pending" || x.TicketStatus == "Escalate") &&
                   x.CreatedDateTime.HasValue && (
                       (x.TypeOfCall == "Complaints" && x.CreatedDateTime.Value.AddMinutes(2) <= now) ||
                       (x.TypeOfCall == "Request" && x.CreatedDateTime.Value.AddHours(2) <= now) ||
                       (x.TypeOfCall == "Enquiries" && x.CreatedDateTime.Value.AddHours(1) <= now)
                   // Adjust SLA logic for Waiting Customers if needed: 
                   // || (x.TypeOfCall == "Waiting For Customer" && x.CreatedDateTime.Value <= now)
                   )
               )
               .ToList().Count();
            ViewBag.OpenTicketsCount = openTicketsCount;
            ViewBag.SolvedTicketsCount = solvedTicketCount;
            ViewBag.PendingTicketsCount = pendingTicketCount;
            ViewBag.OverDueTicketsCount = overDueTicketsCount;
            return View();
        }

        [HttpGet]
        public IActionResult UserManagement()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized(); // Only allow access for admins
            }
            var userList = _context.Users.ToList();
            return View(userList);
        }

        // GET: Admin/Create (Only Admins can access this)
        [HttpGet]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized(); // Only allow access for admins
            }
            return View();
        }

        // POST: Admin/Create (Only Admins can access this)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Login user)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                var hasher = new PasswordHasher<Login>();
                user.Password = hasher.HashPassword(user, user.Password);

                _context.Users.Add(user);
                _context.SaveChanges();

                return RedirectToAction("UserManagement");
            }

            return View(user);
        }

        // GET: Admin/Edit/5 (Only Admins can access this)
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized(); // Only allow access for admins
            }

            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            ViewBag.RoleList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Admin", Value = "Admin" },
                new SelectListItem { Text = "Supervisor", Value = "Supervisor" },
                new SelectListItem { Text = "User", Value = "User" }
            };

            return View(user);
        }

        // POST: Admin/Edit/5 (Only Admins can access this)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Login updatedUser)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized(); // Only allow access for admins
            }

            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                user.Username = updatedUser.Username;
                user.Role = updatedUser.Role;

                if (!string.IsNullOrWhiteSpace(updatedUser.Password))
                {
                    var hasher = new PasswordHasher<Login>();
                    user.Password = hasher.HashPassword(updatedUser, updatedUser.Password);
                }

                _context.Users.Update(user);
                _context.SaveChanges();

                return RedirectToAction("UserManagement");
            }

            ViewBag.RoleList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Admin", Value = "Admin" },
                new SelectListItem { Text = "Admin", Value = "Supervisor" },
                new SelectListItem { Text = "User", Value = "User" }
            };

            return View(updatedUser);
        }

        // POST: Admin/Delete/5 (Only Admins can access this)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized(); // Only allow access for admins
            }

            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            _context.SaveChanges();
            return RedirectToAction("UserManagement");
        }
    }
}
