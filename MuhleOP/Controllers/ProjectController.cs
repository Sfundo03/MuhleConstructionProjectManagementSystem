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

            // Only clients with at least one signed, non-terminated contract can
            // have a project initiated against them - a client with no signed
            // contract has nothing for the system to auto-fill scope/budget from.
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

            // Contracts dropdown starts empty; ClientId selection re-populates it
            // via GetClientContracts() so only the chosen client's signed
            // contracts are ever selectable.
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
        // AJAX: repopulates the Contract dropdown with only the signed,
        // non-terminated contracts belonging to the selected client.
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
        // AJAX: reads the signed contract and returns the data used to
        // auto-fill the Objectives / Scope Definition / Deliverables fields
        // and the contract value used for budget validation. The Project
        // Manager can still edit every auto-filled field before submitting.
        [Authorize(Roles = "Admin, Project Manager")]
        public JsonResult GetContractDetails(int contractId)
        {
            var contract = db.Contracts.Find(contractId);
            if (contract == null || !contract.IsSigned)
            {
                return Json(new { success = false, message = "Contract not found or not signed." }, JsonRequestBehavior.AllowGet);
            }

            // The Contract entity does not store a separate "Objectives" field,
            // so a sensible objectives statement is derived from the contract's
            // scope/milestones/timeline - the Project Manager can edit it freely.
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

                        // Business rule: the project budget can never exceed what the
                        // client contractually agreed to pay.
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

                    // Notify every other stakeholder: Finance (needs to track budget
                    // against the contract), all Admins, and the Client - both as an
                    // in-app notification and by email.
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

                    // Role conflict check: a project can only have one active
                    // Project Manager and one active Site Manager at a time.
                    var isUniqueRole = model.Role == "Project Manager" || model.Role == "Site Manager";
                    var roleConflict = isUniqueRole
                        ? db.ProjectResourceAllocations
                            .Where(a => a.ProjectId == model.ProjectId && a.Role == model.Role && a.IsActive)
                            .Select(a => a.Staff.FirstName + " " + a.Staff.LastName)
                            .FirstOrDefault()
                        : null;

                    // Time-overlap conflict check: is this staff member already
                    // actively allocated to a *different* project whose date
                    // range overlaps the dates requested here?
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
                        // Don't save - surface the conflicts and require the PM to
                        // tick "Override Conflict" (with a justification) to proceed.
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

                    // Staff stays assigned - and keeps showing as unavailable for
                    // other projects during this window - until the project is
                    // marked Completed/Closed or the PM explicitly deassigns them.
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

        // POST: Project/DeassignStaff - releases a staff member from a project
        // ahead of project completion (e.g. reassigned elsewhere, left the
        // project early). Staff otherwise remain assigned until the project is
        // marked Completed/Closed (see UpdateStatus below).
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

        // Finds other ACTIVE allocations for this staff member (on a different
        // project) whose date range overlaps [startDate, endDate]. A null
        // EndDate is treated as "ongoing" (open-ended), so it overlaps anything
        // from its StartDate onward.
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

        // GET: Project/AssignEquipment/5 - Use Case 11: Assign Equipment
        [Authorize(Roles = "Admin, Project Manager")]
        public ActionResult AssignEquipment(int id)
        {
            var project = db.Projects.Find(id);
            if (project == null)
            {
                TempData["ErrorMessage"] = "Project not found.";
                return RedirectToAction("Index");
            }

            var viewModel = PopulateAssignEquipmentDropdowns(new AssignEquipmentViewModel
            {
                ProjectId = project.Id,
                ProjectName = project.ProjectName
            });

            return View(viewModel);
        }

        // POST: Project/AssignEquipment - Use Case 11: Assign Equipment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Project Manager")]
        public async Task<ActionResult> AssignEquipment(AssignEquipmentViewModel model)
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

                    var equipment = db.Equipment.Find(model.EquipmentId);
                    if (equipment == null)
                    {
                        ModelState.AddModelError("EquipmentId", "Equipment not found.");
                        return View(PopulateAssignEquipmentDropdowns(model));
                    }

                    var existingAssignment = db.EquipmentAssignments
                        .FirstOrDefault(a => a.EquipmentId == model.EquipmentId && a.IsActive);

                    if (existingAssignment != null)
                    {
                        ModelState.AddModelError("EquipmentId", "This equipment is already assigned to another project.");
                        return View(PopulateAssignEquipmentDropdowns(model));
                    }

                    var assignment = new EquipmentAssignment
                    {
                        ProjectId = model.ProjectId,
                        EquipmentId = model.EquipmentId,
                        AssignedDate = DateTime.Now,
                        ExpectedReturnDate = model.ExpectedReturnDate,
                        IsActive = true,
                        ConditionOnAssignment = model.ConditionOnAssignment ?? equipment.Condition,
                        Notes = model.Notes,
                        AssignedBy = User.Identity.Name
                    };

                    db.EquipmentAssignments.Add(assignment);

                    equipment.Status = "In Use";
                    equipment.AssignedProjectId = model.ProjectId;

                    await db.SaveChangesAsync();

                    await CreateNotification(
                        User.Identity.GetUserId(),
                        "Equipment Assigned",
                        $"'{equipment.EquipmentName}' assigned to project '{project.ProjectName}'.",
                        "Project",
                        $"/Project/Details/{model.ProjectId}"
                    );

                    TempData["SuccessMessage"] = $"'{equipment.EquipmentName}' assigned successfully to '{project.ProjectName}'.";
                    return RedirectToAction("AssignEquipment", new { id = model.ProjectId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error assigning equipment: " + ex.Message);
                }
            }

            return View(PopulateAssignEquipmentDropdowns(model));
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

            // Staff stay assigned to a project until it's Completed/Closed or the
            // PM explicitly deassigns them (DeassignStaff) - so once a project
            // reaches either terminal state, release every staff member so they
            // show up as available for new assignments elsewhere.
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

            // Re-populate contracts for whichever client was selected so a
            // validation-error redisplay of the form still shows the right list.
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

        // Every internal staff role in the system besides Client and Candidate
        // (those two are never Staff records - they're separate account types).
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

            // Every active staff member not already on THIS project, grouped by
            // role so the Project Manager can see, per role, who is available -
            // e.g. "who is available under Site Managers", "under Crew Members",
            // etc. Anyone with an overlapping active assignment elsewhere for the
            // dates currently on the form is flagged inline as a conflict rather
            // than hidden, so the PM can still pick them and override if needed.
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

            // Flat fallback list kept for backward compatibility / simple <select>.
            model.AvailableStaff = model.AvailableStaffByRole.SelectMany(g => g.Staff).ToList();

            model.AvailableRoles = AllStaffRoles
                .Select(r => new SelectListItem { Value = r, Text = r })
                .ToList();

            model.ProjectName = project?.ProjectName ?? "Project";
            return model;
        }

        private AssignEquipmentViewModel PopulateAssignEquipmentDropdowns(AssignEquipmentViewModel model)
        {
            var project = db.Projects.Find(model.ProjectId);

            model.CurrentAssignments = db.EquipmentAssignments
                .Where(a => a.ProjectId == model.ProjectId && a.IsActive)
                .Select(a => new AssignedEquipmentViewModel
                {
                    Id = a.Id,
                    EquipmentName = a.Equipment != null ? a.Equipment.EquipmentName : "Unknown",
                    EquipmentCode = a.Equipment != null ? a.Equipment.EquipmentCode : "Unknown",
                    AssignedDate = a.AssignedDate,
                    ExpectedReturnDate = a.ExpectedReturnDate,
                    IsActive = a.IsActive,
                    ConditionOnAssignment = a.ConditionOnAssignment
                }).ToList();

            var assignedEquipmentIds = db.EquipmentAssignments
                .Where(a => a.IsActive)
                .Select(a => a.EquipmentId)
                .ToList();

            model.AvailableEquipment = db.Equipment
                .Where(e => e.IsActive && e.Status == "Available" && !assignedEquipmentIds.Contains(e.Id))
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = e.EquipmentName + " (" + e.EquipmentCode + ") - " + e.Condition
                }).ToList();

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

        // Raises an in-app Notification AND emails the same user, given their
        // ApplicationUser id. Used for anyone who needs to actually be alerted
        // outside the app (PM/SM/staff assignments, Finance, Admin, Client).
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

        // Sends directly to a raw email address (used for Clients that don't
        // necessarily have a linked ApplicationUser/login).
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

        // Notifies every stakeholder of a newly initiated project besides the
        // Project Manager / Site Manager (already notified where they're
        // assigned): all active Finance Officers, all Admins, and the Client.
        private async Task NotifyProjectStakeholders(Project project, string projectName)
        {
            var message = $"Project '{projectName}' has been initiated" +
                           (project.Client != null ? $" for {project.Client.CompanyName}" : "") +
                           $". Estimated cost: R{(project.Budget != null ? project.Budget.EstimatedCost : 0):N2}.";
            var link = $"/Project/Details/{project.Id}";

            // Finance Officers - they need to track the budget against the contract.
            var financeOfficers = db.Staff.Where(s => s.Role == "Finance Officer" && s.IsActive).ToList();
            foreach (var fo in financeOfficers)
            {
                await NotifyStakeholder(fo.UserId, "New Project Initiated - Finance Review", message, "Finance", link);
            }

            // Admins.
            var adminIds = db.Users.ToList()
                .Where(u => UserManager.IsInRole(u.Id, "Admin"))
                .Select(u => u.Id)
                .ToList();
            foreach (var adminId in adminIds)
            {
                await NotifyStakeholder(adminId, "New Project Initiated", message, "Project", link);
            }

            // Client.
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