// ============ Controllers/DutyRosterController.cs ============
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MuhleOP.Helpers;
using MuhleOP.Models;
using MuhleOP.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MuhleOP.Controllers
{
    [Authorize]
    public class DutyRosterController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        private ApplicationUserManager _userManager;
        private ApplicationUserManager UserManager
        {
            get { return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>(); }
        }

        // ------------------------------------------------------------------
        // Index
        // ------------------------------------------------------------------

        [Authorize(Roles = "Project Manager, Admin, Site Manager")]
        public ActionResult Index(string status = null, int? projectId = null, DateTime? from = null, DateTime? to = null)
        {
            var staff = ResolveCurrentStaff();

            IQueryable<DutyRoster> query = db.DutyRosters
                .Include(r => r.Project)
                .Include(r => r.Shifts.Select(s => s.Assignments))
                .Where(r => !r.IsDeleted);

            if (User.IsInRole("Admin"))
            {
                // all
            }
            else if (User.IsInRole("Project Manager") && staff != null)
            {
                int pmStaffId = staff.Id;
                query = query.Where(r => r.Project.ProjectManagerId == pmStaffId);
            }
            else if (User.IsInRole("Site Manager") && staff != null)
            {
                int smStaffId = staff.Id;
                query = query.Where(r => r.Project.SiteManagerId == smStaffId);
            }
            else
            {
                query = query.Where(r => false);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                string statusFilter = status;
                query = query.Where(r => r.Status == statusFilter);
            }

            if (projectId.HasValue)
            {
                int pid = projectId.Value;
                query = query.Where(r => r.ProjectId == pid);
            }

            if (from.HasValue)
            {
                DateTime fromDate = from.Value.Date;
                query = query.Where(r => r.PeriodEnd >= fromDate);
            }

            if (to.HasValue)
            {
                DateTime toDate = to.Value.Date;
                query = query.Where(r => r.PeriodStart <= toDate);
            }

            List<DutyRoster> list = query.OrderByDescending(r => r.CreatedDate).ToList();

            bool isPm = User.IsInRole("Project Manager") && staff != null;
            List<int> managedProjectIds = new List<int>();
            if (isPm)
            {
                int pmId = staff.Id;
                managedProjectIds = db.Projects
                    .Where(p => p.ProjectManagerId == pmId && p.IsActive)
                    .Select(p => p.Id)
                    .ToList();
            }

            List<DutyRosterListItemViewModel> items = new List<DutyRosterListItemViewModel>();
            foreach (var r in list)
            {
                int shiftCount = r.Shifts != null ? r.Shifts.Count : 0;
                int assigned = 0;
                if (r.Shifts != null)
                {
                    foreach (var s in r.Shifts)
                    {
                        if (s.Assignments != null)
                            assigned += s.Assignments.Count(a => a.IsActive);
                    }
                }

                bool canManage = isPm && managedProjectIds.Contains(r.ProjectId);

                items.Add(new DutyRosterListItemViewModel
                {
                    Id = r.Id,
                    ProjectName = r.Project != null ? r.Project.ProjectName : null,
                    ProjectCode = r.Project != null ? r.Project.ProjectCode : null,
                    PeriodLabel = r.PeriodStart.ToString("dd MMM") + " – " + r.PeriodEnd.ToString("dd MMM yyyy"),
                    Status = r.Status,
                    StatusBadgeColor = GetStatusBadgeColor(r.Status),
                    Version = r.Version,
                    CreatedBy = r.CreatedBy,
                    CreatedDate = r.CreatedDate,
                    PublishedDate = r.PublishedDate,
                    ShiftCount = shiftCount,
                    AssignedStaffCount = assigned,
                    CanEdit = canManage && r.Status == "Draft",
                    CanDelete = canManage && (r.Status == "Draft" || (r.Status == "Published" && !HasAttendanceAgainstRoster(r.Id))),
                    CanRevise = canManage && r.Status == "Published" && r.IsActive
                });
            }

            DateTime weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday);
            if (weekStart > DateTime.Today) weekStart = weekStart.AddDays(-7);
            DateTime weekEnd = weekStart.AddDays(7);

            decimal totalHours = 0m;
            foreach (var r in list)
            {
                if (r.Status != "Published" || !r.IsActive || r.Shifts == null) continue;
                foreach (var s in r.Shifts)
                {
                    if (s.ShiftDate >= weekStart && s.ShiftDate < weekEnd)
                    {
                        decimal hours = RosterValidationHelper.CalculateShiftHours(s.StartTime, s.EndTime);
                        int staffCount = s.Assignments != null ? s.Assignments.Count(a => a.IsActive) : 0;
                        totalHours += hours * staffCount;
                    }
                }
            }

            var vm = new DutyRosterIndexViewModel
            {
                Rosters = items,
                StatusFilter = status,
                ProjectIdFilter = projectId,
                FromDate = from,
                ToDate = to,
                DraftCount = list.Count(r => r.Status == "Draft"),
                PublishedCount = list.Count(r => r.Status == "Published"),
                SupersededCount = list.Count(r => r.Status == "Superseded"),
                TotalHoursThisWeek = totalHours,
                CanCreate = isPm && managedProjectIds.Count > 0,
                Projects = GetProjectSelectListForViewer(staff),
                Statuses = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "All Statuses" },
                    new SelectListItem { Value = "Draft", Text = "Draft" },
                    new SelectListItem { Value = "Published", Text = "Published" },
                    new SelectListItem { Value = "Superseded", Text = "Superseded" }
                }
            };

            return View(vm);
        }

        // ------------------------------------------------------------------
        // Details
        // ------------------------------------------------------------------

        [Authorize(Roles = "Project Manager, Admin, Site Manager")]
        public ActionResult Details(int id)
        {
            var roster = db.DutyRosters
                .Include(r => r.Project)
                .Include(r => r.Shifts.Select(s => s.Assignments.Select(a => a.Staff)))
                .FirstOrDefault(r => r.Id == id && !r.IsDeleted);

            if (roster == null)
            {
                TempData["ErrorMessage"] = "Roster not found.";
                return RedirectToAction("Index");
            }

            if (!UserCanViewRoster(roster))
            {
                TempData["ErrorMessage"] = "You do not have permission to view this roster.";
                return RedirectToAction("Index");
            }

            var staff = ResolveCurrentStaff();
            bool canManage = User.IsInRole("Project Manager") && staff != null
                             && roster.Project != null
                             && roster.Project.ProjectManagerId == staff.Id;

            var shifts = (roster.Shifts ?? Enumerable.Empty<Shift>())
                .OrderBy(s => s.ShiftDate)
                .ThenBy(s => s.StartTime)
                .ToList();

            var shiftsByDate = shifts
                .GroupBy(s => s.ShiftDate.Date)
                .Select(g => new ShiftDayGroupViewModel
                {
                    Date = g.Key,
                    DateLabel = g.Key.ToString("dddd, dd MMMM yyyy"),
                    Shifts = g.Select(s => new ShiftDetailItemViewModel
                    {
                        Id = s.Id,
                        StartTime = s.StartTime.ToString(@"hh\:mm"),
                        EndTime = s.EndTime.ToString(@"hh\:mm"),
                        ShiftRole = s.ShiftRole,
                        SiteLocation = s.SiteLocation,
                        RequiredStaffCount = s.RequiredStaffCount,
                        IsOvertime = s.IsOvertime,
                        IsSundayShift = s.IsSundayShift,
                        IsHolidayShift = s.IsHolidayShift,
                        Notes = s.Notes,
                        AssignedStaffNames = (s.Assignments ?? Enumerable.Empty<ShiftAssignment>())
                            .Where(a => a.IsActive)
                            .Select(a => a.Staff != null ? a.Staff.FullName : "Unknown")
                            .ToList()
                    }).ToList()
                }).ToList();

            int projectId = roster.ProjectId;
            DateTime periodStart = roster.PeriodStart;
            DateTime periodEnd = roster.PeriodEnd;

            var historyRows = db.DutyRosters
                .Where(r => !r.IsDeleted
                            && r.ProjectId == projectId
                            && r.PeriodStart == periodStart
                            && r.PeriodEnd == periodEnd)
                .OrderByDescending(r => r.Version)
                .ToList();

            var history = historyRows.Select(r => new RosterVersionItemViewModel
            {
                Id = r.Id,
                Version = r.Version,
                Status = r.Status,
                StatusBadgeColor = GetStatusBadgeColor(r.Status),
                CreatedDate = r.CreatedDate,
                CreatedBy = r.CreatedBy,
                PublishedDate = r.PublishedDate
            }).ToList();

            var vm = new DutyRosterDetailsViewModel
            {
                Id = roster.Id,
                ProjectId = roster.ProjectId,
                ProjectName = roster.Project != null ? roster.Project.ProjectName : null,
                ProjectCode = roster.Project != null ? roster.Project.ProjectCode : null,
                PeriodStart = roster.PeriodStart,
                PeriodEnd = roster.PeriodEnd,
                Status = roster.Status,
                StatusBadgeColor = GetStatusBadgeColor(roster.Status),
                Version = roster.Version,
                Notes = roster.Notes,
                CreatedBy = roster.CreatedBy,
                CreatedDate = roster.CreatedDate,
                PublishedBy = roster.PublishedBy,
                PublishedDate = roster.PublishedDate,
                ShiftsByDate = shiftsByDate,
                VersionHistory = history,
                CanManage = canManage,
                CanEdit = canManage && roster.Status == "Draft",
                CanPublish = canManage && roster.Status == "Draft",
                CanRevise = canManage && roster.Status == "Published" && roster.IsActive,
                CanDelete = canManage && (roster.Status == "Draft" || (roster.Status == "Published" && !HasAttendanceAgainstRoster(roster.Id)))
            };

            return View(vm);
        }

        // ------------------------------------------------------------------
        // Create
        // ------------------------------------------------------------------

        [Authorize(Roles = "Project Manager")]
        public ActionResult Create(int? projectId = null)
        {
            var staff = ResolveCurrentStaff();
            if (staff == null)
            {
                TempData["ErrorMessage"] = "Staff record not found.";
                return RedirectToAction("Dashboard", "Home");
            }

            var model = new CreateDutyRosterViewModel
            {
                ProjectId = projectId ?? 0,
                Projects = GetManagedProjectSelectList(staff.Id),
                AvailableStaff = projectId.HasValue
                    ? GetAssignedCrewCheckboxes(projectId.Value, null)
                    : new List<RosterStaffCheckboxViewModel>()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Project Manager")]
        public async Task<ActionResult> Create(CreateDutyRosterViewModel model)
        {
            var staff = ResolveCurrentStaff();
            if (staff == null || !IsProjectManagerForProject(model.ProjectId, staff.Id))
            {
                TempData["ErrorMessage"] = "You may only create rosters for projects you manage.";
                return RedirectToAction("Index");
            }

            model.Projects = GetManagedProjectSelectList(staff.Id);
            model.AvailableStaff = GetAssignedCrewCheckboxes(model.ProjectId, model.SelectedStaffIds);

            if (model.PeriodEnd < model.PeriodStart)
                ModelState.AddModelError("PeriodEnd", "Period end must be on or after period start.");

            if (model.SelectedStaffIds == null || model.SelectedStaffIds.Count == 0)
                ModelState.AddModelError("SelectedStaffIds", "Select at least one crew member assigned to this project.");

            TimeSpan startTs, endTs;
            if (!TimeSpan.TryParse(model.DefaultStartTime, out startTs) || !TimeSpan.TryParse(model.DefaultEndTime, out endTs))
                ModelState.AddModelError("DefaultStartTime", "Invalid start or end time.");
            else if (endTs <= startTs)
                ModelState.AddModelError("DefaultEndTime", "End time must be after start time.");

            // Free unique index slots left by previous soft-deletes
            await PurgeSoftDeletedRostersForPeriodAsync(model.ProjectId, model.PeriodStart.Date, model.PeriodEnd.Date);

            if (RosterPeriodAlreadyExists(model.ProjectId, model.PeriodStart.Date, model.PeriodEnd.Date, null))
            {
                ModelState.AddModelError("", "A duty roster already exists for this project that overlaps the selected dates. Edit or revise the existing roster, or choose different dates.");
            }

            if (model.SelectedStaffIds != null && model.SelectedStaffIds.Count > 0)
            {
                List<string> invalid = GetStaffNotAssignedToProject(model.ProjectId, model.SelectedStaffIds, model.PeriodStart.Date, model.PeriodEnd.Date);
                if (invalid.Count > 0)
                    ModelState.AddModelError("SelectedStaffIds", "These crew members are not actively assigned to this project for the selected period: " + string.Join(", ", invalid));
            }

            if (!ModelState.IsValid)
                return View(model);

            model.Shifts = AutoGenerateShifts(model);
            if (model.Shifts == null || model.Shifts.Count == 0)
            {
                ModelState.AddModelError("", "No shifts were generated (check period and weekdays-only setting).");
                return View(model);
            }

            bool publish = string.Equals(model.SubmitAction, "Publish", StringComparison.OrdinalIgnoreCase);

            RosterValidationResultViewModel validation = ValidateRosterInput(model, staff.Id);
            if (validation.HasBlockers)
            {
                foreach (string b in validation.Blockers)
                    ModelState.AddModelError("", b);
                return View(model);
            }

            int nextVersion = GetNextRosterVersion(model.ProjectId, model.PeriodStart.Date, model.PeriodEnd.Date);

            var roster = new DutyRoster
            {
                ProjectId = model.ProjectId,
                PeriodStart = model.PeriodStart.Date,
                PeriodEnd = model.PeriodEnd.Date,
                Status = publish ? "Published" : "Draft",
                Version = nextVersion,
                IsActive = true,
                Notes = model.Notes,
                CreatedBy = User.Identity.GetUserId(),
                CreatedDate = DateTime.Now
            };

            if (publish)
            {
                roster.PublishedBy = User.Identity.GetUserId();
                roster.PublishedDate = DateTime.Now;
            }

            db.DutyRosters.Add(roster);
            await db.SaveChangesAsync();

            await SaveShiftsFromInput(roster, model.Shifts);
            await db.SaveChangesAsync();

            if (publish)
            {
                await NotifyOnPublish(roster);
                TempData["SuccessMessage"] = "Duty roster published successfully. Crew members have been notified.";
            }
            else
            {
                TempData["SuccessMessage"] = "Duty roster saved as draft (" + model.Shifts.Count + " shifts auto-generated).";
            }

            return RedirectToAction("Details", new { id = roster.Id });
        }

        [HttpGet]
        [Authorize(Roles = "Project Manager")]
        public ActionResult GetAssignedCrew(int projectId)
        {
            var staff = ResolveCurrentStaff();
            if (staff == null || !IsProjectManagerForProject(projectId, staff.Id))
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            var list = GetAssignedCrewCheckboxes(projectId, null)
                .Select(s => new
                {
                    StaffId = s.StaffId,
                    FullName = s.FullName,
                    Role = s.Role,
                    EmployeeId = s.EmployeeId
                }).ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        // ------------------------------------------------------------------
        // Edit
        // ------------------------------------------------------------------

        [Authorize(Roles = "Project Manager")]
        public ActionResult Edit(int id)
        {
            var staff = ResolveCurrentStaff();
            var roster = db.DutyRosters
                .Include(r => r.Shifts.Select(s => s.Assignments))
                .FirstOrDefault(r => r.Id == id && !r.IsDeleted);

            if (roster == null || staff == null || !IsProjectManagerForProject(roster.ProjectId, staff.Id))
            {
                TempData["ErrorMessage"] = "Roster not found or access denied.";
                return RedirectToAction("Index");
            }

            if (roster.Status != "Draft")
            {
                TempData["ErrorMessage"] = "Only draft rosters can be edited. Published rosters must be revised.";
                return RedirectToAction("Details", new { id });
            }

            List<int> staffIds = new List<int>();
            if (roster.Shifts != null)
            {
                foreach (var s in roster.Shifts)
                {
                    if (s.Assignments == null) continue;
                    foreach (var a in s.Assignments)
                    {
                        if (a.IsActive && !staffIds.Contains(a.StaffId))
                            staffIds.Add(a.StaffId);
                    }
                }
            }

            Shift firstShift = null;
            if (roster.Shifts != null)
                firstShift = roster.Shifts.OrderBy(s => s.ShiftDate).FirstOrDefault();

            var model = new CreateDutyRosterViewModel
            {
                Id = roster.Id,
                ProjectId = roster.ProjectId,
                PeriodStart = roster.PeriodStart,
                PeriodEnd = roster.PeriodEnd,
                Notes = roster.Notes,
                DefaultStartTime = firstShift != null ? firstShift.StartTime.ToString(@"hh\:mm") : "07:00",
                DefaultEndTime = firstShift != null ? firstShift.EndTime.ToString(@"hh\:mm") : "16:00",
                DefaultShiftRole = firstShift != null ? firstShift.ShiftRole : "General Labour",
                DefaultSiteLocation = firstShift != null ? firstShift.SiteLocation : "",
                WeekdaysOnly = true,
                SelectedStaffIds = staffIds,
                Projects = GetManagedProjectSelectList(staff.Id),
                AvailableStaff = GetAssignedCrewCheckboxes(roster.ProjectId, staffIds)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Project Manager")]
        public async Task<ActionResult> Edit(CreateDutyRosterViewModel model)
        {
            var staff = ResolveCurrentStaff();
            if (staff == null || !model.Id.HasValue)
            {
                TempData["ErrorMessage"] = "Invalid request.";
                return RedirectToAction("Index");
            }

            int rosterId = model.Id.Value;
            var roster = db.DutyRosters
                .Include(r => r.Shifts.Select(s => s.Assignments))
                .FirstOrDefault(r => r.Id == rosterId && !r.IsDeleted);

            if (roster == null || !IsProjectManagerForProject(roster.ProjectId, staff.Id) || roster.Status != "Draft")
            {
                TempData["ErrorMessage"] = "Roster not found, not a draft, or access denied.";
                return RedirectToAction("Index");
            }

            model.Projects = GetManagedProjectSelectList(staff.Id);
            model.AvailableStaff = GetAssignedCrewCheckboxes(roster.ProjectId, model.SelectedStaffIds);

            if (model.PeriodEnd < model.PeriodStart)
                ModelState.AddModelError("PeriodEnd", "Period end must be on or after period start.");

            if (model.SelectedStaffIds == null || model.SelectedStaffIds.Count == 0)
                ModelState.AddModelError("SelectedStaffIds", "Select at least one crew member.");

            TimeSpan startTs, endTs;
            if (!TimeSpan.TryParse(model.DefaultStartTime, out startTs) || !TimeSpan.TryParse(model.DefaultEndTime, out endTs))
                ModelState.AddModelError("DefaultStartTime", "Invalid start or end time.");
            else if (endTs <= startTs)
                ModelState.AddModelError("DefaultEndTime", "End time must be after start time.");

            if (RosterPeriodAlreadyExists(model.ProjectId, model.PeriodStart.Date, model.PeriodEnd.Date, model.Id))
                ModelState.AddModelError("", "Another duty roster already overlaps these dates for this project.");

            if (!ModelState.IsValid)
                return View(model);

            model.Shifts = AutoGenerateShifts(model);
            if (model.Shifts == null || model.Shifts.Count == 0)
            {
                ModelState.AddModelError("", "No shifts were generated.");
                return View(model);
            }

            bool publish = string.Equals(model.SubmitAction, "Publish", StringComparison.OrdinalIgnoreCase);
            RosterValidationResultViewModel validation = ValidateRosterInput(model, staff.Id);
            if (validation.HasBlockers)
            {
                foreach (string b in validation.Blockers)
                    ModelState.AddModelError("", b);
                return View(model);
            }

            roster.PeriodStart = model.PeriodStart.Date;
            roster.PeriodEnd = model.PeriodEnd.Date;
            roster.Notes = model.Notes;
            roster.ProjectId = model.ProjectId;

            List<Shift> existingShifts = roster.Shifts != null ? roster.Shifts.ToList() : new List<Shift>();
            foreach (var s in existingShifts)
            {
                if (s.Assignments != null)
                    db.ShiftAssignments.RemoveRange(s.Assignments.ToList());
                db.Shifts.Remove(s);
            }
            await db.SaveChangesAsync();

            await SaveShiftsFromInput(roster, model.Shifts);

            if (publish)
            {
                roster.Status = "Published";
                roster.PublishedBy = User.Identity.GetUserId();
                roster.PublishedDate = DateTime.Now;
                await db.SaveChangesAsync();
                await NotifyOnPublish(roster);
                TempData["SuccessMessage"] = "Roster published successfully.";
            }
            else
            {
                await db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Draft roster updated (" + model.Shifts.Count + " shifts).";
            }

            return RedirectToAction("Details", new { id = roster.Id });
        }

        // ------------------------------------------------------------------
        // Publish
        // ------------------------------------------------------------------

        [Authorize(Roles = "Project Manager")]
        public ActionResult Publish(int id)
        {
            var staff = ResolveCurrentStaff();
            var roster = db.DutyRosters
                .Include(r => r.Project)
                .Include(r => r.Shifts.Select(s => s.Assignments))
                .FirstOrDefault(r => r.Id == id && !r.IsDeleted);

            if (roster == null || staff == null || !IsProjectManagerForProject(roster.ProjectId, staff.Id))
            {
                TempData["ErrorMessage"] = "Roster not found or access denied.";
                return RedirectToAction("Index");
            }

            if (roster.Status != "Draft")
            {
                TempData["ErrorMessage"] = "Only draft rosters can be published.";
                return RedirectToAction("Details", new { id });
            }

            var inputModel = BuildInputFromRoster(roster);
            var validation = ValidateRosterInput(inputModel, staff.Id);

            var vm = new PublishRosterViewModel
            {
                DutyRosterId = roster.Id,
                ProjectName = roster.Project != null ? roster.Project.ProjectName : null,
                PeriodLabel = roster.PeriodStart.ToString("dd MMM") + " – " + roster.PeriodEnd.ToString("dd MMM yyyy"),
                ShiftCount = roster.Shifts != null ? roster.Shifts.Count : 0,
                AssignedStaffCount = CountActiveAssignments(roster),
                Blockers = validation.Blockers,
                Warnings = validation.Warnings,
                HasOvertimeWarnings = validation.Warnings.Any(w => w.IndexOf("overtime", StringComparison.OrdinalIgnoreCase) >= 0),
                HasCertificationWarnings = validation.Warnings.Any(w => w.IndexOf("certification", StringComparison.OrdinalIgnoreCase) >= 0)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Project Manager")]
        public async Task<ActionResult> Publish(PublishRosterViewModel model)
        {
            var staff = ResolveCurrentStaff();
            var roster = db.DutyRosters
                .Include(r => r.Project)
                .Include(r => r.Shifts.Select(s => s.Assignments.Select(a => a.Staff)))
                .FirstOrDefault(r => r.Id == model.DutyRosterId && !r.IsDeleted);

            if (roster == null || staff == null || !IsProjectManagerForProject(roster.ProjectId, staff.Id))
            {
                TempData["ErrorMessage"] = "Roster not found or access denied.";
                return RedirectToAction("Index");
            }

            if (roster.Status != "Draft")
            {
                TempData["ErrorMessage"] = "Only draft rosters can be published.";
                return RedirectToAction("Details", new { id = roster.Id });
            }

            var inputModel = BuildInputFromRoster(roster);
            var validation = ValidateRosterInput(inputModel, staff.Id);

            if (validation.HasBlockers)
            {
                TempData["ErrorMessage"] = "Cannot publish: " + string.Join(" ", validation.Blockers);
                return RedirectToAction("Publish", new { id = roster.Id });
            }

            if (validation.Warnings.Any(w => w.IndexOf("overtime", StringComparison.OrdinalIgnoreCase) >= 0)
                && string.IsNullOrWhiteSpace(model.OvertimeOverrideReason))
                ModelState.AddModelError("OvertimeOverrideReason", "Overtime override reason is required.");

            if (validation.Warnings.Any(w => w.IndexOf("certification", StringComparison.OrdinalIgnoreCase) >= 0)
                && string.IsNullOrWhiteSpace(model.CertificationOverrideReason))
                ModelState.AddModelError("CertificationOverrideReason", "Certification override reason is required.");

            if (!model.ConfirmPublish)
                ModelState.AddModelError("ConfirmPublish", "You must confirm before publishing.");

            if (!ModelState.IsValid)
            {
                model.Blockers = validation.Blockers;
                model.Warnings = validation.Warnings;
                model.HasOvertimeWarnings = validation.Warnings.Any(w => w.IndexOf("overtime", StringComparison.OrdinalIgnoreCase) >= 0);
                model.HasCertificationWarnings = validation.Warnings.Any(w => w.IndexOf("certification", StringComparison.OrdinalIgnoreCase) >= 0);
                model.ProjectName = roster.Project != null ? roster.Project.ProjectName : null;
                model.PeriodLabel = roster.PeriodStart.ToString("dd MMM") + " – " + roster.PeriodEnd.ToString("dd MMM yyyy");
                model.ShiftCount = roster.Shifts != null ? roster.Shifts.Count : 0;
                model.AssignedStaffCount = CountActiveAssignments(roster);
                return View(model);
            }

            string userId = User.Identity.GetUserId();
            if (roster.Shifts != null)
            {
                foreach (var shift in roster.Shifts)
                {
                    if (shift.Assignments != null)
                    {
                        foreach (var a in shift.Assignments)
                        {
                            if (!string.IsNullOrWhiteSpace(model.OvertimeOverrideReason) || !string.IsNullOrWhiteSpace(model.CertificationOverrideReason))
                            {
                                List<string> parts = new List<string>();
                                if (!string.IsNullOrWhiteSpace(model.OvertimeOverrideReason)) parts.Add(model.OvertimeOverrideReason);
                                if (!string.IsNullOrWhiteSpace(model.CertificationOverrideReason)) parts.Add(model.CertificationOverrideReason);
                                a.OverrideReason = string.Join(" | ", parts);
                                a.OverrideApprovedBy = userId;
                            }
                        }
                    }
                    if (shift.IsOvertime)
                    {
                        shift.OvertimeApprovedBy = userId;
                        shift.OvertimeApprovedDate = DateTime.Now;
                    }
                }
            }

            roster.Status = "Published";
            roster.PublishedBy = userId;
            roster.PublishedDate = DateTime.Now;
            await db.SaveChangesAsync();

            await NotifyOnPublish(roster);
            TempData["SuccessMessage"] = "Duty roster published. Crew members and Site Manager have been notified.";
            return RedirectToAction("Details", new { id = roster.Id });
        }

        // ------------------------------------------------------------------
        // Revise
        // ------------------------------------------------------------------

        [Authorize(Roles = "Project Manager")]
        public ActionResult Revise(int id)
        {
            var staff = ResolveCurrentStaff();
            var parent = db.DutyRosters
                .Include(r => r.Shifts.Select(s => s.Assignments))
                .FirstOrDefault(r => r.Id == id && !r.IsDeleted);

            if (parent == null || staff == null || !IsProjectManagerForProject(parent.ProjectId, staff.Id))
            {
                TempData["ErrorMessage"] = "Roster not found or access denied.";
                return RedirectToAction("Index");
            }

            if (parent.Status != "Published" || !parent.IsActive)
            {
                TempData["ErrorMessage"] = "Only the active published version can be revised.";
                return RedirectToAction("Details", new { id });
            }

            List<int> staffIds = new List<int>();
            if (parent.Shifts != null)
            {
                foreach (var s in parent.Shifts)
                {
                    if (s.Assignments == null) continue;
                    foreach (var a in s.Assignments)
                    {
                        if (a.IsActive && !staffIds.Contains(a.StaffId))
                            staffIds.Add(a.StaffId);
                    }
                }
            }

            Shift firstShift = null;
            if (parent.Shifts != null)
                firstShift = parent.Shifts.OrderBy(s => s.ShiftDate).FirstOrDefault();

            var model = new ReviseRosterViewModel
            {
                ParentRosterId = parent.Id,
                Version = parent.Version + 1,
                ProjectId = parent.ProjectId,
                PeriodStart = parent.PeriodStart,
                PeriodEnd = parent.PeriodEnd,
                Notes = parent.Notes,
                DefaultStartTime = firstShift != null ? firstShift.StartTime.ToString(@"hh\:mm") : "07:00",
                DefaultEndTime = firstShift != null ? firstShift.EndTime.ToString(@"hh\:mm") : "16:00",
                DefaultShiftRole = firstShift != null ? firstShift.ShiftRole : "General Labour",
                DefaultSiteLocation = firstShift != null ? firstShift.SiteLocation : "",
                WeekdaysOnly = true,
                SelectedStaffIds = staffIds,
                Projects = GetManagedProjectSelectList(staff.Id),
                AvailableStaff = GetAssignedCrewCheckboxes(parent.ProjectId, staffIds)
            };

            return View("Create", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Project Manager")]
        public async Task<ActionResult> Revise(ReviseRosterViewModel model)
        {
            var staff = ResolveCurrentStaff();
            var parent = db.DutyRosters.FirstOrDefault(r => r.Id == model.ParentRosterId && !r.IsDeleted);

            if (parent == null || staff == null || !IsProjectManagerForProject(parent.ProjectId, staff.Id)
                || parent.Status != "Published" || !parent.IsActive)
            {
                TempData["ErrorMessage"] = "Parent roster not found, not active, or access denied.";
                return RedirectToAction("Index");
            }

            model.Projects = GetManagedProjectSelectList(staff.Id);
            model.AvailableStaff = GetAssignedCrewCheckboxes(parent.ProjectId, model.SelectedStaffIds);

            if (model.SelectedStaffIds == null || model.SelectedStaffIds.Count == 0)
                ModelState.AddModelError("SelectedStaffIds", "Select at least one crew member.");

            if (model.PeriodEnd < model.PeriodStart)
                ModelState.AddModelError("PeriodEnd", "Period end must be on or after period start.");

            TimeSpan startTs, endTs;
            if (!TimeSpan.TryParse(model.DefaultStartTime, out startTs) || !TimeSpan.TryParse(model.DefaultEndTime, out endTs))
                ModelState.AddModelError("DefaultStartTime", "Invalid start or end time.");

            if (!ModelState.IsValid)
                return View("Create", model);

            model.Shifts = AutoGenerateShifts(model);
            if (model.Shifts == null || model.Shifts.Count == 0)
            {
                ModelState.AddModelError("", "No shifts were generated.");
                return View("Create", model);
            }

            var validation = ValidateRosterInput(model, staff.Id);
            if (validation.HasBlockers)
            {
                foreach (string b in validation.Blockers)
                    ModelState.AddModelError("", b);
                return View("Create", model);
            }

            parent.Status = "Superseded";
            parent.IsActive = false;

            var newRoster = new DutyRoster
            {
                ProjectId = parent.ProjectId,
                PeriodStart = model.PeriodStart.Date,
                PeriodEnd = model.PeriodEnd.Date,
                Status = "Draft",
                Version = parent.Version + 1,
                ParentRosterId = parent.Id,
                IsActive = true,
                Notes = model.Notes,
                CreatedBy = User.Identity.GetUserId(),
                CreatedDate = DateTime.Now
            };
            db.DutyRosters.Add(newRoster);
            await db.SaveChangesAsync();

            await SaveShiftsFromInput(newRoster, model.Shifts);
            await db.SaveChangesAsync();

            await NotifyOnRevise(newRoster);

            TempData["SuccessMessage"] = "Revision v" + newRoster.Version + " created as draft. Previous version is now superseded.";
            return RedirectToAction("Details", new { id = newRoster.Id });
        }

        // ------------------------------------------------------------------
        // Delete — hard delete so unique index is freed
        // ------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Project Manager")]
        public async Task<ActionResult> Delete(int id)
        {
            var staff = ResolveCurrentStaff();
            var roster = db.DutyRosters
                .Include(r => r.Shifts.Select(s => s.Assignments))
                .FirstOrDefault(r => r.Id == id && !r.IsDeleted);

            if (roster == null || staff == null || !IsProjectManagerForProject(roster.ProjectId, staff.Id))
            {
                TempData["ErrorMessage"] = "Roster not found or access denied.";
                return RedirectToAction("Index");
            }

            if (roster.Status == "Published" && HasAttendanceAgainstRoster(roster.Id))
            {
                TempData["ErrorMessage"] = "This published roster already has clock-in records. It cannot be deleted — please revise it instead.";
                return RedirectToAction("Details", new { id });
            }

            await HardDeleteRosterAsync(roster);

            TempData["SuccessMessage"] = "Roster deleted.";
            return RedirectToAction("Index");
        }

        // ------------------------------------------------------------------
        // My Shifts
        // ------------------------------------------------------------------

        [Authorize(Roles = "Crew Member")]
        public ActionResult MyShifts()
        {
            string userId = User.Identity.GetUserId();
            var staff = db.Staff.FirstOrDefault(s => s.UserId == userId && s.IsActive);
            if (staff == null)
            {
                TempData["ErrorMessage"] = "Staff record not found.";
                return RedirectToAction("Dashboard", "Home");
            }

            DateTime today = DateTime.Today;
            int staffId = staff.Id;

            var assignments = db.ShiftAssignments
                .Include(a => a.Shift.DutyRoster.Project)
                .Include(a => a.Shift.DutyRoster)
                .Where(a => a.StaffId == staffId
                            && a.IsActive
                            && a.Shift.DutyRoster.Status == "Published"
                            && a.Shift.DutyRoster.IsActive
                            && !a.Shift.DutyRoster.IsDeleted)
                .ToList();

            var upcoming = assignments
                .Where(a => a.Shift.ShiftDate >= today)
                .OrderBy(a => a.Shift.ShiftDate)
                .ThenBy(a => a.Shift.StartTime)
                .Select(a => MapMyShiftItem(a, today))
                .ToList();

            var past = assignments
                .Where(a => a.Shift.ShiftDate < today)
                .OrderByDescending(a => a.Shift.ShiftDate)
                .Take(30)
                .Select(a => MapMyShiftItem(a, today))
                .ToList();

            var allItems = past.Concat(upcoming).ToList();
            if (allItems.Count > 0)
            {
                List<int> projectIds = assignments.Select(a => a.Shift.ProjectId).Distinct().ToList();
                DateTime minDate = allItems.Min(i => i.ShiftDate).Date;
                DateTime maxDate = allItems.Max(i => i.ShiftDate).Date.AddDays(1);

                var attendance = db.AttendanceRecords
                    .Where(r => r.StaffId == staffId
                                && projectIds.Contains(r.ProjectId)
                                && r.CheckInTime >= minDate
                                && r.CheckInTime < maxDate)
                    .ToList();

                foreach (var item in allItems)
                {
                    int projectId = 0;
                    foreach (var a in assignments)
                    {
                        if (a.ShiftId == item.ShiftId)
                        {
                            projectId = a.Shift.ProjectId;
                            break;
                        }
                    }

                    DateTime day = item.ShiftDate.Date;
                    var att = attendance
                        .Where(r => r.ProjectId == projectId && r.CheckInTime.Date == day)
                        .OrderByDescending(r => r.CheckInTime)
                        .FirstOrDefault();

                    if (att != null && att.TotalHours.HasValue)
                        item.HoursWorked = att.TotalHours;
                }
            }

            return View(new MyShiftsViewModel
            {
                StaffName = staff.FullName,
                UpcomingShifts = upcoming,
                PastShifts = past
            });
        }

        // ------------------------------------------------------------------
        // Auto-generate / unique-key helpers
        // ------------------------------------------------------------------

        private List<ShiftInputViewModel> AutoGenerateShifts(CreateDutyRosterViewModel model)
        {
            var shifts = new List<ShiftInputViewModel>();
            if (model.SelectedStaffIds == null || model.SelectedStaffIds.Count == 0)
                return shifts;

            DateTime start = model.PeriodStart.Date;
            DateTime end = model.PeriodEnd.Date;
            List<int> staffIds = model.SelectedStaffIds.Distinct().ToList();

            for (DateTime d = start; d <= end; d = d.AddDays(1))
            {
                if (model.WeekdaysOnly && (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday))
                    continue;

                shifts.Add(new ShiftInputViewModel
                {
                    ShiftDate = d,
                    StartTime = model.DefaultStartTime,
                    EndTime = model.DefaultEndTime,
                    ShiftRole = string.IsNullOrWhiteSpace(model.DefaultShiftRole) ? "General Labour" : model.DefaultShiftRole,
                    SiteLocation = model.DefaultSiteLocation,
                    RequiredStaffCount = staffIds.Count,
                    StaffIds = new List<int>(staffIds)
                });
            }

            return shifts;
        }

        private bool RosterPeriodAlreadyExists(int projectId, DateTime periodStart, DateTime periodEnd, int? excludeRosterId)
        {
            IQueryable<DutyRoster> q = db.DutyRosters.Where(r =>
                !r.IsDeleted
                && r.IsActive
                && r.ProjectId == projectId
                && (r.Status == "Draft" || r.Status == "Published")
                && r.PeriodStart <= periodEnd
                && r.PeriodEnd >= periodStart);

            if (excludeRosterId.HasValue)
            {
                int excludeId = excludeRosterId.Value;
                q = q.Where(r => r.Id != excludeId);
            }

            return q.Any();
        }

        /// <summary>
        /// Hard-removes soft-deleted rosters for this exact period so the unique index is free.
        /// </summary>
        private async Task PurgeSoftDeletedRostersForPeriodAsync(int projectId, DateTime periodStart, DateTime periodEnd)
        {
            var softDeleted = db.DutyRosters
                .Include(r => r.Shifts.Select(s => s.Assignments))
                .Where(r => r.IsDeleted
                            && r.ProjectId == projectId
                            && r.PeriodStart == periodStart
                            && r.PeriodEnd == periodEnd)
                .ToList();

            foreach (var r in softDeleted)
                await HardDeleteRosterAsync(r);
        }

        private async Task HardDeleteRosterAsync(DutyRoster roster)
        {
            if (roster == null) return;

            if (roster.Shifts == null)
                db.Entry(roster).Collection(r => r.Shifts).Load();

            List<Shift> shifts = roster.Shifts != null ? roster.Shifts.ToList() : new List<Shift>();
            foreach (var s in shifts)
            {
                if (s.Assignments == null)
                    db.Entry(s).Collection(x => x.Assignments).Load();

                if (s.Assignments != null && s.Assignments.Count > 0)
                    db.ShiftAssignments.RemoveRange(s.Assignments.ToList());

                db.Shifts.Remove(s);
            }

            db.DutyRosters.Remove(roster);
            await db.SaveChangesAsync();
        }

        private int GetNextRosterVersion(int projectId, DateTime periodStart, DateTime periodEnd)
        {
            var versions = db.DutyRosters
                .Where(r => r.ProjectId == projectId
                            && r.PeriodStart == periodStart
                            && r.PeriodEnd == periodEnd)
                .Select(r => r.Version)
                .ToList();

            if (versions.Count == 0) return 1;
            return versions.Max() + 1;
        }

        private List<RosterStaffCheckboxViewModel> GetAssignedCrewCheckboxes(int projectId, List<int> selectedIds)
        {
            DateTime today = DateTime.Today;
            DateTime rangeEnd = today.AddYears(1);
            DateTime rangeStart = today.AddDays(-30);

            List<int> staffIdList = db.ProjectResourceAllocations
                .Where(a => a.ProjectId == projectId
                            && a.IsActive
                            && a.StartDate <= rangeEnd
                            && (a.EndDate == null || a.EndDate >= rangeStart))
                .Select(a => a.StaffId)
                .Distinct()
                .ToList();

            if (staffIdList.Count == 0)
                return new List<RosterStaffCheckboxViewModel>();

            List<Staff> crew = db.Staff
                .Where(s => staffIdList.Contains(s.Id)
                            && s.IsActive
                            && s.Role == "Crew Member")
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToList();

            List<int> selected = selectedIds ?? new List<int>();

            return crew.Select(s => new RosterStaffCheckboxViewModel
            {
                StaffId = s.Id,
                FullName = s.FullName,
                Role = s.Role,
                EmployeeId = s.EmployeeId,
                IsSelected = selected.Contains(s.Id)
            }).ToList();
        }

        private List<string> GetStaffNotAssignedToProject(int projectId, List<int> staffIds, DateTime periodStart, DateTime periodEnd)
        {
            var invalid = new List<string>();
            foreach (int sid in staffIds.Distinct())
            {
                bool ok = db.ProjectResourceAllocations.Any(a =>
                    a.StaffId == sid
                    && a.ProjectId == projectId
                    && a.IsActive
                    && a.StartDate <= periodEnd
                    && (a.EndDate == null || a.EndDate >= periodStart));

                if (!ok)
                {
                    var st = db.Staff.Find(sid);
                    invalid.Add(st != null ? st.FullName : ("#" + sid));
                }
            }
            return invalid;
        }

        // ------------------------------------------------------------------
        // Validation
        // ------------------------------------------------------------------

        private void ValidateShiftAssignmentInternal(
            DateTime shiftDate, TimeSpan start, TimeSpan end, string shiftRole, int projectId,
            Staff staffMember, int? excludeShiftId,
            List<string> blockers, List<string> warnings)
        {
            DateTime shiftDateOnly = shiftDate.Date;
            int staffId = staffMember.Id;

            bool isAssigned = db.ProjectResourceAllocations.Any(a =>
                a.StaffId == staffId
                && a.ProjectId == projectId
                && a.IsActive
                && a.StartDate <= shiftDateOnly
                && (a.EndDate == null || a.EndDate >= shiftDateOnly));
            RosterValidationHelper.ValidateProjectAssignment(isAssigned, blockers);

            if (staffMember.Role != "Crew Member")
                blockers.Add("Only Crew Members can be rostered to shifts.");

            bool hasLeave = db.LeaveRecords.Any(l =>
                l.StaffId == staffId
                && l.Status == "Approved"
                && l.StartDate <= shiftDateOnly
                && l.EndDate >= shiftDateOnly);
            RosterValidationHelper.ValidateLeaveOverlap(hasLeave, shiftDateOnly, blockers);

            IQueryable<ShiftAssignment> overlapQuery = db.ShiftAssignments
                .Include(a => a.Shift)
                .Include(a => a.Shift.DutyRoster)
                .Where(a => a.StaffId == staffId
                            && a.IsActive
                            && a.Shift.ShiftDate == shiftDateOnly
                            && a.Shift.DutyRoster.IsActive
                            && !a.Shift.DutyRoster.IsDeleted
                            && a.Shift.DutyRoster.Status != "Superseded");

            if (excludeShiftId.HasValue)
            {
                int eid = excludeShiftId.Value;
                overlapQuery = overlapQuery.Where(a => a.ShiftId != eid);
            }

            var sameDayTimes = overlapQuery
                .Select(a => new { a.Shift.StartTime, a.Shift.EndTime })
                .ToList();

            bool hasOverlap = sameDayTimes.Any(s => s.StartTime < end && start < s.EndTime);
            RosterValidationHelper.ValidateNoOverlap(hasOverlap, blockers);

            DateTime prevDay = shiftDateOnly.AddDays(-1);
            var prevEnds = db.ShiftAssignments
                .Include(a => a.Shift)
                .Include(a => a.Shift.DutyRoster)
                .Where(a => a.StaffId == staffId
                            && a.IsActive
                            && a.Shift.ShiftDate == prevDay
                            && a.Shift.DutyRoster.IsActive
                            && !a.Shift.DutyRoster.IsDeleted)
                .Select(a => a.Shift.EndTime)
                .ToList();

            if (prevEnds.Count > 0)
            {
                TimeSpan lastEnd = prevEnds.Max();
                DateTime prevEndDt = prevDay.Add(lastEnd);
                DateTime thisStartDt = shiftDateOnly.Add(start);
                RosterValidationHelper.ValidateDailyRest(prevEndDt, thisStartDt, blockers);
            }

            DateTime weekStart = shiftDateOnly.AddDays(-(int)shiftDateOnly.DayOfWeek + (int)DayOfWeek.Monday);
            if (weekStart > shiftDateOnly) weekStart = weekStart.AddDays(-7);
            DateTime weekEnd = weekStart.AddDays(7);

            var weekHours = GetWeeklyHoursForStaff(staffId, weekStart, weekEnd, excludeShiftId);
            decimal proposed = RosterValidationHelper.CalculateShiftHours(start, end);
            bool isOt;
            RosterValidationHelper.ValidateWeeklyHours(weekHours.Ordinary, weekHours.Overtime, proposed, blockers, warnings, out isOt);

            RosterValidationHelper.ValidateCertificationMatch(shiftRole, staffMember.Certifications, blockers, warnings, true);
        }

        private RosterValidationResultViewModel ValidateRosterInput(CreateDutyRosterViewModel model, int currentStaffId)
        {
            var result = new RosterValidationResultViewModel();
            if (model.Shifts == null) return result;

            foreach (var shift in model.Shifts)
            {
                TimeSpan start, end;
                if (!TimeSpan.TryParse(shift.StartTime, out start) || !TimeSpan.TryParse(shift.EndTime, out end))
                {
                    result.Blockers.Add("Invalid times on " + shift.ShiftDate.ToString("dd MMM") + ".");
                    continue;
                }

                RosterValidationHelper.ValidateShiftDuration(start, end, result.Blockers, result.Warnings);
                RosterValidationHelper.ValidateSundayAndHoliday(shift.ShiftDate, result.Warnings);

                if (shift.StaffIds != null)
                {
                    foreach (int sid in shift.StaffIds.Distinct())
                    {
                        var sm = db.Staff.Find(sid);
                        if (sm == null) continue;
                        var b = new List<string>();
                        var w = new List<string>();
                        ValidateShiftAssignmentInternal(shift.ShiftDate, start, end, shift.ShiftRole, model.ProjectId, sm, shift.Id, b, w);
                        foreach (string x in b) result.Blockers.Add(sm.FullName + " (" + shift.ShiftDate.ToString("dd MMM") + "): " + x);
                        foreach (string x in w) result.Warnings.Add(sm.FullName + " (" + shift.ShiftDate.ToString("dd MMM") + "): " + x);
                    }
                }
            }

            result.HasBlockers = result.Blockers.Count > 0;
            result.HasWarnings = result.Warnings.Count > 0;
            result.IsValid = !result.HasBlockers;
            return result;
        }

        private WeekHoursResult GetWeeklyHoursForStaff(int staffId, DateTime weekStart, DateTime weekEnd, int? excludeShiftId)
        {
            IQueryable<ShiftAssignment> q = db.ShiftAssignments
                .Include(a => a.Shift)
                .Include(a => a.Shift.DutyRoster)
                .Where(a => a.StaffId == staffId
                            && a.IsActive
                            && a.Shift.ShiftDate >= weekStart
                            && a.Shift.ShiftDate < weekEnd
                            && a.Shift.DutyRoster.IsActive
                            && !a.Shift.DutyRoster.IsDeleted
                            && a.Shift.DutyRoster.Status != "Superseded");

            if (excludeShiftId.HasValue)
            {
                int eid = excludeShiftId.Value;
                q = q.Where(a => a.ShiftId != eid);
            }

            List<Shift> shifts = q.Select(a => a.Shift).ToList();

            decimal ordinary = 0m;
            decimal overtime = 0m;
            foreach (var s in shifts)
            {
                decimal h = RosterValidationHelper.CalculateShiftHours(s.StartTime, s.EndTime);
                if (s.IsOvertime) overtime += h;
                else ordinary += h;
            }
            return new WeekHoursResult { Ordinary = ordinary, Overtime = overtime };
        }

        private class WeekHoursResult
        {
            public decimal Ordinary { get; set; }
            public decimal Overtime { get; set; }
        }

        // ------------------------------------------------------------------
        // Persistence
        // ------------------------------------------------------------------

        private async Task SaveShiftsFromInput(DutyRoster roster, List<ShiftInputViewModel> inputs)
        {
            if (inputs == null) return;
            string userId = User.Identity.GetUserId();

            foreach (var input in inputs)
            {
                TimeSpan start, end;
                if (!TimeSpan.TryParse(input.StartTime, out start) || !TimeSpan.TryParse(input.EndTime, out end))
                    continue;

                bool isSunday = RosterValidationHelper.IsSunday(input.ShiftDate);
                bool isHoliday = RosterValidationHelper.IsPublicHoliday(input.ShiftDate);
                decimal hours = RosterValidationHelper.CalculateShiftHours(start, end);

                var shift = new Shift
                {
                    DutyRosterId = roster.Id,
                    ProjectId = roster.ProjectId,
                    ShiftDate = input.ShiftDate.Date,
                    StartTime = start,
                    EndTime = end,
                    ShiftRole = input.ShiftRole,
                    SiteLocation = input.SiteLocation,
                    RequiredStaffCount = input.RequiredStaffCount > 0 ? input.RequiredStaffCount : 1,
                    IsOvertime = hours > 8m,
                    IsSundayShift = isSunday,
                    IsHolidayShift = isHoliday,
                    CreatedDate = DateTime.Now
                };
                db.Shifts.Add(shift);
                await db.SaveChangesAsync();

                if (input.StaffIds != null)
                {
                    foreach (int sid in input.StaffIds.Distinct())
                    {
                        db.ShiftAssignments.Add(new ShiftAssignment
                        {
                            ShiftId = shift.Id,
                            StaffId = sid,
                            AssignedBy = userId,
                            AssignedDate = DateTime.Now,
                            IsActive = true
                        });
                    }
                }
            }
        }

        private CreateDutyRosterViewModel BuildInputFromRoster(DutyRoster roster)
        {
            var shifts = new List<ShiftInputViewModel>();
            if (roster.Shifts != null)
            {
                foreach (var s in roster.Shifts)
                {
                    var staffIds = new List<int>();
                    if (s.Assignments != null)
                    {
                        foreach (var a in s.Assignments)
                        {
                            if (a.IsActive) staffIds.Add(a.StaffId);
                        }
                    }
                    shifts.Add(new ShiftInputViewModel
                    {
                        Id = s.Id,
                        ShiftDate = s.ShiftDate,
                        StartTime = s.StartTime.ToString(@"hh\:mm"),
                        EndTime = s.EndTime.ToString(@"hh\:mm"),
                        ShiftRole = s.ShiftRole,
                        SiteLocation = s.SiteLocation,
                        RequiredStaffCount = s.RequiredStaffCount,
                        StaffIds = staffIds
                    });
                }
            }

            return new CreateDutyRosterViewModel
            {
                Id = roster.Id,
                ProjectId = roster.ProjectId,
                PeriodStart = roster.PeriodStart,
                PeriodEnd = roster.PeriodEnd,
                Notes = roster.Notes,
                Shifts = shifts
            };
        }

        private static int CountActiveAssignments(DutyRoster roster)
        {
            int n = 0;
            if (roster.Shifts == null) return 0;
            foreach (var s in roster.Shifts)
            {
                if (s.Assignments == null) continue;
                foreach (var a in s.Assignments)
                {
                    if (a.IsActive) n++;
                }
            }
            return n;
        }

        // ------------------------------------------------------------------
        // Notifications
        // ------------------------------------------------------------------

        private async Task NotifyOnPublish(DutyRoster roster)
        {
            int projectId = roster.ProjectId;
            var project = db.Projects.Include(p => p.SiteManager).FirstOrDefault(p => p.Id == projectId);
            int rosterId = roster.Id;

            var assignments = db.ShiftAssignments
                .Include(a => a.Staff)
                .Where(a => a.Shift.DutyRosterId == rosterId && a.IsActive)
                .ToList();

            var notifiedUserIds = new HashSet<string>();
            string projectName = project != null ? project.ProjectName : "project";
            string periodLabel = roster.PeriodStart.ToString("dd MMM") + " – " + roster.PeriodEnd.ToString("dd MMM");

            foreach (var a in assignments)
            {
                if (a.Staff != null && !string.IsNullOrEmpty(a.Staff.UserId) && notifiedUserIds.Add(a.Staff.UserId))
                {
                    await CreateNotification(a.Staff.UserId,
                        "New Duty Roster Published",
                        "A duty roster for '" + projectName + "' (" + periodLabel + ") has been published. Check My Shifts for your assignments.",
                        "Project",
                        "/DutyRoster/MyShifts");
                }
            }

            if (project != null && project.SiteManager != null && !string.IsNullOrEmpty(project.SiteManager.UserId)
                && notifiedUserIds.Add(project.SiteManager.UserId))
            {
                await CreateNotification(project.SiteManager.UserId,
                    "Duty Roster Published",
                    "Duty roster for '" + projectName + "' (" + periodLabel + ") has been published.",
                    "Project",
                    "/DutyRoster/Details/" + roster.Id);
            }

            await CreateNotification(User.Identity.GetUserId(),
                "Roster Published",
                "You published the duty roster for '" + projectName + "' (" + periodLabel + ").",
                "System",
                "/DutyRoster/Details/" + roster.Id);
        }

        private async Task NotifyOnRevise(DutyRoster newRoster)
        {
            var project = db.Projects.Find(newRoster.ProjectId);
            int newRosterId = newRoster.Id;

            var newAssignments = db.ShiftAssignments
                .Include(a => a.Staff)
                .Where(a => a.Shift.DutyRosterId == newRosterId && a.IsActive)
                .ToList();

            var notified = new HashSet<string>();
            string projectName = project != null ? project.ProjectName : "project";

            foreach (var a in newAssignments)
            {
                if (a.Staff != null && !string.IsNullOrEmpty(a.Staff.UserId) && notified.Add(a.Staff.UserId))
                {
                    await CreateNotification(a.Staff.UserId,
                        "Duty Roster Revised",
                        "Your shifts on '" + projectName + "' have been updated (v" + newRoster.Version + "). Please review My Shifts.",
                        "Project",
                        "/DutyRoster/MyShifts");
                }
            }
        }

        private async Task CreateNotification(string userId, string title, string message, string type, string link)
        {
            if (string.IsNullOrEmpty(userId)) return;
            try
            {
                db.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    Link = link,
                    CreatedDate = DateTime.Now,
                    IsRead = false
                });
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DutyRoster notification error: " + ex.Message);
            }
        }

        // ------------------------------------------------------------------
        // Auth / scoping
        // ------------------------------------------------------------------

        private Staff ResolveCurrentStaff()
        {
            string userId = User.Identity.GetUserId();
            return db.Staff.FirstOrDefault(s => s.UserId == userId && s.IsActive);
        }

        private bool IsProjectManagerForProject(int projectId, int staffId)
        {
            return db.Projects.Any(p => p.Id == projectId && p.ProjectManagerId == staffId && p.IsActive);
        }

        private bool UserCanViewRoster(DutyRoster roster)
        {
            if (User.IsInRole("Admin")) return true;
            var staff = ResolveCurrentStaff();
            if (staff == null) return false;
            if (User.IsInRole("Project Manager"))
                return roster.Project != null && roster.Project.ProjectManagerId == staff.Id;
            if (User.IsInRole("Site Manager"))
                return roster.Project != null && roster.Project.SiteManagerId == staff.Id;
            return false;
        }

        private bool HasAttendanceAgainstRoster(int rosterId)
        {
            var shiftKeys = db.Shifts
                .Where(s => s.DutyRosterId == rosterId)
                .Select(s => new { s.ProjectId, s.ShiftDate })
                .ToList();

            if (shiftKeys.Count == 0) return false;

            foreach (var key in shiftKeys)
            {
                DateTime dayStart = key.ShiftDate.Date;
                DateTime dayEnd = dayStart.AddDays(1);
                int projectId = key.ProjectId;

                bool hasAtt = db.AttendanceRecords.Any(r =>
                    r.ProjectId == projectId
                    && r.CheckInTime >= dayStart
                    && r.CheckInTime < dayEnd);

                if (hasAtt) return true;
            }
            return false;
        }

        private List<SelectListItem> GetManagedProjectSelectList(int staffId)
        {
            return db.Projects
                .Where(p => p.ProjectManagerId == staffId && p.IsActive)
                .OrderBy(p => p.ProjectName)
                .ToList()
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.ProjectName + " (" + p.ProjectCode + ")"
                }).ToList();
        }

        private List<SelectListItem> GetProjectSelectListForViewer(Staff staff)
        {
            IQueryable<Project> q = db.Projects.Where(p => p.IsActive);

            if (User.IsInRole("Admin"))
            {
                // all
            }
            else if (User.IsInRole("Project Manager") && staff != null)
            {
                int sid = staff.Id;
                q = q.Where(p => p.ProjectManagerId == sid);
            }
            else if (User.IsInRole("Site Manager") && staff != null)
            {
                int sid = staff.Id;
                q = q.Where(p => p.SiteManagerId == sid);
            }
            else
            {
                q = q.Where(p => false);
            }

            return q.OrderBy(p => p.ProjectName)
                .ToList()
                .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.ProjectName })
                .ToList();
        }

        private static string GetStatusBadgeColor(string status)
        {
            switch (status)
            {
                case "Draft": return "secondary";
                case "Published": return "success";
                case "Superseded": return "warning";
                default: return "secondary";
            }
        }

        private MyShiftItemViewModel MapMyShiftItem(ShiftAssignment a, DateTime today)
        {
            bool isToday = a.Shift.ShiftDate.Date == today;
            TimeSpan now = DateTime.Now.TimeOfDay;
            bool canClock = isToday
                            && now >= a.Shift.StartTime.Add(TimeSpan.FromHours(-1))
                            && now <= a.Shift.EndTime.Add(TimeSpan.FromHours(1));

            string projectName = "Unknown";
            if (a.Shift.DutyRoster != null && a.Shift.DutyRoster.Project != null)
                projectName = a.Shift.DutyRoster.Project.ProjectName;

            return new MyShiftItemViewModel
            {
                ShiftId = a.Shift.Id,
                ShiftDate = a.Shift.ShiftDate,
                StartTime = a.Shift.StartTime.ToString(@"hh\:mm"),
                EndTime = a.Shift.EndTime.ToString(@"hh\:mm"),
                ProjectName = projectName,
                ShiftRole = a.Shift.ShiftRole,
                SiteLocation = a.Shift.SiteLocation,
                IsToday = isToday,
                CanClockIn = canClock,
                StatusLabel = isToday ? "Today" : (a.Shift.ShiftDate > today ? "Upcoming" : "Past")
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}