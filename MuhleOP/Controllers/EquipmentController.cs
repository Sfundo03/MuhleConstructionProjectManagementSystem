using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
    public class EquipmentController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        private static DateTime _lastMaintenanceCheck = DateTime.MinValue;
        private static readonly object _maintenanceCheckLock = new object();

        private ApplicationUserManager _userManager;
        private ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
        }

        // ------------------------------------------------------------------
        // Part A — Inventory (Admin)
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin")]
        public ActionResult Index(string category = null, string status = null, bool dueOnly = false, string search = null)
        {
            RunMaintenanceDueCheckIfNeeded();

            var query = db.Equipment
                .Include(e => e.AssignedProject)
                .Where(e => e.IsActive);

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(e => e.Category == category);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(e => e.Status == status);

            if (dueOnly)
            {
                var cutoff = DateTime.Today.AddDays(14);
                query = query.Where(e => e.NextMaintenanceDate != null && e.NextMaintenanceDate <= cutoff);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(e =>
                    e.EquipmentName.ToLower().Contains(term) ||
                    e.SerialNumber.ToLower().Contains(term) ||
                    (e.EquipmentCode != null && e.EquipmentCode.ToLower().Contains(term)));
            }

            var list = query
                .OrderBy(e => e.EquipmentName)
                .ToList()
                .Select(e => MapToListItem(e))
                .ToList();

            var allActive = db.Equipment.Where(e => e.IsActive).ToList();
            var viewModel = new EquipmentIndexViewModel
            {
                Items = list,
                TotalCount = allActive.Count,
                AvailableCount = allActive.Count(e => e.Status == "Available"),
                InUseCount = allActive.Count(e => e.Status == "In Use"),
                UnderMaintenanceCount = allActive.Count(e => e.Status == "Under Maintenance" || e.Status == "In Repair" || e.Status == "Damaged"),
                DueCount = allActive.Count(e => e.NextMaintenanceDate != null && e.NextMaintenanceDate <= DateTime.Today.AddDays(14)),
                ActiveFilter = dueOnly ? "due" : (status ?? category ?? "all"),
                Categories = GetCategorySelectList(category),
                Statuses = GetStatusSelectList(status)
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            var viewModel = new CreateEquipmentViewModel
            {
                Status = "Available",
                Condition = "Good",
                Categories = GetCategorySelectList(null),
                Conditions = GetConditionSelectList(null),
                Statuses = GetStatusSelectList(null),
                MaintenanceFrequencies = GetMaintenanceFrequencySelectList(null)
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Create(CreateEquipmentViewModel viewModel)
        {
            if (viewModel == null)
                viewModel = new CreateEquipmentViewModel();

            // Culture-safe decimal parsing for money fields
            FixDecimalBinding(viewModel, "PurchaseCost", v => viewModel.PurchaseCost = v);
            FixDecimalBinding(viewModel, "CurrentValue", v => viewModel.CurrentValue = v);

            if (ModelState.IsValid)
            {
                try
                {
                    var serial = (viewModel.SerialNumber ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(serial))
                    {
                        ModelState.AddModelError("SerialNumber", "Serial number is required.");
                        return View(RepopulateCreateDropdowns(viewModel));
                    }

                    if (db.Equipment.Any(e => e.SerialNumber == serial))
                    {
                        ModelState.AddModelError("SerialNumber", "Serial number must be unique.");
                        return View(RepopulateCreateDropdowns(viewModel));
                    }

                    var equipment = new Equipment
                    {
                        EquipmentName = Def(viewModel.EquipmentName),
                        SerialNumber = serial,
                        Category = Def(viewModel.Category),
                        SubCategory = Def(viewModel.SubCategory),
                        Make = Def(viewModel.Make),
                        Model = Def(viewModel.Model),
                        Year = viewModel.Year,
                        PurchaseDate = viewModel.PurchaseDate,
                        PurchaseCost = viewModel.PurchaseCost ?? 0m,
                        CurrentValue = viewModel.CurrentValue ?? 0m,
                        Status = string.IsNullOrWhiteSpace(viewModel.Status) ? "Available" : viewModel.Status.Trim(),
                        Condition = string.IsNullOrWhiteSpace(viewModel.Condition) ? "Good" : viewModel.Condition.Trim(),
                        Location = Def(viewModel.Location),
                        IsActive = true,
                        Notes = Def(viewModel.Notes),
                        MaintenanceFrequencyDays = viewModel.MaintenanceFrequencyDays,
                        EquipmentCode = GenerateUniqueEquipmentCode()
                    };

                    if (viewModel.MaintenanceFrequencyDays.HasValue && viewModel.MaintenanceFrequencyDays.Value > 0)
                    {
                        equipment.NextMaintenanceDate = DateTime.Today.AddDays(viewModel.MaintenanceFrequencyDays.Value);
                    }

                    db.Equipment.Add(equipment);
                    await db.SaveChangesAsync();

                    try
                    {
                        await CreateNotification(
                            User.Identity.GetUserId(),
                            "Equipment Registered",
                            $"'{equipment.EquipmentName}' ({equipment.EquipmentCode}) has been registered successfully.",
                            "Maintenance",
                            $"/Equipment/Details/{equipment.Id}"
                        );
                    }
                    catch (Exception notifEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"CreateNotification failed (ignored): {notifEx.Message}");
                    }

                    TempData["SuccessMessage"] = $"Equipment '{equipment.EquipmentName}' registered successfully.";
                    return RedirectToAction("Index");
                }
                catch (DbEntityValidationException dbEx)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Entity validation failed:");
                    foreach (var eve in dbEx.EntityValidationErrors)
                    {
                        foreach (var ve in eve.ValidationErrors)
                        {
                            sb.AppendLine($"{ve.PropertyName}: {ve.ErrorMessage}");
                            var key = string.IsNullOrEmpty(ve.PropertyName) ? "" : ve.PropertyName;
                            ModelState.AddModelError(key, ve.ErrorMessage);
                            System.Diagnostics.Debug.WriteLine($"EF Validation: {ve.PropertyName} — {ve.ErrorMessage}");
                        }
                    }
                    ModelState.AddModelError("", sb.ToString());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating equipment: {ex}");
                    ModelState.AddModelError("", "Error registering equipment: " + GetInnermostMessage(ex));
                }
            }

            return View(RepopulateCreateDropdowns(viewModel));
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int id)
        {
            var equipment = db.Equipment.Find(id);
            if (equipment == null || !equipment.IsActive)
            {
                TempData["ErrorMessage"] = "Equipment not found.";
                return RedirectToAction("Index");
            }

            var viewModel = new EditEquipmentViewModel
            {
                Id = equipment.Id,
                EquipmentName = equipment.EquipmentName,
                EquipmentCode = equipment.EquipmentCode,
                SerialNumber = equipment.SerialNumber,
                Category = equipment.Category,
                SubCategory = equipment.SubCategory,
                Make = equipment.Make,
                Model = equipment.Model,
                Year = equipment.Year,
                PurchaseDate = equipment.PurchaseDate,
                PurchaseCost = equipment.PurchaseCost,
                CurrentValue = equipment.CurrentValue,
                Status = equipment.Status,
                Condition = equipment.Condition,
                Location = equipment.Location,
                MaintenanceFrequencyDays = equipment.MaintenanceFrequencyDays,
                NextMaintenanceDate = equipment.NextMaintenanceDate,
                Notes = equipment.Notes,
                Categories = GetCategorySelectList(equipment.Category),
                Conditions = GetConditionSelectList(equipment.Condition),
                Statuses = GetStatusSelectList(equipment.Status),
                MaintenanceFrequencies = GetMaintenanceFrequencySelectList(equipment.MaintenanceFrequencyDays)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Edit(EditEquipmentViewModel viewModel)
        {
            if (viewModel == null)
            {
                TempData["ErrorMessage"] = "Equipment not found.";
                return RedirectToAction("Index");
            }

            FixDecimalBinding(viewModel, "PurchaseCost", v => viewModel.PurchaseCost = v);
            FixDecimalBinding(viewModel, "CurrentValue", v => viewModel.CurrentValue = v);

            if (ModelState.IsValid)
            {
                try
                {
                    var equipment = db.Equipment.Find(viewModel.Id);
                    if (equipment == null || !equipment.IsActive)
                    {
                        TempData["ErrorMessage"] = "Equipment not found.";
                        return RedirectToAction("Index");
                    }

                    var serial = (viewModel.SerialNumber ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(serial))
                    {
                        ModelState.AddModelError("SerialNumber", "Serial number is required.");
                        return View(RepopulateEditDropdowns(viewModel));
                    }

                    if (db.Equipment.Any(e => e.SerialNumber == serial && e.Id != viewModel.Id))
                    {
                        ModelState.AddModelError("SerialNumber", "Serial number must be unique.");
                        return View(RepopulateEditDropdowns(viewModel));
                    }

                    var frequencyChanged = equipment.MaintenanceFrequencyDays != viewModel.MaintenanceFrequencyDays;

                    equipment.EquipmentName = Def(viewModel.EquipmentName);
                    equipment.SerialNumber = serial;
                    equipment.Category = Def(viewModel.Category);
                    equipment.SubCategory = Def(viewModel.SubCategory);
                    equipment.Make = Def(viewModel.Make);
                    equipment.Model = Def(viewModel.Model);
                    equipment.Year = viewModel.Year;
                    equipment.PurchaseDate = viewModel.PurchaseDate;
                    equipment.PurchaseCost = viewModel.PurchaseCost ?? 0m;
                    equipment.CurrentValue = viewModel.CurrentValue ?? 0m;
                    equipment.Status = string.IsNullOrWhiteSpace(viewModel.Status) ? equipment.Status : viewModel.Status.Trim();
                    equipment.Condition = string.IsNullOrWhiteSpace(viewModel.Condition) ? equipment.Condition : viewModel.Condition.Trim();
                    equipment.Location = Def(viewModel.Location);
                    equipment.Notes = Def(viewModel.Notes);
                    equipment.MaintenanceFrequencyDays = viewModel.MaintenanceFrequencyDays;

                    if (string.IsNullOrWhiteSpace(equipment.EquipmentCode))
                        equipment.EquipmentCode = GenerateUniqueEquipmentCode();

                    if (frequencyChanged)
                    {
                        if (viewModel.MaintenanceFrequencyDays.HasValue && viewModel.MaintenanceFrequencyDays.Value > 0)
                            equipment.NextMaintenanceDate = DateTime.Today.AddDays(viewModel.MaintenanceFrequencyDays.Value);
                        else
                            equipment.NextMaintenanceDate = null;
                    }

                    await db.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Equipment '{equipment.EquipmentName}' updated successfully.";
                    return RedirectToAction("Index");
                }
                catch (DbEntityValidationException dbEx)
                {
                    foreach (var eve in dbEx.EntityValidationErrors)
                    {
                        foreach (var ve in eve.ValidationErrors)
                        {
                            var key = string.IsNullOrEmpty(ve.PropertyName) ? "" : ve.PropertyName;
                            ModelState.AddModelError(key, ve.ErrorMessage);
                            System.Diagnostics.Debug.WriteLine($"EF Validation: {ve.PropertyName} — {ve.ErrorMessage}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error editing equipment: {ex}");
                    ModelState.AddModelError("", "Error updating equipment: " + GetInnermostMessage(ex));
                }
            }

            return View(RepopulateEditDropdowns(viewModel));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Remove(int id)
        {
            try
            {
                var equipment = db.Equipment.Find(id);
                if (equipment == null)
                {
                    TempData["ErrorMessage"] = "Equipment not found.";
                    return RedirectToAction("Index");
                }

                equipment.IsActive = false;
                equipment.Status = "Retired";
                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = "Equipment retired.";
            }
            catch (DbEntityValidationException dbEx)
            {
                LogDbEntityValidation(dbEx);
                TempData["ErrorMessage"] = "Validation error while retiring equipment.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retiring equipment: {ex}");
                TempData["ErrorMessage"] = "Error retiring equipment.";
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin, Site Manager, Project Manager, Crew Member, Finance Officer")]
        public ActionResult Details(int id)
        {
            var equipment = db.Equipment
                .Include(e => e.AssignedProject)
                .Include(e => e.AssignedProject.ProjectManager)
                .Include(e => e.Maintenances)
                .Include(e => e.Repairs)
                .Include(e => e.Assignments)
                .FirstOrDefault(e => e.Id == id);

            if (equipment == null)
            {
                TempData["ErrorMessage"] = "Equipment not found.";
                return RedirectToAction("Index");
            }

            var activeAssignment = equipment.Assignments?
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.AssignedDate)
                .FirstOrDefault();

            var viewModel = new EquipmentDetailsViewModel
            {
                Id = equipment.Id,
                EquipmentName = equipment.EquipmentName,
                EquipmentCode = equipment.EquipmentCode,
                SerialNumber = equipment.SerialNumber,
                Category = equipment.Category,
                SubCategory = equipment.SubCategory,
                Make = equipment.Make,
                Model = equipment.Model,
                Year = equipment.Year,
                PurchaseDate = equipment.PurchaseDate,
                PurchaseCost = equipment.PurchaseCost,
                CurrentValue = equipment.CurrentValue,
                Status = equipment.Status,
                Condition = equipment.Condition,
                Location = equipment.Location,
                IsActive = equipment.IsActive,
                LastMaintenanceDate = equipment.LastMaintenanceDate,
                NextMaintenanceDate = equipment.NextMaintenanceDate,
                MaintenanceFrequencyDays = equipment.MaintenanceFrequencyDays,
                Notes = equipment.Notes,
                AssignedProjectId = equipment.AssignedProjectId,
                AssignedProjectName = equipment.AssignedProject != null ? equipment.AssignedProject.ProjectName : null,
                AssignedProjectManager = equipment.AssignedProject?.ProjectManager != null
                    ? equipment.AssignedProject.ProjectManager.FullName : null,
                AssignedDate = activeAssignment?.AssignedDate,
                MaintenanceHistory = (equipment.Maintenances ?? Enumerable.Empty<EquipmentMaintenance>())
                    .OrderByDescending(m => m.MaintenanceDate)
                    .Select(m => new EquipmentMaintenanceListItemViewModel
                    {
                        Id = m.Id,
                        MaintenanceDate = m.MaintenanceDate,
                        MaintenanceType = m.MaintenanceType,
                        Description = m.Description,
                        TechnicianName = m.TechnicianName,
                        Status = m.Status,
                        StatusBadgeColor = GetMaintenanceStatusColor(m.Status)
                    }).ToList(),
                RepairHistory = (equipment.Repairs ?? Enumerable.Empty<EquipmentRepair>())
                    .OrderByDescending(r => r.CreatedDate)
                    .Select(r => new EquipmentRepairListItemViewModel
                    {
                        RepairId = r.Id,
                        TicketLabel = $"#RT-{r.Id}",
                        EquipmentName = equipment.EquipmentName,
                        EquipmentCode = equipment.EquipmentCode,
                        ProjectName = equipment.AssignedProject?.ProjectName,
                        ReportedBy = r.ReportedBy,
                        ReportedDate = r.CreatedDate,
                        Status = r.Status,
                        StatusBadgeColor = GetRepairStatusColor(r.Status),
                        TechnicianName = r.TechnicianName,
                        TotalCost = r.TotalCost
                    }).ToList()
            };

            return View(viewModel);
        }

        // ------------------------------------------------------------------
        // Part B — Maintenance due
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Site Manager")]
        public ActionResult MaintenanceDue()
        {
            RunMaintenanceDueCheckIfNeeded();

            var cutoff = DateTime.Today.AddDays(14);
            var items = db.Equipment
                .Include(e => e.AssignedProject)
                .Where(e => e.IsActive && e.NextMaintenanceDate != null && e.NextMaintenanceDate <= cutoff)
                .OrderBy(e => e.NextMaintenanceDate)
                .ToList()
                .Select(e => MapToListItem(e))
                .ToList();

            var viewModel = new EquipmentIndexViewModel
            {
                Items = items,
                TotalCount = items.Count,
                DueCount = items.Count,
                AvailableCount = 0,
                InUseCount = 0,
                UnderMaintenanceCount = 0,
                ActiveFilter = "due",
                Categories = GetCategorySelectList(null),
                Statuses = GetStatusSelectList(null)
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin, Site Manager")]
        public ActionResult DueForMaintenance()
        {
            return RedirectToAction("MaintenanceDue");
        }

        // ------------------------------------------------------------------
        // Part C — Availability helpers
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Project Manager, Site Manager")]
        public JsonResult GetAvailableForProject(int projectId)
        {
            var assignedIds = db.EquipmentAssignments
                .Where(a => a.IsActive)
                .Select(a => a.EquipmentId)
                .ToList();

            var available = db.Equipment
                .Where(e => e.IsActive && e.Status == "Available" && !assignedIds.Contains(e.Id))
                .GroupBy(e => e.EquipmentName)
                .Select(g => new
                {
                    id = g.FirstOrDefault().Id,
                    name = g.Key,
                    code = g.FirstOrDefault().EquipmentCode,
                    serial = g.FirstOrDefault().SerialNumber,
                    condition = g.FirstOrDefault().Condition,
                    availableCount = g.Count()
                })
                .ToList();

            return Json(available, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Admin, Project Manager, Site Manager")]
        public ActionResult InventoryByProject(int projectId)
        {
            var project = db.Projects.Find(projectId);
            if (project == null)
            {
                TempData["ErrorMessage"] = "Project not found.";
                return RedirectToAction("Index", "Project");
            }

            var assignments = db.EquipmentAssignments
                .Include(a => a.Equipment)
                .Where(a => a.ProjectId == projectId && a.IsActive)
                .ToList();

            ViewBag.ProjectName = project.ProjectName;
            ViewBag.ProjectId = projectId;

            return View(assignments);
        }

        // ------------------------------------------------------------------
        // Part D — Report Damage (Crew Member)
        // ------------------------------------------------------------------

        [Authorize(Roles = "Crew Member")]
        public ActionResult ReportDamage()
        {
            var userId = User.Identity.GetUserId();
            var staff = db.Staff.FirstOrDefault(s => s.UserId == userId && s.IsActive);
            if (staff == null)
            {
                TempData["ErrorMessage"] = "Staff record not found or inactive.";
                return RedirectToAction("Dashboard", "Home");
            }

            var myProjectIds = db.ProjectResourceAllocations
                .Where(a => a.StaffId == staff.Id && a.IsActive
                    && a.StartDate <= DateTime.Today
                    && (a.EndDate == null || a.EndDate >= DateTime.Today))
                .Select(a => a.ProjectId)
                .Distinct()
                .ToList();

            var equipmentQuery = db.Equipment
                .Where(e => e.IsActive
                    && e.Status != "Damaged"
                    && e.Status != "In Repair"
                    && e.Status != "Retired"
                    && (e.Status == "Available"
                        || (e.Status == "In Use" && e.AssignedProjectId.HasValue && myProjectIds.Contains(e.AssignedProjectId.Value))));

            var viewModel = new ReportDamageViewModel
            {
                AvailableEquipment = equipmentQuery
                    .OrderBy(e => e.EquipmentName)
                    .Select(e => new SelectListItem
                    {
                        Value = e.Id.ToString(),
                        Text = e.EquipmentName + " — SN: " + e.SerialNumber + " — " + e.Condition
                    }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Crew Member")]
        public async Task<ActionResult> ReportDamage(ReportDamageViewModel viewModel)
        {
            var userId = User.Identity.GetUserId();
            var staff = db.Staff.FirstOrDefault(s => s.UserId == userId && s.IsActive);
            if (staff == null)
            {
                TempData["ErrorMessage"] = "Staff record not found or inactive.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (viewModel == null)
                viewModel = new ReportDamageViewModel();

            if (ModelState.IsValid)
            {
                try
                {
                    var equipment = db.Equipment.Find(viewModel.EquipmentId);
                    if (equipment == null || !equipment.IsActive)
                    {
                        ModelState.AddModelError("EquipmentId", "Selected equipment is not available.");
                        return View(RepopulateReportDamageDropdowns(viewModel, staff.Id));
                    }

                    var myProjectIds = db.ProjectResourceAllocations
                        .Where(a => a.StaffId == staff.Id && a.IsActive
                            && a.StartDate <= DateTime.Today
                            && (a.EndDate == null || a.EndDate >= DateTime.Today))
                        .Select(a => a.ProjectId)
                        .Distinct()
                        .ToList();

                    bool allowed =
                        equipment.Status == "Available" ||
                        (equipment.Status == "In Use" && equipment.AssignedProjectId.HasValue &&
                         myProjectIds.Contains(equipment.AssignedProjectId.Value));

                    if (!allowed || equipment.Status == "Damaged" || equipment.Status == "In Repair")
                    {
                        ModelState.AddModelError("EquipmentId", "You may only report damage on equipment that is available or assigned to one of your active projects.");
                        return View(RepopulateReportDamageDropdowns(viewModel, staff.Id));
                    }

                    var repair = new EquipmentRepair
                    {
                        EquipmentId = viewModel.EquipmentId,
                        RepairDate = DateTime.Today,
                        IssueDescription = Def(viewModel.IssueDescription),
                        Status = "Reported",
                        ReportedBy = Def(staff.FullName) + " (Crew Member)",
                        CreatedDate = DateTime.Now,
                        PartsCost = 0m,
                        LabourCost = 0m,
                        TotalCost = 0m,
                        PartsReplaced = string.Empty,
                        TechnicianName = string.Empty,
                        RepairNotes = string.Empty,
                        VerifiedBy = string.Empty
                    };

                    db.EquipmentRepairs.Add(repair);
                    equipment.Status = "Damaged";
                    await db.SaveChangesAsync();

                    if (viewModel.Photo != null && viewModel.Photo.ContentLength > 0)
                    {
                        try
                        {
                            var folder = Server.MapPath($"~/Uploads/EquipmentDamage/{repair.Id}");
                            if (!Directory.Exists(folder))
                                Directory.CreateDirectory(folder);

                            var ext = Path.GetExtension(viewModel.Photo.FileName) ?? ".jpg";
                            var fileName = Guid.NewGuid().ToString("N") + ext;
                            var physicalPath = Path.Combine(folder, fileName);
                            viewModel.Photo.SaveAs(physicalPath);

                            var relativePath = $"/Uploads/EquipmentDamage/{repair.Id}/{fileName}";
                            repair.RepairNotes = (repair.RepairNotes ?? string.Empty) + $" [photo: {relativePath}]";
                            await db.SaveChangesAsync();
                        }
                        catch (Exception photoEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Photo upload failed: {photoEx.Message}");
                        }
                    }

                    await NotifyAdminsOfDamage(equipment, repair, staff.FullName ?? "Crew Member");

                    TempData["SuccessMessage"] = $"Damage reported for '{equipment.EquipmentName}'. Admins have been notified.";
                    return RedirectToAction("Dashboard", "Home");
                }
                catch (DbEntityValidationException dbEx)
                {
                    foreach (var eve in dbEx.EntityValidationErrors)
                    {
                        foreach (var ve in eve.ValidationErrors)
                        {
                            var key = string.IsNullOrEmpty(ve.PropertyName) ? "" : ve.PropertyName;
                            ModelState.AddModelError(key, ve.ErrorMessage);
                            System.Diagnostics.Debug.WriteLine($"EF Validation: {ve.PropertyName} — {ve.ErrorMessage}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error reporting damage: {ex}");
                    ModelState.AddModelError("", "Error reporting damage: " + GetInnermostMessage(ex));
                }
            }

            return View(RepopulateReportDamageDropdowns(viewModel, staff.Id));
        }

        // ------------------------------------------------------------------
        // Part E — Repair Queue / Schedule
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Site Manager")]
        public ActionResult RepairQueue(string status = null)
        {
            var query = db.EquipmentRepairs
                .Include(r => r.Equipment)
                .Include(r => r.Equipment.AssignedProject)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);

            var items = query
                .OrderByDescending(r => r.CreatedDate)
                .ToList()
                .Select(r => new EquipmentRepairListItemViewModel
                {
                    RepairId = r.Id,
                    TicketLabel = $"#RT-{r.Id}",
                    EquipmentName = r.Equipment?.EquipmentName ?? "Unknown",
                    EquipmentCode = r.Equipment?.EquipmentCode,
                    ProjectName = r.Equipment?.AssignedProject?.ProjectName,
                    ReportedBy = r.ReportedBy,
                    ReportedDate = r.CreatedDate,
                    Status = r.Status,
                    StatusBadgeColor = GetRepairStatusColor(r.Status),
                    TechnicianName = r.TechnicianName,
                    TotalCost = r.TotalCost
                }).ToList();

            var all = db.EquipmentRepairs.ToList();
            var viewModel = new EquipmentRepairQueueViewModel
            {
                Items = items,
                ReportedCount = all.Count(r => r.Status == "Reported"),
                InRepairCount = all.Count(r => r.Status == "In Repair" || r.Status == "Awaiting Parts"),
                ReadyForVerificationCount = all.Count(r => r.Status == "Ready for Verification"),
                VerifiedCount = all.Count(r => r.Status == "Verified"),
                ActiveFilter = status ?? "all"
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin, Site Manager")]
        public ActionResult RepairDetails(int id)
        {
            var repair = db.EquipmentRepairs
                .Include(r => r.Equipment)
                .Include(r => r.Equipment.AssignedProject)
                .FirstOrDefault(r => r.Id == id);

            if (repair == null)
            {
                TempData["ErrorMessage"] = "Repair ticket not found.";
                return RedirectToAction("RepairQueue");
            }

            var vm = new EquipmentRepairDetailsViewModel
            {
                Id = repair.Id,
                EquipmentId = repair.EquipmentId,
                EquipmentName = repair.Equipment?.EquipmentName,
                EquipmentCode = repair.Equipment?.EquipmentCode,
                EquipmentCategory = repair.Equipment?.Category,
                EquipmentStatus = repair.Equipment?.Status,
                EquipmentCondition = repair.Equipment?.Condition,
                AssignedProjectName = repair.Equipment?.AssignedProject?.ProjectName,
                RepairDate = repair.RepairDate,
                IssueDescription = repair.IssueDescription,
                PartsReplaced = repair.PartsReplaced,
                PartsCost = repair.PartsCost,
                LabourCost = repair.LabourCost,
                TotalCost = repair.TotalCost,
                TechnicianName = repair.TechnicianName,
                Status = repair.Status,
                StatusBadgeColor = GetRepairStatusColor(repair.Status),
                RepairNotes = repair.RepairNotes,
                VerifiedBy = repair.VerifiedBy,
                VerificationDate = repair.VerificationDate,
                ReportedBy = repair.ReportedBy,
                CreatedDate = repair.CreatedDate
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin, Site Manager")]
        public ActionResult ScheduleRepair(int id)
        {
            var repair = db.EquipmentRepairs
                .Include(r => r.Equipment)
                .FirstOrDefault(r => r.Id == id);

            if (repair == null)
            {
                TempData["ErrorMessage"] = "Repair ticket not found.";
                return RedirectToAction("RepairQueue");
            }

            if (repair.Status != "Reported")
            {
                TempData["ErrorMessage"] = "This repair is already scheduled or completed.";
                return RedirectToAction("RepairQueue");
            }

            var viewModel = new ScheduleRepairViewModel
            {
                RepairId = repair.Id,
                EquipmentName = repair.Equipment?.EquipmentName,
                CurrentStatus = repair.Status,
                WorkshopSlotStart = DateTime.Today.AddDays(1),
                WorkshopSlotEnd = DateTime.Today.AddDays(3)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Site Manager")]
        public async Task<ActionResult> ScheduleRepair(ScheduleRepairViewModel viewModel)
        {
            if (viewModel == null)
            {
                TempData["ErrorMessage"] = "Repair ticket not found.";
                return RedirectToAction("RepairQueue");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (viewModel.WorkshopSlotEnd < viewModel.WorkshopSlotStart)
                    {
                        ModelState.AddModelError("WorkshopSlotEnd", "End date must be on or after start date.");
                        return View(viewModel);
                    }

                    var repair = db.EquipmentRepairs
                        .Include(r => r.Equipment)
                        .FirstOrDefault(r => r.Id == viewModel.RepairId);

                    if (repair == null || repair.Status != "Reported")
                    {
                        TempData["ErrorMessage"] = "Repair ticket not found or already processed.";
                        return RedirectToAction("RepairQueue");
                    }

                    repair.Status = "In Repair";
                    repair.TechnicianName = Def(viewModel.TechnicianName);

                    var slotNote = $"Workshop slot: {viewModel.WorkshopSlotStart:dd MMM yyyy} – {viewModel.WorkshopSlotEnd:dd MMM yyyy}. Check-in: {Def(viewModel.CheckInNotes)}. ";
                    if (!string.IsNullOrWhiteSpace(viewModel.RequiredParts))
                        slotNote += $"Required parts: {viewModel.RequiredParts.Trim()}. ";

                    repair.RepairNotes = slotNote + Def(repair.RepairNotes) +
                        $" [{DateTime.Now:yyyy-MM-dd HH:mm} by {User.Identity.Name}] Status → In Repair";

                    if (repair.Equipment != null)
                    {
                        repair.Equipment.Status = "In Repair";
                        repair.Equipment.NextMaintenanceDate = viewModel.WorkshopSlotEnd;
                    }

                    await db.SaveChangesAsync();

                    await NotifyAdmins(
                        "Repair Scheduled",
                        $"Repair #{repair.Id} for '{repair.Equipment?.EquipmentName}' has been scheduled ({viewModel.WorkshopSlotStart:dd MMM} – {viewModel.WorkshopSlotEnd:dd MMM}).",
                        $"/Equipment/RepairDetails/{repair.Id}",
                        skipUserId: User.Identity.GetUserId()
                    );

                    TempData["SuccessMessage"] = "Repair scheduled successfully.";
                    return RedirectToAction("RepairDetails", new { id = repair.Id });
                }
                catch (DbEntityValidationException dbEx)
                {
                    foreach (var eve in dbEx.EntityValidationErrors)
                    {
                        foreach (var ve in eve.ValidationErrors)
                        {
                            var key = string.IsNullOrEmpty(ve.PropertyName) ? "" : ve.PropertyName;
                            ModelState.AddModelError(key, ve.ErrorMessage);
                            System.Diagnostics.Debug.WriteLine($"EF Validation: {ve.PropertyName} — {ve.ErrorMessage}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error scheduling repair: {ex}");
                    ModelState.AddModelError("", "Error scheduling repair: " + GetInnermostMessage(ex));
                }
            }

            return View(viewModel);
        }

        // ------------------------------------------------------------------
        // Part F — Update Progress
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Site Manager")]
        public ActionResult UpdateRepairProgress(int id)
        {
            var repair = db.EquipmentRepairs
                .Include(r => r.Equipment)
                .FirstOrDefault(r => r.Id == id);

            if (repair == null)
            {
                TempData["ErrorMessage"] = "Repair ticket not found.";
                return RedirectToAction("RepairQueue");
            }

            if (repair.Status == "Verified" || repair.Status == "Cancelled")
            {
                TempData["ErrorMessage"] = "This repair is already closed.";
                return RedirectToAction("RepairQueue");
            }

            var viewModel = new UpdateRepairProgressViewModel
            {
                RepairId = repair.Id,
                EquipmentName = repair.Equipment?.EquipmentName,
                TechnicianName = repair.TechnicianName,
                PartsReplaced = repair.PartsReplaced,
                PartsCost = repair.PartsCost,
                LabourCost = repair.LabourCost,
                TotalCost = repair.TotalCost,
                RepairNotes = repair.RepairNotes,
                Status = repair.Status,
                StatusOptions = GetRepairProgressStatusSelectList(repair.Status)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Site Manager")]
        public async Task<ActionResult> UpdateRepairProgress(UpdateRepairProgressViewModel viewModel)
        {
            if (viewModel == null)
            {
                TempData["ErrorMessage"] = "Repair ticket not found.";
                return RedirectToAction("RepairQueue");
            }

            // ----- Culture-safe decimal parsing (fixes "332.8 is not valid") -----
            // The form posts dots (en-US / invariant). SA culture expects commas.
            FixDecimalBinding(viewModel, "PartsCost", v => viewModel.PartsCost = v);
            FixDecimalBinding(viewModel, "LabourCost", v => viewModel.LabourCost = v);
            // TotalCost is always recalculated server-side — ignore posted value
            ModelState.Remove("TotalCost");
            viewModel.TotalCost = viewModel.PartsCost + viewModel.LabourCost;
            // ---------------------------------------------------------------------

            if (ModelState.IsValid)
            {
                try
                {
                    var repair = db.EquipmentRepairs
                        .Include(r => r.Equipment)
                        .FirstOrDefault(r => r.Id == viewModel.RepairId);

                    if (repair == null || repair.Status == "Verified" || repair.Status == "Cancelled")
                    {
                        TempData["ErrorMessage"] = "Repair ticket not found or already closed.";
                        return RedirectToAction("RepairQueue");
                    }

                    repair.TechnicianName = Def(viewModel.TechnicianName);
                    repair.PartsReplaced = Def(viewModel.PartsReplaced);
                    repair.PartsCost = viewModel.PartsCost;
                    repair.LabourCost = viewModel.LabourCost;
                    repair.TotalCost = viewModel.PartsCost + viewModel.LabourCost;
                    repair.Status = string.IsNullOrWhiteSpace(viewModel.Status) ? repair.Status : viewModel.Status.Trim();

                    repair.RepairNotes = Def(repair.RepairNotes) +
                        $" [{DateTime.Now:yyyy-MM-dd HH:mm} by {User.Identity.Name}] Status → {repair.Status}. Notes: {Def(viewModel.RepairNotes)}";

                    if (viewModel.Status == "Cancelled" && repair.Equipment != null)
                    {
                        repair.Equipment.Status = "Available";
                        repair.Equipment.AssignedProjectId = null;

                        var activeAssignments = db.EquipmentAssignments
                            .Where(a => a.EquipmentId == repair.EquipmentId && a.IsActive)
                            .ToList();
                        foreach (var a in activeAssignments)
                        {
                            a.IsActive = false;
                            a.ActualReturnDate = DateTime.Now;
                            a.ConditionOnReturn = "Cancelled repair";
                        }
                    }

                    await db.SaveChangesAsync();

                    if (viewModel.Status == "Ready for Verification")
                    {
                        try
                        {
                            await NotifyAdmins(
                                "Repair Ready for Verification",
                                $"Repair #{repair.Id} for '{repair.Equipment?.EquipmentName}' is ready for final check-out.",
                                $"/Equipment/RepairDetails/{repair.Id}"
                            );
                        }
                        catch (Exception notifEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"NotifyAdmins failed (ignored): {notifEx.Message}");
                        }
                    }

                    TempData["SuccessMessage"] = "Repair progress updated.";
                    return RedirectToAction("RepairDetails", new { id = repair.Id });
                }
                catch (DbEntityValidationException dbEx)
                {
                    foreach (var eve in dbEx.EntityValidationErrors)
                    {
                        foreach (var ve in eve.ValidationErrors)
                        {
                            var key = string.IsNullOrEmpty(ve.PropertyName) ? "" : ve.PropertyName;
                            ModelState.AddModelError(key, ve.ErrorMessage);
                            System.Diagnostics.Debug.WriteLine($"EF Validation: {ve.PropertyName} — {ve.ErrorMessage}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating repair: {ex}");
                    ModelState.AddModelError("", "Error updating repair: " + GetInnermostMessage(ex));
                }
            }

            viewModel.StatusOptions = GetRepairProgressStatusSelectList(viewModel.Status);
            return View(viewModel);
        }

        // ------------------------------------------------------------------
        // Part G — Check-Out
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Site Manager")]
        public ActionResult CheckOutRepair(int id)
        {
            var repair = db.EquipmentRepairs
                .Include(r => r.Equipment)
                .FirstOrDefault(r => r.Id == id);

            if (repair == null)
            {
                TempData["ErrorMessage"] = "Repair ticket not found.";
                return RedirectToAction("RepairQueue");
            }

            if (repair.Status != "Ready for Verification")
            {
                TempData["ErrorMessage"] = "This repair is not ready for check-out.";
                return RedirectToAction("RepairQueue");
            }

            var summary = $"Issue: {repair.IssueDescription}. " +
                          $"Parts: {repair.PartsReplaced ?? "n/a"}. " +
                          $"Parts cost: R{repair.PartsCost:N2}. Labour: R{repair.LabourCost:N2}. " +
                          $"Technician: {repair.TechnicianName ?? "n/a"}.";

            var viewModel = new CheckOutRepairViewModel
            {
                RepairId = repair.Id,
                EquipmentName = repair.Equipment?.EquipmentName,
                EquipmentCode = repair.Equipment?.EquipmentCode,
                TechnicianName = repair.TechnicianName,
                RepairSummary = summary,
                FinalCondition = "Good",
                ConditionOptions = GetConditionSelectList("Good")
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Site Manager")]
        public async Task<ActionResult> CheckOutRepair(CheckOutRepairViewModel viewModel)
        {
            if (viewModel == null)
            {
                TempData["ErrorMessage"] = "Repair ticket not found.";
                return RedirectToAction("RepairQueue");
            }

            if (!viewModel.FinalVerified)
            {
                ModelState.AddModelError("FinalVerified", "You must confirm the final safety/load test.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var repair = db.EquipmentRepairs
                        .Include(r => r.Equipment)
                        .FirstOrDefault(r => r.Id == viewModel.RepairId);

                    if (repair == null || repair.Status != "Ready for Verification")
                    {
                        TempData["ErrorMessage"] = "Repair ticket not found or not ready for check-out.";
                        return RedirectToAction("RepairQueue");
                    }

                    repair.Status = "Verified";
                    repair.VerifiedBy = Def(User.Identity.Name);
                    repair.VerificationDate = DateTime.Now;
                    repair.RepairNotes = Def(repair.RepairNotes) +
                        $" [{DateTime.Now:yyyy-MM-dd HH:mm} by {User.Identity.Name}] Final check-out. Condition: {Def(viewModel.FinalCondition)}. {Def(viewModel.FinalVerificationNotes)}";

                    if (repair.Equipment != null)
                    {
                        repair.Equipment.Status = "Available";
                        repair.Equipment.AssignedProjectId = null;
                        repair.Equipment.Condition = string.IsNullOrWhiteSpace(viewModel.FinalCondition) ? "Good" : viewModel.FinalCondition.Trim();
                        repair.Equipment.LastMaintenanceDate = DateTime.Today;

                        if (repair.Equipment.MaintenanceFrequencyDays.HasValue && repair.Equipment.MaintenanceFrequencyDays.Value > 0)
                        {
                            repair.Equipment.NextMaintenanceDate = DateTime.Today.AddDays(repair.Equipment.MaintenanceFrequencyDays.Value);
                        }
                    }

                    var activeAssignments = db.EquipmentAssignments
                        .Where(a => a.EquipmentId == repair.EquipmentId && a.IsActive)
                        .ToList();
                    foreach (var a in activeAssignments)
                    {
                        a.IsActive = false;
                        a.ActualReturnDate = DateTime.Now;
                        a.ConditionOnReturn = "Repaired";
                    }

                    await db.SaveChangesAsync();

                    var lastAssignment = db.EquipmentAssignments
                        .Include(a => a.Project)
                        .Include(a => a.Project.ProjectManager)
                        .Where(a => a.EquipmentId == repair.EquipmentId)
                        .OrderByDescending(a => a.AssignedDate)
                        .FirstOrDefault();

                    if (lastAssignment?.Project?.ProjectManager != null &&
                        !string.IsNullOrEmpty(lastAssignment.Project.ProjectManager.UserId))
                    {
                        await NotifyStakeholder(
                            lastAssignment.Project.ProjectManager.UserId,
                            "Equipment Back in Service",
                            $"Equipment '{repair.Equipment?.EquipmentName}' has been repaired and is back in service.",
                            "Maintenance",
                            $"/Equipment/Details/{repair.EquipmentId}"
                        );
                    }

                    await NotifyAdmins(
                        "Equipment Checked Out",
                        $"'{repair.Equipment?.EquipmentName}' (repair #{repair.Id}) has been verified and returned to service.",
                        $"/Equipment/RepairDetails/{repair.Id}"
                    );

                    TempData["SuccessMessage"] = "Equipment checked out and returned to service.";
                    return RedirectToAction("RepairDetails", new { id = repair.Id });
                }
                catch (DbEntityValidationException dbEx)
                {
                    foreach (var eve in dbEx.EntityValidationErrors)
                    {
                        foreach (var ve in eve.ValidationErrors)
                        {
                            var key = string.IsNullOrEmpty(ve.PropertyName) ? "" : ve.PropertyName;
                            ModelState.AddModelError(key, ve.ErrorMessage);
                            System.Diagnostics.Debug.WriteLine($"EF Validation: {ve.PropertyName} — {ve.ErrorMessage}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error checking out repair: {ex}");
                    ModelState.AddModelError("", "Error checking out repair: " + GetInnermostMessage(ex));
                }
            }

            viewModel.ConditionOptions = GetConditionSelectList(viewModel.FinalCondition);
            return View(viewModel);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Re-parses a decimal form field using InvariantCulture and CurrentCulture.
        /// Fixes "The value '332.8' is not valid" when the server culture uses comma decimals.
        /// </summary>
        private void FixDecimalBinding(object viewModel, string propertyName, Action<decimal> setter)
        {
            var raw = Request.Form[propertyName];
            if (string.IsNullOrWhiteSpace(raw))
                return;

            decimal value;
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out value) ||
                decimal.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out value))
            {
                setter(value);
                ModelState.Remove(propertyName);
            }
        }

        private static string Def(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string GetInnermostMessage(Exception ex)
        {
            if (ex == null) return "Unknown error";
            while (ex.InnerException != null)
                ex = ex.InnerException;
            return ex.Message ?? "Unknown error";
        }

        private void LogDbEntityValidation(DbEntityValidationException dbEx)
        {
            foreach (var eve in dbEx.EntityValidationErrors)
            {
                foreach (var ve in eve.ValidationErrors)
                {
                    System.Diagnostics.Debug.WriteLine($"EF Validation: {ve.PropertyName} — {ve.ErrorMessage}");
                }
            }
        }

        private string GenerateUniqueEquipmentCode()
        {
            const int maxAttempts = 20;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var code = "EQP" + DateTime.Now.ToString("yyyyMMddHHmmss") +
                           new Random(Guid.NewGuid().GetHashCode()).Next(100, 999).ToString();

                if (!db.Equipment.Any(e => e.EquipmentCode == code))
                    return code;

                System.Threading.Thread.Sleep(5);
            }

            return "EQP" + Guid.NewGuid().ToString("N").Substring(0, 16).ToUpperInvariant();
        }

        private EquipmentListItemViewModel MapToListItem(Equipment e)
        {
            var today = DateTime.Today;
            string badgeText = "OK";
            string badgeColor = "success";

            if (e.NextMaintenanceDate.HasValue)
            {
                if (e.NextMaintenanceDate.Value < today)
                {
                    badgeText = "Overdue";
                    badgeColor = "danger";
                }
                else if (e.NextMaintenanceDate.Value <= today.AddDays(7))
                {
                    badgeText = "Due";
                    badgeColor = "warning";
                }
                else if (e.NextMaintenanceDate.Value <= today.AddDays(14))
                {
                    badgeText = "Due Soon";
                    badgeColor = "info";
                }
            }

            return new EquipmentListItemViewModel
            {
                Id = e.Id,
                EquipmentName = e.EquipmentName,
                EquipmentCode = e.EquipmentCode,
                SerialNumber = e.SerialNumber,
                Category = e.Category,
                Status = e.Status,
                StatusBadgeColor = GetEquipmentStatusColor(e.Status),
                Condition = e.Condition,
                ConditionBadgeColor = GetConditionColor(e.Condition),
                Location = e.Location,
                AssignedProjectName = e.AssignedProject != null ? e.AssignedProject.ProjectName : null,
                NextMaintenanceDate = e.NextMaintenanceDate,
                MaintenanceBadgeColor = badgeColor,
                MaintenanceBadgeText = badgeText
            };
        }

        private void RunMaintenanceDueCheckIfNeeded()
        {
            try
            {
                if (_lastMaintenanceCheck.Date >= DateTime.Today)
                    return;

                lock (_maintenanceCheckLock)
                {
                    if (_lastMaintenanceCheck.Date >= DateTime.Today)
                        return;

                    var dueCutoff = DateTime.Today.AddDays(7);
                    var dueItems = db.Equipment
                        .Where(e => e.IsActive && e.NextMaintenanceDate != null && e.NextMaintenanceDate <= dueCutoff)
                        .ToList();

                    var adminIds = db.Users.ToList()
                        .Where(u => UserManager.IsInRole(u.Id, "Admin"))
                        .Select(u => u.Id)
                        .ToList();

                    var todayStart = DateTime.Today;

                    foreach (var eq in dueItems)
                    {
                        var link = $"/Equipment/Details/{eq.Id}";
                        foreach (var adminId in adminIds)
                        {
                            var alreadyNotified = db.Notifications.Any(n =>
                                n.UserId == adminId &&
                                n.Link == link &&
                                n.Type == "Maintenance" &&
                                n.CreatedDate >= todayStart);

                            if (!alreadyNotified)
                            {
                                var title = "Equipment maintenance due";
                                var message = $"{eq.EquipmentName} ({eq.EquipmentCode}) is due for maintenance on {eq.NextMaintenanceDate:dd MMM yyyy}.";
                                var notification = new Notification
                                {
                                    UserId = adminId,
                                    Title = title,
                                    Message = message,
                                    Type = "Maintenance",
                                    Link = link,
                                    CreatedDate = DateTime.Now,
                                    IsRead = false
                                };
                                db.Notifications.Add(notification);
                            }
                        }
                    }

                    db.SaveChanges();
                    _lastMaintenanceCheck = DateTime.Today;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RunMaintenanceDueCheckIfNeeded error: {ex.Message}");
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
                    Title = title ?? string.Empty,
                    Message = message ?? string.Empty,
                    Type = type ?? "Maintenance",
                    Link = link ?? string.Empty,
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

        private async Task NotifyStakeholder(string userId, string title, string message, string type, string link)
        {
            if (string.IsNullOrEmpty(userId)) return;
            await CreateNotification(userId, title, message, type, link);
        }

        private async Task NotifyAdmins(string title, string message, string link, string skipUserId = null)
        {
            try
            {
                var adminIds = db.Users.ToList()
                    .Where(u => UserManager.IsInRole(u.Id, "Admin") && u.Id != skipUserId)
                    .Select(u => u.Id)
                    .ToList();

                foreach (var adminId in adminIds)
                {
                    await CreateNotification(adminId, title, message, "Maintenance", link);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error notifying admins: {ex.Message}");
            }
        }

        private async Task NotifyAdminsOfDamage(Equipment equipment, EquipmentRepair repair, string reportedBy)
        {
            var message = $"{reportedBy ?? "Crew Member"} reported damage on '{equipment?.EquipmentName}' (SN: {equipment?.SerialNumber}). Ticket #RT-{repair?.Id}.";
            await NotifyAdmins("Equipment Damage Reported", message, $"/Equipment/RepairDetails/{repair?.Id}");
        }

        private List<SelectListItem> GetCategorySelectList(string selected)
        {
            var cats = new[] { "Heavy Equipment", "Light Equipment", "Tools", "Vehicles", "Safety Gear", "Other" };
            return cats.Select(c => new SelectListItem { Value = c, Text = c, Selected = c == selected }).ToList();
        }

        private List<SelectListItem> GetConditionSelectList(string selected)
        {
            var items = new[] { "Excellent", "Good", "Fair", "Poor" };
            return items.Select(c => new SelectListItem { Value = c, Text = c, Selected = c == selected }).ToList();
        }

        private List<SelectListItem> GetStatusSelectList(string selected)
        {
            var items = new[] { "Available", "In Use", "Under Maintenance", "In Repair", "Damaged", "Retired" };
            return items.Select(c => new SelectListItem { Value = c, Text = c, Selected = c == selected }).ToList();
        }

        private List<SelectListItem> GetMaintenanceFrequencySelectList(int? selected)
        {
            var options = new[]
            {
                new { Value = (int?)null, Text = "Never" },
                new { Value = (int?)30, Text = "30 days" },
                new { Value = (int?)60, Text = "60 days" },
                new { Value = (int?)90, Text = "90 days" },
                new { Value = (int?)180, Text = "180 days" },
                new { Value = (int?)365, Text = "365 days" }
            };
            return options.Select(o => new SelectListItem
            {
                Value = o.Value.HasValue ? o.Value.Value.ToString() : "",
                Text = o.Text,
                Selected = o.Value == selected
            }).ToList();
        }

        private List<SelectListItem> GetRepairProgressStatusSelectList(string selected)
        {
            var items = new[] { "In Repair", "Awaiting Parts", "Ready for Verification", "Cancelled" };
            return items.Select(c => new SelectListItem { Value = c, Text = c, Selected = c == selected }).ToList();
        }

        private static string GetEquipmentStatusColor(string status)
        {
            switch (status)
            {
                case "Available": return "success";
                case "In Use": return "primary";
                case "Under Maintenance":
                case "In Repair": return "warning";
                case "Damaged": return "danger";
                case "Retired": return "secondary";
                default: return "secondary";
            }
        }

        private static string GetConditionColor(string condition)
        {
            switch (condition)
            {
                case "Excellent": return "success";
                case "Good": return "info";
                case "Fair": return "warning";
                case "Poor": return "danger";
                default: return "secondary";
            }
        }

        private static string GetRepairStatusColor(string status)
        {
            switch (status)
            {
                case "Reported": return "danger";
                case "In Repair": return "warning";
                case "Awaiting Parts": return "info";
                case "Ready for Verification": return "primary";
                case "Verified": return "success";
                case "Cancelled": return "secondary";
                default: return "secondary";
            }
        }

        private static string GetMaintenanceStatusColor(string status)
        {
            switch (status)
            {
                case "Scheduled": return "info";
                case "In Progress": return "warning";
                case "Completed": return "success";
                case "Cancelled": return "danger";
                default: return "secondary";
            }
        }

        private CreateEquipmentViewModel RepopulateCreateDropdowns(CreateEquipmentViewModel viewModel)
        {
            if (viewModel == null)
                viewModel = new CreateEquipmentViewModel();

            viewModel.Categories = GetCategorySelectList(viewModel.Category);
            viewModel.Conditions = GetConditionSelectList(viewModel.Condition);
            viewModel.Statuses = GetStatusSelectList(viewModel.Status);
            viewModel.MaintenanceFrequencies = GetMaintenanceFrequencySelectList(viewModel.MaintenanceFrequencyDays);
            return viewModel;
        }

        private EditEquipmentViewModel RepopulateEditDropdowns(EditEquipmentViewModel viewModel)
        {
            if (viewModel == null)
                viewModel = new EditEquipmentViewModel();

            viewModel.Categories = GetCategorySelectList(viewModel.Category);
            viewModel.Conditions = GetConditionSelectList(viewModel.Condition);
            viewModel.Statuses = GetStatusSelectList(viewModel.Status);
            viewModel.MaintenanceFrequencies = GetMaintenanceFrequencySelectList(viewModel.MaintenanceFrequencyDays);
            return viewModel;
        }

        private ReportDamageViewModel RepopulateReportDamageDropdowns(ReportDamageViewModel viewModel, int staffId)
        {
            if (viewModel == null)
                viewModel = new ReportDamageViewModel();

            var myProjectIds = db.ProjectResourceAllocations
                .Where(a => a.StaffId == staffId && a.IsActive
                    && a.StartDate <= DateTime.Today
                    && (a.EndDate == null || a.EndDate >= DateTime.Today))
                .Select(a => a.ProjectId)
                .Distinct()
                .ToList();

            viewModel.AvailableEquipment = db.Equipment
                .Where(e => e.IsActive
                    && e.Status != "Damaged" && e.Status != "In Repair" && e.Status != "Retired"
                    && (e.Status == "Available"
                        || (e.Status == "In Use" && e.AssignedProjectId.HasValue && myProjectIds.Contains(e.AssignedProjectId.Value))))
                .OrderBy(e => e.EquipmentName)
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = e.EquipmentName + " — SN: " + e.SerialNumber + " — " + e.Condition
                }).ToList();

            return viewModel;
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