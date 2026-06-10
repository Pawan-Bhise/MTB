using PMEHCRM.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using PMEHCRM.Models;
using PMEHCRM.Data;

namespace PMEHCRM.Controllers
{
    public class CRMFormatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CRMFormatController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Display the CRM Form
        [HttpGet]
        public IActionResult CRMFORMAT()
        {
            if (HttpContext.Session.GetString("Role") != "Admin" && HttpContext.Session.GetString("Role") != "Supervisor" && HttpContext.Session.GetString("Role") != "User")
            {
                return RedirectToAction("UserLogin", "Login"); // Redirect to the Login action in the Account controller
            }
            // Pass the logged-in agent's name from session
            ViewBag.AgentName = HttpContext.Session.GetString("Username");

            return View();
        }

        // POST: Submit the CRM Form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CRMFormatPost(CRMFormat model)
        {
            if (ModelState.IsValid) // GK
            {
                // Set additional metadata
                model.AgentName = HttpContext.Session.GetString("Username"); // Agent name from session
                model.CreatedDateTime = DateTime.Now;
                model.DateTimeSubmitted = DateTime.Now;

                _context.CRMFormatRecords.Add(model);
                _context.SaveChanges();
                TempData["SRCreated"] = "true";
                return RedirectToAction("ViewCRMRecords");
            }

            return View("CRMFORMAT", model);
        }

        // GET: Display the CRM Records
        [HttpGet]
        public IActionResult ViewCRMRecords(CRMFormatViewModel filterRecords, string? actionType)
        {
            if ((HttpContext.Session.GetString("Role") == "Admin" || HttpContext.Session.GetString("Role") == "Supervisor" || HttpContext.Session.GetString("Role") == "User"))
            {
                var model = new CRMFormatViewModel();

                // Always retrieve the records from the database
                var records = _context.CRMFormatRecords.AsQueryable();

                try
                {
                    // Filter by calling number
                    if (!string.IsNullOrEmpty(filterRecords.CallingNumber))
                    {
                        records = records.Where(r => r.CallingNumber.Contains(filterRecords.CallingNumber));
                    }

                    // Filter by Type of call
                    if (!string.IsNullOrEmpty(filterRecords.TypeOfCall))
                    {
                        records = records.Where(r => r.TypeOfCall.Contains(filterRecords.TypeOfCall));
                    }

                    // Filter by Date Submmited (ignore time)
                    if (filterRecords.DateSubmitted.HasValue)
                    {
                        var start = filterRecords.DateSubmitted.Value.Date;
                        var end = start.AddDays(1);
                        records = records.Where(r => r.DateTimeSubmitted >= start && r.DateTimeSubmitted < end);
                    }

                    // Materialize the query to a list (fetch data from the database)
                    model.Records = records.ToList();

                    // Preserve filter values
                    model.CallingNumber = filterRecords.CallingNumber;
                    model.TypeOfCall = filterRecords.TypeOfCall;
                    model.DateSubmitted = filterRecords.DateSubmitted;
                }
                catch (Exception ex)
                {
                    // Log the exception for troubleshooting
                    Console.WriteLine($"Error retrieving records: {ex.Message}");
                    // Optionally log the stack trace for more details
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                }

                // Handle Excel download
                if (actionType == "download")
                {
                    return DownloadExcel(model);
                }
                return View(model);
            }
            else
            {
                return RedirectToAction("UserLogin", "Login");
            }
        }

        // GET: View CRM Record Details
        [HttpGet]
        public IActionResult ViewCRMRecord(int id)
        {
            var record = _context.CRMFormatRecords.Find(id);
            ViewBag.AgentName = HttpContext.Session.GetString("Username");
            return View(record);
        }

        // GET: Edit CRM Record (Only Admins can access this)
        [HttpGet]
        public IActionResult EditCRMRecord(int id)
        {
            var record = _context.CRMFormatRecords.Find(id);
            return View(record);
        }

