using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
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
        // Part A — Inventory (Admin)[cite: 12]
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
                UnderMaintenanceCount = allActive.Count(e => e.Status == "Under Maintenance" || e.Status == "In Repair" || e.Status == "Damaged" || e.Status == "Quarantined"),
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
            var defaultCat = "Heavy Equipment";
            var viewModel = new CreateEquipmentViewModel
            {
                Status = "Available",
                Condition = "Good",
                Category = defaultCat,
                SerialNumber = GenerateSerialNumber(defaultCat),
                EquipmentNameSuggestions = new List<string>
                {
                    "Excavator", "Mini Excavator", "Backhoe Loader", "Bulldozer",
                    "Wheel Loader", "Skid Steer Loader", "Forklift", "Telehandler",
                    "Tower Crane", "Mobile Crane", "Crawler Crane", "Boom Lift",
                    "Scissor Lift", "Concrete Mixer", "Concrete Pump", "Concrete Vibrator",
                    "Compactor", "Plate Compactor", "Vibratory Roller", "Dump Truck",
                    "Tipper Truck", "Flatbed Truck", "Pickup Truck", "Water Tanker",
                    "Generator", "Air Compressor", "Jackhammer", "Demolition Hammer",
                    "Angle Grinder", "Welding Machine", "Scaffolding Set", "Power Trowel",
                    "Concrete Saw", "Reinforcement Bender", "Bar Cutting Machine",
                    "Survey Level", "Total Station", "Van", "Bakkie", "Trailer"
                },
                CategorySuggestions = new List<string>
                {
                    "Heavy Equipment", "Light Equipment", "Tools", "Vehicles",
                    "Safety Gear", "Other", "Lifting Equipment", "Concrete Equipment",
                    "Earthmoving Equipment", "Power Tools", "Survey Equipment"
                },
                Categories = GetCategorySelectList(defaultCat),
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
                        TotalCost = r.TotalCost,
                        IsEscalated = r.IsEscalated,
                        IsDelayFlagged = r.ProgressLogs != null && r.ProgressLogs.Any(l => l.IsDelayFlagged),
                        RequiresProcurement = r.RequiresProcurement
                    }).ToList()
            };

            return View(viewModel);
        }

        // ------------------------------------------------------------------
        // Part B — Maintenance Due[cite: 12]
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
        // Part C — Availability Helpers[cite: 12]
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
        // Part D — Report Damage (Crew Member)[cite: 12]
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

            var myProjects = db.ProjectResourceAllocations
                .Include(a => a.Project)
                .Where(a => a.StaffId == staff.Id && a.IsActive
                    && a.StartDate <= DateTime.Today
                    && (a.EndDate == null || a.EndDate >= DateTime.Today))
                .ToList();

            var myProjectIds = myProjects.Select(a => a.ProjectId).Distinct().ToList();
            var projectNamesList = myProjects.Where(p => p.Project != null).Select(p => p.Project.ProjectName).Distinct().ToList();
            string filterLabel = projectNamesList.Any() ? string.Join(", ", projectNamesList) : "no active projects";

            var equipmentQuery = db.Equipment
                .Where(e => e.IsActive
                    && e.Status != "Damaged"
                    && e.Status != "In Repair"
                    && e.Status != "Retired"
                    && e.Status != "Quarantined"
                    && e.Status == "In Use"
                    && e.AssignedProjectId.HasValue
                    && myProjectIds.Contains(e.AssignedProjectId.Value));

            var viewModel = new ReportDamageViewModel
            {
                ProjectFilterLabel = filterLabel,
                AvailableEquipment = equipmentQuery
                    .OrderBy(e => e.EquipmentName)
                    .Select(e => new SelectListItem
                    {
                        Value = e.Id.ToString(),
                        Text = e.EquipmentName + " — SN: " + e.SerialNumber + " — " + e.Condition
                    }).ToList(),
                SeverityOptions = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Minor", Text = "Minor" },
                    new SelectListItem { Value = "Major", Text = "Major" },
                    new SelectListItem { Value = "Critical", Text = "Critical" },
                    new SelectListItem { Value = "Unsafe", Text = "Unsafe" }
                }
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
                    var existingOpenTicket = db.EquipmentRepairs
                        .FirstOrDefault(r => r.EquipmentId == viewModel.EquipmentId && r.Status != "Verified" && r.Status != "Cancelled");

                    if (existingOpenTicket != null)
                    {
                        ModelState.AddModelError("", $"This equipment already has an open repair ticket (#{existingOpenTicket.Id}). Please contact the Admin instead of reporting it again.");
                        return View(RepopulateReportDamageDropdowns(viewModel, staff.Id));
                    }

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

                    bool allowed = equipment.Status == "In Use" && equipment.AssignedProjectId.HasValue && myProjectIds.Contains(equipment.AssignedProjectId.Value);

                    if (!allowed || equipment.Status == "Damaged" || equipment.Status == "In Repair" || equipment.Status == "Quarantined")
                    {
                        ModelState.AddModelError("EquipmentId", "You may only report damage on equipment assigned to one of your active projects.");
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
                        VerifiedBy = string.Empty,
                        Severity = Def(viewModel.Severity),
                        WitnessName = Def(viewModel.WitnessName),
                        ReportedLocation = equipment.Location
                    };

                    db.EquipmentRepairs.Add(repair);

                    if (string.Equals(repair.Severity, "Critical", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(repair.Severity, "Unsafe", StringComparison.OrdinalIgnoreCase))
                    {
                        equipment.Status = "Quarantined";
                        equipment.AssignedProjectId = null;

                        var activeAssignments = db.EquipmentAssignments
                            .Where(a => a.EquipmentId == equipment.Id && a.IsActive)
                            .ToList();

                        foreach (var assignment in activeAssignments)
                        {
                            assignment.IsActive = false;
                            assignment.ActualReturnDate = DateTime.Now;
                            assignment.ConditionOnReturn = "Quarantined";
                        }
                    }
                    else
                    {
                        equipment.Status = "Damaged";
                    }

                    await db.SaveChangesAsync();

                    if (viewModel.Photos != null)
                    {
                        var pathList = new List<string>();
                        foreach (var photo in viewModel.Photos)
                        {
                            if (photo != null && photo.ContentLength > 0)
                            {
                                try
                                {
                                    var folder = Server.MapPath($"~/Uploads/EquipmentDamage/{repair.Id}");
                                    if (!Directory.Exists(folder))
                                        Directory.CreateDirectory(folder);

                                    var ext = Path.GetExtension(photo.FileName) ?? ".jpg";
                                    var fileName = Guid.NewGuid().ToString("N") + ext;
                                    var physicalPath = Path.Combine(folder, fileName);
                                    photo.SaveAs(physicalPath);

                                    pathList.Add($"/Uploads/EquipmentDamage/{repair.Id}/{fileName}");
                                }
                                catch (Exception photoEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Photo upload failed: {photoEx.Message}");
                                }
                            }
                        }

                        if (pathList.Any())
                        {
                            repair.PhotoEvidencePaths = string.Join(";", pathList);
                            await db.SaveChangesAsync();
                        }
                    }

                    await NotifyAdminsOfDamage(equipment, repair, staff.FullName ?? "Crew Member");

                    TempData["SuccessMessage"] = $"Damage reported for '{equipment.EquipmentName}'. Admins have been notified.";
                    return RedirectToAction("Dashboard", "Home");
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
        // Part E — Repair Queue / Schedule[cite: 12]
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Site Manager")]
        public ActionResult RepairQueue(string status = null)
        {
            var query = db.EquipmentRepairs
                .Include(r => r.Equipment)
                .Include(r => r.Equipment.AssignedProject)
                .Include(r => r.ProgressLogs)
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
                    TotalCost = r.TotalCost,
                    IsEscalated = r.IsEscalated,
                    IsDelayFlagged = r.ProgressLogs != null && r.ProgressLogs.Any(l => l.IsDelayFlagged),
                    Severity = r.Severity,
                    SeverityBadgeColor = GetSeverityBadgeColor(r.Severity),
                    Priority = r.Priority,
                    PriorityBadgeColor = GetPriorityBadgeColor(r.Priority),
                    RequiresApproval = r.RequiresApproval,
                    IsQuarantined = r.Equipment != null && r.Equipment.Status == "Quarantined",
                    RequiresProcurement = r.RequiresProcurement
                }).ToList();

            var all = db.EquipmentRepairs.Include(r => r.ProgressLogs).Include(r => r.Equipment).ToList();
            var viewModel = new EquipmentRepairQueueViewModel
            {
                Items = items,
                ReportedCount = all.Count(r => r.Status == "Reported"),
                InRepairCount = all.Count(r => r.Status == "In Repair" || r.Status == "Awaiting Parts" || r.Status == "Awaiting Approval"),
                ReadyForVerificationCount = all.Count(r => r.Status == "Ready for Verification"),
                VerifiedCount = all.Count(r => r.Status == "Verified"),
                ActiveFilter = status ?? "all"
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin, Site Manager, Project Manager, Crew Member, Finance Officer")]
        public ActionResult RepairDetails(int id)
        {
            var repair = db.EquipmentRepairs
                .Include(r => r.Equipment)
                .Include(r => r.Equipment.AssignedProject)
                .Include(r => r.Equipment.AssignedProject.ProjectManager)
                .Include(r => r.ProgressLogs)
                .Include(r => r.PartUsages.Select(u => u.EquipmentPart))
                .Include(r => r.Technician)
                .Include(r => r.CheckoutChecklist)
                .FirstOrDefault(r => r.Id == id);

            if (repair == null)
            {
                TempData["ErrorMessage"] = "Repair ticket not found.";
                return RedirectToAction("RepairQueue");
            }

            var photoPaths = new List<string>();
            if (!string.IsNullOrEmpty(repair.PhotoEvidencePaths))
            {
                photoPaths.AddRange(repair.PhotoEvidencePaths.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }
            else if (!string.IsNullOrEmpty(repair.PhotoEvidencePath))
            {
                photoPaths.Add(repair.PhotoEvidencePath);
            }

            CheckoutChecklistViewModel checklistVm = null;
            if (repair.CheckoutChecklist != null)
            {
                checklistVm = new CheckoutChecklistViewModel
                {
                    LoadTestPassed = repair.CheckoutChecklist.LoadTestPassed,
                    SafetyCheckPassed = repair.CheckoutChecklist.SafetyCheckPassed,
                    CalibrationVerified = repair.CheckoutChecklist.CalibrationVerified,
                    VisualInspectionPassed = repair.CheckoutChecklist.VisualInspectionPassed,
                    Notes = repair.CheckoutChecklist.Notes,
                    TechnicianSignOffName = repair.CheckoutChecklist.TechnicianSignOffName,
                    TechnicianSignOffDate = repair.CheckoutChecklist.TechnicianSignOffDate,
                    AdminSignOffName = repair.CheckoutChecklist.AdminSignOffName,
                    AdminSignOffDate = repair.CheckoutChecklist.AdminSignOffDate,
                    LoadTestResultKg = repair.CheckoutChecklist.LoadTestResultKg
                };
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
                CreatedDate = repair.CreatedDate,
                ActualHours = repair.ActualHours,
                RevisedCompletionDate = repair.RevisedCompletionDate,
                PhotoEvidencePath = repair.PhotoEvidencePath,
                IsDelayFlagged = repair.ProgressLogs != null && repair.ProgressLogs.Any(l => l.IsDelayFlagged),
                IsEscalated = repair.IsEscalated,
                ProcurementRequestReference = repair.ProcurementRequestReference,
                CancellationReason = repair.CancellationReason,
                Severity = repair.Severity,
                SeverityBadgeColor = GetSeverityBadgeColor(repair.Severity),
                Priority = repair.Priority,
                PriorityBadgeColor = GetPriorityBadgeColor(repair.Priority),
                RepairCategory = repair.RepairCategory,
                DiagnosisNotes = repair.DiagnosisNotes,
                WitnessName = repair.WitnessName,
                ReportedLocation = repair.ReportedLocation,
                PhotoEvidencePaths = photoPaths,
                EstimatedPartsCost = repair.EstimatedPartsCost,
                EstimatedLabourCost = repair.EstimatedLabourCost,
                EstimatedTotalCost = repair.EstimatedTotalCost,
                RequiresApproval = repair.RequiresApproval,
                TechnicianId = repair.TechnicianId,
                CheckoutChecklist = checklistVm,
                IsQuarantined = repair.Equipment != null && repair.Equipment.Status == "Quarantined",
                RequiresProcurement = repair.RequiresProcurement,
                PartsNotes = repair.PartsNotes,
                ProgressLogs = (repair.ProgressLogs ?? Enumerable.Empty<RepairProgressLog>())
                    .OrderByDescending(l => l.ChangedAt)
                    .Select(l => new RepairProgressLogItemViewModel
                    {
                        Id = l.Id,
                        Status = l.Status,
                        Notes = l.Notes,
                        PartsUsedSummary = l.PartsUsedSummary,
                        ActualHours = l.ActualHours,
                        RevisedCompletionDate = l.RevisedCompletionDate,
                        PhotoPath = l.PhotoPath,
                        ChangedBy = l.ChangedBy,
                        ChangedAt = l.ChangedAt,
                        ChangeType = l.ChangeType,
                        IsDelayFlagged = l.IsDelayFlagged
                    }).ToList(),
                PartUsages = (repair.PartUsages ?? Enumerable.Empty<RepairPartUsage>())
                    .Select(u => new RepairPartUsageItemViewModel
                    {
                        Id = u.Id,
                        PartName = u.EquipmentPart?.PartName ?? "Unknown Part",
                        PartCode = u.EquipmentPart?.PartCode,
                        QuantityUsed = u.QuantityUsed,
                        UnitCostAtUse = u.UnitCostAtUse,
                        UsedDate = u.UsedDate,
                        RecordedBy = u.RecordedBy
                    }).ToList()
            };

            return View(vm);
        }

        // ------------------------------------------------------------------
        // Part F — 3-Step Sequential Update Repair Progress Workflow
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Site Manager")]
        public ActionResult UpdateRepairProgress(int id)
        {
            var repair = db.EquipmentRepairs
                .Include(r => r.Equipment)
                .Include(r => r.ProgressLogs)
                .Include(r => r.PartUsages)
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

            // Determine active step sequentially (1, 2, or 3)
            int stepNum = 1;
            string stepTitle = "Step 1: Schedule Repair & Assign Technician";

            bool step1Complete = repair.TechnicianId.HasValue;
            bool step2Complete = (repair.ProgressLogs != null && repair.ProgressLogs.Any(l => l.PartsUsedSummary != null && l.PartsUsedSummary.Contains("Step 2")))
                                 || (repair.PartUsages != null && repair.PartUsages.Any())
                                 || repair.Status == "Awaiting Parts";
            bool step3Complete = repair.Status == "Ready for Verification" || (repair.PartsCost > 0 || repair.LabourCost > 0);

            if (step1Complete)
            {
                stepNum = 2;
                stepTitle = "Step 2: Parts Management & Inventory Selection";
            }
            if (step2Complete || step3Complete)
            {
                stepNum = 3;
                stepTitle = "Step 3: Cost Input & Ready for Verification";
            }

            // Allow selecting unlocked steps via query string parameter if completed
            if (int.TryParse(Request.QueryString["step"], out var reqStep) && reqStep >= 1 && reqStep <= 3)
            {
                if (reqStep == 1)
                {
                    stepNum = 1;
                    stepTitle = "Step 1: Schedule Repair & Assign Technician";
                }
                else if (reqStep == 2 && step1Complete)
                {
                    stepNum = 2;
                    stepTitle = "Step 2: Parts Management & Inventory Selection";
                }
                else if (reqStep == 3 && (step2Complete || step3Complete))
                {
                    stepNum = 3;
                    stepTitle = "Step 3: Cost Input & Ready for Verification";
                }
            }

            var technicians = db.EquipmentTechnicians.Where(t => t.IsActive).ToList();
            var techSelectList = technicians.Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.FullName + (!string.IsNullOrEmpty(t.Specialization) ? " (" + t.Specialization + ")" : ""),
                Selected = repair.TechnicianId == t.Id
            }).ToList();

            var availablePartsList = db.EquipmentParts
                .Where(p => p.IsActive)
                .OrderBy(p => p.PartName)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.PartName + (!string.IsNullOrEmpty(p.PartCode) ? " (" + p.PartCode + ")" : "") + " [Stock: " + p.QuantityOnHand + "]"
                })
                .ToList();

            var viewModel = new UpdateRepairProgressViewModel
            {
                RepairId = repair.Id,
                EquipmentName = repair.Equipment?.EquipmentName,
                CurrentStepNumber = stepNum,
                CurrentStepTitle = stepTitle,
                TechnicianId = repair.TechnicianId,
                TechnicianName = repair.TechnicianName,
                PartsCost = repair.PartsCost,
                LabourCost = repair.LabourCost,
                TotalCost = repair.TotalCost,
                RepairNotes = repair.RepairNotes,
                Status = repair.Status,
                OriginalStatus = repair.Status,
                RevisedCompletionDate = repair.RevisedCompletionDate,
                RowVersion = repair.RowVersion,
                AvailableTechnicians = techSelectList,
                AvailableParts = availablePartsList,
                PartsUsed = (repair.PartUsages ?? Enumerable.Empty<RepairPartUsage>())
                    .Select(u => new UpdateRepairPartInputModel
                    {
                        EquipmentPartId = u.EquipmentPartId,
                        QuantityUsed = u.QuantityUsed
                    }).ToList()
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

            FixDecimalBinding(viewModel, "PartsCost", v => viewModel.PartsCost = v);
            FixDecimalBinding(viewModel, "LabourCost", v => viewModel.LabourCost = v);
            ModelState.Remove("TotalCost");
            ModelState.Remove("Status"); // Status is auto-managed

            viewModel.TotalCost = viewModel.PartsCost + viewModel.LabourCost;

            // DEBUG: Log ModelState errors to Visual Studio Output window
            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    foreach (var err in ModelState[key].Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] ModelState Error -> Key: {key}, Error: {err.ErrorMessage}");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var repair = db.EquipmentRepairs
                        .Include(r => r.Equipment)
                        .Include(r => r.ProgressLogs)
                        .Include(r => r.PartUsages)
                        .FirstOrDefault(r => r.Id == viewModel.RepairId);

                    if (repair == null || repair.Status == "Verified" || repair.Status == "Cancelled")
                    {
                        TempData["ErrorMessage"] = "Repair ticket not found or already closed.";
                        return RedirectToAction("RepairQueue");
                    }

                    string autoStatus = repair.Status;
                    string progressNotesPrefix = string.Empty;

                    if (viewModel.CurrentStepNumber == 1)
                    {
                        EquipmentTechnician selectedTech = null;
                        if (viewModel.TechnicianId.HasValue)
                        {
                            selectedTech = db.EquipmentTechnicians.Find(viewModel.TechnicianId.Value);
                            if (selectedTech != null)
                            {
                                repair.TechnicianId = selectedTech.Id;
                                repair.TechnicianName = selectedTech.FullName;
                            }
                        }

                        if (viewModel.RevisedCompletionDate.HasValue)
                        {
                            repair.RevisedCompletionDate = viewModel.RevisedCompletionDate.Value;
                            if (repair.Equipment != null)
                            {
                                repair.Equipment.NextMaintenanceDate = repair.RevisedCompletionDate.Value;
                            }
                        }

                        if (viewModel.PhotoEvidence != null && viewModel.PhotoEvidence.ContentLength > 0)
                        {
                            try
                            {
                                var folder = Server.MapPath($"~/Uploads/EquipmentDamage/{repair.Id}");
                                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                                var ext = Path.GetExtension(viewModel.PhotoEvidence.FileName) ?? ".jpg";
                                var fileName = Guid.NewGuid().ToString("N") + ext;
                                viewModel.PhotoEvidence.SaveAs(Path.Combine(folder, fileName));
                                repair.PhotoEvidencePath = $"/Uploads/EquipmentDamage/{repair.Id}/{fileName}";
                            }
                            catch (Exception photoEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"[DEBUG] Photo upload failed: {photoEx.Message}");
                            }
                        }

                        autoStatus = "In Repair";
                        progressNotesPrefix = "Completed Step 1: Schedule Repair & Assign Technician.";
                    }
                    else if (viewModel.CurrentStepNumber == 2)
                    {
                        bool partsUnavailable = false;
                        string partsSummary = string.Empty;

                        if (!viewModel.NoPartsNeeded && viewModel.PartsUsed != null && viewModel.PartsUsed.Any())
                        {
                            foreach (var partInput in viewModel.PartsUsed.Where(p => p.QuantityUsed > 0 && p.EquipmentPartId.HasValue))
                            {
                                var partEntity = db.EquipmentParts.Find(partInput.EquipmentPartId.Value);
                                if (partEntity != null)
                                {
                                    int qtyNeeded = partInput.QuantityUsed;
                                    if (partEntity.QuantityOnHand < qtyNeeded)
                                    {
                                        partsUnavailable = true;
                                    }

                                    var usage = new RepairPartUsage
                                    {
                                        EquipmentRepairId = repair.Id,
                                        EquipmentPartId = partEntity.Id,
                                        QuantityUsed = qtyNeeded,
                                        UnitCostAtUse = partEntity.UnitCost ?? 0m,
                                        UsedDate = DateTime.Now,
                                        RecordedBy = User.Identity.Name
                                    };
                                    db.RepairPartUsages.Add(usage);

                                    int deductQty = Math.Min(qtyNeeded, partEntity.QuantityOnHand);
                                    partEntity.QuantityOnHand -= deductQty;

                                    partsSummary += $"{partEntity.PartName} (x{qtyNeeded}); ";
                                }
                            }
                        }

                        if (partsUnavailable && !viewModel.NoPartsNeeded)
                        {
                            autoStatus = "Awaiting Parts";
                            repair.RequiresProcurement = true;

                            var assignment = db.EquipmentAssignments
                                .Include(a => a.Project)
                                .Include(a => a.Project.ProjectManager)
                                .Where(a => a.EquipmentId == repair.EquipmentId && a.IsActive)
                                .OrderByDescending(a => a.AssignedDate)
                                .FirstOrDefault();

                            if (assignment?.Project?.ProjectManager?.UserId != null)
                            {
                                await CreateNotification(
                                    assignment.Project.ProjectManager.UserId,
                                    $"Parts Unavailable — RT-{repair.Id}",
                                    $"Repair #{repair.Id} for '{repair.Equipment?.EquipmentName}' is awaiting inventory parts which are currently out of stock.",
                                    "Maintenance",
                                    $"/Equipment/RepairDetails/{repair.Id}"
                                );
                            }
                        }
                        else
                        {
                            autoStatus = "In Repair";
                        }

                        progressNotesPrefix = "Completed Step 2: Parts Management & Inventory Selection.";
                    }
                    else if (viewModel.CurrentStepNumber == 3)
                    {
                        repair.PartsCost = viewModel.PartsCost;
                        repair.LabourCost = viewModel.LabourCost;
                        repair.TotalCost = viewModel.PartsCost + viewModel.LabourCost;

                        autoStatus = "Ready for Verification";
                        progressNotesPrefix = "Completed Step 3: Cost Input & Ready for Verification.";
                    }

                    repair.Status = autoStatus;
                    repair.RepairNotes = Def(repair.RepairNotes) + $" [{DateTime.Now:yyyy-MM-dd HH:mm} by {User.Identity.Name}] {progressNotesPrefix} {Def(viewModel.RepairNotes)}";

                    var progressLog = new RepairProgressLog
                    {
                        EquipmentRepairId = repair.Id,
                        Status = repair.Status,
                        Notes = Def(viewModel.RepairNotes),
                        PartsUsedSummary = progressNotesPrefix,
                        ChangedBy = Def(User.Identity.Name),
                        ChangedByUserId = Def(User.Identity.GetUserId()),
                        ChangedAt = DateTime.Now,
                        ChangeType = "ProgressUpdate"
                    };
                    db.RepairProgressLogs.Add(progressLog);

                    await db.SaveChangesAsync();

                    if (viewModel.CurrentStepNumber == 1 && repair.TechnicianId.HasValue)
                    {
                        var selectedTech = db.EquipmentTechnicians.Find(repair.TechnicianId.Value);
                        if (selectedTech != null && !string.IsNullOrWhiteSpace(selectedTech.Email))
                        {
                            SendTechnicianAssignedEmail(repair, selectedTech, repair.Equipment?.AssignedProject);
                        }
                    }

                    TempData["SuccessMessage"] = "Repair progress updated successfully.";
                    return RedirectToAction("RepairDetails", new { id = repair.Id });
                }
                catch (DbEntityValidationException dbEx)
                {
                    foreach (var eve in dbEx.EntityValidationErrors)
                    {
                        foreach (var ve in eve.ValidationErrors)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DEBUG DB VALIDATION] Property: {ve.PropertyName}, Error: {ve.ErrorMessage}");
                        }
                    }
                    ModelState.AddModelError("", "Database validation error: " + GetInnermostMessage(dbEx));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG EXCEPTION] UpdateRepairProgress Failed: {ex}");
                    ModelState.AddModelError("", "Error updating repair progress: " + GetInnermostMessage(ex));
                }
            }

            RepopulateUpdateProgressVM(viewModel, null);
            return View(viewModel);
        }

        // ------------------------------------------------------------------
        // Part G — Check-Out[cite: 12]
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Site Manager")]
        public ActionResult CheckOutRepair(int id)
        {
            var repair = db.EquipmentRepairs
                .Include(r => r.Equipment)
                .Include(r => r.CheckoutChecklist)
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

            CheckoutChecklistViewModel checklistVm;
            if (repair.CheckoutChecklist != null)
            {
                checklistVm = new CheckoutChecklistViewModel
                {
                    LoadTestPassed = repair.CheckoutChecklist.LoadTestPassed,
                    SafetyCheckPassed = repair.CheckoutChecklist.SafetyCheckPassed,
                    CalibrationVerified = repair.CheckoutChecklist.CalibrationVerified,
                    VisualInspectionPassed = repair.CheckoutChecklist.VisualInspectionPassed,
                    Notes = repair.CheckoutChecklist.Notes,
                    TechnicianSignOffName = !string.IsNullOrEmpty(repair.CheckoutChecklist.TechnicianSignOffName) ? repair.CheckoutChecklist.TechnicianSignOffName : repair.TechnicianName,
                    TechnicianSignOffDate = repair.CheckoutChecklist.TechnicianSignOffDate,
                    AdminSignOffName = !string.IsNullOrEmpty(repair.CheckoutChecklist.AdminSignOffName) ? repair.CheckoutChecklist.AdminSignOffName : User.Identity.Name,
                    AdminSignOffDate = repair.CheckoutChecklist.AdminSignOffDate,
                    LoadTestResultKg = repair.CheckoutChecklist.LoadTestResultKg
                };
            }
            else
            {
                checklistVm = new CheckoutChecklistViewModel
                {
                    TechnicianSignOffName = repair.TechnicianName,
                    AdminSignOffName = User.Identity.Name
                };
            }

            var reallocations = new List<ReallocationSuggestionViewModel>();
            if (repair.Equipment != null && !string.IsNullOrEmpty(repair.Equipment.Category))
            {
                string cat = repair.Equipment.Category;
                var cutoffDate = DateTime.Today.AddDays(-90);

                var recentAssignments = db.EquipmentAssignments
                    .Include(a => a.Project)
                    .Include(a => a.Equipment)
                    .Where(a => a.Equipment.Category == cat && a.AssignedDate >= cutoffDate && a.Project.IsActive)
                    .OrderByDescending(a => a.AssignedDate)
                    .Take(20)
                    .ToList();

                var seenProjects = new HashSet<int>();
                foreach (var a in recentAssignments)
                {
                    if (a.Project != null && seenProjects.Add(a.ProjectId))
                    {
                        reallocations.Add(new ReallocationSuggestionViewModel
                        {
                            ProjectId = a.ProjectId,
                            ProjectName = a.Project.ProjectName,
                            ProjectCode = a.Project.ProjectCode,
                            AssignedDate = a.AssignedDate
                        });
                        if (reallocations.Count >= 5) break;
                    }
                }
            }

            var viewModel = new CheckOutRepairViewModel
            {
                RepairId = repair.Id,
                EquipmentName = repair.Equipment?.EquipmentName,
                EquipmentCode = repair.Equipment?.EquipmentCode,
                TechnicianName = repair.TechnicianName,
                RepairSummary = summary,
                FinalCondition = "Good",
                ConditionOptions = GetConditionSelectList("Good"),
                CheckoutChecklist = checklistVm,
                ReallocationSuggestions = reallocations
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

            var cl = viewModel.CheckoutChecklist ?? new CheckoutChecklistViewModel();
            if (!cl.LoadTestPassed || !cl.SafetyCheckPassed || !cl.CalibrationVerified || !cl.VisualInspectionPassed)
            {
                ModelState.AddModelError("", "All verification items must be ticked before the equipment can be returned to service.");
            }

            if (string.IsNullOrWhiteSpace(cl.TechnicianSignOffName))
            {
                ModelState.AddModelError("CheckoutChecklist.TechnicianSignOffName", "Technician sign-off name is required.");
            }
            if (string.IsNullOrWhiteSpace(cl.AdminSignOffName))
            {
                ModelState.AddModelError("CheckoutChecklist.AdminSignOffName", "Admin sign-off name is required.");
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
                        .Include(r => r.CheckoutChecklist)
                        .FirstOrDefault(r => r.Id == viewModel.RepairId);

                    if (repair == null || repair.Status != "Ready for Verification")
                    {
                        TempData["ErrorMessage"] = "Repair ticket not found or not ready for check-out.";
                        return RedirectToAction("RepairQueue");
                    }

                    if (repair.CheckoutChecklist == null)
                    {
                        repair.CheckoutChecklist = new EquipmentCheckoutChecklist
                        {
                            EquipmentRepairId = repair.Id
                        };
                        db.EquipmentCheckoutChecklists.Add(repair.CheckoutChecklist);
                    }

                    repair.CheckoutChecklist.LoadTestPassed = cl.LoadTestPassed;
                    repair.CheckoutChecklist.SafetyCheckPassed = cl.SafetyCheckPassed;
                    repair.CheckoutChecklist.CalibrationVerified = cl.CalibrationVerified;
                    repair.CheckoutChecklist.VisualInspectionPassed = cl.VisualInspectionPassed;
                    repair.CheckoutChecklist.Notes = Def(cl.Notes);
                    repair.CheckoutChecklist.TechnicianSignOffName = Def(cl.TechnicianSignOffName);
                    repair.CheckoutChecklist.TechnicianSignOffDate = DateTime.UtcNow;
                    repair.CheckoutChecklist.AdminSignOffName = Def(cl.AdminSignOffName);
                    repair.CheckoutChecklist.AdminSignOffDate = DateTime.UtcNow;
                    repair.CheckoutChecklist.LoadTestResultKg = cl.LoadTestResultKg;

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

                    TempData["SuccessMessage"] = "Equipment checked out and returned to service.";
                    return RedirectToAction("RepairDetails", new { id = repair.Id });
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
        // Helper Methods
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin")]
        public JsonResult RegenerateSerialNumber(string category)
        {
            var serial = GenerateSerialNumber(category);
            return Json(new { success = true, serialNumber = serial }, JsonRequestBehavior.AllowGet);
        }

        private string GenerateSerialNumber(string category)
        {
            string catShort = "GEN";
            if (!string.IsNullOrWhiteSpace(category))
            {
                var clean = new string(category.Where(char.IsLetter).ToArray()).ToUpperInvariant();
                if (clean.Length >= 3)
                    catShort = clean.Substring(0, 3);
                else if (clean.Length > 0)
                    catShort = clean.PadRight(3, 'X');
            }

            string dateStr = DateTime.Today.ToString("yyyyMMdd");
            const int maxAttempts = 20;
            var rnd = new Random();

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int rNum = rnd.Next(1000, 10000);
                string candidate = $"SN-{catShort}-{dateStr}-{rNum}";
                if (!db.Equipment.Any(e => e.SerialNumber == candidate))
                {
                    return candidate;
                }
                System.Threading.Thread.Sleep(5);
            }

            return $"SN-{catShort}-{dateStr}-{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant()}";
        }

        private SmtpClient BuildSmtpClient(out string fromEmail, out string fromName)
        {
            fromEmail = System.Configuration.ConfigurationManager.AppSettings["SmtpFromEmail"] ?? "no-reply@muhleops.com";
            fromName = System.Configuration.ConfigurationManager.AppSettings["SmtpFromName"] ?? "MuhleOps System";
            var host = System.Configuration.ConfigurationManager.AppSettings["SmtpHost"];

            if (string.IsNullOrWhiteSpace(host))
                return null;

            int port = 587;
            if (int.TryParse(System.Configuration.ConfigurationManager.AppSettings["SmtpPort"], out var p))
                port = p;

            bool enableSsl = true;
            if (bool.TryParse(System.Configuration.ConfigurationManager.AppSettings["SmtpUseSsl"], out var ssl))
                enableSsl = ssl;

            var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var user = System.Configuration.ConfigurationManager.AppSettings["SmtpUsername"];
            var pass = System.Configuration.ConfigurationManager.AppSettings["SmtpPassword"];
            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
            {
                client.Credentials = new System.Net.NetworkCredential(user, pass);
            }

            return client;
        }

        private void SendTechnicianAssignedEmail(EquipmentRepair repair, EquipmentTechnician tech, Project project)
        {
            try
            {
                if (tech == null || string.IsNullOrWhiteSpace(tech.Email))
                    return;

                var client = BuildSmtpClient(out var fromEmail, out var fromName);
                if (client == null)
                    return;

                string subject = $"Repair Assigned — RT-{repair.Id} | {repair.Equipment?.EquipmentName}";
                var sb = new StringBuilder();
                sb.AppendLine("<!DOCTYPE html>");
                sb.AppendLine("<html><head><style>");
                sb.AppendLine("body { font-family: Arial, sans-serif; color: #333; line-height: 1.5; }");
                sb.AppendLine(".container { max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; }");
                sb.AppendLine(".header { background-color: #F5A623; color: #fff; padding: 15px; text-align: center; border-radius: 4px 4px 0 0; font-size: 18px; font-weight: bold; }");
                sb.AppendLine(".content { padding: 20px; background-color: #fafafa; }");
                sb.AppendLine(".footer { margin-top: 20px; font-size: 12px; color: #777; text-align: center; border-top: 1px solid #eee; padding-top: 10px; }");
                sb.AppendLine("</style></head><body>");
                sb.AppendLine("<div class=\"container\">");
                sb.AppendLine($"<div class=\"header\">Repair Assignment Notification</div>");
                sb.AppendLine("<div class=\"content\">");
                sb.AppendLine($"<p>Dear <strong>{tech.FullName}</strong>,</p>");
                sb.AppendLine($"<p>You have been assigned to repair ticket <strong>#RT-{repair.Id}</strong>.</p>");
                sb.AppendLine("<ul>");
                sb.AppendLine($"<li><strong>Equipment:</strong> {repair.Equipment?.EquipmentName} ({repair.Equipment?.EquipmentCode})</li>");
                if (project != null)
                {
                    sb.AppendLine($"<li><strong>Project:</strong> {project.ProjectName}</li>");
                }
                sb.AppendLine($"<li><strong>Priority:</strong> {repair.Priority}</li>");
                sb.AppendLine($"<li><strong>Category:</strong> {repair.RepairCategory}</li>");
                sb.AppendLine("</ul>");
                sb.AppendLine("<p>Please log into the workshop schedule or report for assignment. This is an automated notification from MuhleOps.</p>");
                sb.AppendLine("</div>");
                sb.AppendLine("<div class=\"footer\">MuhleOps Project Management System &copy; " + DateTime.Now.Year + "</div>");
                sb.AppendLine("</div></body></html>");

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = sb.ToString(),
                    IsBodyHtml = true
                };
                mailMessage.To.Add(tech.Email);

                client.Send(mailMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendTechnicianAssignedEmail failed: {ex.Message}");
            }
        }

        private void RepopulateUpdateProgressVM(UpdateRepairProgressViewModel viewModel, EquipmentRepair repair)
        {
            if (viewModel == null) return;
            viewModel.AvailableParts = db.EquipmentParts
                .Where(p => p.IsActive)
                .OrderBy(p => p.PartName)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.PartName + (!string.IsNullOrEmpty(p.PartCode) ? " (" + p.PartCode + ")" : "") + " [Stock: " + p.QuantityOnHand + "]"
                })
                .ToList();
        }

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

            var latestRepair = e.Repairs != null ? e.Repairs.OrderByDescending(r => r.CreatedDate).FirstOrDefault() : null;
            bool isEsc = latestRepair != null && latestRepair.IsEscalated;
            bool isDelay = latestRepair != null && latestRepair.ProgressLogs != null && latestRepair.ProgressLogs.Any(l => l.IsDelayFlagged);

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
                MaintenanceBadgeText = badgeText,
                IsEscalated = isEsc,
                IsDelayFlagged = isDelay
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
                                var notification = new Notification
                                {
                                    UserId = adminId,
                                    Title = "Equipment maintenance due",
                                    Message = $"{eq.EquipmentName} ({eq.EquipmentCode}) is due for maintenance on {eq.NextMaintenanceDate:dd MMM yyyy}.",
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
            var message = $"{reportedBy ?? "Crew Member"} reported damage on '{equipment?.EquipmentName}' (SN: {equipment?.SerialNumber}). Ticket #RT-{repair?.Id}. Severity: {repair?.Severity}.";
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
            var items = new[] { "Available", "In Use", "Under Maintenance", "In Repair", "Damaged", "Quarantined", "Retired" };
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

        private static string GetEquipmentStatusColor(string status)
        {
            switch (status)
            {
                case "Available": return "success";
                case "In Use": return "primary";
                case "Under Maintenance":
                case "In Repair": return "warning";
                case "Damaged": return "danger";
                case "Quarantined": return "dark";
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
                case "Awaiting Parts":
                case "Awaiting Approval": return "info";
                case "Ready for Verification": return "primary";
                case "Verified": return "success";
                case "Cancelled": return "secondary";
                default: return "secondary";
            }
        }

        private static string GetSeverityBadgeColor(string severity)
        {
            if (string.IsNullOrEmpty(severity)) return "secondary";
            switch (severity.ToLower())
            {
                case "minor": return "info";
                case "major": return "warning";
                case "critical": return "danger";
                case "unsafe": return "dark";
                default: return "secondary";
            }
        }

        private static string GetPriorityBadgeColor(string priority)
        {
            if (string.IsNullOrEmpty(priority)) return "secondary";
            switch (priority.ToLower())
            {
                case "low": return "secondary";
                case "medium": return "info";
                case "high": return "warning";
                case "emergency": return "danger";
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

        private ReportDamageViewModel RepopulateReportDamageDropdowns(ReportDamageViewModel viewModel, int staffId)
        {
            if (viewModel == null)
                viewModel = new ReportDamageViewModel();

            var myProjects = db.ProjectResourceAllocations
                .Include(a => a.Project)
                .Where(a => a.StaffId == staffId && a.IsActive
                    && a.StartDate <= DateTime.Today
                    && (a.EndDate == null || a.EndDate >= DateTime.Today))
                .ToList();

            var myProjectIds = myProjects.Select(a => a.ProjectId).Distinct().ToList();
            var projectNamesList = myProjects.Where(p => p.Project != null).Select(p => p.Project.ProjectName).Distinct().ToList();
            viewModel.ProjectFilterLabel = projectNamesList.Any() ? string.Join(", ", projectNamesList) : "no active projects";

            viewModel.AvailableEquipment = db.Equipment
                .Where(e => e.IsActive
                    && e.Status != "Damaged" && e.Status != "In Repair" && e.Status != "Retired" && e.Status != "Quarantined"
                    && e.Status == "In Use"
                    && e.AssignedProjectId.HasValue
                    && myProjectIds.Contains(e.AssignedProjectId.Value))
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