// ============ Controllers/PayrollController.cs ============
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Newtonsoft.Json;
using MuhleOP.Helpers;
using MuhleOP.Models;
using MuhleOP.ViewModels;

namespace MuhleOP.Controllers
{
    [Authorize]
    public class PayrollController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        // ------------------------------------------------------------------
        // Index (read-only for Admin; full for Finance Officer)
        // ------------------------------------------------------------------

        [Authorize(Roles = "Finance Officer, Admin")]
        public ActionResult Index(string status = null, int? year = null, int? month = null)
        {
            var query = db.Payrolls
                .Include(p => p.Staff)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(p => p.Status == status);

            if (year.HasValue)
                query = query.Where(p => p.PayPeriodStart.Year == year.Value);

            if (month.HasValue)
                query = query.Where(p => p.PayPeriodStart.Month == month.Value);

            var list = query
                .OrderByDescending(p => p.PayPeriodStart)
                .ThenBy(p => p.Staff.LastName)
                .ToList()
                .Select(p => MapToListItem(p))
                .ToList();

            var all = db.Payrolls.ToList();

            bool isLocked = false;
            PayrollPeriodLock periodLock = null;
            if (year.HasValue && month.HasValue)
            {
                var pStart = new DateTime(year.Value, month.Value, 1);
                periodLock = db.PayrollPeriodLocks.FirstOrDefault(l => l.PeriodStart == pStart);
                isLocked = periodLock != null && periodLock.IsLocked;
            }

            foreach (var item in list)
            {
                var pStart = new DateTime(item.PayPeriodStart.Year, item.PayPeriodStart.Month, 1);
                var lk = db.PayrollPeriodLocks.FirstOrDefault(l => l.PeriodStart == pStart);
                item.IsPeriodLocked = lk != null && lk.IsLocked;
                item.HasPayslip = !string.IsNullOrEmpty(item.PayslipPdfPath);
                item.HasWarning = !string.IsNullOrEmpty(item.ValidationWarningsJson);
            }

            var viewModel = new PayrollIndexViewModel
            {
                Items = list,
                TotalCount = all.Count,
                DraftCount = all.Count(p => p.Status == "Draft"),
                CalculatedCount = all.Count(p => p.Status == "Calculated"),
                ApprovedCount = all.Count(p => p.Status == "Approved"),
                ProcessedCount = all.Count(p => p.Status == "Processed"),
                PaidCount = all.Count(p => p.Status == "Paid"),
                ActiveFilter = status ?? "all",
                FilterYear = year,
                FilterMonth = month,
                Years = GetYearSelectList(year),
                Months = GetMonthSelectList(month),
                Statuses = GetStatusSelectList(status),
                TotalGross = list.Sum(i => i.GrossPay),
                TotalNet = list.Sum(i => i.NetPay),
                IsPeriodLocked = isLocked,
                PeriodLockReason = periodLock?.LockReason,
                PeriodLockedBy = periodLock?.LockedBy,
                PeriodLockedDate = periodLock?.LockedDate
            };

            return View(viewModel);
        }

        // ------------------------------------------------------------------
        // ExportCsv (CSV Export for Payroll Index)
        // ------------------------------------------------------------------

