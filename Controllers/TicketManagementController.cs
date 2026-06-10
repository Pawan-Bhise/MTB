using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PMEHCRM.Data;
using PMEHCRM.Models;
using PMEHCRM.Models.ViewModels;

namespace PMEHCRM.Controllers
{
    public class TicketManagementController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;


        // GET: Display the Ticket Management Form
        [HttpGet]
        public IActionResult TicketManagement()
        {
            if (HttpContext.Session.GetString("Role") != "Admin" && HttpContext.Session.GetString("Role") != "Supervisor" && HttpContext.Session.GetString("Role") != "User")
            {
                return RedirectToAction("UserLogin", "Login");
            }

            ViewBag.AgentName = HttpContext.Session.GetString("Username");
            // Fetch all usernames from Users table
            ViewBag.UserList = _context.Users
                .Select(u => new SelectListItem
                {
                    Value = u.Username,   // this will be stored in the DB / model
                    Text = u.Username     // this will be displayed in the dropdown
                })
                .ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TicketManagementPost(TicketManagement model, List<IFormFile> Attachments)
        {
            if (ModelState.IsValid)
            {
                model.AgentName = HttpContext.Session.GetString("Username");
                model.CreatedDateTime = DateTime.Now;
                model.DateTimeSubmitted = DateTime.Now;

                if (model.TicketStatus == "Escalate")
                {
                    model.EscalatedDateTime = DateTime.Now;
                }
                else if (model.TicketStatus == "Resolve")
                {
                    model.ResolvedDateTime = DateTime.Now;
                    //model.EscalatedDateTime = DateTime.Now;
                }
                else if (model.TicketStatus == "Close")
                {
                    model.ResolvedDateTime = DateTime.Now;
                    model.ClosedDateTime = DateTime.Now;
                }
                // Helper Method
                bool IsOverdue(string typeOfCall, DateTime? created, DateTime? now)
                {
                    if (!created.HasValue || !now.HasValue)
                        return false;

                    return typeOfCall switch
                    {
                        "Complaints" => created.Value.AddMinutes(2) <= now.Value,
                        "Request" => created.Value.AddHours(2) <= now.Value,
                        "Enquiries" => created.Value.AddHours(1) <= now.Value,
                        _ => false
                    };
                }
                model.SLAStatus = IsOverdue(model.TypeOfCall, model.CreatedDateTime, DateTime.Now) ? "Exceeded SLA Time" : "Within SLA Time";

                model.Attachments ??= new List<Attachment>();

                if (Attachments != null && Attachments.Any())
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ticket-attachments");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);
                    long maxFileSize = 10 * 1024 * 1024; // 10 MB in bytes
                    foreach (var file in Attachments)
                    {

                        if (file.Length <= maxFileSize)
                        {
                            var extension = Path.GetExtension(file.FileName);
                            var safeFileName = Path.GetFileNameWithoutExtension(file.FileName); // strip path if included
                            var uniqueFileName = $"{Guid.NewGuid()}{extension}";

                            // Final stored file name = UserFileName-UniqueID.ext
                            var storedFileName = $"{safeFileName}-{uniqueFileName}";
                            var filePath = Path.Combine(uploadsFolder, storedFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            model.Attachments.Add(new Attachment
                            {
                                // What user sees in DB/UI
                                FileName = storedFileName,

                                // Where it’s actually stored
                                FilePath = $"/ticket-attachments/{storedFileName}",

                                FileType = file.ContentType
                            });
                        }
                        else
                        {
                            ModelState.AddModelError("Attachments", $"{file.FileName} exceeds 10 MB limit.");
                            return View("TicketManagement", model);
                        }
                    }
                }

                _context.TicketManagementRecords.Add(model);
                await _context.SaveChangesAsync();

                // Now that Id is generated, create TicketNumber
                model.TicketNumber = $"{model.Id:D6}";

                // Only update the TicketNumber property
                _context.Entry(model).Property(x => x.TicketNumber).IsModified = true;
                await _context.SaveChangesAsync();

                TempData["TicketCreated"] = "true";
                return RedirectToAction("ViewTicketRecords");
            }

            return View("TicketManagement", model);
        }


        // GET: Display the Ticket Management Records
        [HttpGet]
        public IActionResult ViewTicketRecords(TicketManagementViewModel filterRecords, string? actionType)
        {
            if ((HttpContext.Session.GetString("Role") == "Admin" || HttpContext.Session.GetString("Role") == "Supervisor" || HttpContext.Session.GetString("Role") == "User"))
            {
                var model = new TicketManagementViewModel();

                var records = _context.TicketManagementRecords.AsQueryable();

                try
                {
                    // Filter by Ticket Status
                    if (!string.IsNullOrEmpty(filterRecords.TicketStatus))
                    {
                        records = records.Where(r => r.TicketStatus.Contains(filterRecords.TicketStatus));
                    }

                    // Filter by Channel
                    if (!string.IsNullOrEmpty(filterRecords.Channel))
                    {
                        records = records.Where(r => r.Channel.Contains(filterRecords.Channel));
                    }

                    // Filter by Ticket or SR(Service Request)
                    if (!string.IsNullOrEmpty(filterRecords.SrOrTicket))
                    {
                        records = records.Where(r => r.SrOrTicket == filterRecords.SrOrTicket);
                    }

                    // Filter by Start Date (ignore time)
                    if (filterRecords.StartDate.HasValue)
                    {
                        records = records.Where(r => r.DateTimeSubmitted >= filterRecords.StartDate.Value.Date);
                    }

                    // Filter by End Date (ignore time)
                    if (filterRecords.EndDate.HasValue)
                    {
                        DateTime endOfDay = filterRecords.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                        records = records.Where(r => r.DateTimeSubmitted <= endOfDay);
                    }

                    // Filter by Created Date (ignore time)
                    if (filterRecords.CreatedDate.HasValue)
                    {
                        DateTime start = filterRecords.CreatedDate.Value.Date;
                        DateTime end = start.AddDays(1).AddTicks(-1);
                        records = records.Where(r => r.CreatedDateTime >= start && r.CreatedDateTime <= end);
                    }

                    // Filter by Resolved Date (ignore time)
                    if (filterRecords.ResolvedDate.HasValue)
                    {
                        DateTime start = filterRecords.ResolvedDate.Value.Date;
                        DateTime end = start.AddDays(1).AddTicks(-1);
                        records = records.Where(r => r.ResolvedDateTime >= start && r.ResolvedDateTime <= end);
                    }

                    // Filter by Closed Date (ignore time)
                    if (filterRecords.ClosedDate.HasValue)
                    {
                        DateTime start = filterRecords.ClosedDate.Value.Date;
                        DateTime end = start.AddDays(1).AddTicks(-1);
                        records = records.Where(r => r.ClosedDateTime >= start && r.ClosedDateTime <= end);
                    }

                    // Filter by Calling Number
                    if (!string.IsNullOrEmpty(filterRecords.CallingNumber))
                    {
                        records = records.Where(r => r.CallingNumber.Contains(filterRecords.CallingNumber));
                    }

                    // Materialize to list
                    model.Records = records.ToList();

                    // Preserve filter values
                    model.TicketStatus = filterRecords.TicketStatus;
                    model.Channel = filterRecords.Channel;
                    model.StartDate = filterRecords.StartDate;
                    model.EndDate = filterRecords.EndDate;
                    model.CreatedDate = filterRecords.CreatedDate;
                    model.ResolvedDate = filterRecords.ResolvedDate;
                    model.ClosedDate = filterRecords.ClosedDate;
                    model.CallingNumber = filterRecords.CallingNumber;
                    model.SrOrTicket = filterRecords.SrOrTicket;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving records: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                }

                // Handle Excel download
                if (actionType == "download")
                {
                    return DownloadExcel(model);
                }
                var a = model;

                return View(model);
            }
            else
            {
                return RedirectToAction("UserLogin", "Login");
            }
        }

        // GET: View Ticket Record Details
        [HttpGet]
        public IActionResult ViewTicketRecord(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin" && HttpContext.Session.GetString("Role") != "Supervisor" && HttpContext.Session.GetString("Role") != "User")
            {
                return Unauthorized();
            }
            var record = _context.TicketManagementRecords
                .Include(t => t.Attachments)
                .FirstOrDefault(t => t.Id == id);

            ViewBag.AgentName = HttpContext.Session.GetString("Username");
            ViewBag.Resolver = new List<SelectListItem>
            {
                new SelectListItem { Text = record.Resolver, Value = record.Resolver }
            };
            ViewBag.ModifiedBy = record.ModifiedBy;
            ViewBag.ModifiedDate = record.DateTimeModified;

            if (record == null)
            {
                return NotFound();
            }

            return View(record);
        }


        // GET: Edit Ticket Record (Only Admins can access this)
        [HttpGet]
        public IActionResult EditTicketRecord(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin" && HttpContext.Session.GetString("Role") != "Supervisor" && HttpContext.Session.GetString("Role") != "User")
            {
                return Unauthorized();
            }

            var record = _context.TicketManagementRecords
                      .Include(t => t.Attachments)
                      .FirstOrDefault(t => t.Id == id);
            ViewBag.AgentName = HttpContext.Session.GetString("Username");
            // Fetch all usernames from Users table
            ViewBag.UserList = _context.Users
                .Select(u => new SelectListItem
                {
                    Value = u.Username,   // this will be stored in the DB / model
                    Text = u.Username     // this will be displayed in the dropdown
                })
                .ToList();

            if (record == null)
            {
                return NotFound();
            }

            return View(record);
        }

        // Post: Edit Tciket Record & handling the tciket attachments.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTicketRecord(int id, TicketManagement updatedRecord, List<IFormFile> Attachments)
        {
            if (HttpContext.Session.GetString("Role") != "Admin" && HttpContext.Session.GetString("Role") != "Supervisor" && HttpContext.Session.GetString("Role") != "User")
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var record = _context.TicketManagementRecords
                                         .Include(t => t.Attachments)
                                         .FirstOrDefault(t => t.Id == id);

                    ViewBag.Resolver = new List<SelectListItem>
                    {
                        new SelectListItem { Text = record.Resolver, Value = record.Resolver }
                    };

                    if (record == null)
                    {
                        return NotFound();
                    }

                    // =========================
                    //   Delete old attachments
                    // =========================
                    if (record.Attachments != null && record.Attachments.Any() && Attachments != null && Attachments.Any())
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ticket-attachments");

                        foreach (var oldFile in record.Attachments.ToList())
                        {
                            var oldFilePath = Path.Combine(uploadsFolder, Path.GetFileName(oldFile.FilePath));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }

                            _context.Attachments.Remove(oldFile); // remove from DB
                        }
                    }

