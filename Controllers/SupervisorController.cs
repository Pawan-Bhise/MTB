using Microsoft.AspNetCore.Mvc;
using PMEHCRM.Data;

namespace PMEHCRM.Controllers
{
    public class SupervisorController : Controller
    {
        private readonly ApplicationDbContext _context;
        public SupervisorController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var now = DateTime.Now;
            var openTicketsCount = _context.TicketManagementRecords.Where(x => x.TicketStatus != null && x.TicketStatus == "Open").ToList().Count();
            var solvedTicketCount = _context.TicketManagementRecords.Where(x => x.TicketStatus != null && x.TicketStatus == "Close").ToList().Count();
            var pendingTicketCount = _context.TicketManagementRecords.Where(x => x.TicketStatus != null && x.TicketStatus == "Pending").ToList().Count();
            var overDueTicketsCount = _context.TicketManagementRecords
               .Where(x => x.TicketStatus != null && x.TicketStatus == "Open" &&
                   x.CreatedDateTime.HasValue && (
                       (x.TypeOfCall == "Complaints" && x.CreatedDateTime.Value.AddHours(4) <= now) ||
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
    }
}
