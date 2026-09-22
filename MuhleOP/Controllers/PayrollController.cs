// ============ Controllers/PayrollController.cs ============
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
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
                TotalNet = list.Sum(i => i.NetPay)
            };

            return View(viewModel);
        }

        // ------------------------------------------------------------------
        // Generate (select period → create Draft records) – Finance Officer only
        // ------------------------------------------------------------------

        [Authorize(Roles = "Finance Officer")]
        public ActionResult Generate()
        {
            var now = DateTime.Today;
            var model = new GeneratePayrollViewModel
            {
                Year = now.Year,
                Month = now.Month == 1 ? 12 : now.Month - 1,
                Years = GetYearSelectList(now.Year),
                Months = GetMonthSelectList(now.Month == 1 ? 12 : now.Month - 1)
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

            try
            {
                var periodStart = new DateTime(model.Year, model.Month, 1);
                var periodEnd = periodStart.AddMonths(1).AddDays(-1);

                var activeStaff = db.Staff
                    .Where(s => s.IsActive)
                    .ToList();

                int generated = 0;
                int skipped = 0;
                var messages = new List<string>();

                foreach (var staff in activeStaff)
                {
                    var exists = db.Payrolls.Any(p =>
                        p.StaffId == staff.Id &&
                        p.PayPeriodStart == periodStart &&
                        p.PayPeriodEnd == periodEnd);

                    if (exists)
                    {
                        skipped++;
                        messages.Add($"Skipped {staff.FullName} — payroll already exists for {periodStart:MMM yyyy}.");
                        continue;
                    }

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
                    (skipped > 0 ? $"{skipped} staff skipped (already exist or missing pay rate)." : "No active staff found.");
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

                if (payroll.Staff == null || payroll.Staff.PayRatePerHour <= 0)
                {
                    TempData["ErrorMessage"] = "Staff pay rate is missing or zero. Cannot calculate.";
                    return RedirectToAction("Details", new { id });
                }

                payroll.HourlyRate = payroll.Staff.PayRatePerHour;
                payroll.OvertimeRate = payroll.Staff.PayRatePerHour * 1.5m;
                payroll.Bonus = 0;

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
                foreach (var payroll in drafts)
                {
                    if (payroll.Staff == null || payroll.Staff.PayRatePerHour <= 0)
                        continue;

                    payroll.HourlyRate = payroll.Staff.PayRatePerHour;
                    payroll.OvertimeRate = payroll.Staff.PayRatePerHour * 1.5m;
                    payroll.Bonus = 0;

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

                TempData["SuccessMessage"] = $"Calculated {count} payroll record(s) for {periodStart:MMMM yyyy}.";
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

                if (payroll.Status != "Processed")
                {
                    TempData["ErrorMessage"] = "Only Processed payrolls can be marked as Paid.";
                    return RedirectToAction("Details", new { id });
                }

                payroll.Status = "Paid";
                if (!payroll.PaymentDate.HasValue)
                    payroll.PaymentDate = DateTime.Today;

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
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Aggregates hours for a payroll period.
        /// When UseAttendanceForPayroll is true (default), sums closed AttendanceRecords.
        /// Otherwise falls back to approved TimesheetEntries.
        /// Regular vs overtime is split using PayrollRegularHoursPerPeriod (default 176).
        /// </summary>
        private void AggregateHours(Payroll payroll, int staffId, DateTime periodStart, DateTime periodEnd)
        {
            // Feature flag: UseAttendanceForPayroll (default true). Set to "false" in web.config to fall back to TimesheetEntries.
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
                // Closed attendance sessions only (CheckOutTime not null)
                var records = db.AttendanceRecords
                    .Where(r => r.StaffId == staffId
                                && r.CheckOutTime != null
                                && r.CheckInTime >= periodStart
                                && r.CheckInTime <= periodEnd
                                && r.TotalHours != null)
                    .ToList();

                decimal totalHours = records.Sum(r => r.TotalHours.Value);

                // Regular hours threshold for the period (default 176 = 22 × 8)
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
                // Legacy path: TimesheetEntries
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
                PaymentMethod = p.PaymentMethod
            };
        }

        private PayrollDetailsViewModel MapToDetails(Payroll p)
        {
            return new PayrollDetailsViewModel
            {
                Id = p.Id,
                StaffId = p.StaffId,
                StaffName = p.Staff != null ? p.Staff.FullName : "Unknown",
                EmployeeId = p.Staff?.EmployeeId,
                StaffRole = p.Staff?.Role,
                StaffEmail = p.Staff?.Email,
                BankAccount = p.BankAccount ?? p.Staff?.BankAccount,
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
                CanMarkPaid = p.Status == "Processed"
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