        [Authorize(Roles = "Finance Officer, Admin")]
        public ActionResult ExportCsv(string status = null, int? year = null, int? month = null)
        {
            var query = db.Payrolls
                .Include(p => p.Staff)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(p => p.Status == status);

            if (year.HasValue)
                query = query.Where(p => p.PayPeriodStart.Year == year.Value);

            if (month.HasValue)
                query = query.Where(p => p.PayPeriodStart.Month == month.Value);

            // Fetch list into memory first so computed properties like FullName can be evaluated in LINQ to Objects
            var records = query
                .OrderByDescending(p => p.PayPeriodStart)
                .ThenBy(p => p.Staff.LastName)
                .ToList()
                .Select(p => new
                {
                    EmployeeId = p.Staff != null ? p.Staff.EmployeeId : "",
                    StaffName = p.Staff != null ? p.Staff.FullName : "",
                    Role = p.Staff != null ? p.Staff.Role : "",
                    PayPeriodStart = p.PayPeriodStart,
                    PayPeriodEnd = p.PayPeriodEnd,
                    RegularHours = p.RegularHours,
                    OvertimeHours = p.OvertimeHours,
                    GrossPay = p.GrossPay,
                    NetPay = p.NetPay,
                    TotalAmount = p.TotalAmount,
                    Status = p.Status,
                    PaymentDate = p.PaymentDate,
                    PaymentMethod = p.PaymentMethod
                })
                .ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Employee ID,Staff Name,Role,Period Start,Period End,Regular Hours,Overtime Hours,Gross Pay,Net Pay,Total Amount,Status,Payment Date,Payment Method");

            foreach (var r in records)
            {
                var line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}",
                    EscapeCsv(r.EmployeeId),
                    EscapeCsv(r.StaffName),
                    EscapeCsv(r.Role),
                    r.PayPeriodStart.ToString("yyyy-MM-dd"),
                    r.PayPeriodEnd.ToString("yyyy-MM-dd"),
                    r.RegularHours.ToString("0.00", CultureInfo.InvariantCulture),
                    r.OvertimeHours.ToString("0.00", CultureInfo.InvariantCulture),
                    r.GrossPay.ToString("0.00", CultureInfo.InvariantCulture),
                    r.NetPay.ToString("0.00", CultureInfo.InvariantCulture),
                    r.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture),
                    EscapeCsv(r.Status),
                    r.PaymentDate.HasValue ? r.PaymentDate.Value.ToString("yyyy-MM-dd") : "",
                    EscapeCsv(r.PaymentMethod)
                );
                sb.AppendLine(line);
            }

            string filename;
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            if (year.HasValue && month.HasValue)
            {
                filename = $"PayrollReport_{year.Value}_{month.Value}_{timestamp}.csv";
            }
            else if (year.HasValue)
            {
                filename = $"PayrollReport_{year.Value}_{timestamp}.csv";
            }
            else
            {
                filename = $"PayrollReport_All_{timestamp}.csv";
            }

            var preamble = System.Text.Encoding.UTF8.GetPreamble();
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var fileBytes = new byte[preamble.Length + contentBytes.Length];
            Buffer.BlockCopy(preamble, 0, fileBytes, 0, preamble.Length);
            Buffer.BlockCopy(contentBytes, 0, fileBytes, preamble.Length, contentBytes.Length);

            return File(fileBytes, "text/csv", filename);
        }

        // ------------------------------------------------------------------
        // Generate (select period → create Draft records) – Finance Officer only
        // ------------------------------------------------------------------

        [Authorize(Roles = "Finance Officer")]
        public ActionResult Generate()
        {
            var now = DateTime.Today;
            int defaultYear = now.Year;
            int defaultMonth = now.Month == 1 ? 12 : now.Month - 1;
            if (now.Month == 1) defaultYear--;

            var periodStart = new DateTime(defaultYear, defaultMonth, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            if (IsPeriodLocked(defaultYear, defaultMonth))
            {
                TempData["ErrorMessage"] = $"Payroll period {periodStart:MMMM yyyy} is locked.";
            }

            var activeStaff = db.Staff.Where(s => s.IsActive).ToList();
            int activeCount = activeStaff.Count(s => s.PayRatePerHour > 0);
            int zeroRateCount = activeStaff.Count(s => s.PayRatePerHour <= 0);

            var attRecords = db.AttendanceRecords
                .Where(r => r.CheckInTime >= periodStart && r.CheckInTime <= periodEnd && r.CheckOutTime == null)
                .ToList();

            var model = new GeneratePayrollViewModel
            {
                Year = defaultYear,
                Month = defaultMonth,
                Years = GetYearSelectList(defaultYear),
                Months = GetMonthSelectList(defaultMonth),
                PreviewActiveStaffCount = activeCount,
                PreviewZeroRateStaffCount = zeroRateCount,
                PreviewMissingClockOutCount = attRecords.Count,
                IsPeriodLocked = IsPeriodLocked(defaultYear, defaultMonth)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Finance Officer")]
        public async Task<ActionResult> Generate(GeneratePayrollViewModel model)
        {
            model.Years = GetYearSelectList(model.Year);
            model.Months = GetMonthSelectList(model.Month);

            if (!ModelState.IsValid)
                return View(model);

            if (IsPeriodLocked(model.Year, model.Month))
            {
                TempData["ErrorMessage"] = "Cannot generate payroll: this period is locked.";
                return View(model);
            }

            try
            {
                var periodStart = new DateTime(model.Year, model.Month, 1);
                var periodEnd = periodStart.AddMonths(1).AddDays(-1);

                var existingCount = db.Payrolls.Count(p => p.PayPeriodStart == periodStart && p.PayPeriodEnd == periodEnd);
                if (existingCount > 0)
                {
                    TempData["ErrorMessage"] = $"Payroll records already exist for {periodStart:MMMM yyyy}. Please delete existing draft records first.";
                    return View(model);
                }

                var activeStaff = db.Staff
                    .Where(s => s.IsActive)
                    .ToList();

                int generated = 0;
                int skipped = 0;
                var messages = new List<string>();

                foreach (var staff in activeStaff)
                {
                    if (staff.PayRatePerHour <= 0)
                    {
                        skipped++;
                        messages.Add($"Skipped {staff.FullName} — PayRatePerHour is zero or not set.");
                        continue;
                    }

                    var payroll = new Payroll
                    {
                        StaffId = staff.Id,
                        PayPeriodStart = periodStart,
                        PayPeriodEnd = periodEnd,
                        HourlyRate = staff.PayRatePerHour,
                        OvertimeRate = staff.PayRatePerHour * 1.5m,
                        Status = "Draft",
                        BankAccount = staff.BankAccount,
                        Bonus = 0,
                        Deductions = 0,
                        Tax = 0,
                        RegularHours = 0,
                        OvertimeHours = 0
                    };

                    AggregateHours(payroll, staff.Id, periodStart, periodEnd);

                    db.Payrolls.Add(payroll);
                    generated++;
                }

                await db.SaveChangesAsync();

                model.GeneratedCount = generated;
                model.SkippedCount = skipped;
                model.Messages = messages;

                if (generated > 0)
                {
                    TempData["SuccessMessage"] = $"Generated {generated} draft payroll record(s) for {periodStart:MMMM yyyy}." +
                        (skipped > 0 ? $" {skipped} skipped." : "");
                    return RedirectToAction("Index", new { year = model.Year, month = model.Month });
                }

                TempData["ErrorMessage"] = "No new payroll records were created. " +
                    (skipped > 0 ? $"{skipped} staff skipped (missing pay rate)." : "No active staff found.");
                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating payroll: {ex.Message}");
                ModelState.AddModelError("", "Error generating payroll: " + ex.Message);
                return View(model);
            }
        }

        // ------------------------------------------------------------------
        // Details (read-only for Admin; full for Finance Officer)
        // ------------------------------------------------------------------

        [Authorize(Roles = "Finance Officer, Admin")]
        public ActionResult Details(int id)
        {
            var payroll = db.Payrolls
                .Include(p => p.Staff)
                .FirstOrDefault(p => p.Id == id);

            if (payroll == null)
            {
                TempData["ErrorMessage"] = "Payroll record not found.";
                return RedirectToAction("Index");
            }

            var vm = MapToDetails(payroll);
            return View(vm);
        }

        // ------------------------------------------------------------------
        // Calculate (single) – Finance Officer only
        // ------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Finance Officer")]
        public async Task<ActionResult> Calculate(int id)
        {
            try
            {
                var payroll = db.Payrolls
                    .Include(p => p.Staff)
                    .FirstOrDefault(p => p.Id == id);

                if (payroll == null)
                {
                    TempData["ErrorMessage"] = "Payroll record not found.";
                    return RedirectToAction("Index");
                }

                if (payroll.Status != "Draft" && payroll.Status != "Calculated")
                {
                    TempData["ErrorMessage"] = "Only Draft or Calculated payrolls can be (re)calculated.";
                    return RedirectToAction("Details", new { id });
                }

                var validation = ValidatePayroll(payroll);
                if (validation.HasBlocker)
                {
                    var blockerMsg = string.Join("; ", validation.Anomalies.Where(a => a.Severity == "Blocker").Select(a => a.Message));
                    System.Diagnostics.Debug.WriteLine($"Payroll calculation blocked for ID {id}: {blockerMsg}");
                    TempData["ErrorMessage"] = $"Calculation blocked due to blockers: {blockerMsg}";
                    return RedirectToAction("Details", new { id });
                }

                payroll.ValidationWarningsJson = validation.Anomalies.Any() ? JsonConvert.SerializeObject(validation.Anomalies) : null;

                if (payroll.Staff == null || payroll.Staff.PayRatePerHour <= 0)
                {
                    TempData["ErrorMessage"] = "Staff pay rate is missing or zero. Cannot calculate.";
                    return RedirectToAction("Details", new { id });
                }

                payroll.HourlyRate = payroll.Staff.PayRatePerHour;
                payroll.OvertimeRate = payroll.Staff.PayRatePerHour * 1.5m;

                AggregateHours(payroll, payroll.StaffId, payroll.PayPeriodStart, payroll.PayPeriodEnd);

                payroll.Deductions = 0;
                payroll.Tax = 0;
                payroll.CalculateTotals();

                // UIF = 1% of Gross, capped at R177.12
                var uif = Math.Min(payroll.GrossPay * 0.01m, 177.12m);
                payroll.Deductions = uif;

                // TODO: replace with SARS tax tables for production
                payroll.Tax = payroll.GrossPay * 0.18m;

                payroll.CalculateTotals();

                payroll.Status = "Calculated";
                payroll.CalculatedBy = User.Identity.Name;
                payroll.BankAccount = payroll.Staff.BankAccount ?? payroll.BankAccount;

                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Payroll calculated for {payroll.Staff.FullName}. Net: R {payroll.NetPay:N2}";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating payroll: {ex.Message}");
                TempData["ErrorMessage"] = "Error calculating payroll: " + ex.Message;
                return RedirectToAction("Details", new { id });
            }
        }

        // ------------------------------------------------------------------
        // Bulk Calculate – Finance Officer only
        // ------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Finance Officer")]
        public async Task<ActionResult> BulkCalculate(int year, int month)
        {
            if (IsPeriodLocked(year, month))
            {
                TempData["ErrorMessage"] = "Cannot bulk calculate: this period is locked.";
                return RedirectToAction("Index", new { year, month });
            }

            try
            {
                var periodStart = new DateTime(year, month, 1);
                var periodEnd = periodStart.AddMonths(1).AddDays(-1);

                var drafts = db.Payrolls
                    .Include(p => p.Staff)
                    .Where(p => p.PayPeriodStart == periodStart &&
                                p.PayPeriodEnd == periodEnd &&
                                (p.Status == "Draft" || p.Status == "Calculated"))
                    .ToList();

                int count = 0;
                int blockedCount = 0;
                foreach (var payroll in drafts)
                {
                    var validation = ValidatePayroll(payroll);
                    if (validation.HasBlocker)
                    {
                        blockedCount++;
                        System.Diagnostics.Debug.WriteLine($"BulkCalculate skipped payroll ID {payroll.Id} due to blockers.");
                        continue;
                    }

                    payroll.ValidationWarningsJson = validation.Anomalies.Any() ? JsonConvert.SerializeObject(validation.Anomalies) : null;

                    if (payroll.Staff == null || payroll.Staff.PayRatePerHour <= 0)
                        continue;

                    payroll.HourlyRate = payroll.Staff.PayRatePerHour;
                    payroll.OvertimeRate = payroll.Staff.PayRatePerHour * 1.5m;

                    AggregateHours(payroll, payroll.StaffId, periodStart, periodEnd);

                    payroll.Deductions = 0;
                    payroll.Tax = 0;
                    payroll.CalculateTotals();

                    var uif = Math.Min(payroll.GrossPay * 0.01m, 177.12m);
                    payroll.Deductions = uif;

                    // TODO: replace with SARS tax tables for production
                    payroll.Tax = payroll.GrossPay * 0.18m;

                    payroll.CalculateTotals();
                    payroll.Status = "Calculated";
                    payroll.CalculatedBy = User.Identity.Name;
                    payroll.BankAccount = payroll.Staff.BankAccount ?? payroll.BankAccount;
                    count++;
                }

                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Calculated {count} payroll record(s) for {periodStart:MMMM yyyy}." + (blockedCount > 0 ? $" {blockedCount} skipped due to validation blockers." : "");
                return RedirectToAction("Index", new { year, month });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error bulk calculating: {ex.Message}");
                TempData["ErrorMessage"] = "Error bulk calculating: " + ex.Message;
                return RedirectToAction("Index", new { year, month });
            }
        }

        // ------------------------------------------------------------------
        // Approve – Finance Officer only
        // ------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Finance Officer")]
        public async Task<ActionResult> Approve(int id)
        {
            try
            {
                var payroll = db.Payrolls
                    .Include(p => p.Staff)
                    .FirstOrDefault(p => p.Id == id);

                if (payroll == null)
                {
                    TempData["ErrorMessage"] = "Payroll record not found.";
                    return RedirectToAction("Index");
                }

                if (payroll.Status != "Calculated")
                {
                    TempData["ErrorMessage"] = "Only Calculated payrolls can be approved.";
                    return RedirectToAction("Details", new { id });
                }

                payroll.Status = "Approved";
                payroll.ApprovedBy = User.Identity.Name;
                payroll.ApprovalDate = DateTime.Now;

                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Payroll approved for {payroll.Staff?.FullName}.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error approving payroll: {ex.Message}");
                TempData["ErrorMessage"] = "Error approving payroll: " + ex.Message;
                return RedirectToAction("Details", new { id });
            }
        }

        // ------------------------------------------------------------------
        // Process (enter payment details → Processed) – Finance Officer only
        // ------------------------------------------------------------------

        [Authorize(Roles = "Finance Officer")]
        public ActionResult Process(int id)
        {
            var payroll = db.Payrolls
                .Include(p => p.Staff)
                .FirstOrDefault(p => p.Id == id);

            if (payroll == null)
            {
                TempData["ErrorMessage"] = "Payroll record not found.";
                return RedirectToAction("Index");
            }

            if (payroll.Status != "Approved" && payroll.Status != "Processed")
            {
                TempData["ErrorMessage"] = "Only Approved payrolls can be processed for payment.";
                return RedirectToAction("Details", new { id });
            }

            var model = new ProcessPayrollViewModel
            {
                Id = payroll.Id,
                StaffName = payroll.Staff?.FullName,
                EmployeeId = payroll.Staff?.EmployeeId,
                PeriodLabel = $"{payroll.PayPeriodStart:MMMM yyyy}",
                NetPay = payroll.NetPay,
                CurrentBankAccount = payroll.BankAccount ?? payroll.Staff?.BankAccount,
                BankAccount = payroll.BankAccount ?? payroll.Staff?.BankAccount,
                PaymentDate = DateTime.Today,
                PaymentMethod = payroll.PaymentMethod ?? "Bank Transfer",
                Notes = payroll.Notes,
                PaymentMethods = GetPaymentMethodSelectList(payroll.PaymentMethod ?? "Bank Transfer")
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Finance Officer")]
        public async Task<ActionResult> Process(ProcessPayrollViewModel model)
        {
            model.PaymentMethods = GetPaymentMethodSelectList(model.PaymentMethod);

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var payroll = db.Payrolls
                    .Include(p => p.Staff)
                    .FirstOrDefault(p => p.Id == model.Id);

                if (payroll == null || (payroll.Status != "Approved" && payroll.Status != "Processed"))
                {
                    TempData["ErrorMessage"] = "Payroll not found or not in a processable state.";
                    return RedirectToAction("Index");
                }

                payroll.PaymentMethod = model.PaymentMethod;
                payroll.BankAccount = model.BankAccount;
                payroll.PaymentDate = model.PaymentDate;
                payroll.Notes = model.Notes;
                payroll.Status = "Processed";
                payroll.ProcessedBy = User.Identity.Name;
                payroll.ProcessedDate = DateTime.Now;

                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Payment processed for {payroll.Staff?.FullName}. Amount: R {payroll.NetPay:N2}";
                return RedirectToAction("Details", new { id = payroll.Id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing payroll: {ex.Message}");
                ModelState.AddModelError("", "Error processing payroll: " + ex.Message);
                return View(model);
            }
        }

        // ------------------------------------------------------------------
        // Mark Paid – Finance Officer only
        // ------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Finance Officer")]
        public async Task<ActionResult> MarkPaid(int id)
        {
            try
            {
                var payroll = db.Payrolls
                    .Include(p => p.Staff)
                    .FirstOrDefault(p => p.Id == id);

                if (payroll == null)
                {
                    TempData["ErrorMessage"] = "Payroll record not found.";
                    return RedirectToAction("Index");
                }

                if (payroll.Status == "Paid")
                {
                    TempData["ErrorMessage"] = "This payroll record has already been marked as Paid.";
                    return RedirectToAction("Details", new { id });
                }

                if (payroll.Status != "Processed")
                {
                    TempData["ErrorMessage"] = "Only Processed payrolls can be marked as Paid.";
                    return RedirectToAction("Details", new { id });
                }

                if (IsPeriodLocked(payroll.PayPeriodStart.Year, payroll.PayPeriodStart.Month) && payroll.Status == "Paid")
                {
                    TempData["ErrorMessage"] = "This period is locked and the payroll is already paid.";
                    return RedirectToAction("Details", new { id });
                }

                payroll.Status = "Paid";
                if (!payroll.PaymentDate.HasValue)
                    payroll.PaymentDate = DateTime.Today;

                try
                {
                    if (payroll.Staff != null)
                    {
                        byte[] pdfBytes = PayslipPdfGenerator.GeneratePayslip(payroll, payroll.Staff);
                        string uploadsDir = Server.MapPath("~/Uploads/Payslips");
                        if (!Directory.Exists(uploadsDir))
                        {
                            Directory.CreateDirectory(uploadsDir);
                        }
                        string fileName = $"Payslip_{payroll.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                        string relativePath = $"/Uploads/Payslips/{fileName}";
                        string fullPath = Path.Combine(uploadsDir, fileName);
                        System.IO.File.WriteAllBytes(fullPath, pdfBytes);
                        payroll.PayslipPdfPath = relativePath;
                    }
                }
                catch (Exception pdfEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error generating payslip PDF: {pdfEx.Message}");
                }

                await db.SaveChangesAsync();

                if (payroll.Staff != null && !string.IsNullOrEmpty(payroll.Staff.UserId))
                {
                    var periodLabel = $"{payroll.PayPeriodStart:MMMM yyyy}";
                    await CreateNotification(
                        payroll.Staff.UserId,
                        "Payslip Available",
                        $"Your salary for {periodLabel} has been paid. Net amount: R {payroll.NetPay:N2}.",
                        "Finance",
                        $"/Payroll/Details/{payroll.Id}"
                    );
                }

                await NotifyFinanceTeam(
                    "Payroll Marked Paid",
                    $"Payroll for {payroll.Staff?.FullName} ({payroll.PayPeriodStart:MMM yyyy}) marked as Paid — R {payroll.NetPay:N2}.",
                    $"/Payroll/Details/{payroll.Id}",
                    skipUserId: User.Identity.GetUserId()
                );

                TempData["SuccessMessage"] = $"Payroll marked as Paid for {payroll.Staff?.FullName}.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking payroll paid: {ex.Message}");
                TempData["ErrorMessage"] = "Error marking payroll paid: " + ex.Message;
                return RedirectToAction("Details", new { id });
            }
        }

        // ------------------------------------------------------------------
        // UC22 New Actions: UpdateDeductions, DownloadPayslip, MyPayslips, LockPeriod, UnlockPeriod
        // ------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Finance Officer")]
        public async Task<ActionResult> UpdateDeductions(int id, decimal? medicalAid, decimal? pension, decimal? otherDed, decimal? otherAdd, decimal? advance, string deductionNotes, string additionNotes)
        {
            try
            {
                var payroll = db.Payrolls
                    .Include(p => p.Staff)
                    .FirstOrDefault(p => p.Id == id);

                if (payroll == null)
                {
                    TempData["ErrorMessage"] = "Payroll record not found.";
                    return RedirectToAction("Index");
                }

                if (payroll.Status != "Draft" && payroll.Status != "Calculated")
                {
                    TempData["ErrorMessage"] = "Deductions and additions can only be updated while Draft or Calculated.";
                    return RedirectToAction("Details", new { id });
                }

                payroll.MedicalAidContribution = medicalAid ?? 0m;
                payroll.PensionContribution = pension ?? 0m;
                payroll.OtherDeductions = otherDed ?? 0m;
                payroll.OtherAdditions = otherAdd ?? 0m;
                payroll.AdvanceRecovery = advance ?? 0m;
                payroll.DeductionNotes = deductionNotes;
                payroll.AdditionNotes = additionNotes;

                payroll.CalculateTotals();
                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = "Deductions and additions updated successfully.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating deductions: {ex.Message}");
                TempData["ErrorMessage"] = "Error updating deductions: " + ex.Message;
                return RedirectToAction("Details", new { id });
            }
        }

        [Authorize(Roles = "Finance Officer, Admin")]
        public ActionResult DownloadPayslip(int id)
        {
            var payroll = db.Payrolls
                .Include(p => p.Staff)
                .FirstOrDefault(p => p.Id == id);

            if (payroll == null || string.IsNullOrEmpty(payroll.PayslipPdfPath))
            {
                TempData["ErrorMessage"] = "Payslip PDF not found or not generated yet.";
                return RedirectToAction("Details", new { id });
            }

            string fullPath = Server.MapPath(payroll.PayslipPdfPath);
            if (!System.IO.File.Exists(fullPath))
            {
                TempData["ErrorMessage"] = "Payslip file missing from server storage.";
                return RedirectToAction("Details", new { id });
            }

            byte[] fileBytes = System.IO.File.ReadAllBytes(fullPath);
            string downloadName = $"Payslip_{payroll.Staff?.FullName?.Replace(" ", "_")}_{payroll.PayPeriodStart:yyyyMM}.pdf";
            return File(fileBytes, "application/pdf", downloadName);
        }

        [Authorize(Roles = "Crew Member, Site Manager, Project Manager, Finance Officer, Admin")]
        public ActionResult MyPayslips()
        {
            var userId = User.Identity.GetUserId();
            var staff = db.Staff.FirstOrDefault(s => s.UserId == userId);
            if (staff == null)
            {
                TempData["ErrorMessage"] = "Staff profile not found for current user.";
                return RedirectToAction("Index", "Home");
            }

            var myPayrolls = db.Payrolls
                .Include(p => p.Staff)
                .Where(p => p.StaffId == staff.Id && p.Status == "Paid")
                .OrderByDescending(p => p.PayPeriodStart)
                .ToList()
                .Select(p => MapToListItem(p))
                .ToList();

            foreach (var item in myPayrolls)
            {
                item.HasPayslip = !string.IsNullOrEmpty(db.Payrolls.Where(x => x.Id == item.Id).Select(x => x.PayslipPdfPath).FirstOrDefault());
            }

            return View(myPayrolls);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Finance Officer")]
        public async Task<ActionResult> LockPeriod(int year, int month, string reason)
        {
            try
            {
                var pStart = new DateTime(year, month, 1);
                var pEnd = pStart.AddMonths(1).AddDays(-1);

                var periodLock = db.PayrollPeriodLocks.FirstOrDefault(l => l.PeriodStart == pStart);
                if (periodLock == null)
                {
                    periodLock = new PayrollPeriodLock
                    {
                        PeriodStart = pStart,
                        PeriodEnd = pEnd
                    };
                    db.PayrollPeriodLocks.Add(periodLock);
                }

                periodLock.IsLocked = true;
                periodLock.LockedBy = User.Identity.Name;
                periodLock.LockedDate = DateTime.Now;
                periodLock.LockReason = reason;

                await db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Payroll period {pStart:MMMM yyyy} has been locked.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error locking period: {ex.Message}");
                TempData["ErrorMessage"] = "Error locking period: " + ex.Message;
            }
            return RedirectToAction("Index", new { year, month });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Finance Officer")]
        public async Task<ActionResult> UnlockPeriod(int year, int month, string reason)
        {
            try
            {
                var pStart = new DateTime(year, month, 1);
                var periodLock = db.PayrollPeriodLocks.FirstOrDefault(l => l.PeriodStart == pStart);
                if (periodLock != null)
                {
                    periodLock.IsLocked = false;
                    periodLock.LockedBy = User.Identity.Name;
                    periodLock.LockedDate = DateTime.Now;
                    periodLock.LockReason = reason;
                    await db.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Payroll period {pStart:MMMM yyyy} has been unlocked.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error unlocking period: {ex.Message}");
                TempData["ErrorMessage"] = "Error unlocking period: " + ex.Message;
            }
            return RedirectToAction("Index", new { year, month });
        }

        private bool IsPeriodLocked(int year, int month)
        {
            var pStart = new DateTime(year, month, 1);
            var lk = db.PayrollPeriodLocks.FirstOrDefault(l => l.PeriodStart == pStart);
            return lk != null && lk.IsLocked;
        }

        private PayrollValidationResult ValidatePayroll(Payroll payroll)
        {
            var useAttendance = true;
            var flag = ConfigurationManager.AppSettings["UseAttendanceForPayroll"];
            if (!string.IsNullOrWhiteSpace(flag) &&
                (flag.Equals("false", StringComparison.OrdinalIgnoreCase) || flag == "0"))
            {
                useAttendance = false;
            }

            List<AttendanceRecord> attendance = new List<AttendanceRecord>();
            List<TimesheetEntry> timesheets = new List<TimesheetEntry>();

            if (useAttendance)
            {
                attendance = db.AttendanceRecords
                    .Where(r => r.StaffId == payroll.StaffId &&
                                r.CheckInTime >= payroll.PayPeriodStart &&
                                r.CheckInTime <= payroll.PayPeriodEnd)
                    .ToList();
            }
            else
            {
                timesheets = db.TimesheetEntries
                    .Include(e => e.Timesheet)
                    .Where(e => e.Timesheet.StaffId == payroll.StaffId &&
                                e.Date >= payroll.PayPeriodStart &&
                                e.Date <= payroll.PayPeriodEnd)
                    .ToList();
            }

            return new PayrollValidationResult
            {
                Anomalies = DetectAnomalies(payroll, attendance, timesheets)
            };
        }

        private List<PayrollAnomaly> DetectAnomalies(Payroll payroll, List<AttendanceRecord> attendance, List<TimesheetEntry> timesheets)
        {
            var list = new List<PayrollAnomaly>();

            if (attendance != null)
            {
                var missing = attendance.Where(r => r.CheckOutTime == null).ToList();
                foreach (var m in missing)
                {
                    list.Add(new PayrollAnomaly
                    {
                        Code = "MISSING_CHECKOUT",
                        Severity = "Warning",
                        Message = $"Attendance record on {m.CheckInTime:dd MMM yyyy} has no check-out time.",
                        RelatedRecord = $"Attendance ID {m.Id}"
                    });
                }

                var excessive = attendance.Where(r => r.TotalHours.HasValue && r.TotalHours.Value > 14m).ToList();
                foreach (var e in excessive)
                {
                    list.Add(new PayrollAnomaly
                    {
                        Code = "EXCESSIVE_HOURS",
                        Severity = "Warning",
                        Message = $"Session on {e.CheckInTime:dd MMM yyyy} has excessive hours ({e.TotalHours:N2} hrs).",
                        RelatedRecord = $"Attendance ID {e.Id}"
                    });
                }

                var closed = attendance.Where(r => r.CheckOutTime != null && r.TotalHours.HasValue).ToList();
                var groups = closed.GroupBy(r => r.CheckInTime.Date);
                foreach (var g in groups)
                {
                    if (g.Count() > 1 && g.Sum(x => x.TotalHours.Value) > 16m)
                    {
                        list.Add(new PayrollAnomaly
                        {
                            Code = "DUPLICATE_ENTRIES",
                            Severity = "Warning",
                            Message = $"Multiple attendance sessions on {g.Key:dd MMM yyyy} total {g.Sum(x => x.TotalHours.Value):N2} hours.",
                            RelatedRecord = g.Key.ToString("dd MMM yyyy")
                        });
                    }
                }
            }

            if (timesheets != null)
            {
                var unapprovedOt = timesheets.Where(t => t.OvertimeHours > 0 && t.Timesheet != null && t.Timesheet.Status != "Approved").ToList();
                foreach (var u in unapprovedOt)
                {
                    list.Add(new PayrollAnomaly
                    {
                        Code = "UNAPPROVED_OVERTIME",
                        Severity = "Blocker",
                        Message = $"Timesheet entry on {u.Date:dd MMM yyyy} has overtime but parent timesheet status is '{u.Timesheet.Status}'.",
                        RelatedRecord = $"Timesheet ID {u.TimesheetId}"
                    });
                }
            }

            if (payroll.Staff == null || payroll.Staff.PayRatePerHour <= 0)
            {
                list.Add(new PayrollAnomaly
                {
                    Code = "ZERO_PAY_RATE",
                    Severity = "Blocker",
                    Message = "Staff pay rate per hour is zero or not set.",
                    RelatedRecord = payroll.Staff?.FullName ?? "Staff"
                });
            }

            var bankAcc = payroll.BankAccount ?? payroll.Staff?.BankAccount;
            if (string.IsNullOrWhiteSpace(bankAcc))
            {
                list.Add(new PayrollAnomaly
                {
                    Code = "MISSING_BANK_ACCOUNT",
                    Severity = "Warning",
                    Message = "Staff member does not have a bank account configured.",
                    RelatedRecord = payroll.Staff?.FullName ?? "Staff"
                });
            }

            return list;
        }

        // ------------------------------------------------------------------
        // Manage Pay Rates – Finance Officer only
        // ------------------------------------------------------------------

        [Authorize(Roles = "Finance Officer")]
        public ActionResult ManageRates(string search = null, string role = null, string sortBy = "name", string sortDir = "asc")
        {
            var currentUserId = User.Identity.GetUserId();
            var currentStaff = db.Staff.FirstOrDefault(s => s.UserId == currentUserId && s.IsActive);

            var query = db.Staff.Where(s => s.IsActive && s.Role != "Admin");

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(s => s.Role == role);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(s =>
                    (s.FirstName + " " + s.LastName).ToLower().Contains(term) ||
                    (s.EmployeeId != null && s.EmployeeId.ToLower().Contains(term)));
            }

            var staffList = query.ToList();

            var staffIds = staffList.Select(s => s.Id).ToList();
            var latestAudits = db.PayRateAudits
                .Where(a => staffIds.Contains(a.StaffId))
                .GroupBy(a => a.StaffId)
                .Select(g => new { StaffId = g.Key, ChangedAt = g.Max(x => x.ChangedAt) })
                .ToList()
                .ToDictionary(x => x.StaffId, x => (DateTime?)x.ChangedAt);

            var rows = staffList.Select(s => new StaffRateRowViewModel
            {
                StaffId = s.Id,
                FullName = s.FullName,
                EmployeeId = s.EmployeeId,
                Role = s.Role,
                PayRatePerHour = s.PayRatePerHour,
                BankAccountMasked = MaskBankAccount(s.BankAccount),
                LastModified = latestAudits.ContainsKey(s.Id) ? latestAudits[s.Id] : null,
                IsCurrentUser = currentStaff != null && currentStaff.Id == s.Id
            }).ToList();

            sortBy = (sortBy ?? "name").ToLowerInvariant();
            sortDir = (sortDir ?? "asc").ToLowerInvariant();
            bool asc = sortDir != "desc";

            switch (sortBy)
            {
                case "role":
                    rows = asc
                        ? rows.OrderBy(r => r.Role).ThenBy(r => r.FullName).ToList()
                        : rows.OrderByDescending(r => r.Role).ThenBy(r => r.FullName).ToList();
                    break;
                case "rate":
                    rows = asc
                        ? rows.OrderBy(r => r.PayRatePerHour).ThenBy(r => r.FullName).ToList()
                        : rows.OrderByDescending(r => r.PayRatePerHour).ThenBy(r => r.FullName).ToList();
                    break;
                default:
                    rows = asc
                        ? rows.OrderBy(r => r.FullName).ToList()
                        : rows.OrderByDescending(r => r.FullName).ToList();
                    break;
            }

            var recentAudits = db.PayRateAudits
                .Include(a => a.Staff)
                .OrderByDescending(a => a.ChangedAt)
                .Take(20)
                .ToList()
                .Select(a => new PayRateAuditItemViewModel
                {
                    Id = a.Id,
                    StaffName = a.Staff != null ? a.Staff.FullName : "Unknown",
                    OldRate = a.OldRate,
                    NewRate = a.NewRate,
                    ChangedBy = a.ChangedBy,
                    ChangedAt = a.ChangedAt
                }).ToList();

            var roles = new[] { "Crew Member", "Site Manager", "Project Manager", "Finance Officer", "Procurement Officer" };
            var roleItems = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All Roles", Selected = string.IsNullOrEmpty(role) }
            };
            roleItems.AddRange(roles.Select(r => new SelectListItem
            {
                Value = r,
                Text = r,
                Selected = r == role
            }));

            var vm = new ManageRatesViewModel
            {
                StaffRows = rows,
                RecentAudits = recentAudits,
                Search = search,
                RoleFilter = role,
                SortBy = sortBy,
                SortDir = sortDir,
                Roles = roleItems
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Finance Officer")]
        public async Task<ActionResult> UpdateRate(int staffId, string newRate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newRate) ||
                    !decimal.TryParse(newRate, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal rate))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid rate format. Use a number such as 120.50."
                    });
                }

                rate = Math.Round(rate, 2);

                if (rate < 1.00m || rate > 10000.00m)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Pay rate must be between R 1.00 and R 10,000.00 per hour."
                    });
                }

                var staff = db.Staff.FirstOrDefault(s => s.Id == staffId && s.IsActive);
                if (staff == null)
                {
                    return Json(new { success = false, message = "Staff member not found or inactive." });
                }

                if (string.Equals(staff.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "Cannot change the pay rate of an Admin." });
                }

                var currentUserId = User.Identity.GetUserId();
                var currentStaff = db.Staff.FirstOrDefault(s => s.UserId == currentUserId && s.IsActive);
                if (currentStaff != null && currentStaff.Id == staffId)
                {
                    return Json(new
                    {
                        success = false,
                        message = "You cannot change your own pay rate. Please ask another Finance Officer or an Admin."
                    });
                }

                var oldRate = staff.PayRatePerHour;
                if (oldRate == rate)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Rate unchanged.",
                        newRate = rate
                    });
                }

                staff.PayRatePerHour = rate;

                var audit = new PayRateAudit
                {
                    StaffId = staff.Id,
                    OldRate = oldRate,
                    NewRate = rate,
                    ChangedBy = User.Identity.Name ?? currentUserId,
                    ChangedByUserId = currentUserId,
                    ChangedAt = DateTime.Now
                };
                db.PayRateAudits.Add(audit);

                await db.SaveChangesAsync();

                await NotifyFinanceTeam(
                    "Pay Rate Updated",
                    $"Pay rate for {staff.FullName} changed from R {oldRate:N2} to R {rate:N2} by {User.Identity.Name}.",
                    "/Payroll/ManageRates",
                    skipUserId: currentUserId
                );

                return Json(new
                {
                    success = true,
                    message = $"Rate updated to R {rate:N2}.",
                    newRate = rate
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateRate error: {ex}");
                return Json(new { success = false, message = "An error occurred while updating the rate." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Finance Officer")]
        public async Task<ActionResult> UpdateBankAccount(int staffId, string bankAccount)
        {
            try
            {
                bankAccount = (bankAccount ?? string.Empty).Trim().Replace(" ", "");

                if (string.IsNullOrEmpty(bankAccount))
                {
                    return Json(new { success = false, message = "Bank account cannot be empty." });
                }

                if (bankAccount.Length > 100)
                {
                    return Json(new { success = false, message = "Bank account must be 100 characters or fewer." });
                }

                if (!Regex.IsMatch(bankAccount, @"^[A-Za-z0-9\-]+$"))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Bank account may only contain letters, digits and hyphens (no spaces)."
                    });
                }

                var staff = db.Staff.FirstOrDefault(s => s.Id == staffId && s.IsActive);
                if (staff == null)
                {
                    return Json(new { success = false, message = "Staff member not found or inactive." });
                }

                if (string.Equals(staff.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "Cannot change the bank account of an Admin." });
                }

                var currentUserId = User.Identity.GetUserId();
                var currentStaff = db.Staff.FirstOrDefault(s => s.UserId == currentUserId && s.IsActive);
                if (currentStaff != null && currentStaff.Id == staffId)
                {
                    return Json(new
                    {
                        success = false,
                        message = "You cannot change your own bank account. Please ask another Finance Officer or an Admin."
                    });
                }

                var oldAccount = (staff.BankAccount ?? string.Empty).Trim().Replace(" ", "");
                if (string.Equals(oldAccount, bankAccount, StringComparison.Ordinal))
                {
                    return Json(new
                    {
                        success = true,
                        message = "Bank account unchanged.",
                        bankAccountMasked = MaskBankAccount(bankAccount)
                    });
                }

                staff.BankAccount = bankAccount;
                await db.SaveChangesAsync();

                await NotifyFinanceTeam(
                    "Bank Account Updated",
                    $"Bank account for {staff.FullName} was updated by {User.Identity.Name}.",
                    "/Payroll/ManageRates",
                    skipUserId: currentUserId
                );

                return Json(new
                {
                    success = true,
                    message = "Bank account updated.",
                    bankAccountMasked = MaskBankAccount(bankAccount)
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateBankAccount error: {ex}");
                return Json(new { success = false, message = "An error occurred while updating the bank account." });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Finance Officer")]
        public ActionResult RateAuditHistory()
        {
            var items = db.PayRateAudits
                .Include(a => a.Staff)
                .OrderByDescending(a => a.ChangedAt)
                .Take(20)
                .ToList()
                .Select(a => new
                {
                    id = a.Id,
                    staffName = a.Staff != null ? a.Staff.FullName : "Unknown",
                    oldRate = a.OldRate,
                    newRate = a.NewRate,
                    changedBy = a.ChangedBy,
                    changedAt = a.ChangedAt.ToString("dd MMM yyyy HH:mm")
                });

            return Json(new { success = true, items = items }, JsonRequestBehavior.AllowGet);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private void AggregateHours(Payroll payroll, int staffId, DateTime periodStart, DateTime periodEnd)
        {
            var useAttendance = true;
            var flag = ConfigurationManager.AppSettings["UseAttendanceForPayroll"];
            if (!string.IsNullOrWhiteSpace(flag) &&
                (flag.Equals("false", StringComparison.OrdinalIgnoreCase) || flag == "0"))
            {
                useAttendance = false;
            }

            decimal regular = 0;
            decimal overtime = 0;

            if (useAttendance)
            {
                var records = db.AttendanceRecords
                    .Where(r => r.StaffId == staffId
                                && r.CheckOutTime != null
                                && r.CheckInTime >= periodStart
                                && r.CheckInTime <= periodEnd
                                && r.TotalHours != null)
                    .ToList();

                decimal totalHours = records.Sum(r => r.TotalHours.Value);

                decimal regularCap = 176m;
                var capSetting = ConfigurationManager.AppSettings["PayrollRegularHoursPerPeriod"];
                if (!string.IsNullOrWhiteSpace(capSetting))
                {
                    decimal parsed;
                    if (decimal.TryParse(capSetting, out parsed) && parsed > 0)
                        regularCap = parsed;
                }

                if (totalHours <= regularCap)
                {
                    regular = totalHours;
                    overtime = 0;
                }
                else
                {
                    regular = regularCap;
                    overtime = totalHours - regularCap;
                }
            }
            else
            {
                var entries = db.TimesheetEntries
                    .Include(e => e.Timesheet)
                    .Where(e => e.Timesheet.StaffId == staffId &&
                                e.Timesheet.Status == "Approved" &&
                                e.Date >= periodStart &&
                                e.Date <= periodEnd)
                    .ToList();

                foreach (var e in entries)
                {
                    if (e.OvertimeHours > 0)
                    {
                        overtime += e.OvertimeHours;
                        regular += Math.Max(0, e.HoursWorked - e.OvertimeHours);
                    }
                    else if (e.IsOvertime)
                    {
                        overtime += e.HoursWorked;
                    }
                    else
                    {
                        regular += e.HoursWorked;
                    }
                }
            }

            payroll.RegularHours = Math.Round(regular, 2);
            payroll.OvertimeHours = Math.Round(overtime, 2);
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        private static string MaskBankAccount(string account)
        {
            if (string.IsNullOrWhiteSpace(account))
                return "—";

            var cleaned = account.Trim().Replace(" ", "");
            if (cleaned.Length <= 4)
                return new string('•', cleaned.Length);
            if (cleaned.Length <= 8)
                return cleaned.Substring(0, 2) + new string('•', cleaned.Length - 4) + cleaned.Substring(cleaned.Length - 2);

            return cleaned.Substring(0, 4) + "••••" + cleaned.Substring(cleaned.Length - 4);
        }

        private PayrollListItemViewModel MapToListItem(Payroll p)
        {
            return new PayrollListItemViewModel
            {
                Id = p.Id,
                StaffId = p.StaffId,
                StaffName = p.Staff != null ? p.Staff.FullName : "Unknown",
                EmployeeId = p.Staff?.EmployeeId,
                Role = p.Staff?.Role,
                PayPeriodStart = p.PayPeriodStart,
                PayPeriodEnd = p.PayPeriodEnd,
                PeriodLabel = $"{p.PayPeriodStart:MMM yyyy}",
                RegularHours = p.RegularHours,
                OvertimeHours = p.OvertimeHours,
                GrossPay = p.GrossPay,
                NetPay = p.NetPay,
                TotalAmount = p.TotalAmount,
                Status = p.Status,
                StatusBadgeColor = GetStatusBadgeColor(p.Status),
                PaymentDate = p.PaymentDate,
                PaymentMethod = p.PaymentMethod,
                PayslipPdfPath = p.PayslipPdfPath,
                ValidationWarningsJson = p.ValidationWarningsJson
            };
        }

        private PayrollDetailsViewModel MapToDetails(Payroll p)
        {
            var anomaliesList = new List<PayrollAnomalyViewModel>();
            if (!string.IsNullOrEmpty(p.ValidationWarningsJson))
            {
                try
                {
                    var rawAnomalies = JsonConvert.DeserializeObject<List<PayrollAnomaly>>(p.ValidationWarningsJson);
                    if (rawAnomalies != null)
                    {
                        anomaliesList = rawAnomalies.Select(a => new PayrollAnomalyViewModel
                        {
                            Code = a.Code,
                            Severity = a.Severity,
                            Message = a.Message,
                            RelatedRecord = a.RelatedRecord
                        }).ToList();
                    }
                }
                catch { }
            }

            var pStart = new DateTime(p.PayPeriodStart.Year, p.PayPeriodStart.Month, 1);
            var lk = db.PayrollPeriodLocks.FirstOrDefault(l => l.PeriodStart == pStart);
            bool isLocked = lk != null && lk.IsLocked;

            return new PayrollDetailsViewModel
            {
                Id = p.Id,
                StaffId = p.StaffId,
                StaffName = p.Staff != null ? p.Staff.FullName : "Unknown",
                EmployeeId = p.Staff?.EmployeeId,
                StaffRole = p.Staff?.Role,
                StaffEmail = p.Staff?.Email,
                BankAccount = MaskBankAccount(p.BankAccount ?? p.Staff?.BankAccount),
                PayPeriodStart = p.PayPeriodStart,
                PayPeriodEnd = p.PayPeriodEnd,
                PeriodLabel = $"{p.PayPeriodStart:MMMM yyyy}",
                RegularHours = p.RegularHours,
                OvertimeHours = p.OvertimeHours,
                HourlyRate = p.HourlyRate,
                OvertimeRate = p.OvertimeRate,
                RegularPay = p.RegularPay,
                OvertimePay = p.OvertimePay,
                Bonus = p.Bonus,
                GrossPay = p.GrossPay,
                Deductions = p.Deductions,
                Tax = p.Tax,
                NetPay = p.NetPay,
                TotalAmount = p.TotalAmount,
                Status = p.Status,
                StatusBadgeColor = GetStatusBadgeColor(p.Status),
                CalculatedBy = p.CalculatedBy,
                ApprovedBy = p.ApprovedBy,
                ApprovalDate = p.ApprovalDate,
                ProcessedBy = p.ProcessedBy,
                ProcessedDate = p.ProcessedDate,
                PaymentDate = p.PaymentDate,
                PaymentMethod = p.PaymentMethod,
                Notes = p.Notes,
                CanCalculate = p.Status == "Draft" || p.Status == "Calculated",
                CanApprove = p.Status == "Calculated",
                CanProcess = p.Status == "Approved" || p.Status == "Processed",
                CanMarkPaid = p.Status == "Processed",
                Anomalies = anomaliesList,
                HasBlocker = anomaliesList.Any(a => a.Severity == "Blocker"),
                HasWarning = anomaliesList.Any(a => a.Severity == "Warning"),
                MedicalAidContribution = p.MedicalAidContribution ?? 0m,
                PensionContribution = p.PensionContribution ?? 0m,
                OtherDeductions = p.OtherDeductions ?? 0m,
                OtherAdditions = p.OtherAdditions ?? 0m,
                AdvanceRecovery = p.AdvanceRecovery ?? 0m,
                DeductionNotes = p.DeductionNotes,
                AdditionNotes = p.AdditionNotes,
                PayslipPdfPath = p.PayslipPdfPath,
                IsPeriodLocked = isLocked
            };
        }

        private static string GetStatusBadgeColor(string status)
        {
            switch (status)
            {
                case "Draft": return "secondary";
                case "Calculated": return "info";
                case "Approved": return "primary";
                case "Processed": return "warning";
                case "Paid": return "success";
                default: return "secondary";
            }
        }

        private List<SelectListItem> GetYearSelectList(int? selected)
        {
            var current = DateTime.Today.Year;
            var years = Enumerable.Range(current - 2, 5).Reverse();
            return years.Select(y => new SelectListItem
            {
                Value = y.ToString(),
                Text = y.ToString(),
                Selected = y == selected
            }).ToList();
        }

        private List<SelectListItem> GetMonthSelectList(int? selected)
        {
            var months = new[]
            {
                new { V = 1, T = "January" }, new { V = 2, T = "February" }, new { V = 3, T = "March" },
                new { V = 4, T = "April" }, new { V = 5, T = "May" }, new { V = 6, T = "June" },
                new { V = 7, T = "July" }, new { V = 8, T = "August" }, new { V = 9, T = "September" },
                new { V = 10, T = "October" }, new { V = 11, T = "November" }, new { V = 12, T = "December" }
            };
            return months.Select(m => new SelectListItem
            {
                Value = m.V.ToString(),
                Text = m.T,
                Selected = m.V == selected
            }).ToList();
        }

        private List<SelectListItem> GetStatusSelectList(string selected)
        {
            var items = new[] { "Draft", "Calculated", "Approved", "Processed", "Paid" };
            return items.Select(s => new SelectListItem
            {
                Value = s,
                Text = s,
                Selected = s == selected
            }).ToList();
        }

        private List<SelectListItem> GetPaymentMethodSelectList(string selected)
        {
            var items = new[] { "Bank Transfer", "Cash", "Cheque" };
            return items.Select(s => new SelectListItem
            {
                Value = s,
                Text = s,
                Selected = s == selected
            }).ToList();
        }

        private async Task CreateNotification(string userId, string title, string message, string type, string link)
        {
            if (string.IsNullOrEmpty(userId)) return;
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    Link = link,
                    CreatedDate = DateTime.Now,
                    IsRead = false
                };
                db.Notifications.Add(notification);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating notification: {ex.Message}");
            }
        }

        private async Task NotifyFinanceTeam(string title, string message, string link, string skipUserId = null)
        {
            try
            {
                var userIds = db.Users.ToList()
                    .Where(u => (UserManager.IsInRole(u.Id, "Finance Officer") || UserManager.IsInRole(u.Id, "Admin"))
                                && u.Id != skipUserId)
                    .Select(u => u.Id)
                    .ToList();

                foreach (var uid in userIds)
                {
                    await CreateNotification(uid, title, message, "Finance", link);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error notifying finance team: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}