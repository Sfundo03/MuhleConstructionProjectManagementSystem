using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MuhleOP.Models;
using MuhleOP.ViewModels;
using MuhleOP.Helpers;
using System.Collections.Generic;
using System.Configuration;

namespace MuhleOP.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        private ApplicationUserManager _userManager;
        private ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
        }

        // GET: Project/Index - View all projects
        [Authorize(Roles = "Admin, Project Manager, Finance Officer")]
        public ActionResult Index()
        {
            var projects = db.Projects
                .OrderByDescending(p => p.CreatedDate)
                .ToList();

            var viewModels = projects.Select(p => new ProjectViewModel
            {
                Id = p.Id,
                ProjectName = p.ProjectName,
                ProjectCode = p.ProjectCode,
                ClientId = p.ClientId,
                ClientName = p.Client != null ? p.Client.CompanyName : "N/A",
                ContractId = p.ContractId,
                ContractNumber = p.Contract != null ? p.Contract.ContractNumber : "N/A",
                ProjectManagerId = p.ProjectManagerId,
                ProjectManagerName = p.ProjectManager != null ? p.ProjectManager.FullName : "N/A",
                SiteManagerId = p.SiteManagerId,
                SiteManagerName = p.SiteManager != null ? p.SiteManager.FullName : "N/A",
                Status = p.Status,
                StartDate = p.StartDate,
                EstimatedEndDate = p.EstimatedEndDate,
                ActualEndDate = p.ActualEndDate,
                ProjectLocation = p.ProjectLocation,
                IsActive = p.IsActive,
                CreatedDate = p.CreatedDate,
                CreatedBy = p.CreatedBy,
                EstimatedCost = p.Budget != null ? (decimal?)p.Budget.EstimatedCost : null,
                ActualCost = p.Budget != null ? (decimal?)p.Budget.ActualCost : null,
                DeliverableCount = p.Deliverables.Count,
                CompletedDeliverableCount = p.Deliverables.Count(d => d.Status == "Completed" || d.Status == "Approved"),
                ResourceCount = p.ResourceAllocations.Count,
                VariationCount = p.Variations.Count
            }).ToList();

            return View(viewModels);
        }

        // GET: Project/Details/5
        [Authorize(Roles = "Admin, Project Manager, Finance Officer")]
        public ActionResult Details(int id)
        {
            var project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }

            var viewModel = new ProjectViewModel
            {
                Id = project.Id,
                ProjectName = project.ProjectName,
                ProjectCode = project.ProjectCode,
                ClientId = project.ClientId,
                ClientName = project.Client != null ? project.Client.CompanyName : "N/A",
                ContractId = project.ContractId,
                ContractNumber = project.Contract != null ? project.Contract.ContractNumber : "N/A",
                ProjectManagerId = project.ProjectManagerId,
                ProjectManagerName = project.ProjectManager != null ? project.ProjectManager.FullName : "N/A",
                SiteManagerId = project.SiteManagerId,
                SiteManagerName = project.SiteManager != null ? project.SiteManager.FullName : "N/A",
                Status = project.Status,
                StartDate = project.StartDate,
                EstimatedEndDate = project.EstimatedEndDate,
                ActualEndDate = project.ActualEndDate,
                ProjectLocation = project.ProjectLocation,
                IsActive = project.IsActive,
                CreatedDate = project.CreatedDate,
                CreatedBy = project.CreatedBy,
                EstimatedCost = project.Budget != null ? (decimal?)project.Budget.EstimatedCost : null,
                ActualCost = project.Budget != null ? (decimal?)project.Budget.ActualCost : null,
                DeliverableCount = project.Deliverables.Count,
                CompletedDeliverableCount = project.Deliverables.Count(d => d.Status == "Completed" || d.Status == "Approved"),
                ResourceCount = project.ResourceAllocations.Count,
                VariationCount = project.Variations.Count
            };

            ViewBag.Resources = project.ResourceAllocations.ToList();
            ViewBag.Deliverables = project.Deliverables.ToList();
            ViewBag.Variations = project.Variations.ToList();

            var handover = db.ProjectHandovers
                .Include("Project")
                .Include("Project.Client")
                .Where(h => h.ProjectId == id)
                .OrderByDescending(h => h.CreatedDate)
                .FirstOrDefault();

            ViewBag.Handover = handover;

            return View(viewModel);
        }

        // GET: Project/Initiate - Use Case 9: Initiate Project
        [Authorize(Roles = "Admin, Project Manager")]
        public ActionResult Initiate()
        {
            var viewModel = new InitiateProjectViewModel
            {
                StartDate = DateTime.Now,
                EstimatedEndDate = DateTime.Now.AddMonths(6)
            };

            var clientIdsWithSignedContract = db.Contracts
                .Where(c => c.IsSigned && c.Status != "Terminated")
                .Select(c => c.ClientId)
                .Distinct();

            viewModel.Clients = db.Clients
                .Where(c => c.IsActive && clientIdsWithSignedContract.Contains(c.Id))
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.CompanyName
                }).ToList();

            viewModel.Contracts = new List<SelectListItem>();

            viewModel.ProjectManagers = db.Staff
                .Where(s => s.Role == "Project Manager" && s.IsActive)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.FirstName + " " + s.LastName
                }).ToList();

            viewModel.SiteManagers = db.Staff
                .Where(s => s.Role == "Site Manager" && s.IsActive)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.FirstName + " " + s.LastName
                }).ToList();

            return View(viewModel);
        }

        // GET: Project/GetClientContracts?clientId=5
        [Authorize(Roles = "Admin, Project Manager")]
        public JsonResult GetClientContracts(int clientId)
        {
            var contracts = db.Contracts
                .Where(c => c.ClientId == clientId && c.IsSigned && c.Status != "Terminated")
                .Select(c => new
                {
                    id = c.Id,
                    text = c.ProjectName + " (" + c.ContractNumber + ")"
                })
                .ToList();

            return Json(contracts, JsonRequestBehavior.AllowGet);
        }

        // GET: Project/GetContractDetails?contractId=12
        [Authorize(Roles = "Admin, Project Manager")]
        public JsonResult GetContractDetails(int contractId)
        {
            var contract = db.Contracts.Find(contractId);
            if (contract == null || !contract.IsSigned)
            {
                return Json(new { success = false, message = "Contract not found or not signed." }, JsonRequestBehavior.AllowGet);
            }

            var objectives = $"Deliver the project scope agreed in contract {contract.ContractNumber} " +
                              $"for {(contract.Client != null ? contract.Client.CompanyName : "the client")}, " +
                              $"achieving the agreed milestones{(string.IsNullOrWhiteSpace(contract.Milestones) ? "" : ": " + contract.Milestones)} " +
                              $"within the contracted timeline{(string.IsNullOrWhiteSpace(contract.Timeline) ? "." : ": " + contract.Timeline)}";

            return Json(new
            {
                success = true,
                contractValue = contract.ContractValue,
                scopeDefinition = contract.ProjectScope,
                deliverables = contract.Deliverables,
                objectives,
                projectName = contract.ProjectName,
                endDate = contract.EndDate.ToString("yyyy-MM-dd")
            }, JsonRequestBehavior.AllowGet);
        }

        // POST: Project/Initiate - Use Case 9: Initiate Project
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Project Manager")]
        public async Task<ActionResult> Initiate(InitiateProjectViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (model.EstimatedEndDate < model.StartDate)
                    {
                        ModelState.AddModelError("EstimatedEndDate", "Estimated end date must be after start date.");
                        return View(PopulateInitiateDropdowns(model));
                    }

                    var existingProject = db.Projects
                        .FirstOrDefault(p => p.ProjectName == model.ProjectName && p.Status != "Closed");

                    if (existingProject != null)
                    {
                        ModelState.AddModelError("ProjectName", "A project with this name already exists and is active.");
                        return View(PopulateInitiateDropdowns(model));
                    }

                    Contract selectedContract = null;
                    if (model.ContractId.HasValue)
                    {
                        selectedContract = db.Contracts.Find(model.ContractId.Value);
                        if (selectedContract == null || !selectedContract.IsSigned)
                        {
                            ModelState.AddModelError("ContractId", "Selected contract is not signed or does not exist.");
                            return View(PopulateInitiateDropdowns(model));
                        }

                        if (selectedContract.ClientId != model.ClientId)
                        {
                            ModelState.AddModelError("ContractId", "Selected contract does not belong to the selected client.");
                            return View(PopulateInitiateDropdowns(model));
                        }

                        if (model.EstimatedCost > selectedContract.ContractValue)
                        {
                            ModelState.AddModelError("EstimatedCost",
                                $"Estimated cost (R{model.EstimatedCost:N2}) cannot exceed the contract value (R{selectedContract.ContractValue:N2}).");
                            return View(PopulateInitiateDropdowns(model));
                        }
                    }

                    var project = new Project
                    {
                        ProjectName = model.ProjectName,
                        ClientId = model.ClientId,
                        ContractId = model.ContractId,
                        ProjectManagerId = model.ProjectManagerId,
                        SiteManagerId = model.SiteManagerId,
                        StartDate = model.StartDate,
                        EstimatedEndDate = model.EstimatedEndDate,
                        ProjectLocation = model.ProjectLocation,
                        Status = "Initiated",
                        IsActive = true,
                        CreatedBy = User.Identity.Name,
                        CreatedDate = DateTime.Now
                    };

                    db.Projects.Add(project);
                    await db.SaveChangesAsync();

                    var projectScope = new ProjectScope
                    {
                        ProjectId = project.Id,
                        Objectives = model.Objectives,
                        ScopeDefinition = model.ScopeDefinition,
                        OutOfScope = model.OutOfScope,
                        Assumptions = model.Assumptions,
                        Constraints = model.Constraints,
                        LastUpdated = DateTime.Now,
                        IsApproved = false
                    };

                    db.ProjectScopes.Add(projectScope);
                    await db.SaveChangesAsync();

                    var budget = new ProjectBudget
                    {
                        ProjectId = project.Id,
                        EstimatedCost = model.EstimatedCost,
                        AllocatedAmount = model.EstimatedCost,
                        SpentAmount = 0,
                        RemainingAmount = model.EstimatedCost,
                        BudgetCategory = model.BudgetCategory,
                        FiscalYear = DateTime.Now.Year,
                        IsApproved = false,
                        LastUpdated = DateTime.Now
                    };

                    db.ProjectBudgets.Add(budget);
                    await db.SaveChangesAsync();

                    if (!string.IsNullOrEmpty(model.Deliverables))
                    {
                        var deliverables = model.Deliverables.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var del in deliverables)
                        {
                            if (!string.IsNullOrEmpty(del.Trim()))
                            {
                                var deliverable = new ProjectDeliverable
                                {
                                    ProjectId = project.Id,
                                    DeliverableName = del.Trim(),
                                    Status = "Pending",
                                    Priority = "Medium",
                                    DueDate = model.EstimatedEndDate
                                };
                                db.ProjectDeliverables.Add(deliverable);
                            }
                        }
                        await db.SaveChangesAsync();
                    }

                    var pmAllocation = new ProjectResourceAllocation
                    {
                        ProjectId = project.Id,
                        StaffId = model.ProjectManagerId,
                        Role = "Project Manager",
                        AllocationPercentage = 100,
                        StartDate = model.StartDate,
                        EndDate = model.EstimatedEndDate,
                        IsActive = true,
                        AssignedBy = User.Identity.Name,
                        AssignedDate = DateTime.Now
                    };

                    db.ProjectResourceAllocations.Add(pmAllocation);

                    if (model.SiteManagerId.HasValue)
                    {
                        var smAllocation = new ProjectResourceAllocation
                        {
                            ProjectId = project.Id,
                            StaffId = model.SiteManagerId.Value,
                            Role = "Site Manager",
                            AllocationPercentage = 100,
                            StartDate = model.StartDate,
                            EndDate = model.EstimatedEndDate,
                            IsActive = true,
                            AssignedBy = User.Identity.Name,
                            AssignedDate = DateTime.Now
                        };
                        db.ProjectResourceAllocations.Add(smAllocation);
                    }

                    await db.SaveChangesAsync();

                    var pm = db.Staff.Find(model.ProjectManagerId);
                    if (pm != null)
                    {
                        await NotifyStakeholder(
                            pm.UserId,
                            "New Project Assigned",
                            $"You have been assigned as Project Manager for '{model.ProjectName}'. Please review and start planning.",
                            "Project",
                            $"/Project/Details/{project.Id}"
                        );
                    }

                    if (model.SiteManagerId.HasValue)
                    {
                        var sm = db.Staff.Find(model.SiteManagerId.Value);
                        if (sm != null)
                        {
                            await NotifyStakeholder(
                                sm.UserId,
                                "New Project Assigned",
                                $"You have been assigned as Site Manager for '{model.ProjectName}'.",
                                "Project",
                                $"/Project/Details/{project.Id}"
                            );
                        }
                    }

                    await CreateNotification(
                        User.Identity.GetUserId(),
                        "Project Initiated",
                        $"Project '{model.ProjectName}' has been initiated successfully.",
                        "System",
                        $"/Project/Details/{project.Id}"
                    );

                    await NotifyProjectStakeholders(project, model.ProjectName);

                    TempData["SuccessMessage"] = $"Project '{model.ProjectName}' was successfully created. Finance, Admin and the client have been notified.";
                    return RedirectToAction("Details", new { id = project.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error initiating project: " + ex.Message);
                }
            }

            return View(PopulateInitiateDropdowns(model));
        }

        // GET: Project/AssignStaff/5 - Use Case 10: Assign Staff
        [Authorize(Roles = "Admin, Project Manager")]
        public ActionResult AssignStaff(int id)
        {
            var project = db.Projects.Find(id);
            if (project == null)
            {
                TempData["ErrorMessage"] = "Project not found.";
                return RedirectToAction("Index");
            }

            var viewModel = PopulateAssignStaffDropdowns(new AssignStaffViewModel
            {
                ProjectId = project.Id,
                ProjectName = project.ProjectName,
                StartDate = project.StartDate,
                EndDate = project.EstimatedEndDate
            });

            return View(viewModel);
        }

        // POST: Project/AssignStaff - Use Case 10: Assign Staff
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Project Manager")]
        public async Task<ActionResult> AssignStaff(AssignStaffViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var project = db.Projects.Find(model.ProjectId);
                    if (project == null)
                    {
                        TempData["ErrorMessage"] = "Project not found.";
                        return RedirectToAction("Index");
                    }

                    var staff = db.Staff.Find(model.StaffId);
                    if (staff == null || !staff.IsActive)
                    {
                        ModelState.AddModelError("StaffId", "Selected staff member is not available.");
                        return View(PopulateAssignStaffDropdowns(model));
                    }

                    var existingAssignment = db.ProjectResourceAllocations
                        .FirstOrDefault(a => a.ProjectId == model.ProjectId && a.StaffId == model.StaffId && a.IsActive);

                    if (existingAssignment != null)
                    {
                        ModelState.AddModelError("StaffId", "This staff member is already assigned to the project.");
                        return View(PopulateAssignStaffDropdowns(model));
                    }

                    var isUniqueRole = model.Role == "Project Manager" || model.Role == "Site Manager";
                    var roleConflict = isUniqueRole
                        ? db.ProjectResourceAllocations
                            .Where(a => a.ProjectId == model.ProjectId && a.Role == model.Role && a.IsActive)
                            .Select(a => a.Staff.FirstName + " " + a.Staff.LastName)
                            .FirstOrDefault()
                        : null;

                    var overlappingAssignments = GetOverlappingAssignments(model.StaffId, model.ProjectId, model.StartDate, model.EndDate);

                    var conflictMessages = new List<string>();
                    if (roleConflict != null)
                    {
                        conflictMessages.Add($"This project already has {roleConflict} assigned as {model.Role}.");
                    }
                    foreach (var overlap in overlappingAssignments)
                    {
                        conflictMessages.Add($"{staff.FullName} is already assigned to '{overlap.Project.ProjectName}' as {overlap.Role} " +
                                              $"from {overlap.StartDate:dd MMM yyyy} to {(overlap.EndDate.HasValue ? overlap.EndDate.Value.ToString("dd MMM yyyy") : "ongoing")}, " +
                                              "which overlaps the dates requested here.");
                    }

                    if (conflictMessages.Any() && !model.OverrideConflict)
                    {
                        var vm = PopulateAssignStaffDropdowns(model);
                        vm.ConflictWarnings = conflictMessages;
                        ModelState.AddModelError("", "A scheduling conflict was found. Review the warning below - if you have permission to proceed anyway, tick \"Override Conflict\", provide a reason, and resubmit.");
                        return View(vm);
                    }

                    if (conflictMessages.Any() && model.OverrideConflict && string.IsNullOrWhiteSpace(model.OverrideReason))
                    {
                        var vm = PopulateAssignStaffDropdowns(model);
                        vm.ConflictWarnings = conflictMessages;
                        ModelState.AddModelError("OverrideReason", "Please provide a reason/justification for overriding this conflict.");
                        return View(vm);
                    }

                    var notes = model.Notes;
                    if (conflictMessages.Any() && model.OverrideConflict)
                    {
                        notes = (string.IsNullOrWhiteSpace(notes) ? "" : notes + " ") +
                                $"[Conflict overridden by {User.Identity.Name} on {DateTime.Now:dd MMM yyyy HH:mm} - reason: {model.OverrideReason}]";
                    }

                    var allocation = new ProjectResourceAllocation
                    {
                        ProjectId = model.ProjectId,
                        StaffId = model.StaffId,
                        Role = model.Role,
                        AllocationPercentage = model.AllocationPercentage,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate,
                        IsActive = true,
                        AssignedBy = User.Identity.Name,
                        AssignedDate = DateTime.Now,
                        Notes = notes
                    };

                    db.ProjectResourceAllocations.Add(allocation);
                    await db.SaveChangesAsync();

                    if (model.Role == "Project Manager")
                    {
                        project.ProjectManagerId = model.StaffId;
                        project.Status = "In Progress";
                        await db.SaveChangesAsync();
                    }
                    else if (model.Role == "Site Manager")
                    {
                        project.SiteManagerId = model.StaffId;
                        await db.SaveChangesAsync();
                    }

                    await NotifyStakeholder(
                        staff.UserId,
                        "Assigned to New Project",
                        $"You have been assigned to project '{project.ProjectName}' as {model.Role}, effective {model.StartDate:dd MMM yyyy}" +
                        (model.EndDate.HasValue ? $" to {model.EndDate.Value:dd MMM yyyy}." : ", ongoing until reassigned."),
                        "Project",
                        $"/Project/Details/{model.ProjectId}"
                    );

                    await CreateNotification(
                        User.Identity.GetUserId(),
                        "Staff Assigned",
                        $"{staff.FullName} assigned as {model.Role} to '{project.ProjectName}'.",
                        "System",
                        $"/Project/Details/{model.ProjectId}"
                    );

                    TempData["SuccessMessage"] = $"{staff.FullName} was successfully assigned as {model.Role}. They have been notified by email and in-app notification.";
                    return RedirectToAction("AssignStaff", new { id = model.ProjectId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error assigning staff: " + ex.Message);
                }
            }

            return View(PopulateAssignStaffDropdowns(model));
        }

        // POST: Project/DeassignStaff
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Project Manager")]
        public async Task<ActionResult> DeassignStaff(int allocationId, int projectId)
        {
            var allocation = db.ProjectResourceAllocations.Find(allocationId);
            if (allocation == null || allocation.ProjectId != projectId || !allocation.IsActive)
            {
                TempData["ErrorMessage"] = "Assignment not found or already inactive.";
                return RedirectToAction("AssignStaff", new { id = projectId });
            }

            allocation.IsActive = false;
            allocation.EndDate = DateTime.Now;
            await db.SaveChangesAsync();

            var project = db.Projects.Find(projectId);
            var staff = db.Staff.Find(allocation.StaffId);

            if (staff != null)
            {
                await NotifyStakeholder(
                    staff.UserId,
                    "Removed from Project",
                    $"You have been deassigned from project '{project?.ProjectName}'. You are now available for other assignments.",
                    "Project",
                    $"/Project/Details/{projectId}"
                );
            }

            TempData["SuccessMessage"] = $"{staff?.FullName ?? "Staff member"} has been deassigned and is now available for other projects.";
            return RedirectToAction("AssignStaff", new { id = projectId });
        }

        private List<ProjectResourceAllocation> GetOverlappingAssignments(int staffId, int excludeProjectId, DateTime startDate, DateTime? endDate)
        {
            var rangeEnd = endDate ?? DateTime.MaxValue.AddDays(-1);

            return db.ProjectResourceAllocations
                .Where(a => a.StaffId == staffId && a.IsActive && a.ProjectId != excludeProjectId)
                .ToList()
                .Where(a =>
                {
                    var otherStart = a.StartDate;
                    var otherEnd = a.EndDate ?? DateTime.MaxValue.AddDays(-1);
                    return otherStart <= rangeEnd && startDate <= otherEnd;
                })
                .ToList();
        }

        // GET: Project/AssignEquipment/5 - Use Case 12: Assign Equipment
        [Authorize(Roles = "Admin, Project Manager")]
        public ActionResult AssignEquipment(int id)
        {
            System.Diagnostics.Debug.WriteLine("=====================================================");
            System.Diagnostics.Debug.WriteLine($"AssignEquipment GET — ProjectId = {id}");
            System.Diagnostics.Debug.WriteLine("=====================================================");

            var project = db.Projects.Find(id);
            if (project == null)
            {
                System.Diagnostics.Debug.WriteLine("!! Project not found. Redirecting.");
                TempData["ErrorMessage"] = "Project not found.";
                return RedirectToAction("Index");
            }

            System.Diagnostics.Debug.WriteLine($"Project: '{project.ProjectName}' (Code={project.ProjectCode}, IsActive={project.IsActive}, Location='{project.ProjectLocation}')");

            decimal threshold = 100000m;
            if (decimal.TryParse(ConfigurationManager.AppSettings["EquipmentApprovalThreshold"], out decimal parsedThresh))
            {
                threshold = parsedThresh;
            }
            System.Diagnostics.Debug.WriteLine($"ApprovalThreshold = R{threshold:N2}");

            // Diagnostic counts
            var totalEquipment = db.Equipment.Count();
            var activeEquipment = db.Equipment.Count(e => e.IsActive);
            var availableEquipment = db.Equipment.Count(e => e.IsActive && e.Status == "Available");
            var activeAssignmentIds = db.EquipmentAssignments
                .Where(a => a.IsActive)
                .Select(a => a.EquipmentId)
                .Distinct()
                .ToList();
            var afterAllFilters = db.Equipment
                .Count(e => e.IsActive && e.Status == "Available" && !activeAssignmentIds.Contains(e.Id));

            System.Diagnostics.Debug.WriteLine($"Equipment totals: Total={totalEquipment}, Active={activeEquipment}, Status=Available={availableEquipment}");
            System.Diagnostics.Debug.WriteLine($"Equipment IDs with ACTIVE assignments (excluded): [{string.Join(", ", activeAssignmentIds)}]");
            System.Diagnostics.Debug.WriteLine($"Equipment remaining after both filters: {afterAllFilters}");

            var viewModel = PopulateAssignEquipmentDropdowns(new AssignEquipmentViewModel
            {
                ProjectId = project.Id,
                ProjectName = project.ProjectName,
                StartDate = DateTime.Now,
                ApprovalThreshold = threshold
            });

            System.Diagnostics.Debug.WriteLine($"View model: AvailableEquipment={viewModel.AvailableEquipment?.Count ?? 0}, CurrentAssignments={viewModel.CurrentAssignments?.Count ?? 0}, WaitlistSuggestions={viewModel.WaitlistSuggestions?.Count ?? 0}, UnderRepair={viewModel.UnderRepairEquipment?.Count ?? 0}");

            if (viewModel.AvailableEquipment == null || !viewModel.AvailableEquipment.Any())
            {
                System.Diagnostics.Debug.WriteLine("!! AvailableEquipment dropdown is EMPTY. Check the filter reasons above.");
            }

            return View(viewModel);
        }

        // POST: Project/AssignEquipment - Use Case 12: Assign Equipment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Project Manager")]
        public async Task<ActionResult> AssignEquipment(AssignEquipmentViewModel model)
        {
            System.Diagnostics.Debug.WriteLine("=====================================================");
            System.Diagnostics.Debug.WriteLine("AssignEquipment POST");
            System.Diagnostics.Debug.WriteLine($"  Model null? {model == null}");
            if (model != null)
            {
                System.Diagnostics.Debug.WriteLine($"  ProjectId = {model.ProjectId}");
                System.Diagnostics.Debug.WriteLine($"  EquipmentId = {model.EquipmentId}");
                System.Diagnostics.Debug.WriteLine($"  StartDate = {model.StartDate}");
                System.Diagnostics.Debug.WriteLine($"  EndDate = {model.EndDate}");
                System.Diagnostics.Debug.WriteLine($"  ConditionOnAssignment = '{model.ConditionOnAssignment}'");
                System.Diagnostics.Debug.WriteLine($"  Notes = '{model.Notes}'");
                System.Diagnostics.Debug.WriteLine($"  OverrideCertification = {model.OverrideCertification}");
                System.Diagnostics.Debug.WriteLine($"  CertificationOverrideReason = '{model.CertificationOverrideReason}'");
                System.Diagnostics.Debug.WriteLine($"  SecondaryApprovalRequested = {model.SecondaryApprovalRequested}");
            }
            System.Diagnostics.Debug.WriteLine($"  ModelState.IsValid = {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    var entry = ModelState[key];
                    foreach (var err in entry.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"  ModelState error [{key}]: {err.ErrorMessage}");
                    }
                }
            }

            if (model == null)
            {
                System.Diagnostics.Debug.WriteLine("!! model == null, redirecting to Index.");
                TempData["ErrorMessage"] = "Invalid submission.";
                return RedirectToAction("Index");
            }

            decimal threshold = 100000m;
            if (decimal.TryParse(ConfigurationManager.AppSettings["EquipmentApprovalThreshold"], out decimal parsedThresh))
            {
                threshold = parsedThresh;
            }
            model.ApprovalThreshold = threshold;

            var project = db.Projects.Find(model.ProjectId);
            if (project == null)
            {
                System.Diagnostics.Debug.WriteLine($"!! Project {model.ProjectId} not found.");
                TempData["ErrorMessage"] = "Project not found.";
                return RedirectToAction("Index");
            }
            System.Diagnostics.Debug.WriteLine($"Project found: '{project.ProjectName}'");

            var equipment = db.Equipment.Find(model.EquipmentId);
            if (equipment == null)
            {
                System.Diagnostics.Debug.WriteLine($"!! Equipment {model.EquipmentId} not found in DB.");
                ModelState.AddModelError("EquipmentId", "Equipment not found.");
                return View(PopulateAssignEquipmentDropdowns(model));
            }
            System.Diagnostics.Debug.WriteLine($"Equipment found: '{equipment.EquipmentName}' (Code={equipment.EquipmentCode}, Status={equipment.Status}, IsActive={equipment.IsActive}, CurrentLocation='{equipment.CurrentLocation}', AssignedProjectId={equipment.AssignedProjectId})");

            // Exception flow — Damaged / In Repair blocked
            if (equipment.Status == "Damaged" || equipment.Status == "In Repair")
            {
                System.Diagnostics.Debug.WriteLine($"!! Equipment status is '{equipment.Status}' — blocked by exception flow.");
                ModelState.AddModelError("EquipmentId", $"This equipment is currently {equipment.Status} and cannot be allocated.");
                return View(PopulateAssignEquipmentDropdowns(model));
            }

            model.EquipmentValue = equipment.CurrentValue ?? equipment.PurchaseCost ?? 0;
            model.ExceedsApprovalThreshold = model.EquipmentValue > model.ApprovalThreshold;
            System.Diagnostics.Debug.WriteLine($"EquipmentValue = R{model.EquipmentValue:N2}, ExceedsApprovalThreshold = {model.ExceedsApprovalThreshold}");

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Availability check with DATE RANGE
                    var rangeEnd = model.EndDate ?? DateTime.MaxValue.AddDays(-1);
                    var overlappingAssignments = db.EquipmentAssignments
                        .Include("Project")
                        .Where(a => a.EquipmentId == model.EquipmentId && a.IsActive)
                        .ToList()
                        .Where(a =>
                        {
                            var otherStart = a.AssignedDate;
                            var otherEnd = a.ExpectedReturnDate ?? DateTime.MaxValue.AddDays(-1);
                            return otherStart <= rangeEnd && model.StartDate <= otherEnd;
                        })
                        .ToList();

                    System.Diagnostics.Debug.WriteLine($"Date-range overlap check: {overlappingAssignments.Count} conflicting active assignment(s) found.");

                    if (overlappingAssignments.Any())
                    {
                        var conflict = overlappingAssignments.First();
                        System.Diagnostics.Debug.WriteLine($"!! Overlap with ProjectId={conflict.ProjectId} ('{conflict.Project?.ProjectName}') from {conflict.AssignedDate:dd MMM yyyy} to {(conflict.ExpectedReturnDate?.ToString("dd MMM yyyy") ?? "ongoing")}.");
                        ModelState.AddModelError("EquipmentId", $"This equipment is already assigned to '{conflict.Project?.ProjectName ?? "Another Project"}' from {conflict.AssignedDate:dd MMM yyyy} to {(conflict.ExpectedReturnDate.HasValue ? conflict.ExpectedReturnDate.Value.ToString("dd MMM yyyy") : "ongoing")}, which overlaps the selected dates.");
                        return View(PopulateAssignEquipmentDropdowns(model));
                    }

                    // 2. Skill / certification check
                    var reqCerts = db.EquipmentCertificationRequirements
                        .Where(c => c.EquipmentId == model.EquipmentId)
                        .Select(c => c.RequiredCertification)
                        .ToList();

                    System.Diagnostics.Debug.WriteLine($"Certification requirements for this equipment: {(reqCerts.Any() ? string.Join(", ", reqCerts) : "none")}");

                    if (reqCerts.Any())
                    {
                        var assignedStaffMembers = db.ProjectResourceAllocations
                            .Where(a => a.ProjectId == model.ProjectId && a.IsActive)
                            .Select(a => a.Staff)
                            .ToList();

                        if (project.ProjectManager != null && !assignedStaffMembers.Any(s => s.Id == project.ProjectManager.Id))
                        {
                            assignedStaffMembers.Add(project.ProjectManager);
                        }
                        if (project.SiteManager != null && !assignedStaffMembers.Any(s => s.Id == project.SiteManager.Id))
                        {
                            assignedStaffMembers.Add(project.SiteManager);
                        }

                        System.Diagnostics.Debug.WriteLine($"Checking {assignedStaffMembers.Count} assigned staff member(s) for required certs.");

                        var missingCerts = new List<string>();
                        foreach (var req in reqCerts)
                        {
                            bool foundStaffWithCert = assignedStaffMembers.Any(s =>
                                !string.IsNullOrWhiteSpace(s.Certifications) &&
                                s.Certifications.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(c => c.Trim().ToLower())
                                    .Contains(req.Trim().ToLower())
                            );

                            if (!foundStaffWithCert)
                            {
                                missingCerts.Add(req);
                            }
                        }

                        if (missingCerts.Any())
                        {
                            System.Diagnostics.Debug.WriteLine($"!! Missing certifications: {string.Join(", ", missingCerts)}. OverrideCertification = {model.OverrideCertification}, Reason = '{model.CertificationOverrideReason}'");

                            if (!model.OverrideCertification)
                            {
                                ModelState.AddModelError("", $"Warning: None of the assigned project staff hold the required certification(s): {string.Join(", ", missingCerts)}. To proceed, tick \"Override Certification\" and provide a justification.");
                                var vmFail = PopulateAssignEquipmentDropdowns(model);
                                vmFail.CertificationWarnings = missingCerts;
                                return View(vmFail);
                            }

                            if (string.IsNullOrWhiteSpace(model.CertificationOverrideReason))
                            {
                                ModelState.AddModelError("CertificationOverrideReason", "Please provide a justification for overriding the missing certifications.");
                                var vmFail = PopulateAssignEquipmentDropdowns(model);
                                vmFail.CertificationWarnings = missingCerts;
                                return View(vmFail);
                            }
                        }
                    }

                    // 4. Approval threshold check
                    bool isAdmin = User.IsInRole("Admin");
                    System.Diagnostics.Debug.WriteLine($"User is Admin: {isAdmin}. Threshold exceeded: {model.ExceedsApprovalThreshold}. SecondaryApprovalRequested: {model.SecondaryApprovalRequested}");

                    if (model.ExceedsApprovalThreshold && !isAdmin)
                    {
                        if (!model.SecondaryApprovalRequested)
                        {
                            System.Diagnostics.Debug.WriteLine("!! Threshold exceeded and no secondary approval requested — blocked.");
                            ModelState.AddModelError("SecondaryApprovalRequested", $"This equipment value (R{model.EquipmentValue:N2}) exceeds the approval threshold (R{model.ApprovalThreshold:N2}). Secondary approval is required.");
                            return View(PopulateAssignEquipmentDropdowns(model));
                        }
                    }

                    // 3. Proximity / transport note & formatting notes
                    string finalNotes = model.Notes;
                    if (!string.IsNullOrWhiteSpace(equipment.CurrentLocation) &&
                        !string.IsNullOrWhiteSpace(project.ProjectLocation) &&
                        !equipment.CurrentLocation.Trim().Equals(project.ProjectLocation.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        string transportHint = $"Transport required (currently at: {equipment.CurrentLocation})";
                        finalNotes = $"— {transportHint}" + (string.IsNullOrWhiteSpace(finalNotes) ? "" : " | " + finalNotes);
                        System.Diagnostics.Debug.WriteLine($"Transport hint added: {transportHint}");
                    }

                    if (reqCerts.Any() && model.OverrideCertification)
                    {
                        string certOverrideNote = $"[Certification overridden by {User.Identity.Name} - Reason: {model.CertificationOverrideReason}]";
                        finalNotes = certOverrideNote + (string.IsNullOrWhiteSpace(finalNotes) ? "" : " " + finalNotes);
                    }

                    if (model.ExceedsApprovalThreshold && model.SecondaryApprovalRequested)
                    {
                        string approvalNote = $"[SECONDARY APPROVAL REQUESTED by {User.Identity.Name} on {DateTime.Now:dd MMM yyyy HH:mm}]";
                        finalNotes = approvalNote + (string.IsNullOrWhiteSpace(finalNotes) ? "" : " " + finalNotes);
                    }

                    System.Diagnostics.Debug.WriteLine($"Final notes: '{finalNotes}'");
                    System.Diagnostics.Debug.WriteLine("All checks passed. Creating EquipmentAssignment...");

                    var assignment = new EquipmentAssignment
                    {
                        ProjectId = model.ProjectId,
                        EquipmentId = model.EquipmentId,
                        AssignedDate = model.StartDate,
                        ExpectedReturnDate = model.EndDate,
                        IsActive = true,
                        ConditionOnAssignment = model.ConditionOnAssignment ?? equipment.Condition,
                        Notes = finalNotes,
                        AssignedBy = User.Identity.Name
                    };

                    db.EquipmentAssignments.Add(assignment);

                    equipment.Status = "In Use";
                    equipment.AssignedProjectId = model.ProjectId;

                    await db.SaveChangesAsync();

                    System.Diagnostics.Debug.WriteLine($"SUCCESS: Assignment Id={assignment.Id} saved. Equipment now Status='In Use', AssignedProjectId={model.ProjectId}.");

                    await CreateNotification(
                        User.Identity.GetUserId(),
                        "Equipment Assigned",
                        $"'{equipment.EquipmentName}' assigned to project '{project.ProjectName}'.",
                        "Project",
                        $"/Project/Details/{model.ProjectId}"
                    );

                    var pm = db.Staff.Find(project.ProjectManagerId);
                    if (pm != null && !string.IsNullOrEmpty(pm.UserId))
                    {
                        await NotifyStakeholder(
                            pm.UserId,
                            "Equipment Assigned to Your Project",
                            $"'{equipment.EquipmentName}' has been assigned to project '{project.ProjectName}'.",
                            "Project",
                            $"/Project/Details/{model.ProjectId}"
                        );
                    }

                    TempData["SuccessMessage"] = $"'{equipment.EquipmentName}' assigned successfully to '{project.ProjectName}'.";
                    return RedirectToAction("AssignEquipment", new { id = model.ProjectId });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("!! EXCEPTION in AssignEquipment POST:");
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    ModelState.AddModelError("", "Error assigning equipment: " + ex.Message);
                }
            }

            System.Diagnostics.Debug.WriteLine("Falling through to redisplay View() — a check above blocked the save.");
            var redisplay = PopulateAssignEquipmentDropdowns(model);
            System.Diagnostics.Debug.WriteLine($"Redisplay dropdown count: {redisplay.AvailableEquipment?.Count ?? 0}");
            return View(redisplay);
        }

        // POST: Project/UpdateStatus
        [HttpPost]
        [Authorize(Roles = "Admin, Project Manager")]
        public async Task<ActionResult> UpdateStatus(int id, string status)
        {
            var project = db.Projects.Find(id);
            if (project == null)
            {
                return Json(new { success = false, message = "Project not found" });
            }

            project.Status = status;
            if (status == "Completed")
            {
                project.ActualEndDate = DateTime.Now;
            }
            else if (status == "Closed")
            {
                project.IsActive = false;
                project.ActualEndDate = DateTime.Now;
            }

            await db.SaveChangesAsync();

            if (status == "Completed" || status == "Closed")
            {
                var activeAllocations = db.ProjectResourceAllocations
                    .Where(a => a.ProjectId == id && a.IsActive)
                    .ToList();

                foreach (var allocation in activeAllocations)
                {
                    allocation.IsActive = false;
                    allocation.EndDate = DateTime.Now;
                }

                await db.SaveChangesAsync();
            }

            var pm = db.Staff.Find(project.ProjectManagerId);
            if (pm != null)
            {
                await CreateNotification(
                    pm.UserId,
                    $"Project Status Updated to {status}",
                    $"Project '{project.ProjectName}' status has been updated to {status}.",
                    "Project",
                    $"/Project/Details/{id}"
                );
            }

            return Json(new { success = true });
        }

        #region Helper Methods

        private InitiateProjectViewModel PopulateInitiateDropdowns(InitiateProjectViewModel model)
        {
            var clientIdsWithSignedContract = db.Contracts
                .Where(c => c.IsSigned && c.Status != "Terminated")
                .Select(c => c.ClientId)
                .Distinct();

            model.Clients = db.Clients
                .Where(c => c.IsActive && clientIdsWithSignedContract.Contains(c.Id))
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.CompanyName
                }).ToList();

            model.Contracts = db.Contracts
                .Where(c => c.IsSigned && c.Status != "Terminated" && c.ClientId == model.ClientId)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.ProjectName + " (" + c.ContractNumber + ")"
                }).ToList();

            model.ProjectManagers = db.Staff
                .Where(s => s.Role == "Project Manager" && s.IsActive)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.FirstName + " " + s.LastName
                }).ToList();

            model.SiteManagers = db.Staff
                .Where(s => s.Role == "Site Manager" && s.IsActive)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.FirstName + " " + s.LastName
                }).ToList();

            return model;
        }

        private static readonly string[] AllStaffRoles =
        {
            "Site Manager", "Crew Member", "Project Manager", "Finance Officer", "Procurement Officer"
        };

        private AssignStaffViewModel PopulateAssignStaffDropdowns(AssignStaffViewModel model)
        {
            var project = db.Projects.Find(model.ProjectId);

            model.CurrentAssignments = db.ProjectResourceAllocations
                .Where(a => a.ProjectId == model.ProjectId && a.IsActive)
                .Select(a => new AssignedStaffViewModel
                {
                    Id = a.Id,
                    StaffId = a.StaffId,
                    StaffName = a.Staff != null ? a.Staff.FirstName + " " + a.Staff.LastName : "Unknown",
                    Role = a.Role,
                    AllocationPercentage = a.AllocationPercentage,
                    StartDate = a.StartDate,
                    EndDate = a.EndDate,
                    IsActive = a.IsActive
                }).ToList();

            var assignedStaffIds = db.ProjectResourceAllocations
                .Where(a => a.ProjectId == model.ProjectId && a.IsActive)
                .Select(a => a.StaffId)
                .ToList();

            var candidateStaff = db.Staff
                .Where(s => s.IsActive && !assignedStaffIds.Contains(s.Id))
                .ToList();

            model.AvailableStaffByRole = AllStaffRoles
                .Select(role => new StaffRoleGroupViewModel
                {
                    Role = role,
                    Staff = candidateStaff
                        .Where(s => s.Role == role)
                        .Select(s =>
                        {
                            var overlaps = GetOverlappingAssignments(s.Id, model.ProjectId, model.StartDate, model.EndDate);
                            var label = s.FullName;
                            if (overlaps.Any())
                            {
                                label += $" - CONFLICT: busy on '{overlaps.First().Project.ProjectName}' until " +
                                          (overlaps.First().EndDate.HasValue ? overlaps.First().EndDate.Value.ToString("dd MMM yyyy") : "further notice");
                            }
                            else
                            {
                                label += " - Available";
                            }
                            return new SelectListItem { Value = s.Id.ToString(), Text = label };
                        }).ToList()
                })
                .Where(g => g.Staff.Any())
                .ToList();

            model.AvailableStaff = model.AvailableStaffByRole.SelectMany(g => g.Staff).ToList();

            model.AvailableRoles = AllStaffRoles
                .Select(r => new SelectListItem { Value = r, Text = r })
                .ToList();

            model.ProjectName = project?.ProjectName ?? "Project";
            return model;
        }

        private AssignEquipmentViewModel PopulateAssignEquipmentDropdowns(AssignEquipmentViewModel model)
        {
            if (model == null) model = new AssignEquipmentViewModel();

            var project = db.Projects.Find(model.ProjectId);

            model.CurrentAssignments = db.EquipmentAssignments
                .Where(a => a.ProjectId == model.ProjectId && a.IsActive)
                .Select(a => new AssignedEquipmentViewModel
                {
                    Id = a.Id,
                    EquipmentName = a.Equipment != null ? a.Equipment.EquipmentName : "Unknown",
                    EquipmentCode = a.Equipment != null ? a.Equipment.EquipmentCode : "Unknown",
                    AssignedDate = a.AssignedDate,
                    StartDate = a.AssignedDate,
                    EndDate = a.ExpectedReturnDate,
                    ExpectedReturnDate = a.ExpectedReturnDate,
                    IsActive = a.IsActive,
                    ConditionOnAssignment = a.ConditionOnAssignment,
                    Notes = a.Notes,
                    HasSecondaryApprovalRequest = a.Notes != null && a.Notes.Contains("[SECONDARY APPROVAL REQUESTED]"),
                    HasTransportNote = a.Notes != null && a.Notes.Contains("Transport required")
                }).ToList();

            var assignedEquipmentIds = db.EquipmentAssignments
                .Where(a => a.IsActive)
                .Select(a => a.EquipmentId)
                .ToList();

            var rawAvailable = db.Equipment
                .Where(e => e.IsActive && e.Status == "Available" && !assignedEquipmentIds.Contains(e.Id))
                .ToList();

            model.AvailableEquipment = rawAvailable.Select(e =>
            {
                string text = $"{e.EquipmentName} ({e.EquipmentCode}) - {e.Condition}";
                if (!string.IsNullOrEmpty(e.CurrentLocation) &&
                    project != null &&
                    !string.IsNullOrEmpty(project.ProjectLocation) &&
                    !e.CurrentLocation.Trim().Equals(project.ProjectLocation.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    text += $" — Transport required (currently at: {e.CurrentLocation})";
                }
                return new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = text
                };
            }).ToList();

            if (!model.AvailableEquipment.Any())
            {
                var fourteenDaysOut = DateTime.Now.AddDays(14);
                var activeInUseAssignments = db.EquipmentAssignments
                    .Include("Equipment")
                    .Include("Project")
                    .Where(a => a.IsActive && a.Equipment.IsActive && a.Equipment.Status == "In Use")
                    .ToList();

                model.WaitlistSuggestions = activeInUseAssignments
                    .Where(a => a.ExpectedReturnDate.HasValue && a.ExpectedReturnDate.Value <= fourteenDaysOut)
                    .Take(5)
                    .Select(a => new WaitlistEquipmentViewModel
                    {
                        EquipmentId = a.EquipmentId,
                        EquipmentName = a.Equipment?.EquipmentName ?? "Unknown",
                        EquipmentCode = a.Equipment?.EquipmentCode ?? "Unknown",
                        CurrentProjectName = a.Project?.ProjectName ?? "Unknown",
                        ExpectedFreeDate = a.ExpectedReturnDate
                    })
                    .ToList();
            }

            model.UnderRepairEquipment = db.Equipment
                .Where(e => e.IsActive && (e.Status == "Damaged" || e.Status == "In Repair"))
                .Select(e => new UnderRepairEquipmentViewModel
                {
                    EquipmentId = e.Id,
                    EquipmentName = e.EquipmentName,
                    EquipmentCode = e.EquipmentCode,
                    Status = e.Status,
                    Condition = e.Condition
                })
                .ToList();

            model.ProjectName = project?.ProjectName ?? "Project";
            return model;
        }

        #endregion

        #region Notification Helpers

        private async Task CreateNotification(string userId, string title, string message, string type, string link)
        {
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

        private async Task NotifyStakeholder(string userId, string title, string message, string type, string link)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            await CreateNotification(userId, title, message, type, link);

            try
            {
                var user = db.Users.Find(userId);
                if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                {
                    var fullLink = link != null ? $"{Request.Url.Scheme}://{Request.Url.Authority}{link}" : null;
                    var body = BuildEmailBody(title, message, fullLink);
                    await EmailHelper.SendEmailAsync(user.Email, title, body);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error emailing user {userId}: {ex.Message}");
            }
        }

        private async Task NotifyEmailOnly(string toEmail, string toName, string title, string message, string link)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return;
            }

            try
            {
                var fullLink = link != null ? $"{Request.Url.Scheme}://{Request.Url.Authority}{link}" : null;
                var body = BuildEmailBody(title, message, fullLink);
                await EmailHelper.SendEmailAsync(toEmail, title, body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error emailing {toEmail}: {ex.Message}");
            }
        }

        private string BuildEmailBody(string title, string message, string link)
        {
            var linkHtml = string.IsNullOrEmpty(link) ? "" : $"<p><a href=\"{link}\">View in MuhleOps</a></p>";
            return $"<h3>{title}</h3><p>{message}</p>{linkHtml}<p style=\"color:#888;font-size:12px;\">This is an automated notification from MuhleOps.</p>";
        }

        private async Task NotifyProjectStakeholders(Project project, string projectName)
        {
            var message = $"Project '{projectName}' has been initiated" +
                           (project.Client != null ? $" for {project.Client.CompanyName}" : "") +
                           $". Estimated cost: R{(project.Budget != null ? project.Budget.EstimatedCost : 0):N2}.";
            var link = $"/Project/Details/{project.Id}";

            var financeOfficers = db.Staff.Where(s => s.Role == "Finance Officer" && s.IsActive).ToList();
            foreach (var fo in financeOfficers)
            {
                await NotifyStakeholder(fo.UserId, "New Project Initiated - Finance Review", message, "Finance", link);
            }

            var adminIds = db.Users.ToList()
                .Where(u => UserManager.IsInRole(u.Id, "Admin"))
                .Select(u => u.Id)
                .ToList();
            foreach (var adminId in adminIds)
            {
                await NotifyStakeholder(adminId, "New Project Initiated", message, "Project", link);
            }

            if (project.ClientId.HasValue)
            {
                var client = db.Clients.Find(project.ClientId.Value);
                if (client != null)
                {
                    if (!string.IsNullOrEmpty(client.UserId))
                    {
                        await NotifyStakeholder(client.UserId, "Your Project Has Started", message, "Project", link);
                    }
                    else if (!string.IsNullOrWhiteSpace(client.ContactEmail))
                    {
                        await NotifyEmailOnly(client.ContactEmail, client.CompanyName, "Your Project Has Started", message, link);
                    }
                }
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