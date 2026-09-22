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

        /// <summary>Rolling QR window length in seconds (5 minutes).</summary>
        private const int QrWindowSeconds = 300;

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
        /// Accepts current 5-minute window token OR previous window (edge-of-window).
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

                token = token.Trim().ToLowerInvariant();

                // Resolve staff by validating rolling token against active, non-revoked crew
                var crewCandidates = db.Staff
                    .Where(s => s.IsActive
                                && !s.QrTokenRevoked
                                && s.Role == "Crew Member"
                                && s.QrToken != null
                                && s.QrToken != "")
                    .ToList();

                Staff staff = null;
                DateTime? matchedWindowStart = null;
                foreach (var candidate in crewCandidates)
                {
                    if (IsTokenValidForStaff(candidate, token, out matchedWindowStart))
                    {
                        staff = candidate;
                        break;
                    }
                }

                if (staff == null)
                {
                    result.Message = "Unknown, expired, or invalid QR code.";
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

                var openRecord = db.AttendanceRecords
                    .Where(r => r.StaffId == staff.Id && r.CheckOutTime == null)
                    .OrderByDescending(r => r.CheckInTime)
                    .FirstOrDefault();

                int projectId;
                if (openRecord != null)
                {
                    projectId = openRecord.ProjectId;
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

                // ---- Idempotency: same staff + same scanner within 30 seconds ----
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
        // Rolling QR token – Crew Member JSON endpoints
        // ------------------------------------------------------------------

        /// <summary>
        /// Returns the current rolling QR payload + seconds remaining in the window.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Crew Member")]
        public ActionResult GetCurrentQrToken()
        {
            var userId = User.Identity.GetUserId();
            var staff = db.Staff.FirstOrDefault(s => s.UserId == userId && s.IsActive);
            if (staff == null || string.IsNullOrEmpty(staff.QrToken) || staff.QrTokenRevoked)
            {
                return Json(new
                {
                    success = false,
                    message = "No valid QR seed. Please regenerate your QR code."
                }, JsonRequestBehavior.AllowGet);
            }

            var utcNow = DateTime.UtcNow;
            var token = GenerateRollingToken(staff, utcNow);
            var secondsRemaining = GetSecondsRemainingInWindow(utcNow);

            return Json(new
            {
                success = true,
                token = token,
                secondsRemaining = secondsRemaining,
                windowSeconds = QrWindowSeconds
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Lightweight status for crew-member polling (real-time confirmation overlay).
        /// Returns the most recent attendance event for the current user.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Crew Member")]
        public ActionResult GetAttendanceStatus()
        {
            var userId = User.Identity.GetUserId();
            var staff = db.Staff.FirstOrDefault(s => s.UserId == userId && s.IsActive);
            if (staff == null)
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }

            var last = db.AttendanceRecords
                .Include(r => r.Project)
                .Where(r => r.StaffId == staff.Id)
                .OrderByDescending(r => r.CheckOutTime ?? r.CheckInTime)
                .FirstOrDefault();

            if (last == null)
            {
                return Json(new
                {
                    success = true,
                    hasEvent = false,
                    eventKey = (string)null
                }, JsonRequestBehavior.AllowGet);
            }

            var isOut = last.CheckOutTime.HasValue;
            var eventTime = isOut ? last.CheckOutTime.Value : last.CheckInTime;
            // Unique key so the client can detect a new scan without storing server state
            var eventKey = last.Id + ":" + (isOut ? "OUT" : "IN") + ":" + eventTime.Ticks;

            return Json(new
            {
                success = true,
                hasEvent = true,
                eventKey = eventKey,
                action = isOut ? "CheckOut" : "CheckIn",
                projectName = last.Project != null ? last.Project.ProjectName : "Unknown Project",
                timestamp = eventTime.ToString("HH:mm"),
                fullTimestamp = eventTime.ToString("dd MMM yyyy HH:mm:ss"),
                hoursWorked = isOut ? last.TotalHours : (decimal?)null
            }, JsonRequestBehavior.AllowGet);
        }

        // ------------------------------------------------------------------
        // Today's Attendance – Site Manager, Admin, Project Manager, Finance Officer
        // ------------------------------------------------------------------

        [Authorize(Roles = "Site Manager, Admin, Project Manager, Finance Officer")]
        public ActionResult Today(int? projectId = null, string status = null)
        {
            var eligibleProjectIds = GetEligibleProjectIdsForViewer();

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
        // History / report – Site Manager, Admin, Project Manager, Finance Officer
        // ------------------------------------------------------------------

        [Authorize(Roles = "Site Manager, Admin, Project Manager, Finance Officer")]
        public ActionResult History(DateTime? from = null, DateTime? to = null, int? projectId = null)
        {
            var eligibleProjectIds = GetEligibleProjectIdsForViewer();

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
        // Admin / Finance Officer – all attendance
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Finance Officer")]
        public ActionResult AllAttendance(DateTime? from = null, DateTime? to = null, int? projectId = null)
        {
            return History(from, to, projectId);
        }

        // ------------------------------------------------------------------
        // Crew Member – My Hours
        // ------------------------------------------------------------------

        [Authorize(Roles = "Crew Member")]
        public ActionResult MyHours(int? year = null, int? month = null)
        {
            var userId = User.Identity.GetUserId();
            var staff = db.Staff.FirstOrDefault(s => s.UserId == userId && s.IsActive);
            if (staff == null)
            {
                TempData["ErrorMessage"] = "Staff record not found.";
                return RedirectToAction("Dashboard", "Home");
            }

            var y = year ?? DateTime.Today.Year;
            var m = month ?? DateTime.Today.Month;
            var periodStart = new DateTime(y, m, 1);
            var periodEnd = periodStart.AddMonths(1);

            var records = db.AttendanceRecords
                .Include(r => r.Project)
                .Where(r => r.StaffId == staff.Id
                            && r.CheckInTime >= periodStart
                            && r.CheckInTime < periodEnd)
                .OrderByDescending(r => r.CheckInTime)
                .ToList()
                .Select(r => new AttendanceHistoryItemViewModel
                {
                    Id = r.Id,
                    StaffName = staff.FullName,
                    ProjectName = r.Project?.ProjectName ?? "Unknown",
                    CheckInTime = r.CheckInTime,
                    CheckOutTime = r.CheckOutTime,
                    TotalHours = r.TotalHours,
                    Status = r.CheckOutTime == null ? "IN" : "OUT",
                    StatusBadgeColor = r.CheckOutTime == null ? "success" : "secondary"
                }).ToList();

            var closedHours = records.Where(r => r.TotalHours.HasValue).Sum(r => r.TotalHours.Value);

            var viewModel = new MyHoursViewModel
            {
                StaffName = staff.FullName,
                Year = y,
                Month = m,
                Records = records,
                MonthHoursTotal = closedHours,
                SessionCount = records.Count,
                OpenSessionCount = records.Count(r => r.Status == "IN"),
                Years = Enumerable.Range(DateTime.Today.Year - 2, 5).Reverse()
                    .Select(yy => new SelectListItem
                    {
                        Value = yy.ToString(),
                        Text = yy.ToString(),
                        Selected = yy == y
                    }).ToList(),
                Months = Enumerable.Range(1, 12)
                    .Select(mm => new SelectListItem
                    {
                        Value = mm.ToString(),
                        Text = new DateTime(2000, mm, 1).ToString("MMMM"),
                        Selected = mm == m
                    }).ToList()
            };

            return View(viewModel);
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

            if (string.IsNullOrEmpty(staff.QrToken) || staff.QrTokenRevoked)
            {
                GenerateNewToken(staff);
                db.SaveChanges();
            }

            // Initial token for first paint; client will refresh every window via GetCurrentQrToken
            var utcNow = DateTime.UtcNow;
            var rollingToken = GenerateRollingToken(staff, utcNow);
            var qrBase64 = GenerateQrPngBase64(rollingToken);
            var secondsRemaining = GetSecondsRemainingInWindow(utcNow);

            var viewModel = new MyQrViewModel
            {
                StaffName = staff.FullName,
                EmployeeId = staff.EmployeeId,
                QrToken = rollingToken,
                QrImageBase64 = qrBase64,
                TokenCreatedDate = staff.QrTokenCreatedDate,
                HasToken = true,
                SecondsRemaining = secondsRemaining,
                WindowSeconds = QrWindowSeconds
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

            staff.QrTokenRevoked = true;
            GenerateNewToken(staff);
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your QR code has been regenerated. The old code no longer works.";
            return RedirectToAction("MyQr");
        }

        // ------------------------------------------------------------------
        // Helpers – rolling token
        // ------------------------------------------------------------------

        /// <summary>
        /// Deterministic rolling token for a staff member at a given UTC instant.
        /// HMAC-SHA256(seed = QrToken + Id, message = floor(unixSeconds / 300)) → 32 hex chars.
        /// </summary>
        private static string GenerateRollingToken(Staff staff, DateTime forUtcTime)
        {
            if (staff == null || string.IsNullOrEmpty(staff.QrToken))
                return null;

            var utc = forUtcTime.Kind == DateTimeKind.Utc
                ? forUtcTime
                : forUtcTime.ToUniversalTime();

            var unixSeconds = (long)(utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            var windowIndex = unixSeconds / QrWindowSeconds;

            var keyMaterial = Encoding.UTF8.GetBytes(staff.QrToken + staff.Id.ToString());
            var message = Encoding.UTF8.GetBytes(windowIndex.ToString());

            using (var hmac = new HMACSHA256(keyMaterial))
            {
                var hash = hmac.ComputeHash(message);
                // First 16 bytes → 32 hex chars (compact QR payload)
                var sb = new StringBuilder(32);
                for (int i = 0; i < 16; i++)
                    sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>
        /// Validates token against current window and previous window (edge-of-window scans).
        /// </summary>
        private static bool IsTokenValidForStaff(Staff staff, string token, out DateTime? matchedWindowStart)
        {
            matchedWindowStart = null;
            if (staff == null || string.IsNullOrEmpty(staff.QrToken) || staff.QrTokenRevoked)
                return false;
            if (string.IsNullOrWhiteSpace(token))
                return false;

            token = token.Trim().ToLowerInvariant();
            var utcNow = DateTime.UtcNow;

            // Current window
            var current = GenerateRollingToken(staff, utcNow);
            if (string.Equals(current, token, StringComparison.OrdinalIgnoreCase))
            {
                matchedWindowStart = GetWindowStartUtc(utcNow);
                return true;
            }

            // Previous window (allow scans near rotation)
            var previousUtc = utcNow.AddSeconds(-QrWindowSeconds);
            var previous = GenerateRollingToken(staff, previousUtc);
            if (string.Equals(previous, token, StringComparison.OrdinalIgnoreCase))
            {
                matchedWindowStart = GetWindowStartUtc(previousUtc);
                return true;
            }

            return false;
        }

        private static DateTime GetWindowStartUtc(DateTime utc)
        {
            var unixSeconds = (long)(utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            var windowIndex = unixSeconds / QrWindowSeconds;
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(windowIndex * QrWindowSeconds);
        }

        private static int GetSecondsRemainingInWindow(DateTime utcNow)
        {
            var unixSeconds = (long)(utcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            var elapsedInWindow = (int)(unixSeconds % QrWindowSeconds);
            return QrWindowSeconds - elapsedInWindow;
        }

        // ------------------------------------------------------------------
        // Helpers – project eligibility
        // ------------------------------------------------------------------

        /// <summary>
        /// Projects the current scanner may operate on (Site Manager / Admin).
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

        /// <summary>
        /// Projects the current viewer may see attendance for.
        /// Admin / Finance Officer: all active.
        /// Project Manager: managed projects.
        /// Site Manager: supervised + allocations.
        /// </summary>
        private List<int> GetEligibleProjectIdsForViewer()
        {
            // Admin and Finance Officer see every active project
            if (User.IsInRole("Admin") || User.IsInRole("Finance Officer"))
            {
                return db.Projects.Where(p => p.IsActive).Select(p => p.Id).ToList();
            }

            var userId = User.Identity.GetUserId();
            var currentStaff = db.Staff.FirstOrDefault(s => s.UserId == userId && s.IsActive);
            if (currentStaff == null)
                return new List<int>();

            if (User.IsInRole("Project Manager"))
            {
                return db.Projects
                    .Where(p => p.ProjectManagerId == currentStaff.Id && p.IsActive)
                    .Select(p => p.Id)
                    .ToList();
            }

            // Site Manager (and any dual-role users)
            return GetEligibleProjectIdsForScanner(currentStaff, false);
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