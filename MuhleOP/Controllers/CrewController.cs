// Controllers/CrewController.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MuhleOP.Models;
using MuhleOP.ViewModels;
using Newtonsoft.Json;

namespace MuhleOP.Controllers
{
    [Authorize]
    public class CrewController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // TODO: replace with distributed cache for multi-server
        private static readonly ConcurrentDictionary<string, PendingScanToken> PendingTokens =
            new ConcurrentDictionary<string, PendingScanToken>();

        private ApplicationUserManager _userManager;
        private ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
        }

        // GET: Crew/ClockInOut
        [Authorize(Roles = "Crew Member")]
        public ActionResult ClockInOut()
        {
            var userId = User.Identity.GetUserId();
            var staff = db.Staff.FirstOrDefault(s => s.UserId == userId && s.IsActive);
            if (staff == null)
            {
                TempData["ErrorMessage"] = "Staff record not found or inactive.";
                return RedirectToAction("Dashboard", "Home");
            }

            PurgeExpiredTokens();

            var today = DateTime.Today;
            var openEntry = db.TimesheetEntries
                .FirstOrDefault(e => e.Timesheet.StaffId == staff.Id
                    && e.Date == today
                    && e.StartTime != null
                    && e.EndTime == null);

            var token = Guid.NewGuid().ToString("N");
            var action = openEntry != null ? "ClockOut" : "ClockIn";

            PendingTokens[token] = new PendingScanToken
            {
                StaffId = staff.Id,
                Action = action,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(5),
                Used = false
            };

            string projectName = null;
            var projectId = GetCurrentProjectIdForStaff(staff.Id);
            if (projectId.HasValue)
            {
                var project = db.Projects.Find(projectId.Value);
                projectName = project?.ProjectName;
            }

            var viewModel = new ClockInOutViewModel
            {
                StaffId = staff.Id,
                StaffName = staff.FullName,
                Action = action,
                Token = token,
                TimesheetEntryId = openEntry?.Id,
                ClockInTime = openEntry?.StartTime,
                ProjectName = projectName ?? "No active project",
                WeeklyHoursSummary = CalculateWeeklyHoursSummary(staff.Id)
            };

            return View(viewModel);
        }

        // GET: Crew/ClockInOutStatus?token=...
        [Authorize(Roles = "Crew Member")]
        public JsonResult ClockInOutStatus(string token)
        {
            PurgeExpiredTokens();

            if (string.IsNullOrEmpty(token) || !PendingTokens.TryGetValue(token, out var stored))
            {
                return Json(new
                {
                    success = false,
                    scanned = false,
                    message = "Token not found or expired."
                }, JsonRequestBehavior.AllowGet);
            }

            if (!stored.Used)
            {
                return Json(new
                {
                    success = true,
                    scanned = false,
                    message = "Waiting for scan..."
                }, JsonRequestBehavior.AllowGet);
            }

            var staff = db.Staff.Find(stored.StaffId);
            var todayHours = CalculateTodayHours(stored.StaffId);
            var scannedAt = stored.ScannedAtUtc.HasValue
                ? stored.ScannedAtUtc.Value.ToLocalTime().ToString("HH:mm")
                : DateTime.Now.ToString("HH:mm");

            var message = stored.ResultAction == "ClockIn"
                ? $"You have been clocked in at {scannedAt}."
                : $"You have been clocked out. Hours worked today: {FormatHours(todayHours)}.";

            // Remove used token after reporting
            PendingTokens.TryRemove(token, out _);

            return Json(new
            {
                success = true,
                scanned = true,
                action = stored.ResultAction,
                scannedAt,
                message,
                todayHours = Math.Round(todayHours, 2)
            }, JsonRequestBehavior.AllowGet);
        }

        // GET: Crew/ScanQR
        [Authorize(Roles = "Admin,Site Manager")]
        public ActionResult ScanQR()
        {
            return View(new ScanQrRequestViewModel());
        }

        // POST: Crew/ScanQR
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Site Manager")]
        public async Task<ActionResult> ScanQR(ScanQrRequestViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model?.Payload))
            {
                return Json(new { success = false, message = "Invalid QR code." });
            }

            QrPayloadViewModel payload;
            try
            {
                payload = JsonConvert.DeserializeObject<QrPayloadViewModel>(model.Payload);
                if (payload == null || string.IsNullOrEmpty(payload.Token) || string.IsNullOrEmpty(payload.Action))
                {
                    return Json(new { success = false, message = "Invalid QR code." });
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Invalid QR code." });
            }

            PurgeExpiredTokens();

            if (!PendingTokens.TryGetValue(payload.Token, out var stored)
                || stored.Used
                || stored.ExpiresUtc < DateTime.UtcNow)
            {
                return Json(new
                {
                    success = false,
                    message = "QR code has expired or was already used. Ask the crew member to refresh."
                });
            }

            if (payload.StaffId != stored.StaffId || payload.Action != stored.Action)
            {
                return Json(new { success = false, message = "QR code does not match the expected scan." });
            }

            var staff = db.Staff.Find(payload.StaffId);
            if (staff == null || !staff.IsActive)
            {
                return Json(new { success = false, message = "Staff member not found or inactive." });
            }

            var today = DateTime.Today;
            var now = DateTime.Now;
            var timeStr = now.ToString("HH:mm");

            try
            {
                if (payload.Action == "ClockIn")
                {
                    var openEntry = db.TimesheetEntries
                        .FirstOrDefault(e => e.Timesheet.StaffId == staff.Id
                            && e.Date == today
                            && e.StartTime != null
                            && e.EndTime == null);

                    if (openEntry != null)
                    {
                        return Json(new { success = false, message = $"{staff.FullName} is already clocked in." });
                    }

                    var weekStart = GetWeekStartDate(now);
                    var timesheet = db.Timesheets
                        .FirstOrDefault(t => t.StaffId == staff.Id && t.WeekStarting == weekStart);

                    if (timesheet == null)
                    {
                        timesheet = new Timesheet
                        {
                            StaffId = staff.Id,
                            WeekStarting = weekStart,
                            Status = "Draft",
                            TotalHours = 0,
                            OvertimeHours = 0
                        };
                        db.Timesheets.Add(timesheet);
                        await db.SaveChangesAsync();
                    }

                    var projectId = GetCurrentProjectIdForStaff(staff.Id);

                    var entry = new TimesheetEntry
                    {
                        TimesheetId = timesheet.Id,
                        Date = today,
                        StartTime = now.TimeOfDay,
                        EndTime = null,
                        HoursWorked = 0,
                        OvertimeHours = 0,
                        TaskDescription = "On-site work",
                        ProjectId = projectId,
                        IsOvertime = false
                    };
                    db.TimesheetEntries.Add(entry);
                    await db.SaveChangesAsync();

                    stored.Used = true;
                    stored.ScannedAtUtc = DateTime.UtcNow;
                    stored.ScannedByName = User.Identity.Name;
                    stored.ResultAction = "ClockIn";
                    stored.ResultTimesheetEntryId = entry.Id;

                    await CreateNotification(
                        staff.UserId,
                        "Clocked In",
                        $"You were clocked in at {timeStr}" +
                        (projectId.HasValue ? $" (project linked)." : "."),
                        "Attendance",
                        "/Crew/ClockInOut"
                    );

                    await NotifyAdmins(
                        $"{staff.FullName} Clocked In",
                        $"{staff.FullName} clocked in at {timeStr}." +
                        (payload.Lat != 0 || payload.Lng != 0
                            ? $" Location: {payload.Lat:F5}, {payload.Lng:F5}."
                            : ""),
                        "/Crew/ScanQR"
                    );

                    return Json(new
                    {
                        success = true,
                        action = "ClockIn",
                        staffName = staff.FullName,
                        time = timeStr,
                        todayHours = 0m,
                        message = $"{staff.FullName} — Clocked In at {timeStr}"
                    });
                }
                else if (payload.Action == "ClockOut")
                {
                    var openEntry = db.TimesheetEntries
                        .FirstOrDefault(e => e.Timesheet.StaffId == staff.Id
                            && e.Date == today
                            && e.StartTime != null
                            && e.EndTime == null);

                    if (openEntry == null)
                    {
                        return Json(new { success = false, message = $"{staff.FullName} is not currently clocked in." });
                    }

                    openEntry.EndTime = now.TimeOfDay;

                    var start = openEntry.StartTime.Value;
                    var end = openEntry.EndTime.Value;
                    var worked = end - start;
                    if (worked.TotalHours < 0)
                    {
                        // Cross-midnight edge case
                        worked = worked.Add(TimeSpan.FromHours(24));
                    }

                    var breakMinutes = openEntry.BreakDurationMinutes ?? 0;
                    var hoursWorked = (decimal)(worked.TotalHours - (breakMinutes / 60.0));
                    if (hoursWorked < 0) hoursWorked = 0;

                    openEntry.HoursWorked = Math.Round(hoursWorked, 2);
                    openEntry.IsOvertime = hoursWorked > 9m;
                    openEntry.OvertimeHours = openEntry.IsOvertime
                        ? Math.Round(hoursWorked - 9m, 2)
                        : 0m;

                    var timesheet = db.Timesheets.Find(openEntry.TimesheetId);
                    if (timesheet != null)
                    {
                        timesheet.TotalHours = timesheet.Entries.Sum(e => e.HoursWorked);
                        timesheet.OvertimeHours = timesheet.Entries.Sum(e => e.OvertimeHours);
                    }

                    await db.SaveChangesAsync();

                    var todayHours = CalculateTodayHours(staff.Id);

                    stored.Used = true;
                    stored.ScannedAtUtc = DateTime.UtcNow;
                    stored.ScannedByName = User.Identity.Name;
                    stored.ResultAction = "ClockOut";
                    stored.ResultTimesheetEntryId = openEntry.Id;

                    await CreateNotification(
                        staff.UserId,
                        "Clocked Out",
                        $"You were clocked out at {timeStr}. Hours worked today: {FormatHours(todayHours)}.",
                        "Attendance",
                        "/Crew/ClockInOut"
                    );

                    await NotifyAdmins(
                        $"{staff.FullName} Clocked Out",
                        $"{staff.FullName} clocked out at {timeStr}. Hours today: {FormatHours(todayHours)}." +
                        (payload.Lat != 0 || payload.Lng != 0
                            ? $" Location: {payload.Lat:F5}, {payload.Lng:F5}."
                            : ""),
                        "/Crew/ScanQR"
                    );

                    return Json(new
                    {
                        success = true,
                        action = "ClockOut",
                        staffName = staff.FullName,
                        time = timeStr,
                        todayHours = Math.Round(todayHours, 2),
                        message = $"{staff.FullName} — Clocked Out at {timeStr} — {FormatHours(todayHours)} today"
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Unknown action in QR code." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing QR scan: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while processing the scan." });
            }
        }

        #region Helpers

        private int? GetCurrentProjectIdForStaff(int staffId)
        {
            var today = DateTime.Today;
            var allocation = db.ProjectResourceAllocations
                .Where(a => a.StaffId == staffId
                    && a.IsActive
                    && a.StartDate <= today
                    && (a.EndDate == null || a.EndDate >= today))
                .OrderByDescending(a => a.AssignedDate)
                .FirstOrDefault();

            return allocation?.ProjectId;
        }

        private decimal CalculateTodayHours(int staffId)
        {
            var today = DateTime.Today;
            return db.TimesheetEntries
                .Where(e => e.Timesheet.StaffId == staffId
                    && e.Date == today
                    && e.EndTime != null)
                .Sum(e => (decimal?)e.HoursWorked) ?? 0m;
        }

        private string CalculateWeeklyHoursSummary(int staffId)
        {
            var weekStart = GetWeekStartDate(DateTime.Now);
            var total = db.TimesheetEntries
                .Where(e => e.Timesheet.StaffId == staffId
                    && e.Timesheet.WeekStarting == weekStart
                    && e.EndTime != null)
                .Sum(e => (decimal?)e.HoursWorked) ?? 0m;
            return FormatHours(total);
        }

        private static DateTime GetWeekStartDate(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }

        private static string FormatHours(decimal hours)
        {
            var h = (int)Math.Floor(hours);
            var m = (int)Math.Round((hours - h) * 60);
            if (m == 60) { h++; m = 0; }
            return $"{h}h {m:D2}m";
        }

        private void PurgeExpiredTokens()
        {
            var now = DateTime.UtcNow;
            var expired = PendingTokens
                .Where(kvp => kvp.Value.ExpiresUtc < now || (kvp.Value.Used && kvp.Value.ScannedAtUtc.HasValue
                    && kvp.Value.ScannedAtUtc.Value.AddMinutes(2) < now))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expired)
            {
                PendingTokens.TryRemove(key, out _);
            }
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

        private async Task NotifyAdmins(string title, string message, string link)
        {
            try
            {
                var adminIds = db.Users.ToList()
                    .Where(u => UserManager.IsInRole(u.Id, "Admin"))
                    .Select(u => u.Id)
                    .ToList();

                foreach (var adminId in adminIds)
                {
                    await CreateNotification(adminId, title, message, "Attendance", link);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error notifying admins: {ex.Message}");
            }
        }

        #endregion

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