        // POST: Edit CRM Record (Only Admins can access this)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCRMRecord(int id, CRMFormat updatedRecord)
        {
            if (HttpContext.Session.GetString("Role") != "Admin" && HttpContext.Session.GetString("Role") != "Supervisor")
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                var record = _context.CRMFormatRecords.Find(id);
                if (record == null)
                {
                    return NotFound();
                }
                _context.Entry(record).CurrentValues.SetValues(updatedRecord);

                // Protect field from overwritten
                //_context.Entry(record).Property(r => r.DateTimeSubmitted).IsModified = false;

                // update last modified Date 
                record.DateTimeSubmitted = DateTime.Now;

                _context.SaveChanges();

                return RedirectToAction("ViewCRMRecords");
            }
            return View(updatedRecord);
        }

        // GET: Download filtered records as Excel
        [HttpGet]
        public IActionResult DownloadExcel(CRMFormatViewModel filterRecords)
        {
            if (HttpContext.Session.GetString("Role") == "Admin" || HttpContext.Session.GetString("Role") == "Supervisor")
            {
                // Start by getting all records as an IQueryable
                var records = _context.CRMFormatRecords.AsQueryable();
                if (filterRecords != null)
                {
                    try
                    {
                        // Filter by calling number
                        if (!string.IsNullOrEmpty(filterRecords.CallingNumber))
                        {
                            records = records.Where(r => r.CallingNumber.Contains(filterRecords.CallingNumber));
                        }

                        // Filter by Type of call
                        if (!string.IsNullOrEmpty(filterRecords.TypeOfCall))
                        {
                            records = records.Where(r => r.TypeOfCall.Contains(filterRecords.TypeOfCall));
                        }

                        // Filter by Date Submmited (ignore time)
                        if (filterRecords.DateSubmitted.HasValue)
                        {
                            var start = filterRecords.DateSubmitted.Value.Date;
                            var end = start.AddDays(1);
                            records = records.Where(r => r.DateTimeSubmitted >= start && r.DateTimeSubmitted < end);
                        }

                        // Materialize the query to a list (fetch data from the database)
                        var filteredRecords = records.ToList();

                        // Now create the Excel workbook using the filtered data
                        using (var workbook = new ClosedXML.Excel.XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("CRM Records");
                            var currentRow = 1;
                            var serialNumber = 0;

                            // Headers
                            worksheet.Cell(currentRow, 1).Value = "SrNo";
                            worksheet.Cell(currentRow, 2).Value = "Name";
                            worksheet.Cell(currentRow, 3).Value = "Calling Number";
                            worksheet.Cell(currentRow, 4).Value = "Type Of Caller";
                            worksheet.Cell(currentRow, 5).Value = "Customer Segment";
                            worksheet.Cell(currentRow, 6).Value = "Type Of Call";
                            worksheet.Cell(currentRow, 7).Value = "Category";
                            worksheet.Cell(currentRow, 8).Value = "Sub Category";
                            worksheet.Cell(currentRow, 9).Value = "Sub Sub Category";
                            worksheet.Cell(currentRow, 10).Value = "Customer Name";
                            worksheet.Cell(currentRow, 11).Value = "Phone No";
                            worksheet.Cell(currentRow, 12).Value = "Email";
                            worksheet.Cell(currentRow, 13).Value = "Agent Name";
                            worksheet.Cell(currentRow, 14).Value = "Request Details";
                            worksheet.Cell(currentRow, 15).Value = "Remarks";
                            worksheet.Cell(currentRow, 16).Value = "Date Submitted";

                            // Records
                            foreach (var record in filteredRecords)
                            {
                                currentRow++; serialNumber++;
                                worksheet.Cell(currentRow, 1).Value = serialNumber;
                                worksheet.Cell(currentRow, 2).Value = record.Name;
                                worksheet.Cell(currentRow, 3).Value = record.CallingNumber;
                                worksheet.Cell(currentRow, 4).Value = record.TypeOfCaller;
                                worksheet.Cell(currentRow, 5).Value = record.CustomerSegment;
                                worksheet.Cell(currentRow, 6).Value = record.TypeOfCall;
                                worksheet.Cell(currentRow, 7).Value = record.Category;
                                worksheet.Cell(currentRow, 8).Value = record.SubCategory;
                                worksheet.Cell(currentRow, 9).Value = record.SubSubCategory;
                                worksheet.Cell(currentRow, 10).Value = record.CustomerName;
                                worksheet.Cell(currentRow, 11).Value = record.PhoneNo;
                                worksheet.Cell(currentRow, 12).Value = record.Email;
                                worksheet.Cell(currentRow, 13).Value = record.AgentName;
                                worksheet.Cell(currentRow, 14).Value = record.RequestDetails;
                                worksheet.Cell(currentRow, 15).Value = record.Remarks;
                                worksheet.Cell(currentRow, 16).Value = record.DateTimeSubmitted.ToString("yyyy-MM-dd HH:mm:ss");
                            }

                            using (var stream = new MemoryStream())
                            {
                                workbook.SaveAs(stream);
                                var content = stream.ToArray();
                                return File(content,
                                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                    "CRMRecordsFiltered.xlsx");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                return RedirectToAction("ViewCRMRecords");
            }
            else if (HttpContext.Session.GetString("Role") == "User")
            {
                return RedirectToAction("ViewCRMRecords");
            }
            else
            {
                return RedirectToAction("UserLogin", "Login");
            }
        }
    }
}
