using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
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
    public class QualityController : Controller
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

        // ------------------------------------------------------------------
        // Dashboard
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Project Manager, Site Manager, Client")]
        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();
            var staff = ResolveCurrentStaff();
            var client = db.Clients.FirstOrDefault(c => c.UserId == userId);

            IQueryable<SiteInspection> inspections = db.SiteInspections.AsQueryable();
            IQueryable<ProjectVariation> variations = db.ProjectVariations.AsQueryable();

            if (User.IsInRole("Admin"))
            {
                // all
            }
            else if (User.IsInRole("Project Manager") && staff != null)
            {
                var pmProjectIds = db.Projects.Where(p => p.ProjectManagerId == staff.Id).Select(p => p.Id).ToList();
                inspections = inspections.Where(i => pmProjectIds.Contains(i.ProjectId));
                variations = variations.Where(v => pmProjectIds.Contains(v.ProjectId));
            }
            else if (User.IsInRole("Site Manager") && staff != null)
            {
                var smProjectIds = db.Projects.Where(p => p.SiteManagerId == staff.Id).Select(p => p.Id).ToList();
                inspections = inspections.Where(i => smProjectIds.Contains(i.ProjectId) || i.PerformedByStaffId == staff.Id);
                variations = variations.Where(v => smProjectIds.Contains(v.ProjectId));
            }
            else if (User.IsInRole("Client") && client != null)
            {
                var clientProjectIds = db.Projects.Where(p => p.ClientId == client.Id).Select(p => p.Id).ToList();
                inspections = inspections.Where(i => clientProjectIds.Contains(i.ProjectId));
                variations = variations.Where(v => clientProjectIds.Contains(v.ProjectId));
            }
            else
            {
                inspections = inspections.Where(i => false);
                variations = variations.Where(v => false);
            }

            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            var vm = new QualityIndexViewModel
            {
                TotalInspections = inspections.Count(),
                VariationsPending = variations.Count(v => v.Status == "Requested" || v.Status == "Under Review"),
                VariationsApprovedThisMonth = variations.Count(v => v.Status == "Approved" && v.ApprovalDate >= monthStart),
                ProjectsWithActiveNonConformances = inspections
                    .Where(i => i.ActionRequired && i.Status != "Reviewed")
                    .Select(i => i.ProjectId)
                    .Distinct()
                    .Count()
            };

            return View(vm);
        }

        // ------------------------------------------------------------------
        // Conduct Inspection
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Site Manager")]
        public ActionResult ConductInspection()
        {
            var model = new CreateInspectionViewModel
            {
                InspectionDate = DateTime.Today,
                Projects = GetProjectSelectListForUser(),
                InspectionTypes = GetInspectionTypeSelectList(null)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Site Manager")]
        public async Task<ActionResult> ConductInspection(CreateInspectionViewModel model)
        {
            var staff = ResolveCurrentStaff();
            if (staff == null && User.IsInRole("Site Manager"))
            {
                TempData["ErrorMessage"] = "Staff record not found or inactive.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var project = db.Projects.Find(model.ProjectId);
                    if (project == null || !project.IsActive)
                    {
                        ModelState.AddModelError("ProjectId", "Selected project is not available.");
                        return View(RepopulateCreateInspection(model));
                    }

                    // Ownership: Site Manager may only inspect projects they supervise
                    if (User.IsInRole("Site Manager") && !User.IsInRole("Admin"))
                    {
                        if (staff == null || project.SiteManagerId != staff.Id)
                        {
                            ModelState.AddModelError("ProjectId", "You may only inspect projects you supervise.");
                            return View(RepopulateCreateInspection(model));
                        }
                    }

                    var inspection = new SiteInspection
                    {
                        ProjectId = model.ProjectId,
                        PerformedByStaffId = staff != null ? staff.Id : 0,
                        InspectionDate = model.InspectionDate ?? DateTime.Today,
                        InspectionType = model.InspectionType,
                        InspectionArea = model.InspectionArea,
                        Findings = model.Findings,
                        Recommendations = model.Recommendations,
                        Score = model.Score,
                        Status = "Completed",
                        ActionRequired = model.ActionRequired,
                        ActionItems = model.ActionItems,
                        DueDate = model.DueDate,
                        CreatedDate = DateTime.Now
                    };

                    if (model.Score.HasValue)
                        inspection.IsPassed = model.Score.Value >= 80;
                    else
                        inspection.IsPassed = !model.ActionRequired;

                    db.SiteInspections.Add(inspection);
                    await db.SaveChangesAsync();

                    // Optional file uploads
                    if (model.Photos != null && model.Photos.ContentLength > 0)
                    {
                        try
                        {
                            var folder = Server.MapPath($"~/Uploads/Inspections/{inspection.Id}");
                            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                            var ext = Path.GetExtension(model.Photos.FileName);
                            var fileName = Guid.NewGuid().ToString("N") + ext;
                            var physicalPath = Path.Combine(folder, fileName);
                            model.Photos.SaveAs(physicalPath);
                            inspection.PhotosPath = $"/Uploads/Inspections/{inspection.Id}/{fileName}";
                        }
                        catch (Exception photoEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Photo upload failed: {photoEx.Message}");
                        }
                    }

                    if (model.Report != null && model.Report.ContentLength > 0)
                    {
                        try
                        {
                            var folder = Server.MapPath($"~/Uploads/Inspections/{inspection.Id}");
                            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                            var ext = Path.GetExtension(model.Report.FileName);
                            var fileName = Guid.NewGuid().ToString("N") + ext;
                            var physicalPath = Path.Combine(folder, fileName);
                            model.Report.SaveAs(physicalPath);
                            inspection.ReportPath = $"/Uploads/Inspections/{inspection.Id}/{fileName}";
                        }
                        catch (Exception reportEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Report upload failed: {reportEx.Message}");
                        }
                    }

                    await db.SaveChangesAsync();

                    // Notify Project Manager
                    var pm = db.Staff.Find(project.ProjectManagerId);
                    if (pm != null && !string.IsNullOrEmpty(pm.UserId))
                    {
                        await NotifyStakeholder(
                            pm.UserId,
                            "Inspection Recorded",
                            $"A {inspection.InspectionType} inspection was recorded on '{project.ProjectName}' by {(staff != null ? staff.FullName : User.Identity.Name)}.",
                            "Project",
                            $"/Quality/InspectionDetails/{inspection.Id}"
                        );
                    }

                    TempData["SuccessMessage"] = "Inspection recorded successfully.";
                    return RedirectToAction("InspectionDetails", new { id = inspection.Id });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating inspection: {ex.Message}");
                    ModelState.AddModelError("", "Error recording inspection: " + ex.Message);
                }
            }

            return View(RepopulateCreateInspection(model));
        }

        // ------------------------------------------------------------------
        // Inspection Details / Review
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Project Manager, Site Manager, Client")]
        public ActionResult InspectionDetails(int id)
        {
            var inspection = db.SiteInspections
                .Include(i => i.Project)
                .Include(i => i.Project.ProjectManager)
                .Include(i => i.Project.SiteManager)
                .Include(i => i.PerformedBy)
                .FirstOrDefault(i => i.Id == id);

            if (inspection == null)
            {
                TempData["ErrorMessage"] = "Inspection not found.";
                return RedirectToAction("Index");
            }

            if (!UserCanViewInspection(inspection))
            {
                TempData["ErrorMessage"] = "You do not have permission to view this inspection.";
                return RedirectToAction("Index");
            }

            // Related variations (SiteInspectionId FK is now present)
            var relatedVariations = db.ProjectVariations
                .Where(v => v.SiteInspectionId == id)
                .OrderByDescending(v => v.RequestDate)
                .ToList();

            var vm = new InspectionDetailsViewModel
            {
                Id = inspection.Id,
                ProjectId = inspection.ProjectId,
                ProjectName = inspection.Project?.ProjectName,
                ProjectCode = inspection.Project?.ProjectCode,
                PerformedByName = inspection.PerformedBy != null ? inspection.PerformedBy.FullName : "N/A",
                InspectionDate = inspection.InspectionDate,
                InspectionType = inspection.InspectionType,
                InspectionArea = inspection.InspectionArea,
                Findings = inspection.Findings,
                Recommendations = inspection.Recommendations,
                Score = inspection.Score,
                Status = inspection.Status,
                StatusBadgeColor = GetInspectionStatusColor(inspection.Status),
                IsPassed = inspection.IsPassed,
                ActionRequired = inspection.ActionRequired,
                ActionItems = inspection.ActionItems,
                DueDate = inspection.DueDate,
                CompletionDate = inspection.CompletionDate,
                ReviewedBy = inspection.ReviewedBy,
                ReviewDate = inspection.ReviewDate,
                PhotosPath = inspection.PhotosPath,
                ReportPath = inspection.ReportPath,
                CreatedDate = inspection.CreatedDate,
                RelatedVariations = relatedVariations.Select(v => new VariationListItemViewModel
                {
                    Id = v.Id,
                    VariationNumber = v.VariationNumber,
                    VariationType = v.VariationType,
                    Status = v.Status,
                    StatusBadgeColor = GetVariationStatusColor(v.Status),
                    RequestDate = v.RequestDate,
                    ImpactOnCost = v.ImpactOnCost
                }).ToList(),
                CanReview = (User.IsInRole("Admin") || User.IsInRole("Project Manager")) && inspection.Status == "Completed",
                CanRequestVariation = (User.IsInRole("Admin") || User.IsInRole("Project Manager") || User.IsInRole("Site Manager") || User.IsInRole("Client"))
                    && (inspection.ActionRequired || (inspection.Score.HasValue && inspection.Score.Value < 80) || inspection.Status != "Reviewed")
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Project Manager")]
        public async Task<ActionResult> MarkInspectionReviewed(int id)
        {
            var inspection = db.SiteInspections
                .Include(i => i.Project)
                .Include(i => i.PerformedBy)
                .FirstOrDefault(i => i.Id == id);

            if (inspection == null)
            {
                TempData["ErrorMessage"] = "Inspection not found.";
                return RedirectToAction("Index");
            }

            if (!UserCanViewInspection(inspection))
            {
                TempData["ErrorMessage"] = "You do not have permission to review this inspection.";
                return RedirectToAction("Index");
            }

            if (inspection.Status != "Completed")
            {
                TempData["ErrorMessage"] = "Only completed inspections can be marked as reviewed.";
                return RedirectToAction("InspectionDetails", new { id });
            }

            inspection.Status = "Reviewed";
            inspection.ReviewedBy = User.Identity.Name;
            inspection.ReviewDate = DateTime.Now;
            await db.SaveChangesAsync();

            if (inspection.PerformedBy != null && !string.IsNullOrEmpty(inspection.PerformedBy.UserId))
            {
                await NotifyStakeholder(
                    inspection.PerformedBy.UserId,
                    "Inspection Reviewed",
                    $"Your inspection on '{inspection.Project?.ProjectName}' has been reviewed by {User.Identity.Name}.",
                    "Project",
                    $"/Quality/InspectionDetails/{inspection.Id}"
                );
            }

            TempData["SuccessMessage"] = "Inspection marked as reviewed.";
            return RedirectToAction("InspectionDetails", new { id });
        }

        // ------------------------------------------------------------------
        // Inspection Queue
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Project Manager, Site Manager")]
        public ActionResult InspectionQueue(string status = null, int? projectId = null, DateTime? from = null, DateTime? to = null)
        {
            var staff = ResolveCurrentStaff();
            IQueryable<SiteInspection> query = db.SiteInspections
                .Include(i => i.Project)
                .Include(i => i.PerformedBy);

            if (User.IsInRole("Admin"))
            {
                // all
            }
            else if (User.IsInRole("Project Manager") && staff != null)
            {
                var pmIds = db.Projects.Where(p => p.ProjectManagerId == staff.Id).Select(p => p.Id).ToList();
                query = query.Where(i => pmIds.Contains(i.ProjectId));
            }
            else if (User.IsInRole("Site Manager") && staff != null)
            {
                var smIds = db.Projects.Where(p => p.SiteManagerId == staff.Id).Select(p => p.Id).ToList();
                query = query.Where(i => smIds.Contains(i.ProjectId) || i.PerformedByStaffId == staff.Id);
            }
            else
            {
                query = query.Where(i => false);
            }

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(i => i.Status == status);
            if (projectId.HasValue)
                query = query.Where(i => i.ProjectId == projectId.Value);
            if (from.HasValue)
                query = query.Where(i => i.InspectionDate >= from.Value);
            if (to.HasValue)
                query = query.Where(i => i.InspectionDate <= to.Value);

            var items = query
                .OrderByDescending(i => i.InspectionDate)
                .ToList()
                .Select(i => new InspectionListItemViewModel
                {
                    Id = i.Id,
                    ProjectName = i.Project?.ProjectName,
                    ProjectCode = i.Project?.ProjectCode,
                    InspectionType = i.InspectionType,
                    InspectionDate = i.InspectionDate,
                    PerformedByName = i.PerformedBy != null ? i.PerformedBy.FullName : "N/A",
                    Status = i.Status,
                    StatusBadgeColor = GetInspectionStatusColor(i.Status),
                    Score = i.Score,
                    ActionRequired = i.ActionRequired,
                    IsPassed = i.IsPassed
                }).ToList();

            var vm = new InspectionQueueViewModel
            {
                Items = items,
                StatusFilter = status,
                ProjectIdFilter = projectId,
                FromDate = from,
                ToDate = to,
                Projects = GetProjectSelectListForUser(),
                Statuses = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "All Statuses" },
                    new SelectListItem { Value = "Completed", Text = "Completed" },
                    new SelectListItem { Value = "Reviewed", Text = "Reviewed" }
                }
            };

            return View(vm);
        }

        // ------------------------------------------------------------------
        // Request Variation
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Project Manager, Site Manager, Client")]
        public ActionResult RequestVariation(int? inspectionId = null)
        {
            var model = new RequestVariationViewModel
            {
                Projects = GetProjectSelectListForUser(),
                VariationTypes = GetVariationTypeSelectList(null)
            };

            if (inspectionId.HasValue)
            {
                var inspection = db.SiteInspections
                    .Include(i => i.Project)
                    .FirstOrDefault(i => i.Id == inspectionId.Value);

                if (inspection != null && UserCanViewInspection(inspection))
                {
                    model.SiteInspectionId = inspection.Id;
                    model.ProjectId = inspection.ProjectId;
                    model.RelatedInspectionSummary = $"{inspection.InspectionType} on {inspection.InspectionDate:dd MMM yyyy} – {inspection.InspectionArea}";
                    if (string.IsNullOrWhiteSpace(model.Description) && !string.IsNullOrWhiteSpace(inspection.Findings))
                        model.Description = "Triggered by inspection findings: " + inspection.Findings;
                    if (string.IsNullOrWhiteSpace(model.Reason) && inspection.ActionRequired)
                        model.Reason = "Non-conformance / action required from inspection.";
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Project Manager, Site Manager, Client")]
        public async Task<ActionResult> RequestVariation(RequestVariationViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var project = db.Projects
                        .Include(p => p.Client)
                        .FirstOrDefault(p => p.Id == model.ProjectId);

                    if (project == null || !project.IsActive)
                    {
                        ModelState.AddModelError("ProjectId", "Selected project is not available.");
                        return View(RepopulateRequestVariation(model));
                    }

                    if (!UserCanRequestVariationOnProject(project))
                    {
                        ModelState.AddModelError("ProjectId", "You do not have permission to request a variation on this project.");
                        return View(RepopulateRequestVariation(model));
                    }

                    // Optional inspection link validation
                    if (model.SiteInspectionId.HasValue)
                    {
                        var insp = db.SiteInspections.Find(model.SiteInspectionId.Value);
                        if (insp == null || insp.ProjectId != model.ProjectId)
                        {
                            ModelState.AddModelError("SiteInspectionId", "Linked inspection does not belong to the selected project.");
                            return View(RepopulateRequestVariation(model));
                        }
                    }

                    var variation = new ProjectVariation
                    {
                        ProjectId = model.ProjectId,
                        VariationType = model.VariationType,
                        Description = model.Description,
                        Reason = model.Reason,
                        ImpactOnScope = model.ImpactOnScope,
                        ImpactOnCost = model.ImpactOnCost,
                        ImpactOnTimelineDays = model.ImpactOnTimelineDays,
                        RequestedBy = User.Identity.Name,
                        RequestDate = DateTime.Now,
                        Status = "Requested",
                        SiteInspectionId = model.SiteInspectionId
                        // VariationNumber is set by the entity constructor – do not overwrite
                    };

                    db.ProjectVariations.Add(variation);
                    await db.SaveChangesAsync();

                    // Supporting documents
                    if (model.SupportingDocuments != null && model.SupportingDocuments.ContentLength > 0)
                    {
                        try
                        {
                            var folder = Server.MapPath($"~/Uploads/Variations/{variation.Id}");
                            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                            var ext = Path.GetExtension(model.SupportingDocuments.FileName);
                            var fileName = Guid.NewGuid().ToString("N") + ext;
                            var physicalPath = Path.Combine(folder, fileName);
                            model.SupportingDocuments.SaveAs(physicalPath);
                            variation.SupportingDocumentsPath = $"/Uploads/Variations/{variation.Id}/{fileName}";
                            await db.SaveChangesAsync();
                        }
                        catch (Exception docEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Supporting doc upload failed: {docEx.Message}");
                        }
                    }

                    // Notify Project Manager + Admins
                    var pm = db.Staff.Find(project.ProjectManagerId);
                    if (pm != null && !string.IsNullOrEmpty(pm.UserId))
                    {
                        await NotifyStakeholder(
                            pm.UserId,
                            "Variation Requested",
                            $"Variation {variation.VariationNumber} ({variation.VariationType}) has been requested on '{project.ProjectName}' by {User.Identity.Name}.",
                            "Project",
                            $"/Quality/VariationDetails/{variation.Id}"
                        );
                    }

                    var adminIds = db.Users.ToList()
                        .Where(u => UserManager.IsInRole(u.Id, "Admin"))
                        .Select(u => u.Id)
                        .ToList();
                    foreach (var adminId in adminIds)
                    {
                        await CreateNotification(adminId, "Variation Requested",
                            $"Variation {variation.VariationNumber} requested on '{project.ProjectName}'.",
                            "Project", $"/Quality/VariationDetails/{variation.Id}");
                    }

                    TempData["SuccessMessage"] = $"Variation {variation.VariationNumber} submitted successfully.";
                    return RedirectToAction("VariationDetails", new { id = variation.Id });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error requesting variation: {ex.Message}");
                    ModelState.AddModelError("", "Error submitting variation: " + ex.Message);
                }
            }

            return View(RepopulateRequestVariation(model));
        }

        // ------------------------------------------------------------------
        // Variation Queue / Details
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Project Manager, Client")]
        public ActionResult VariationQueue(string status = null, int? projectId = null, string type = null)
        {
            var staff = ResolveCurrentStaff();
            var userId = User.Identity.GetUserId();
            var client = db.Clients.FirstOrDefault(c => c.UserId == userId);

            IQueryable<ProjectVariation> query = db.ProjectVariations
                .Include(v => v.Project);

            if (User.IsInRole("Admin"))
            {
                // all
            }
            else if (User.IsInRole("Project Manager") && staff != null)
            {
                var pmIds = db.Projects.Where(p => p.ProjectManagerId == staff.Id).Select(p => p.Id).ToList();
                query = query.Where(v => pmIds.Contains(v.ProjectId));
            }
            else if (User.IsInRole("Client") && client != null)
            {
                var clientIds = db.Projects.Where(p => p.ClientId == client.Id).Select(p => p.Id).ToList();
                query = query.Where(v => clientIds.Contains(v.ProjectId));
            }
            else
            {
                query = query.Where(v => false);
            }

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(v => v.Status == status);
            if (projectId.HasValue)
                query = query.Where(v => v.ProjectId == projectId.Value);
            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(v => v.VariationType == type);

            var items = query
                .OrderByDescending(v => v.RequestDate)
                .ToList()
                .Select(v => new VariationListItemViewModel
                {
                    Id = v.Id,
                    VariationNumber = v.VariationNumber,
                    ProjectName = v.Project?.ProjectName,
                    VariationType = v.VariationType,
                    Status = v.Status,
                    StatusBadgeColor = GetVariationStatusColor(v.Status),
                    RequestDate = v.RequestDate,
                    RequestedBy = v.RequestedBy,
                    ImpactOnCost = v.ImpactOnCost,
                    ImpactOnTimelineDays = v.ImpactOnTimelineDays
                }).ToList();

            var vm = new VariationQueueViewModel
            {
                Items = items,
                StatusFilter = status,
                ProjectIdFilter = projectId,
                TypeFilter = type,
                Projects = GetProjectSelectListForUser(),
                Statuses = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "All Statuses" },
                    new SelectListItem { Value = "Requested", Text = "Requested" },
                    new SelectListItem { Value = "Under Review", Text = "Under Review" },
                    new SelectListItem { Value = "Approved", Text = "Approved" },
                    new SelectListItem { Value = "Rejected", Text = "Rejected" },
                    new SelectListItem { Value = "Implemented", Text = "Implemented" }
                },
                VariationTypes = GetVariationTypeSelectList(type)
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin, Project Manager, Client")]
        public ActionResult VariationDetails(int id)
        {
            var variation = db.ProjectVariations
                .Include(v => v.Project)
                .Include(v => v.Project.Client)
                .Include(v => v.Project.ProjectManager)
                .Include(v => v.Approvals)
                .FirstOrDefault(v => v.Id == id);

            if (variation == null)
            {
                TempData["ErrorMessage"] = "Variation not found.";
                return RedirectToAction("Index");
            }

            if (!UserCanViewVariation(variation))
            {
                TempData["ErrorMessage"] = "You do not have permission to view this variation.";
                return RedirectToAction("Index");
            }

            SiteInspection linkedInspection = null;
            if (variation.SiteInspectionId.HasValue)
            {
                linkedInspection = db.SiteInspections
                    .Include(i => i.PerformedBy)
                    .FirstOrDefault(i => i.Id == variation.SiteInspectionId.Value);
            }

            var vm = new VariationDetailsViewModel
            {
                Id = variation.Id,
                VariationNumber = variation.VariationNumber,
                ProjectId = variation.ProjectId,
                ProjectName = variation.Project?.ProjectName,
                ProjectCode = variation.Project?.ProjectCode,
                VariationType = variation.VariationType,
                Description = variation.Description,
                Reason = variation.Reason,
                ImpactOnScope = variation.ImpactOnScope,
                ImpactOnCost = variation.ImpactOnCost,
                ImpactOnTimelineDays = variation.ImpactOnTimelineDays,
                RequestedBy = variation.RequestedBy,
                RequestDate = variation.RequestDate,
                Status = variation.Status,
                StatusBadgeColor = GetVariationStatusColor(variation.Status),
                ApprovedBy = variation.ApprovedBy,
                ApprovalDate = variation.ApprovalDate,
                RejectionReason = variation.RejectionReason,
                ImplementationDate = variation.ImplementationDate,
                ImplementationNotes = variation.ImplementationNotes,
                SupportingDocumentsPath = variation.SupportingDocumentsPath,
                LinkedInspectionId = linkedInspection?.Id,
                LinkedInspectionSummary = linkedInspection != null
                    ? $"{linkedInspection.InspectionType} on {linkedInspection.InspectionDate:dd MMM yyyy} by {(linkedInspection.PerformedBy != null ? linkedInspection.PerformedBy.FullName : "N/A")}"
                    : null,
                Approvals = (variation.Approvals ?? Enumerable.Empty<VariationApproval>())
                    .OrderByDescending(a => a.ApprovalDate)
                    .Select(a => new VariationApprovalItemViewModel
                    {
                        ApproverName = a.ApproverName,
                        ApprovalStatus = a.ApprovalStatus,
                        Comments = a.Comments,
                        // FIX: ApprovalDate is DateTime? on the entity, non-nullable on the view-model
                        ApprovalDate = a.ApprovalDate ?? DateTime.MinValue,
                        ApprovalLevel = a.ApprovalLevel
                    }).ToList(),
                CanApprove = (User.IsInRole("Admin") || User.IsInRole("Project Manager"))
                    && (variation.Status == "Requested" || variation.Status == "Under Review")
            };

            return View(vm);
        }

        // ------------------------------------------------------------------
        // Approve / Reject Variation
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Project Manager")]
        public ActionResult ApproveVariation(int id)
        {
            var variation = db.ProjectVariations
                .Include(v => v.Project)
                .FirstOrDefault(v => v.Id == id);

            if (variation == null)
            {
                TempData["ErrorMessage"] = "Variation not found.";
                return RedirectToAction("VariationQueue");
            }

            if (!UserCanViewVariation(variation))
            {
                TempData["ErrorMessage"] = "You do not have permission to approve this variation.";
                return RedirectToAction("VariationQueue");
            }

            if (variation.Status != "Requested" && variation.Status != "Under Review")
            {
                TempData["ErrorMessage"] = "This variation is not currently awaiting approval.";
                return RedirectToAction("VariationDetails", new { id });
            }

            // Extra ownership: non-Admin PM must manage the project
            if (!User.IsInRole("Admin"))
            {
                var staff = ResolveCurrentStaff();
                if (staff == null || variation.Project?.ProjectManagerId != staff.Id)
                {
                    TempData["ErrorMessage"] = "You may only approve variations on projects you manage.";
                    return RedirectToAction("VariationDetails", new { id });
                }
            }

            var model = new ApproveVariationViewModel
            {
                VariationId = variation.Id,
                VariationNumber = variation.VariationNumber,
                ProjectName = variation.Project?.ProjectName,
                VariationType = variation.VariationType,
                Description = variation.Description,
                Reason = variation.Reason,
                ImpactOnCost = variation.ImpactOnCost,
                ImpactOnTimelineDays = variation.ImpactOnTimelineDays,
                ImplementationDate = DateTime.Today
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Project Manager")]
        public async Task<ActionResult> ApproveVariation(ApproveVariationViewModel model)
        {
            if (model.Decision != "Approve" && model.Decision != "Reject")
            {
                ModelState.AddModelError("Decision", "Please choose Approve or Reject.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var variation = db.ProjectVariations
                        .Include(v => v.Project)
                        .Include(v => v.Project.Budget)
                        .Include(v => v.Project.Client)
                        .FirstOrDefault(v => v.Id == model.VariationId);

                    if (variation == null)
                    {
                        TempData["ErrorMessage"] = "Variation not found.";
                        return RedirectToAction("VariationQueue");
                    }

                    if (!UserCanViewVariation(variation))
                    {
                        TempData["ErrorMessage"] = "You do not have permission to process this variation.";
                        return RedirectToAction("VariationQueue");
                    }

                    if (variation.Status != "Requested" && variation.Status != "Under Review")
                    {
                        TempData["ErrorMessage"] = "This variation is not currently awaiting approval.";
                        return RedirectToAction("VariationDetails", new { id = variation.Id });
                    }

                    if (!User.IsInRole("Admin"))
                    {
                        var staff = ResolveCurrentStaff();
                        if (staff == null || variation.Project?.ProjectManagerId != staff.Id)
                        {
                            TempData["ErrorMessage"] = "You may only approve variations on projects you manage.";
                            return RedirectToAction("VariationDetails", new { id = variation.Id });
                        }
                    }

                    if (model.Decision == "Approve")
                    {
                        variation.Status = "Approved";
                        variation.ApprovedBy = User.Identity.Name;
                        variation.ApprovalDate = DateTime.Now;
                        variation.ImplementationDate = model.ImplementationDate ?? DateTime.Now;
                        variation.ImplementationNotes = model.Comments;

                        var approval = new VariationApproval
                        {
                            VariationId = variation.Id,
                            ApproverName = User.Identity.Name,
                            ApproverEmail = User.Identity.Name,
                            ApprovalStatus = "Approved",
                            Comments = model.Comments,
                            ApprovalDate = DateTime.Now,
                            ApprovalLevel = 1
                        };
                        db.VariationApprovals.Add(approval);

                        // Update project baseline
                        if (variation.Project != null)
                        {
                            if (variation.Project.Budget != null && variation.ImpactOnCost.HasValue)
                            {
                                var delta = variation.ImpactOnCost.Value;
                                variation.Project.Budget.EstimatedCost += delta;
                                variation.Project.Budget.AllocatedAmount += delta;
                                variation.Project.Budget.RemainingAmount += delta;
                                variation.Project.Budget.LastUpdated = DateTime.Now;
                            }

                            if (variation.ImpactOnTimelineDays.HasValue && variation.ImpactOnTimelineDays.Value > 0
                                && variation.Project.EstimatedEndDate.HasValue)
                            {
                                variation.Project.EstimatedEndDate = variation.Project.EstimatedEndDate.Value
                                    .AddDays(variation.ImpactOnTimelineDays.Value);
                            }

                            variation.Project.LastModifiedDate = DateTime.Now;
                            variation.Project.LastModifiedBy = User.Identity.Name;
                        }
                    }
                    else // Reject
                    {
                        variation.Status = "Rejected";
                        variation.ApprovedBy = User.Identity.Name;
                        variation.ApprovalDate = DateTime.Now;
                        variation.RejectionReason = model.Comments;

                        var approval = new VariationApproval
                        {
                            VariationId = variation.Id,
                            ApproverName = User.Identity.Name,
                            ApproverEmail = User.Identity.Name,
                            ApprovalStatus = "Rejected",
                            Comments = model.Comments,
                            ApprovalDate = DateTime.Now,
                            ApprovalLevel = 1
                        };
                        db.VariationApprovals.Add(approval);
                        // Do NOT touch Project.Budget or EstimatedEndDate
                    }

                    await db.SaveChangesAsync();

                    // Notify Client + Admins
                    if (variation.Project?.Client != null && !string.IsNullOrEmpty(variation.Project.Client.UserId))
                    {
                        var title = model.Decision == "Approve" ? "Variation Approved" : "Variation Rejected";
                        var msg = model.Decision == "Approve"
                            ? $"Variation {variation.VariationNumber} on '{variation.Project.ProjectName}' has been approved."
                            : $"Variation {variation.VariationNumber} on '{variation.Project.ProjectName}' has been rejected. Reason: {model.Comments}";
                        await NotifyStakeholder(variation.Project.Client.UserId, title, msg, "Project",
                            $"/Quality/VariationDetails/{variation.Id}");
                    }

                    var adminIds = db.Users.ToList()
                        .Where(u => UserManager.IsInRole(u.Id, "Admin"))
                        .Select(u => u.Id)
                        .ToList();
                    foreach (var adminId in adminIds)
                    {
                        await CreateNotification(adminId,
                            model.Decision == "Approve" ? "Variation Approved" : "Variation Rejected",
                            $"Variation {variation.VariationNumber} was {model.Decision.ToLower()}d by {User.Identity.Name}.",
                            "Project", $"/Quality/VariationDetails/{variation.Id}");
                    }

                    TempData["SuccessMessage"] = model.Decision == "Approve"
                        ? "Variation approved and project baseline updated."
                        : "Variation rejected.";
                    return RedirectToAction("VariationDetails", new { id = variation.Id });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing variation: {ex.Message}");
                    ModelState.AddModelError("", "Error processing variation: " + ex.Message);
                }
            }

            return View(model);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private Staff ResolveCurrentStaff()
        {
            var userId = User.Identity.GetUserId();
            return db.Staff.FirstOrDefault(s => s.UserId == userId && s.IsActive);
        }

        private bool UserCanViewInspection(SiteInspection inspection)
        {
            if (User.IsInRole("Admin")) return true;
            var staff = ResolveCurrentStaff();
            var userId = User.Identity.GetUserId();
            var client = db.Clients.FirstOrDefault(c => c.UserId == userId);

            if (User.IsInRole("Project Manager") && staff != null)
                return inspection.Project != null && inspection.Project.ProjectManagerId == staff.Id;
            if (User.IsInRole("Site Manager") && staff != null)
                return (inspection.Project != null && inspection.Project.SiteManagerId == staff.Id)
                    || inspection.PerformedByStaffId == staff.Id;
            if (User.IsInRole("Client") && client != null)
                return inspection.Project != null && inspection.Project.ClientId == client.Id;
            return false;
        }

        private bool UserCanViewVariation(ProjectVariation variation)
        {
            if (User.IsInRole("Admin")) return true;
            var staff = ResolveCurrentStaff();
            var userId = User.Identity.GetUserId();
            var client = db.Clients.FirstOrDefault(c => c.UserId == userId);

            if (User.IsInRole("Project Manager") && staff != null)
                return variation.Project != null && variation.Project.ProjectManagerId == staff.Id;
            if (User.IsInRole("Client") && client != null)
                return variation.Project != null && variation.Project.ClientId == client.Id;
            return false;
        }

        private bool UserCanRequestVariationOnProject(Project project)
        {
            if (User.IsInRole("Admin")) return true;
            var staff = ResolveCurrentStaff();
            var userId = User.Identity.GetUserId();
            var client = db.Clients.FirstOrDefault(c => c.UserId == userId);

            if (User.IsInRole("Project Manager") && staff != null)
                return project.ProjectManagerId == staff.Id;
            if (User.IsInRole("Site Manager") && staff != null)
                return project.SiteManagerId == staff.Id;
            if (User.IsInRole("Client") && client != null)
                return project.ClientId == client.Id;
            return false;
        }

        private List<SelectListItem> GetProjectSelectListForUser()
        {
            var staff = ResolveCurrentStaff();
            var userId = User.Identity.GetUserId();
            var client = db.Clients.FirstOrDefault(c => c.UserId == userId);

            IQueryable<Project> query = db.Projects.Where(p => p.IsActive);

            if (User.IsInRole("Admin"))
            {
                // all active
            }
            else if (User.IsInRole("Project Manager") && staff != null)
            {
                query = query.Where(p => p.ProjectManagerId == staff.Id);
            }
            else if (User.IsInRole("Site Manager") && staff != null)
            {
                query = query.Where(p => p.SiteManagerId == staff.Id);
            }
            else if (User.IsInRole("Client") && client != null)
            {
                query = query.Where(p => p.ClientId == client.Id);
            }
            else
            {
                query = query.Where(p => false);
            }

            return query
                .OrderBy(p => p.ProjectName)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.ProjectName + " (" + p.ProjectCode + ")"
                }).ToList();
        }

        private List<SelectListItem> GetInspectionTypeSelectList(string selected)
        {
            var items = new[] { "Safety", "Quality", "Progress", "Compliance" };
            return items.Select(c => new SelectListItem { Value = c, Text = c, Selected = c == selected }).ToList();
        }

        private List<SelectListItem> GetVariationTypeSelectList(string selected)
        {
            var items = new[] { "Change Order", "Scope Change", "Time Extension", "Cost Adjustment" };
            return items.Select(c => new SelectListItem { Value = c, Text = c, Selected = c == selected }).ToList();
        }

        private static string GetInspectionStatusColor(string status)
        {
            switch (status)
            {
                case "Completed": return "info";
                case "Reviewed": return "success";
                default: return "secondary";
            }
        }

        private static string GetVariationStatusColor(string status)
        {
            switch (status)
            {
                case "Requested": return "warning";
                case "Under Review": return "info";
                case "Approved": return "success";
                case "Rejected": return "danger";
                case "Implemented": return "primary";
                default: return "secondary";
            }
        }

        private CreateInspectionViewModel RepopulateCreateInspection(CreateInspectionViewModel model)
        {
            model.Projects = GetProjectSelectListForUser();
            model.InspectionTypes = GetInspectionTypeSelectList(model.InspectionType);
            return model;
        }

        private RequestVariationViewModel RepopulateRequestVariation(RequestVariationViewModel model)
        {
            model.Projects = GetProjectSelectListForUser();
            model.VariationTypes = GetVariationTypeSelectList(model.VariationType);
            return model;
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

        private async Task NotifyStakeholder(string userId, string title, string message, string type, string link)
        {
            if (string.IsNullOrEmpty(userId)) return;
            await CreateNotification(userId, title, message, type, link);
            // Email omitted for brevity – pattern matches ProjectController if EmailHelper is available
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