                    // =========================
                    //   Upload new attachments
                    // =========================
                    if (Attachments != null && Attachments.Any())
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ticket-attachments");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        long maxFileSize = 10 * 1024 * 1024; // 10 MB
                        record.Attachments = new List<Attachment>(); // reset

                        foreach (var file in Attachments)
                        {
                            if (file.Length <= maxFileSize)
                            {
                                var extension = Path.GetExtension(file.FileName);
                                var safeFileName = Path.GetFileNameWithoutExtension(file.FileName);
                                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                                var storedFileName = $"{safeFileName}-{uniqueFileName}";
                                var filePath = Path.Combine(uploadsFolder, storedFileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }

                                record.Attachments.Add(new Attachment
                                {
                                    FileName = storedFileName,
                                    FilePath = $"/ticket-attachments/{storedFileName}",
                                    FileType = file.ContentType
                                });
                            }
                            else
                            {
                                ModelState.AddModelError("Attachments", $"{file.FileName} exceeds 10 MB limit.");
                                return View(updatedRecord);
                            }
                        }
                    }

                    // =========================
                    //   Updating other fields
                    // =========================
                    _context.Entry(record).CurrentValues.SetValues(updatedRecord);

                    // Protect DateTime from overwritten
                    _context.Entry(record).Property(r => r.CreatedDateTime).IsModified = false;
                    _context.Entry(record).Property(r => r.EscalatedDateTime).IsModified = false;
                    _context.Entry(record).Property(r => r.ResolvedDateTime).IsModified = false;
                    _context.Entry(record).Property(r => r.DateTimeSubmitted).IsModified = false;

                    if (updatedRecord.TicketStatus == "Escalate")
                    {
                        record.EscalatedDateTime = DateTime.Now;
                    }
                    else if (record.TicketStatus == "Resolve")
                    {
                        record.ResolvedDateTime = DateTime.Now;
                        //record.EscalatedDateTime = DateTime.Now;
                    }
                    else if (record.TicketStatus == "Close")
                    {
                        //record.ResolvedDateTime = DateTime.Now;
                        record.ClosedDateTime = DateTime.Now;
                    }
                    // Helper Method
                    bool IsOverdue(string typeOfCall, DateTime? created, DateTime? now)
                    {
                        if (!created.HasValue || !now.HasValue)
                            return false;

                        return typeOfCall switch
                        {
                            "Complaints" => created.Value.AddHours(4) <= now.Value,
                            "Request" => created.Value.AddHours(2) <= now.Value,
                            "Enquiries" => created.Value.AddHours(1) <= now.Value,
                            _ => false
                        };
                    }

                    record.SLAStatus = IsOverdue(record.TypeOfCall, record.CreatedDateTime, DateTime.Now) ? "Exceeded SLA Time!" : "Within SLA Time.";
                    // update last modified date 
                    var modifiedBy = HttpContext.Session.GetString("Username");
                    record.DateTimeModified = DateTime.Now;
                    record.ModifiedBy = modifiedBy;
                    await _context.SaveChangesAsync();
                    return RedirectToAction("ViewTicketRecords");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return View(updatedRecord);
        }


        // GET: Download filtered records as Excel
        [HttpGet]
        public IActionResult DownloadExcel(TicketManagementViewModel filterRecords)
        {
            if (HttpContext.Session.GetString("Role") == "Admin" || HttpContext.Session.GetString("Role") == "Supervisor")
            {
                var model = new TicketManagementViewModel();
                var records = _context.TicketManagementRecords.AsQueryable();

                if (filterRecords != null)
                {
                    try
                    {
                        // Appled same filters as in ViewTicketRecords
                        if (!string.IsNullOrEmpty(filterRecords.TicketStatus))
                            records = records.Where(r => r.TicketStatus.Contains(filterRecords.TicketStatus));

                        if (!string.IsNullOrEmpty(filterRecords.Channel))
                            records = records.Where(r => r.Channel.Contains(filterRecords.Channel));

                        if (filterRecords.StartDate.HasValue)
                            records = records.Where(r => r.DateTimeSubmitted >= filterRecords.StartDate.Value.Date);

                        if (filterRecords.EndDate.HasValue)
                        {
                            DateTime endOfDay = filterRecords.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                            records = records.Where(r => r.DateTimeSubmitted <= endOfDay);
                        }

                        if (filterRecords.CreatedDate.HasValue)
                        {
                            DateTime createdStart = filterRecords.CreatedDate.Value.Date;
                            DateTime createdEnd = createdStart.AddDays(1).AddTicks(-1);
                            records = records.Where(r => r.CreatedDateTime >= createdStart && r.CreatedDateTime <= createdEnd);
                        }

                        if (filterRecords.ResolvedDate.HasValue)
                        {
                            DateTime resolvedStart = filterRecords.ResolvedDate.Value.Date;
                            DateTime resolvedEnd = resolvedStart.AddDays(1).AddTicks(-1);
                            records = records.Where(r => r.ResolvedDateTime >= resolvedStart && r.ResolvedDateTime <= resolvedEnd);
                        }

                        if (filterRecords.ClosedDate.HasValue)
                        {
                            DateTime closedStart = filterRecords.ClosedDate.Value.Date;
                            DateTime closedEnd = closedStart.AddDays(1).AddTicks(-1);
                            records = records.Where(r => r.ClosedDateTime >= closedStart && r.ClosedDateTime <= closedEnd);
                        }

                        if (!string.IsNullOrEmpty(filterRecords.CallingNumber))
                        {
                            records = records.Where(r => r.CallingNumber.Contains(filterRecords.CallingNumber));
                        }


                        // Filter by Ticket or SR(Service Request)
                        if (!string.IsNullOrEmpty(filterRecords.SrOrTicket))
                        {
                            records = records.Where(r => r.SrOrTicket == filterRecords.SrOrTicket);
                        }

                        var filteredRecords = records.ToList();

                        using (var workbook = new ClosedXML.Excel.XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Ticket Records");
                            var currentRow = 1;
                            var serialNumber = 0;

                            // Headers
                            worksheet.Cell(currentRow, 1).Value = "SrNo";
                            worksheet.Cell(currentRow, 2).Value = "Name";
                            worksheet.Cell(currentRow, 3).Value = "Ticket Number";
                            worksheet.Cell(currentRow, 4).Value = "Ticket Status";
                            worksheet.Cell(currentRow, 5).Value = "SLA Status";
                            worksheet.Cell(currentRow, 6).Value = "Calling Number";
                            worksheet.Cell(currentRow, 7).Value = "Type Of Caller";
                            worksheet.Cell(currentRow, 8).Value = "Customer Segment";
                            worksheet.Cell(currentRow, 9).Value = "Type Of Call";
                            worksheet.Cell(currentRow, 10).Value = "Category";
                            worksheet.Cell(currentRow, 11).Value = "Sub Category";
                            worksheet.Cell(currentRow, 12).Value = "Sub Sub Category";

                            worksheet.Cell(currentRow, 13).Value = "Customer Name";
                            worksheet.Cell(currentRow, 14).Value = "Phone No";
                            worksheet.Cell(currentRow, 15).Value = "Email";
                            worksheet.Cell(currentRow, 16).Value = "Agent Name";
                            worksheet.Cell(currentRow, 17).Value = "Request Details";
                            worksheet.Cell(currentRow, 18).Value = "Remarks";

                            worksheet.Cell(currentRow, 19).Value = "Level Of Conflict";
                            worksheet.Cell(currentRow, 20).Value = "Service Level";
                            worksheet.Cell(currentRow, 21).Value = "Assigne";
                            worksheet.Cell(currentRow, 22).Value = "Re-Assigne";
                            worksheet.Cell(currentRow, 23).Value = "Resolver";
                            worksheet.Cell(currentRow, 24).Value = "Channel";
                            worksheet.Cell(currentRow, 25).Value = "Comment";

                            worksheet.Cell(currentRow, 26).Value = "Date Created";
                            worksheet.Cell(currentRow, 27).Value = "Escalated Date";
                            worksheet.Cell(currentRow, 28).Value = "Resolved Date";
                            worksheet.Cell(currentRow, 29).Value = "Close Date";
                            worksheet.Cell(currentRow, 30).Value = "Date Submitted";


                            // Rows
                            foreach (var record in filteredRecords)
                            {
                                currentRow++; serialNumber++;
                                worksheet.Cell(currentRow, 1).Value = serialNumber;
                                worksheet.Cell(currentRow, 2).Value = record.Name;
                                worksheet.Cell(currentRow, 3).Value = record.TicketNumber;
                                worksheet.Cell(currentRow, 4).Value = record.TicketStatus;
                                worksheet.Cell(currentRow, 5).Value = record.SLAStatus;
                                worksheet.Cell(currentRow, 6).Value = record.CallingNumber;
                                worksheet.Cell(currentRow, 7).Value = record.TypeOfCaller;
                                worksheet.Cell(currentRow, 8).Value = record.CustomerSegment;
                                worksheet.Cell(currentRow, 9).Value = record.TypeOfCall;
                                worksheet.Cell(currentRow, 10).Value = record.Category;
                                worksheet.Cell(currentRow, 11).Value = record.SubCategory;
                                worksheet.Cell(currentRow, 12).Value = record.SubSubCategory;

                                worksheet.Cell(currentRow, 13).Value = record.CustomerName;
                                worksheet.Cell(currentRow, 14).Value = record.PhoneNo;
                                worksheet.Cell(currentRow, 15).Value = record.Email;
                                worksheet.Cell(currentRow, 16).Value = record.AgentName;
                                worksheet.Cell(currentRow, 17).Value = record.RequestDetails;
                                worksheet.Cell(currentRow, 18).Value = record.Remarks;

                                worksheet.Cell(currentRow, 19).Value = record.LevelOfConflict;
                                worksheet.Cell(currentRow, 20).Value = record.ServiceLevel;
                                worksheet.Cell(currentRow, 21).Value = record.Assignee;
                                worksheet.Cell(currentRow, 22).Value = record.ReAssignee;
                                worksheet.Cell(currentRow, 23).Value = record.Resolver;
                                worksheet.Cell(currentRow, 24).Value = record.Channel;
                                worksheet.Cell(currentRow, 25).Value = record.Comment;

                                worksheet.Cell(currentRow, 26).Value = record.CreatedDateTime?.ToString("yyyy-MM-dd HH:mm:ss");
                                worksheet.Cell(currentRow, 27).Value = record.EscalatedDateTime?.ToString("yyyy-MM-dd HH:mm:ss");
                                worksheet.Cell(currentRow, 28).Value = record.ResolvedDateTime?.ToString("yyyy-MM-dd HH:mm:ss");
                                worksheet.Cell(currentRow, 29).Value = record.ClosedDateTime?.ToString("yyyy-MM-dd HH:mm:ss");
                                worksheet.Cell(currentRow, 30).Value = record.DateTimeSubmitted.ToString("yyyy-MM-dd HH:mm:ss");
                            }

                            using (var stream = new MemoryStream())
                            {
                                workbook.SaveAs(stream);
                                var content = stream.ToArray();
                                return File(content,
                                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                    "TicketRecordsFiltered.xlsx");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                return RedirectToAction("ViewTicketRecords");
            }
            else if (HttpContext.Session.GetString("Role") == "User")
            {
                return RedirectToAction("ViewTicketRecords");
            }
            else
            {
                return RedirectToAction("UserLogin", "Login");
            }
        }

        public IActionResult ViewOpenTickets()
        {
            var openTicketsList = _context.TicketManagementRecords.Where(x => x.TicketStatus != null && x.TicketStatus == "Open").ToList();
            ViewBag.OpenTickets = openTicketsList;
            return View();
        }

        [HttpGet]
        public IActionResult ViewSolvedTickets()
        {
            var solvedTicketsList = _context.TicketManagementRecords.Where(x => x.TicketStatus != null && x.TicketStatus == "Close").ToList();
            ViewBag.SolvedTickets = solvedTicketsList;
            return View();
        }

        [HttpGet]
        public IActionResult ViewPendingTickets()
        {
            var pendingTicketsList = _context.TicketManagementRecords.Where(x => x.TicketStatus != null && x.TicketStatus == "Pending").ToList();
            ViewBag.PendingTickets = pendingTicketsList;
            return View();
        }

        [HttpGet]
        public IActionResult ViewOverDueTickets()
        {
            var now = DateTime.Now;

            var overDueTicketsList = _context.TicketManagementRecords
                .Where(x => x.TicketStatus != null && (x.TicketStatus == "Open" || x.TicketStatus == "Reopen" || x.TicketStatus == "Pending" || x.TicketStatus == "Escalate") &&
                    x.CreatedDateTime.HasValue && (
                        (x.TypeOfCall == "Complaints" && x.CreatedDateTime.Value.AddMinutes(2) <= now) ||
                        (x.TypeOfCall == "Request" && x.CreatedDateTime.Value.AddHours(2) <= now) ||
                        (x.TypeOfCall == "Enquiries" && x.CreatedDateTime.Value.AddHours(1) <= now)
                    // Adjust SLA logic for Waiting Customers if needed: 
                    // || (x.TypeOfCall == "Waiting For Customer" && x.CreatedDateTime.Value <= now)
                    )
                )
                .ToList();
            ViewBag.OverDueTickets = overDueTicketsList;
            return View();
        }

    }
}
