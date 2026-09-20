// ============ Controllers/AttendanceController.cs ============
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MuhleOP.Models;
using MuhleOP.ViewModels;
using QRCoder;

namespace MuhleOP.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
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
        // Scan page (camera UI) – Site Manager + Admin
        // ------------------------------------------------------------------

        [Authorize(Roles = "Site Manager, Admin")]
        public ActionResult Scan()
        {
            return View();
        }

        /// <summary>
        /// Processes a scanned QR token. Returns JSON for the client-side scanner.
        /// POST body: token=...  (or form field "token")
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Site Manager, Admin")]
        public async Task<ActionResult> Scan(string token)
        {
            var result = new AttendanceScanResultViewModel { Success = false };

            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    result.Message = "No QR token received.";
                    return Json(result);
                }

                token = token.Trim();

                var staff = db.Staff.FirstOrDefault(s => s.QrToken == token);
                if (staff == null)
                {
                    result.Message = "Unknown or invalid QR code.";
                    return Json(result);
                }

                if (staff.QrTokenRevoked)
                {
                    result.Message = "This QR code has been revoked. Ask the crew member to regenerate it.";
                    return Json(result);
                }

                if (!staff.IsActive)
                {
                    result.Message = $"{staff.FullName} is not an active staff member.";
                    return Json(result);
                }

                if (staff.Role != "Crew Member")
                {
                    result.Message = "Only Crew Members can be checked in via QR.";
                    return Json(result);
                }

                var scannerUserId = User.Identity.GetUserId();
                var isAdmin = User.IsInRole("Admin");

                // Resolve the Site Manager's Staff record (Admins may not have one)
                Staff scannerStaff = null;
                if (!isAdmin)
                {
                    scannerStaff = db.Staff.FirstOrDefault(s => s.UserId == scannerUserId && s.IsActive);
                    if (scannerStaff == null)
                    {
                        result.Message = "Your staff profile could not be found.";
                        return Json(result);
                    }
                }

                // ---- Project eligibility ----
                // A crew member may be scanned if they have an ACTIVE allocation on a
                // project that the Site Manager supervises (Project.SiteManagerId) OR
                // on which the Site Manager has an active "Site Manager" allocation.
                // Admin may scan anyone on any of their active projects.
                var today = DateTime.Today;
                var eligibleProjectIds = GetEligibleProjectIdsForScanner(scannerStaff, isAdmin);

                var crewAllocations = db.ProjectResourceAllocations
                    .Where(a => a.StaffId == staff.Id
                                && a.IsActive
                                && a.StartDate <= today
                                && (a.EndDate == null || a.EndDate >= today)
                                && eligibleProjectIds.Contains(a.ProjectId))
                    .OrderByDescending(a => a.StartDate)
                    .ToList();

                if (!crewAllocations.Any())
                {
                    result.Message = isAdmin
                        ? $"{staff.FullName} has no active project assignment."
                        : $"{staff.FullName} is not assigned to any of your projects today.";
                    return Json(result);
                }

                // Prefer the project that already has an open check-in; otherwise the most recent allocation
                var openRecord = db.AttendanceRecords
                    .Where(r => r.StaffId == staff.Id && r.CheckOutTime == null)
                    .OrderByDescending(r => r.CheckInTime)
                    .FirstOrDefault();

                int projectId;
                if (openRecord != null)
                {
                    projectId = openRecord.ProjectId;
                    // Still verify the open session is on an eligible project
                    if (!eligibleProjectIds.Contains(projectId) && !isAdmin)
                    {
                        result.Message = "Open check-in is on a project you do not supervise.";
                        return Json(result);
                    }
                }
                else
                {
                    projectId = crewAllocations.First().ProjectId;
                }

                var project = db.Projects.Find(projectId);
                var projectName = project?.ProjectName ?? "Unknown Project";

                // ---- Idempotency: same token + same scanner within 30 seconds ----
                var thirtySecondsAgo = DateTime.Now.AddSeconds(-30);
                var recentSame = db.AttendanceRecords
                    .Where(r => r.StaffId == staff.Id
                                && (r.CheckInByUserId == scannerUserId || r.CheckOutByUserId == scannerUserId)
                                && (
                                    (r.CheckOutTime == null && r.CheckInTime >= thirtySecondsAgo)
                                    || (r.CheckOutTime != null && r.CheckOutTime >= thirtySecondsAgo)
                                ))
                    .OrderByDescending(r => r.CheckOutTime ?? r.CheckInTime)
                    .FirstOrDefault();

                if (recentSame != null)
                {
                    // Return the same result without toggling
                    bool wasCheckOut = recentSame.CheckOutTime.HasValue
                                       && recentSame.CheckOutTime >= thirtySecondsAgo;
                    result.Success = true;
                    result.Action = wasCheckOut ? "CheckOut" : "CheckIn";
                    result.StaffName = staff.FullName;
                    result.StaffPhotoUrl = staff.ProfilePicturePath;
                    result.ProjectName = projectName;
                    result.Timestamp = (wasCheckOut ? recentSame.CheckOutTime : recentSame.CheckInTime)
                                       .Value.ToString("dd MMM yyyy HH:mm:ss");
                    result.HoursWorked = wasCheckOut ? recentSame.TotalHours : null;
                    result.Message = wasCheckOut
                        ? $"Already checked out {staff.FullName} a moment ago."
                        : $"Already checked in {staff.FullName} a moment ago.";
                    return Json(result);
                }

                // ---- Check-in or Check-out ----
                if (openRecord == null)
                {
                    // CHECK-IN
                    var record = new AttendanceRecord
                    {
                        StaffId = staff.Id,
                        ProjectId = projectId,
                        CheckInTime = DateTime.Now,
                        CheckInByUserId = scannerUserId,
                        CheckInMethod = "QR",
                        CreatedDate = DateTime.Now
                    };
                    db.AttendanceRecords.Add(record);
                    await db.SaveChangesAsync();

                    result.Success = true;
                    result.Action = "CheckIn";
                    result.StaffName = staff.FullName;
                    result.StaffPhotoUrl = staff.ProfilePicturePath;
                    result.ProjectName = projectName;
                    result.Timestamp = record.CheckInTime.ToString("dd MMM yyyy HH:mm:ss");
                    result.Message = $"{staff.FullName} checked IN on {projectName}.";

                    await NotifyStaffOfAttendance(staff.UserId, "Checked In",
                        $"You were checked in on project '{projectName}' at {record.CheckInTime:HH:mm}.");
                }
                else
                {
                    // CHECK-OUT
                    openRecord.CheckOutTime = DateTime.Now;
                    openRecord.CheckOutByUserId = scannerUserId;
                    openRecord.CheckOutMethod = "QR";

                    var span = openRecord.CheckOutTime.Value - openRecord.CheckInTime;
                    openRecord.TotalHours = Math.Round((decimal)span.TotalHours, 2);

                    await db.SaveChangesAsync();

                    result.Success = true;
                    result.Action = "CheckOut";
                    result.StaffName = staff.FullName;
                    result.StaffPhotoUrl = staff.ProfilePicturePath;
                    result.ProjectName = projectName;
                    result.Timestamp = openRecord.CheckOutTime.Value.ToString("dd MMM yyyy HH:mm:ss");
                    result.HoursWorked = openRecord.TotalHours;
                    result.Message = $"{staff.FullName} checked OUT from {projectName}. Hours: {openRecord.TotalHours:N2}h.";

                    await NotifyStaffOfAttendance(staff.UserId, "Checked Out",
                        $"You were checked out from project '{projectName}' at {openRecord.CheckOutTime:HH:mm}. Total: {openRecord.TotalHours:N2} hours.");
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Attendance Scan error: {ex}");
                result.Message = "An error occurred while processing the scan.";
                return Json(result);
            }
        }

        // ------------------------------------------------------------------
        // Today's Attendance – Site Manager + Admin
        // ------------------------------------------------------------------

        [Authorize(Roles = "Site Manager, Admin")]
        public ActionResult Today(int? projectId = null, string status = null)
        {
            var isAdmin = User.IsInRole("Admin");
            Staff scannerStaff = null;
            if (!isAdmin)
            {
                var userId = User.Identity.GetUserId();
                scannerStaff = db.Staff.FirstOrDefault(s => s.UserId == userId && s.IsActive);
            }

            var eligibleProjectIds = GetEligibleProjectIdsForScanner(scannerStaff, isAdmin);

            var startOfDay = DateTime.Today;
            var endOfDay = startOfDay.AddDays(1);

            var query = db.AttendanceRecords
                .Include(r => r.Staff)
                .Include(r => r.Project)
                .Where(r => r.CheckInTime >= startOfDay && r.CheckInTime < endOfDay
                            && eligibleProjectIds.Contains(r.ProjectId));

            if (projectId.HasValue)
                query = query.Where(r => r.ProjectId == projectId.Value);

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("IN", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(r => r.CheckOutTime == null);
                else if (status.Equals("OUT", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(r => r.CheckOutTime != null);
            }

            var records = query
                .OrderByDescending(r => r.CheckInTime)
                .ToList()
                .Select(r => new AttendanceTodayItemViewModel
                {
                    Id = r.Id,
                    StaffName = r.Staff?.FullName ?? "Unknown",
                    StaffPhotoUrl = r.Staff?.ProfilePicturePath,
                    ProjectName = r.Project?.ProjectName ?? "Unknown",
                    CheckInTime = r.CheckInTime,
                    CheckOutTime = r.CheckOutTime,
                    TotalHours = r.TotalHours,
                    Status = r.CheckOutTime == null ? "IN" : "OUT",
                    StatusBadgeColor = r.CheckOutTime == null ? "success" : "secondary",
                    CheckInBy = r.CheckInByUserId
                }).ToList();

            var viewModel = new AttendanceTodayViewModel
            {
                Records = records,
                CurrentlyInCount = records.Count(r => r.Status == "IN"),
                CheckedOutCount = records.Count(r => r.Status == "OUT"),
                TotalSessions = records.Count,
                FilterProjectId = projectId,
                FilterStatus = status,
                Projects = GetProjectSelectList(eligibleProjectIds, projectId)
            };

            return View(viewModel);
        }

        // ------------------------------------------------------------------
        // History / report – Site Manager, Admin, Project Manager
        // ------------------------------------------------------------------

        [Authorize(Roles = "Site Manager, Admin, Project Manager")]
        public ActionResult History(DateTime? from = null, DateTime? to = null, int? projectId = null)
        {
            var isAdmin = User.IsInRole("Admin");
            var isPm = User.IsInRole("Project Manager");
            Staff currentStaff = null;
            var userId = User.Identity.GetUserId();
            currentStaff = db.Staff.FirstOrDefault(s => s.UserId == userId && s.IsActive);

            List<int> eligibleProjectIds;
            if (isAdmin)
            {
                eligibleProjectIds = db.Projects.Where(p => p.IsActive).Select(p => p.Id).ToList();
            }
            else if (isPm && currentStaff != null)
            {
                eligibleProjectIds = db.Projects
                    .Where(p => p.ProjectManagerId == currentStaff.Id && p.IsActive)
                    .Select(p => p.Id)
                    .ToList();
            }
            else
            {
                eligibleProjectIds = GetEligibleProjectIdsForScanner(currentStaff, false);
            }

            var fromDate = from ?? DateTime.Today.AddDays(-7);
            var toDate = (to ?? DateTime.Today).Date.AddDays(1); // exclusive end

            var query = db.AttendanceRecords
                .Include(r => r.Staff)
                .Include(r => r.Project)
                .Where(r => r.CheckInTime >= fromDate
                            && r.CheckInTime < toDate
                            && eligibleProjectIds.Contains(r.ProjectId));

            if (projectId.HasValue)
                query = query.Where(r => r.ProjectId == projectId.Value);

            var list = query
                .OrderByDescending(r => r.CheckInTime)
                .ToList()
                .Select(r => new AttendanceHistoryItemViewModel
                {
                    Id = r.Id,
                    StaffName = r.Staff?.FullName ?? "Unknown",
                    ProjectName = r.Project?.ProjectName ?? "Unknown",
                    CheckInTime = r.CheckInTime,
                    CheckOutTime = r.CheckOutTime,
                    TotalHours = r.TotalHours,
                    Status = r.CheckOutTime == null ? "IN" : "OUT",
                    StatusBadgeColor = r.CheckOutTime == null ? "success" : "secondary"
                }).ToList();

            var viewModel = new AttendanceHistoryViewModel
            {
                Records = list,
                FromDate = from ?? DateTime.Today.AddDays(-7),
                ToDate = to ?? DateTime.Today,
                ProjectId = projectId,
                Projects = GetProjectSelectList(eligibleProjectIds, projectId),
                TotalHoursSum = list.Where(r => r.TotalHours.HasValue).Sum(r => r.TotalHours.Value),
                SessionCount = list.Count
            };

            return View(viewModel);
        }

        // ------------------------------------------------------------------
        // Admin – all attendance
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin")]
        public ActionResult AllAttendance(DateTime? from = null, DateTime? to = null, int? projectId = null)
        {
            // Re-use History view with all projects
            return History(from, to, projectId);
        }

        // ------------------------------------------------------------------
        // Crew Member – My QR code
        // ------------------------------------------------------------------

        [Authorize(Roles = "Crew Member")]
        public ActionResult MyQr()
        {
            var userId = User.Identity.GetUserId();
            var staff = db.Staff.FirstOrDefault(s => s.UserId == userId && s.IsActive);
            if (staff == null)
            {
                TempData["ErrorMessage"] = "Staff record not found.";
                return RedirectToAction("Dashboard", "Home");
            }

            // Ensure a token exists
            if (string.IsNullOrEmpty(staff.QrToken) || staff.QrTokenRevoked)
            {
                GenerateNewToken(staff);
                db.SaveChanges();
            }

            var qrBase64 = GenerateQrPngBase64(staff.QrToken);

            var viewModel = new MyQrViewModel
            {
                StaffName = staff.FullName,
                EmployeeId = staff.EmployeeId,
                QrToken = staff.QrToken,
                QrImageBase64 = qrBase64,
                TokenCreatedDate = staff.QrTokenCreatedDate,
                HasToken = true
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Crew Member")]
        public async Task<ActionResult> RegenerateToken()
        {
            var userId = User.Identity.GetUserId();
            var staff = db.Staff.FirstOrDefault(s => s.UserId == userId && s.IsActive);
            if (staff == null)
            {
                TempData["ErrorMessage"] = "Staff record not found.";
                return RedirectToAction("Dashboard", "Home");
            }

            // Revoke old token then issue a new one
            staff.QrTokenRevoked = true;
            GenerateNewToken(staff);
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your QR code has been regenerated. The old code no longer works.";
            return RedirectToAction("MyQr");
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Projects the current scanner may operate on.
        /// Site Manager: projects where they are SiteManagerId OR have an active
        /// "Site Manager" resource allocation.
        /// Admin: all active projects.
        /// </summary>
        private List<int> GetEligibleProjectIdsForScanner(Staff scannerStaff, bool isAdmin)
        {
            if (isAdmin)
            {
                return db.Projects.Where(p => p.IsActive).Select(p => p.Id).ToList();
            }

            if (scannerStaff == null)
                return new List<int>();

            var bySiteManagerId = db.Projects
                .Where(p => p.SiteManagerId == scannerStaff.Id && p.IsActive)
                .Select(p => p.Id);

            var byAllocation = db.ProjectResourceAllocations
                .Where(a => a.StaffId == scannerStaff.Id
                            && a.IsActive
                            && a.Role == "Site Manager"
                            && a.StartDate <= DateTime.Today
                            && (a.EndDate == null || a.EndDate >= DateTime.Today))
                .Select(a => a.ProjectId);

            return bySiteManagerId.Union(byAllocation).Distinct().ToList();
        }

        private List<SelectListItem> GetProjectSelectList(List<int> projectIds, int? selected)
        {
            return db.Projects
                .Where(p => projectIds.Contains(p.Id))
                .OrderBy(p => p.ProjectName)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.ProjectName,
                    Selected = selected.HasValue && p.Id == selected.Value
                }).ToList();
        }

        private void GenerateNewToken(Staff staff)
        {
            // 32 random bytes → 64 hex chars (unguessable)
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            staff.QrToken = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            staff.QrTokenCreatedDate = DateTime.Now;
            staff.QrTokenRevoked = false;
        }

        private string GenerateQrPngBase64(string payload)
        {
            using (var generator = new QRCodeGenerator())
            using (var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCode(data))
            using (var bitmap = qrCode.GetGraphic(8))
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
            }
        }

        private async Task NotifyStaffOfAttendance(string userId, string title, string message)
        {
            if (string.IsNullOrEmpty(userId)) return;
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = "System",
                    Link = "/Attendance/MyQr",
                    CreatedDate = DateTime.Now,
                    IsRead = false
                };
                db.Notifications.Add(notification);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Attendance notification error: {ex.Message}");
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