// ============ Controllers/DutyRosterController.cs ============
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MuhleOP.Helpers;
using MuhleOP.Models;
using MuhleOP.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
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
        private ApplicationUserManager UserManager =>
            _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();

        // ------------------------------------------------------------------
        // Index (read-only for PM / Admin / Site Manager)
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
                query = query.Where(r => r.Project.ProjectManagerId == staff.Id);
            }
            else if (User.IsInRole("Site Manager") && staff != null)
            {
                query = query.Where(r => r.Project.SiteManagerId == staff.Id);
            }
            else
            {
                query = query.Where(r => false);
            }

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);
            if (projectId.HasValue)
                query = query.Where(r => r.ProjectId == projectId.Value);
            if (from.HasValue)
                query = query.Where(r => r.PeriodEnd >= from.Value.Date);
            if (to.HasValue)
                query = query.Where(r => r.PeriodStart <= to.Value.Date);

            var list = query.OrderByDescending(r => r.CreatedDate).ToList();

            var isPm = User.IsInRole("Project Manager") && staff != null;
            var managedProjectIds = isPm
                ? db.Projects.Where(p => p.ProjectManagerId == staff.Id && p.IsActive).Select(p => p.Id).ToList()
                : new List<int>();

            var items = list.Select(r =>
            {
                var shiftCount = r.Shifts?.Count ?? 0;
                var assigned = r.Shifts?.SelectMany(s => s.Assignments ?? Enumerable.Empty<ShiftAssignment>())
                    .Count(a => a.IsActive) ?? 0;
                var canManage = isPm && managedProjectIds.Contains(r.ProjectId);
                return new DutyRosterListItemViewModel
                {
                    Id = r.Id,
                    ProjectName = r.Project?.ProjectName,
                    ProjectCode = r.Project?.ProjectCode,
                    PeriodLabel = $"{r.PeriodStart:dd MMM} – {r.PeriodEnd:dd MMM yyyy}",
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
                };
            }).ToList();

            var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday);
            if (weekStart > DateTime.Today) weekStart = weekStart.AddDays(-7);
            var weekEnd = weekStart.AddDays(7);

            decimal totalHours = 0m;
            var relevantRosters = list.Where(r => r.Status == "Published" && r.IsActive).ToList();
            foreach (var r in relevantRosters)
            {
                foreach (var s in r.Shifts ?? Enumerable.Empty<Shift>())
                {
                    if (s.ShiftDate >= weekStart && s.ShiftDate < weekEnd)
                        totalHours += RosterValidationHelper.CalculateShiftHours(s.StartTime, s.EndTime)
                                      * (s.Assignments?.Count(a => a.IsActive) ?? 0);
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
                CanCreate = isPm && managedProjectIds.Any(),
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
            var canManage = User.IsInRole("Project Manager") && staff != null
                            && roster.Project != null
                            && roster.Project.ProjectManagerId == staff.Id;

            var shiftsByDate = (roster.Shifts ?? Enumerable.Empty<Shift>())
                .OrderBy(s => s.ShiftDate).ThenBy(s => s.StartTime)
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
                            .Select(a => a.Staff?.FullName ?? "Unknown")
                            .ToList()
                    }).ToList()
                }).ToList();

            // Version history (same project + period lineage)
            var history = db.DutyRosters
                .Where(r => !r.IsDeleted
                            && r.ProjectId == roster.ProjectId
                            && r.PeriodStart == roster.PeriodStart
                            && r.PeriodEnd == roster.PeriodEnd)
                .OrderByDescending(r => r.Version)
                .ToList()
                .Select(r => new RosterVersionItemViewModel
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
                ProjectName = roster.Project?.ProjectName,
                ProjectCode = roster.Project?.ProjectCode,
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
                Projects = GetManagedProjectSelectList(staff.Id)
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

            if (model.Shifts == null || !model.Shifts.Any())
                ModelState.AddModelError("", "Add at least one shift before saving.");

            if (model.PeriodEnd < model.PeriodStart)
                ModelState.AddModelError("PeriodEnd", "Period end must be on or after period start.");

            if (!ModelState.IsValid)
                return View(model);

            var publish = string.Equals(model.SubmitAction, "Publish", StringComparison.OrdinalIgnoreCase);

            // Full validation
            var validation = ValidateRosterInput(model, staff.Id);
            if (validation.HasBlockers)
            {
                foreach (var b in validation.Blockers)
                    ModelState.AddModelError("", b);
                return View(model);
            }

            var roster = new DutyRoster
            {
                ProjectId = model.ProjectId,
                PeriodStart = model.PeriodStart.Date,
                PeriodEnd = model.PeriodEnd.Date,
                Status = publish ? "Published" : "Draft",
                Version = 1,
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

            await SaveShiftsFromInput(roster, model.Shifts, staff.Id);
            await db.SaveChangesAsync();

            if (publish)
            {
                await NotifyOnPublish(roster);
                TempData["SuccessMessage"] = "Duty roster published successfully. Crew members have been notified.";
            }
            else
            {
                TempData["SuccessMessage"] = "Duty roster saved as draft.";
            }

            return RedirectToAction("Details", new { id = roster.Id });
        }

        // ------------------------------------------------------------------
        // Edit (Draft only)
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

            var model = new CreateDutyRosterViewModel
            {
                Id = roster.Id,
                ProjectId = roster.ProjectId,
                PeriodStart = roster.PeriodStart,
                PeriodEnd = roster.PeriodEnd,
                Notes = roster.Notes,
                Projects = GetManagedProjectSelectList(staff.Id),
                Shifts = (roster.Shifts ?? Enumerable.Empty<Shift>()).Select(s => new ShiftInputViewModel
                {
                    Id = s.Id,
                    ShiftDate = s.ShiftDate,
                    StartTime = s.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.EndTime.ToString(@"hh\:mm"),
                    ShiftRole = s.ShiftRole,
                    SiteLocation = s.SiteLocation,
                    RequiredStaffCount = s.RequiredStaffCount,
                    StaffIds = (s.Assignments ?? Enumerable.Empty<ShiftAssignment>())
                        .Where(a => a.IsActive).Select(a => a.StaffId).ToList()
                }).ToList()
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

            var roster = db.DutyRosters
                .Include(r => r.Shifts.Select(s => s.Assignments))
                .FirstOrDefault(r => r.Id == model.Id.Value && !r.IsDeleted);

            if (roster == null || !IsProjectManagerForProject(roster.ProjectId, staff.Id) || roster.Status != "Draft")
            {
                TempData["ErrorMessage"] = "Roster not found, not a draft, or access denied.";
                return RedirectToAction("Index");
            }

            model.Projects = GetManagedProjectSelectList(staff.Id);

            if (model.Shifts == null || !model.Shifts.Any())
                ModelState.AddModelError("", "Add at least one shift before saving.");

            if (!ModelState.IsValid)
                return View(model);

            var publish = string.Equals(model.SubmitAction, "Publish", StringComparison.OrdinalIgnoreCase);
            var validation = ValidateRosterInput(model, staff.Id);
            if (validation.HasBlockers)
            {
                foreach (var b in validation.Blockers)
                    ModelState.AddModelError("", b);
                return View(model);
            }

            roster.PeriodStart = model.PeriodStart.Date;
            roster.PeriodEnd = model.PeriodEnd.Date;
            roster.Notes = model.Notes;

            // Remove existing shifts/assignments
            foreach (var s in roster.Shifts.ToList())
            {
                db.ShiftAssignments.RemoveRange(s.Assignments);
                db.Shifts.Remove(s);
            }
            await db.SaveChangesAsync();

            await SaveShiftsFromInput(roster, model.Shifts, staff.Id);

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
                TempData["SuccessMessage"] = "Draft roster updated.";
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
                ProjectName = roster.Project?.ProjectName,
                PeriodLabel = $"{roster.PeriodStart:dd MMM} – {roster.PeriodEnd:dd MMM yyyy}",
                ShiftCount = roster.Shifts?.Count ?? 0,
                AssignedStaffCount = roster.Shifts?.SelectMany(s => s.Assignments ?? Enumerable.Empty<ShiftAssignment>()).Count(a => a.IsActive) ?? 0,
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
            {
                ModelState.AddModelError("OvertimeOverrideReason", "Overtime override reason is required.");
            }
            if (validation.Warnings.Any(w => w.IndexOf("certification", StringComparison.OrdinalIgnoreCase) >= 0)
                && string.IsNullOrWhiteSpace(model.CertificationOverrideReason))
            {
                ModelState.AddModelError("CertificationOverrideReason", "Certification override reason is required.");
            }
            if (!model.ConfirmPublish)
            {
                ModelState.AddModelError("ConfirmPublish", "You must confirm before publishing.");
            }

            if (!ModelState.IsValid)
            {
                model.Blockers = validation.Blockers;
                model.Warnings = validation.Warnings;
                model.HasOvertimeWarnings = validation.Warnings.Any(w => w.IndexOf("overtime", StringComparison.OrdinalIgnoreCase) >= 0);
                model.HasCertificationWarnings = validation.Warnings.Any(w => w.IndexOf("certification", StringComparison.OrdinalIgnoreCase) >= 0);
                model.ProjectName = roster.Project?.ProjectName;
                model.PeriodLabel = $"{roster.PeriodStart:dd MMM} – {roster.PeriodEnd:dd MMM yyyy}";
                return View(model);
            }

            // Apply override reasons to assignments that need them
            foreach (var shift in roster.Shifts ?? Enumerable.Empty<Shift>())
            {
                foreach (var a in shift.Assignments ?? Enumerable.Empty<ShiftAssignment>())
                {
                    if (!string.IsNullOrWhiteSpace(model.OvertimeOverrideReason) || !string.IsNullOrWhiteSpace(model.CertificationOverrideReason))
                    {
                        a.OverrideReason = string.Join(" | ",
                            new[] { model.OvertimeOverrideReason, model.CertificationOverrideReason }
                                .Where(x => !string.IsNullOrWhiteSpace(x)));
                        a.OverrideApprovedBy = User.Identity.GetUserId();
                    }
                }
                if (shift.IsOvertime)
                {
                    shift.OvertimeApprovedBy = User.Identity.GetUserId();
                    shift.OvertimeApprovedDate = DateTime.Now;
                }
            }

            roster.Status = "Published";
            roster.PublishedBy = User.Identity.GetUserId();
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

            var model = new ReviseRosterViewModel
            {
                ParentRosterId = parent.Id,
                Version = parent.Version + 1,
                ProjectId = parent.ProjectId,
                PeriodStart = parent.PeriodStart,
                PeriodEnd = parent.PeriodEnd,
                Notes = parent.Notes,
                Projects = GetManagedProjectSelectList(staff.Id),
                Shifts = (parent.Shifts ?? Enumerable.Empty<Shift>()).Select(s => new ShiftInputViewModel
                {
                    ShiftDate = s.ShiftDate,
                    StartTime = s.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.EndTime.ToString(@"hh\:mm"),
                    ShiftRole = s.ShiftRole,
                    SiteLocation = s.SiteLocation,
                    RequiredStaffCount = s.RequiredStaffCount,
                    StaffIds = (s.Assignments ?? Enumerable.Empty<ShiftAssignment>())
                        .Where(a => a.IsActive).Select(a => a.StaffId).ToList()
                }).ToList()
            };

            return View("Create", model); // reuse Create view with revise context
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

            if (model.Shifts == null || !model.Shifts.Any())
                ModelState.AddModelError("", "Add at least one shift.");

            if (!ModelState.IsValid)
                return View("Create", model);

            var validation = ValidateRosterInput(model, staff.Id);
            if (validation.HasBlockers)
            {
                foreach (var b in validation.Blockers)
                    ModelState.AddModelError("", b);
                return View("Create", model);
            }

            // Supersede parent
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

            await SaveShiftsFromInput(newRoster, model.Shifts, staff.Id);
            await db.SaveChangesAsync();

            // Notify only crew whose shifts changed (simplified: notify all assigned on new version)
            await NotifyOnRevise(parent, newRoster);

            TempData["SuccessMessage"] = $"Revision v{newRoster.Version} created as draft. Previous version is now superseded.";
            return RedirectToAction("Details", new { id = newRoster.Id });
        }

        // ------------------------------------------------------------------
        // Delete
        // ------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Project Manager")]
        public async Task<ActionResult> Delete(int id)
        {
            var staff = ResolveCurrentStaff();
            var roster = db.DutyRosters
                .Include(r => r.Shifts)
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

            roster.IsDeleted = true;
            roster.DeletedBy = User.Identity.GetUserId();
            roster.DeletedDate = DateTime.Now;
            roster.IsActive = false;
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Roster deleted.";
            return RedirectToAction("Index");
        }

        // ------------------------------------------------------------------
        // AJAX validation & available staff
        // ------------------------------------------------------------------

        [HttpPost]
        [Authorize(Roles = "Project Manager")]
        public ActionResult ValidateShift(ShiftInputViewModel shift, int projectId)
        {
            var result = new RosterValidationResultViewModel();
            if (shift == null)
            {
                result.Blockers.Add("Invalid shift data.");
                result.HasBlockers = true;
                return Json(result);
            }

            if (!TimeSpan.TryParse(shift.StartTime, out var start) || !TimeSpan.TryParse(shift.EndTime, out var end))
            {
                result.Blockers.Add("Invalid start or end time.");
                result.HasBlockers = true;
                return Json(result);
            }

            RosterValidationHelper.ValidateShiftDuration(start, end, result.Blockers, result.Warnings);
            RosterValidationHelper.ValidateSundayAndHoliday(shift.ShiftDate, result.Warnings);

            if (shift.StaffIds != null)
            {
                foreach (var staffId in shift.StaffIds.Distinct())
                {
                    var staffMember = db.Staff.Find(staffId);
                    if (staffMember == null) continue;

                    var blockers = new List<string>();
                    var warnings = new List<string>();

                    ValidateShiftAssignmentInternal(shift.ShiftDate, start, end, shift.ShiftRole, projectId, staffMember, null, blockers, warnings);

                    foreach (var b in blockers) result.Blockers.Add($"{staffMember.FullName}: {b}");
                    foreach (var w in warnings) result.Warnings.Add($"{staffMember.FullName}: {w}");
                }
            }

            result.HasBlockers = result.Blockers.Any();
            result.HasWarnings = result.Warnings.Any();
            result.IsValid = !result.HasBlockers;

            if (result.HasBlockers)
            {
                // Suggest replacements for the first blocked staff slot
                var tempShift = new Shift
                {
                    ProjectId = projectId,
                    ShiftDate = shift.ShiftDate,
                    StartTime = start,
                    EndTime = end,
                    ShiftRole = shift.ShiftRole
                };
                result.SuggestedReplacements = SuggestReplacements(tempShift, shift.StaffIds ?? new List<int>());
            }

            return Json(result);
        }

        [HttpGet]
        [Authorize(Roles = "Project Manager")]
        public ActionResult GetAvailableStaff(int projectId, DateTime shiftDate, string startTime, string endTime, string shiftRole)
        {
            if (!TimeSpan.TryParse(startTime, out var start) || !TimeSpan.TryParse(endTime, out var end))
                return Json(new List<AvailableStaffItemViewModel>(), JsonRequestBehavior.AllowGet);

            var staff = ResolveCurrentStaff();
            if (staff == null || !IsProjectManagerForProject(projectId, staff.Id))
                return Json(new List<AvailableStaffItemViewModel>(), JsonRequestBehavior.AllowGet);

            var allocated = db.ProjectResourceAllocations
                .Where(a => a.ProjectId == projectId
                            && a.IsActive
                            && a.StartDate <= shiftDate
                            && (a.EndDate == null || a.EndDate >= shiftDate)
                            && a.Staff.Role == "Crew Member"
                            && a.Staff.IsActive)
                .Select(a => a.Staff)
                .Distinct()
                .ToList();

            var result = new List<AvailableStaffItemViewModel>();
            foreach (var s in allocated)
            {
                var blockers = new List<string>();
                var warnings = new List<string>();
                ValidateShiftAssignmentInternal(shiftDate, start, end, shiftRole, projectId, s, null, blockers, warnings);

                result.Add(new AvailableStaffItemViewModel
                {
                    StaffId = s.Id,
                    FullName = s.FullName,
                    Role = s.Role,
                    Certifications = s.Certifications,
                    IsAvailable = !blockers.Any(),
                    UnavailableReason = blockers.FirstOrDefault()
                });
            }

            return Json(result.OrderBy(x => x.FullName), JsonRequestBehavior.AllowGet);
        }

        // ------------------------------------------------------------------
        // My Shifts (Crew Member)
        // ------------------------------------------------------------------

        [Authorize(Roles = "Crew Member")]
        public ActionResult MyShifts()
        {
            var userId = User.Identity.GetUserId();
            var staff = db.Staff.FirstOrDefault(s => s.UserId == userId && s.IsActive);
            if (staff == null)
            {
                TempData["ErrorMessage"] = "Staff record not found.";
                return RedirectToAction("Dashboard", "Home");
            }

            var today = DateTime.Today;
            var assignments = db.ShiftAssignments
                .Include(a => a.Shift.DutyRoster.Project)
                .Include(a => a.Shift.DutyRoster)
                .Where(a => a.StaffId == staff.Id
                            && a.IsActive
                            && a.Shift.DutyRoster.Status == "Published"
                            && a.Shift.DutyRoster.IsActive
                            && !a.Shift.DutyRoster.IsDeleted)
                .ToList();

            var upcoming = assignments
                .Where(a => a.Shift.ShiftDate >= today)
                .OrderBy(a => a.Shift.ShiftDate).ThenBy(a => a.Shift.StartTime)
                .Select(a => MapMyShiftItem(a, today))
                .ToList();

            var past = assignments
                .Where(a => a.Shift.ShiftDate < today)
                .OrderByDescending(a => a.Shift.ShiftDate)
                .Take(30)
                .Select(a => MapMyShiftItem(a, today))
                .ToList();

            // Pull hours from AttendanceRecord where possible (same day + project)
            foreach (var item in past.Concat(upcoming))
            {
                var att = db.AttendanceRecords
                    .Where(r => r.StaffId == staff.Id
                                && r.ProjectId == db.Shifts.Find(item.ShiftId).ProjectId
                                && DbFunctions.TruncateTime(r.CheckInTime) == item.ShiftDate.Date)
                    .OrderByDescending(r => r.CheckInTime)
                    .FirstOrDefault();
                if (att != null && att.TotalHours.HasValue)
                    item.HoursWorked = att.TotalHours;
            }

            var vm = new MyShiftsViewModel
            {
                StaffName = staff.FullName,
                UpcomingShifts = upcoming,
                PastShifts = past
            };
            return View(vm);
        }

        // ------------------------------------------------------------------
        // Private helpers – validation engine
        // ------------------------------------------------------------------

        private void ValidateShiftAssignmentInternal(
            DateTime shiftDate, TimeSpan start, TimeSpan end, string shiftRole, int projectId,
            Staff staffMember, int? excludeShiftId,
            List<string> blockers, List<string> warnings)
        {
            // Project assignment
            var isAssigned = db.ProjectResourceAllocations.Any(a =>
                a.StaffId == staffMember.Id
                && a.ProjectId == projectId
                && a.IsActive
                && a.StartDate <= shiftDate
                && (a.EndDate == null || a.EndDate >= shiftDate));
            RosterValidationHelper.ValidateProjectAssignment(isAssigned, blockers);

            // Role check
            if (staffMember.Role != "Crew Member")
                blockers.Add("Only Crew Members can be rostered to shifts.");

            // Leave
            var hasLeave = db.LeaveRecords.Any(l =>
                l.StaffId == staffMember.Id
                && l.Status == "Approved"
                && l.StartDate <= shiftDate
                && l.EndDate >= shiftDate);
            RosterValidationHelper.ValidateLeaveOverlap(hasLeave, shiftDate, blockers);

            // Overlap (any project)
            var hasOverlap = db.ShiftAssignments
                .Include(a => a.Shift)
                .Any(a => a.StaffId == staffMember.Id
                          && a.IsActive
                          && a.Shift.ShiftDate == shiftDate.Date
                          && (excludeShiftId == null || a.ShiftId != excludeShiftId)
                          && a.Shift.DutyRoster.IsActive
                          && !a.Shift.DutyRoster.IsDeleted
                          && a.Shift.DutyRoster.Status != "Superseded"
                          && TimesOverlap(a.Shift.StartTime, a.Shift.EndTime, start, end));
            RosterValidationHelper.ValidateNoOverlap(hasOverlap, blockers);

            // Daily rest (previous day last shift)
            var prevDay = shiftDate.Date.AddDays(-1);
            var prevShifts = db.ShiftAssignments
                .Include(a => a.Shift)
                .Where(a => a.StaffId == staffMember.Id
                            && a.IsActive
                            && a.Shift.ShiftDate == prevDay
                            && a.Shift.DutyRoster.IsActive
                            && !a.Shift.DutyRoster.IsDeleted)
                .Select(a => a.Shift)
                .ToList();
            if (prevShifts.Any())
            {
                var lastEnd = prevShifts.Max(s => s.EndTime);
                var prevEndDt = prevDay.Add(lastEnd);
                var thisStartDt = shiftDate.Date.Add(start);
                RosterValidationHelper.ValidateDailyRest(prevEndDt, thisStartDt, blockers);
            }

            // Weekly hours
            var weekStart = shiftDate.Date.AddDays(-(int)shiftDate.DayOfWeek + (int)DayOfWeek.Monday);
            if (weekStart > shiftDate.Date) weekStart = weekStart.AddDays(-7);
            var weekEnd = weekStart.AddDays(7);
            var weekHours = GetWeeklyHoursForStaff(staffMember.Id, weekStart, weekEnd, excludeShiftId);
            var proposed = RosterValidationHelper.CalculateShiftHours(start, end);
            bool isOt;
            RosterValidationHelper.ValidateWeeklyHours(weekHours.Ordinary, weekHours.Overtime, proposed, blockers, warnings, out isOt);

            // Certification
            RosterValidationHelper.ValidateCertificationMatch(shiftRole, staffMember.Certifications, blockers, warnings, allowOverride: true);
        }

        private RosterValidationResultViewModel ValidateRosterInput(CreateDutyRosterViewModel model, int currentStaffId)
        {
            var result = new RosterValidationResultViewModel();
            if (model.Shifts == null) return result;

            foreach (var shift in model.Shifts)
            {
                if (!TimeSpan.TryParse(shift.StartTime, out var start) || !TimeSpan.TryParse(shift.EndTime, out var end))
                {
                    result.Blockers.Add($"Invalid times on {shift.ShiftDate:dd MMM}.");
                    continue;
                }

                RosterValidationHelper.ValidateShiftDuration(start, end, result.Blockers, result.Warnings);
                RosterValidationHelper.ValidateSundayAndHoliday(shift.ShiftDate, result.Warnings);

                if (shift.StaffIds != null)
                {
                    foreach (var sid in shift.StaffIds.Distinct())
                    {
                        var sm = db.Staff.Find(sid);
                        if (sm == null) continue;
                        var b = new List<string>();
                        var w = new List<string>();
                        ValidateShiftAssignmentInternal(shift.ShiftDate, start, end, shift.ShiftRole, model.ProjectId, sm, shift.Id, b, w);
                        foreach (var x in b) result.Blockers.Add($"{sm.FullName} ({shift.ShiftDate:dd MMM}): {x}");
                        foreach (var x in w) result.Warnings.Add($"{sm.FullName} ({shift.ShiftDate:dd MMM}): {x}");
                    }
                }
            }

            result.HasBlockers = result.Blockers.Any();
            result.HasWarnings = result.Warnings.Any();
            result.IsValid = !result.HasBlockers;
            return result;
        }

        private (decimal Ordinary, decimal Overtime) GetWeeklyHoursForStaff(int staffId, DateTime weekStart, DateTime weekEnd, int? excludeShiftId)
        {
            var assignments = db.ShiftAssignments
                .Include(a => a.Shift)
                .Where(a => a.StaffId == staffId
                            && a.IsActive
                            && a.Shift.ShiftDate >= weekStart
                            && a.Shift.ShiftDate < weekEnd
                            && a.Shift.DutyRoster.IsActive
                            && !a.Shift.DutyRoster.IsDeleted
                            && a.Shift.DutyRoster.Status != "Superseded"
                            && (excludeShiftId == null || a.ShiftId != excludeShiftId))
                .Select(a => a.Shift)
                .ToList();

            decimal ordinary = 0m, overtime = 0m;
            foreach (var s in assignments)
            {
                var h = RosterValidationHelper.CalculateShiftHours(s.StartTime, s.EndTime);
                if (s.IsOvertime) overtime += h;
                else ordinary += h;
            }
            return (ordinary, overtime);
        }

        private List<ReplacementSuggestionViewModel> SuggestReplacements(Shift shift, List<int> excludeStaffIds)
        {
            var candidates = db.ProjectResourceAllocations
                .Where(a => a.ProjectId == shift.ProjectId
                            && a.IsActive
                            && a.StartDate <= shift.ShiftDate
                            && (a.EndDate == null || a.EndDate >= shift.ShiftDate)
                            && a.Staff.Role == "Crew Member"
                            && a.Staff.IsActive
                            && !excludeStaffIds.Contains(a.StaffId))
                .Select(a => a.Staff)
                .Distinct()
                .ToList();

            var suggestions = new List<ReplacementSuggestionViewModel>();
            foreach (var c in candidates.Take(8))
            {
                var blockers = new List<string>();
                var warnings = new List<string>();
                ValidateShiftAssignmentInternal(shift.ShiftDate, shift.StartTime, shift.EndTime, shift.ShiftRole, shift.ProjectId, c, null, blockers, warnings);
                if (!blockers.Any())
                {
                    suggestions.Add(new ReplacementSuggestionViewModel
                    {
                        StaffId = c.Id,
                        FullName = c.FullName,
                        Role = c.Role,
                        Reason = warnings.Any() ? "Available (with warnings)" : "Available and holds required certification"
                    });
                }
            }
            return suggestions;
        }

        // ------------------------------------------------------------------
        // Persistence helpers
        // ------------------------------------------------------------------

        private async Task SaveShiftsFromInput(DutyRoster roster, List<ShiftInputViewModel> inputs, int assignedByStaffId)
        {
            if (inputs == null) return;
            var userId = User.Identity.GetUserId();

            foreach (var input in inputs)
            {
                if (!TimeSpan.TryParse(input.StartTime, out var start) || !TimeSpan.TryParse(input.EndTime, out var end))
                    continue;

                var isSunday = RosterValidationHelper.IsSunday(input.ShiftDate);
                var isHoliday = RosterValidationHelper.IsPublicHoliday(input.ShiftDate);
                var hours = RosterValidationHelper.CalculateShiftHours(start, end);

                // Rough overtime flag (full weekly calc is done at validation time)
                var isOt = hours > 8m; // simple heuristic; refined on publish

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
                    IsOvertime = isOt,
                    IsSundayShift = isSunday,
                    IsHolidayShift = isHoliday,
                    CreatedDate = DateTime.Now
                };
                db.Shifts.Add(shift);
                await db.SaveChangesAsync();

                if (input.StaffIds != null)
                {
                    foreach (var sid in input.StaffIds.Distinct())
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
            return new CreateDutyRosterViewModel
            {
                Id = roster.Id,
                ProjectId = roster.ProjectId,
                PeriodStart = roster.PeriodStart,
                PeriodEnd = roster.PeriodEnd,
                Notes = roster.Notes,
                Shifts = (roster.Shifts ?? Enumerable.Empty<Shift>()).Select(s => new ShiftInputViewModel
                {
                    Id = s.Id,
                    ShiftDate = s.ShiftDate,
                    StartTime = s.StartTime.ToString(@"hh\:mm"),
                    EndTime = s.EndTime.ToString(@"hh\:mm"),
                    ShiftRole = s.ShiftRole,
                    SiteLocation = s.SiteLocation,
                    RequiredStaffCount = s.RequiredStaffCount,
                    StaffIds = (s.Assignments ?? Enumerable.Empty<ShiftAssignment>())
                        .Where(a => a.IsActive).Select(a => a.StaffId).ToList()
                }).ToList()
            };
        }

        // ------------------------------------------------------------------
        // Notifications
        // ------------------------------------------------------------------

        private async Task NotifyOnPublish(DutyRoster roster)
        {
            var project = db.Projects.Include(p => p.SiteManager).FirstOrDefault(p => p.Id == roster.ProjectId);
            var assignments = db.ShiftAssignments
                .Include(a => a.Staff)
                .Include(a => a.Shift)
                .Where(a => a.Shift.DutyRosterId == roster.Id && a.IsActive)
                .ToList();

            var notifiedUserIds = new HashSet<string>();

            foreach (var a in assignments)
            {
                if (a.Staff != null && !string.IsNullOrEmpty(a.Staff.UserId) && notifiedUserIds.Add(a.Staff.UserId))
                {
                    await CreateNotification(a.Staff.UserId,
                        "New Duty Roster Published",
                        $"A duty roster for '{project?.ProjectName}' ({roster.PeriodStart:dd MMM} – {roster.PeriodEnd:dd MMM}) has been published. Check My Shifts for your assignments.",
                        "Project",
                        "/DutyRoster/MyShifts");
                }
            }

            if (project?.SiteManager != null && !string.IsNullOrEmpty(project.SiteManager.UserId)
                && notifiedUserIds.Add(project.SiteManager.UserId))
            {
                await CreateNotification(project.SiteManager.UserId,
                    "Duty Roster Published",
                    $"Duty roster for '{project.ProjectName}' ({roster.PeriodStart:dd MMM} – {roster.PeriodEnd:dd MMM}) has been published.",
                    "Project",
                    $"/DutyRoster/Details/{roster.Id}");
            }

            // Confirmation for PM (in-app only)
            var pmUserId = User.Identity.GetUserId();
            await CreateNotification(pmUserId,
                "Roster Published",
                $"You published the duty roster for '{project?.ProjectName}' ({roster.PeriodStart:dd MMM} – {roster.PeriodEnd:dd MMM}).",
                "System",
                $"/DutyRoster/Details/{roster.Id}");
        }

        private async Task NotifyOnRevise(DutyRoster oldRoster, DutyRoster newRoster)
        {
            var project = db.Projects.Find(newRoster.ProjectId);
            var newAssignments = db.ShiftAssignments
                .Include(a => a.Staff)
                .Where(a => a.Shift.DutyRosterId == newRoster.Id && a.IsActive)
                .ToList();

            var notified = new HashSet<string>();
            foreach (var a in newAssignments)
            {
                if (a.Staff != null && !string.IsNullOrEmpty(a.Staff.UserId) && notified.Add(a.Staff.UserId))
                {
                    await CreateNotification(a.Staff.UserId,
                        "Duty Roster Revised",
                        $"Your shifts on '{project?.ProjectName}' have been updated (v{newRoster.Version}). Please review My Shifts.",
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
                System.Diagnostics.Debug.WriteLine($"DutyRoster notification error: {ex.Message}");
            }
        }

        // ------------------------------------------------------------------
        // Auth / scoping helpers
        // ------------------------------------------------------------------

        private Staff ResolveCurrentStaff()
        {
            var userId = User.Identity.GetUserId();
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
            // Any AttendanceRecord on the same project + date as a shift in this roster
            var shifts = db.Shifts.Where(s => s.DutyRosterId == rosterId).Select(s => new { s.ProjectId, s.ShiftDate }).ToList();
            foreach (var s in shifts)
            {
                if (db.AttendanceRecords.Any(r => r.ProjectId == s.ProjectId
                                                  && DbFunctions.TruncateTime(r.CheckInTime) == s.ShiftDate.Date))
                    return true;
            }
            return false;
        }

        private static bool TimesOverlap(TimeSpan aStart, TimeSpan aEnd, TimeSpan bStart, TimeSpan bEnd)
        {
            return aStart < bEnd && bStart < aEnd;
        }

        private List<SelectListItem> GetManagedProjectSelectList(int staffId)
        {
            return db.Projects
                .Where(p => p.ProjectManagerId == staffId && p.IsActive)
                .OrderBy(p => p.ProjectName)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.ProjectName + " (" + p.ProjectCode + ")"
                }).ToList();
        }

        private List<SelectListItem> GetProjectSelectListForViewer(Staff staff)
        {
            IQueryable<Project> q = db.Projects.Where(p => p.IsActive);
            if (User.IsInRole("Admin")) { /* all */ }
            else if (User.IsInRole("Project Manager") && staff != null)
                q = q.Where(p => p.ProjectManagerId == staff.Id);
            else if (User.IsInRole("Site Manager") && staff != null)
                q = q.Where(p => p.SiteManagerId == staff.Id);
            else
                q = q.Where(p => false);

            return q.OrderBy(p => p.ProjectName)
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
            var isToday = a.Shift.ShiftDate.Date == today;
            var now = DateTime.Now.TimeOfDay;
            var canClock = isToday
                           && now >= a.Shift.StartTime.Add(TimeSpan.FromHours(-1))
                           && now <= a.Shift.EndTime.Add(TimeSpan.FromHours(1));

            return new MyShiftItemViewModel
            {
                ShiftId = a.Shift.Id,
                ShiftDate = a.Shift.ShiftDate,
                StartTime = a.Shift.StartTime.ToString(@"hh\:mm"),
                EndTime = a.Shift.EndTime.ToString(@"hh\:mm"),
                ProjectName = a.Shift.DutyRoster?.Project?.ProjectName ?? "Unknown",
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