using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using MuhleOP.Models;
using MuhleOP.ViewModels;
using System.Collections.Generic;

namespace MuhleOP.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

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

            viewModel.Clients = db.Clients
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.CompanyName
                }).ToList();

            viewModel.Contracts = db.Contracts
                .Where(c => c.IsSigned && c.Status != "Terminated")
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.ProjectName + " (" + c.ContractNumber + ")"
                }).ToList();

            viewModel.ProjectManagers = db.Staff
                .Where(s => s.Role == "Project Manager" && s.IsActive)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.FullName
                }).ToList();

            viewModel.SiteManagers = db.Staff
                .Where(s => s.Role == "Site Manager" && s.IsActive)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.FullName
                }).ToList();

            return View(viewModel);
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

                    if (model.ContractId.HasValue)
                    {
                        var contract = db.Contracts.Find(model.ContractId.Value);
                        if (contract == null || !contract.IsSigned)
                        {
                            ModelState.AddModelError("ContractId", "Selected contract is not signed or does not exist.");
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
                        await CreateNotification(
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
                            await CreateNotification(
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

                    TempData["SuccessMessage"] = $"Project '{model.ProjectName}' initiated successfully!";
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
                StartDate = DateTime.Now
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

                    var existingAssignment = db.ProjectResourceAllocations
                        .FirstOrDefault(a => a.ProjectId == model.ProjectId && a.StaffId == model.StaffId && a.IsActive);

                    if (existingAssignment != null)
                    {
                        ModelState.AddModelError("StaffId", "This staff member is already assigned to the project.");
                        return View(PopulateAssignStaffDropdowns(model));
                    }

                    var roleConflict = db.ProjectResourceAllocations
                        .FirstOrDefault(a => a.ProjectId == model.ProjectId && a.Role == model.Role && a.IsActive);

                    if (roleConflict != null && model.Role == "Project Manager")
                    {
                        ModelState.AddModelError("Role", "This project already has a Project Manager assigned.");
                        return View(PopulateAssignStaffDropdowns(model));
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
                        Notes = model.Notes
                    };

                    db.ProjectResourceAllocations.Add(allocation);
                    await db.SaveChangesAsync();

                    var staff = db.Staff.Find(model.StaffId);
                    if (staff != null)
                    {
                        await CreateNotification(
                            staff.UserId,
                            "Assigned to New Project",
                            $"You have been assigned to project '{project.ProjectName}' as {model.Role}.",
                            "Project",
                            $"/Project/Details/{model.ProjectId}"
                        );
                    }

                    if (model.Role == "Project Manager")
                    {
                        project.ProjectManagerId = model.StaffId;
                        project.Status = "In Progress";
                        await db.SaveChangesAsync();
                    }

                    await CreateNotification(
                        User.Identity.GetUserId(),
                        "Staff Assigned",
                        $"{staff?.FullName ?? "Staff"} assigned as {model.Role} to '{project.ProjectName}'.",
                        "System",
                        $"/Project/Details/{model.ProjectId}"
                    );

                    TempData["SuccessMessage"] = $"{staff?.FullName ?? "Staff"} assigned successfully as {model.Role}.";
                    return RedirectToAction("AssignStaff", new { id = model.ProjectId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error assigning staff: " + ex.Message);
                }
            }

            return View(PopulateAssignStaffDropdowns(model));
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
            model.Clients = db.Clients
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.CompanyName
                }).ToList();

            model.Contracts = db.Contracts
                .Where(c => c.IsSigned && c.Status != "Terminated")
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
                    Text = s.FullName
                }).ToList();

            model.SiteManagers = db.Staff
                .Where(s => s.Role == "Site Manager" && s.IsActive)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.FullName
                }).ToList();

            return model;
        }

        private AssignStaffViewModel PopulateAssignStaffDropdowns(AssignStaffViewModel model)
        {
            var project = db.Projects.Find(model.ProjectId);

            model.CurrentAssignments = db.ProjectResourceAllocations
                .Where(a => a.ProjectId == model.ProjectId && a.IsActive)
                .Select(a => new AssignedStaffViewModel
                {
                    Id = a.Id,
                    StaffName = a.Staff != null ? a.Staff.FullName : "Unknown",
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

            model.AvailableStaff = db.Staff
                .Where(s => s.IsActive && !assignedStaffIds.Contains(s.Id))
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.FullName + " (" + s.Role + ")"
                }).ToList();

            model.AvailableRoles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Crew Member", Text = "Crew Member" },
                new SelectListItem { Value = "Site Manager", Text = "Site Manager" },
                new SelectListItem { Value = "Project Manager", Text = "Project Manager" },
                new SelectListItem { Value = "Finance Officer", Text = "Finance Officer" },
                new SelectListItem { Value = "Procurement Officer", Text = "Procurement Officer" }
            };

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