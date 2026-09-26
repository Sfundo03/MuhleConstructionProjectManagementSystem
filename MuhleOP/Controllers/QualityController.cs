using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MuhleOP.Models;
using MuhleOP.ViewModels;
using MuhleOP.Helpers;

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
                VariationsPending = variations.Count(v => v.Status == "Requested" || v.Status == "Under Review" || v.Status == "PendingValidation" || v.Status == "PendingFinanceApproval"),
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
            var allTemplateItems = db.InspectionTemplateItems
                .Where(t => t.IsActive)
                .OrderBy(t => t.InspectionType)
                .ThenBy(t => t.Order)
                .ToList()
                .Select(t => new InspectionTemplateItemViewModel
                {
                    Id = t.Id,
                    InspectionType = t.InspectionType,
                    ItemLabel = t.ItemLabel,
                    Category = t.Category,
                    Order = t.Order
                }).ToList();

            var model = new CreateInspectionViewModel
            {
                InspectionDate = DateTime.Today,
                Projects = GetProjectSelectListForUser(),
                InspectionTypes = GetInspectionTypeSelectList(null),
                TemplateItems = allTemplateItems
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

            bool isSiteManagerOnly = User.IsInRole("Site Manager") && !User.IsInRole("Admin");
            if (isSiteManagerOnly && string.IsNullOrWhiteSpace(model.SignatureData))
            {
                ModelState.AddModelError("SignatureData", "Site Manager digital signature is required to finalise the inspection.");
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

                    if (isSiteManagerOnly)
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
                        MeasurementNotes = model.MeasurementNotes,
                        SignedOffBy = staff != null ? staff.FullName : User.Identity.Name,
                        SignedOffDate = DateTime.Now,
                        CreatedDate = DateTime.Now
                    };

                    if (model.Score.HasValue)
                        inspection.IsPassed = model.Score.Value >= 80;
                    else
                        inspection.IsPassed = !model.ActionRequired;

                    db.SiteInspections.Add(inspection);
                    await db.SaveChangesAsync();

                    if (!string.IsNullOrWhiteSpace(model.SignatureData))
                    {
                        try
                        {
                            var folder = Server.MapPath($"~/Uploads/Inspections/{inspection.Id}");
                            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                            var base64Data = model.SignatureData.Contains(",") ? model.SignatureData.Split(',')[1] : model.SignatureData;
                            var bytes = Convert.FromBase64String(base64Data);
                            var sigFileName = $"signature_{Guid.NewGuid():N}.png";
                            var physicalSigPath = Path.Combine(folder, sigFileName);
                            System.IO.File.WriteAllBytes(physicalSigPath, bytes);
                            inspection.SignatureDataPath = $"/Uploads/Inspections/{inspection.Id}/{sigFileName}";
                        }
                        catch (Exception sigEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Signature saving failed: {sigEx.Message}");
                        }
                    }

                    if (model.ItemResultsInput != null)
                    {
                        foreach (var ir in model.ItemResultsInput)
                        {
                            if (!string.IsNullOrEmpty(ir.Result))
                            {
                                db.InspectionItemResults.Add(new InspectionItemResult
                                {
                                    SiteInspectionId = inspection.Id,
                                    TemplateItemId = ir.TemplateItemId,
                                    Result = ir.Result,
                                    Notes = ir.Notes
                                });
                            }
                        }
                    }

                    bool hasCritical = false;
                    if (model.NonConformancesInput != null)
                    {
                        foreach (var ncInput in model.NonConformancesInput)
                        {
                            if (!string.IsNullOrWhiteSpace(ncInput.Description))
                            {
                                var nc = new NonConformance
                                {
                                    SiteInspectionId = inspection.Id,
                                    Description = ncInput.Description,
                                    Severity = ncInput.Severity ?? "Minor",
                                    ResponsibleParty = string.IsNullOrWhiteSpace(ncInput.ResponsibleParty) ? "Site Contractor" : ncInput.ResponsibleParty,
                                    DueDate = ncInput.DueDate,
                                    ReferenceNumber = "TEMP"
                                };
                                db.NonConformances.Add(nc);
                                await db.SaveChangesAsync();

                                nc.ReferenceNumber = $"NC-{nc.Id:D4}";
                                if (nc.Severity == "Critical")
                                {
                                    hasCritical = true;
                                }
                            }
                        }
                    }

                    if (hasCritical)
                    {
                        inspection.ActionRequired = true;
                        inspection.IsPassed = false;
                    }

                    if (model.Photos != null)
                    {
                        var photoPaths = new List<string>();
                        if (!string.IsNullOrEmpty(inspection.PhotosPath))
                        {
                            photoPaths.Add(inspection.PhotosPath);
                        }

                        foreach (var photo in model.Photos)
                        {
                            if (photo != null && photo.ContentLength > 0)
                            {
                                try
                                {
                                    var folder = Server.MapPath($"~/Uploads/Inspections/{inspection.Id}");
                                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                                    var ext = Path.GetExtension(photo.FileName);
                                    var fileName = Guid.NewGuid().ToString("N") + ext;
                                    var physicalPath = Path.Combine(folder, fileName);
                                    photo.SaveAs(physicalPath);
                                    photoPaths.Add($"/Uploads/Inspections/{inspection.Id}/{fileName}");
                                }
                                catch (Exception photoEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Photo upload failed: {photoEx.Message}");
                                }
                            }
                        }
                        if (photoPaths.Any())
                        {
                            inspection.PhotosPath = string.Join(";", photoPaths);
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

                    var pm = db.Staff.Find(project.ProjectManagerId);
                    if (pm != null && !string.IsNullOrEmpty(pm.UserId))
                    {
                        await NotifyStakeholder(
                            pm.UserId,
                            hasCritical ? "CRITICAL SAFETY ISSUE — Inspection Recorded" : "Inspection Recorded",
                            hasCritical
                                ? $"CRITICAL safety issue on '{project.ProjectName}' in inspection #{inspection.Id}. Affected task halted."
                                : $"A {inspection.InspectionType} inspection was recorded on '{project.ProjectName}' by {(staff != null ? staff.FullName : User.Identity.Name)}.",
                            "Project",
                            $"/Quality/InspectionDetails/{inspection.Id}"
                        );
                    }

                    if (hasCritical)
                    {
                        var adminIds = db.Users.ToList()
                            .Where(u => UserManager.IsInRole(u.Id, "Admin"))
                            .Select(u => u.Id)
                            .ToList();
                        foreach (var adminId in adminIds)
                        {
                            await CreateNotification(adminId, "CRITICAL SAFETY ISSUE",
                                $"Critical safety issue recorded on '{project.ProjectName}' in inspection #{inspection.Id}.",
                                "Project", $"/Quality/InspectionDetails/{inspection.Id}");
                        }
                    }

                    TempData["SuccessMessage"] = hasCritical ? "Inspection recorded. CRITICAL SAFETY ISSUE detected and stakeholders notified!" : "Inspection recorded successfully.";
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
                .Include(i => i.ItemResults.Select(r => r.TemplateItem))
                .Include(i => i.NonConformances)
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

            var relatedVariations = db.ProjectVariations
                .Where(v => v.SiteInspectionId == id)
                .OrderByDescending(v => v.RequestDate)
                .ToList();

            var nonConformancesList = (inspection.NonConformances ?? new List<NonConformance>())
                .Select(nc => new NonConformanceViewModel
                {
                    Id = nc.Id,
                    ReferenceNumber = nc.ReferenceNumber,
                    Description = nc.Description,
                    Severity = nc.Severity,
                    ResponsibleParty = nc.ResponsibleParty,
                    Status = nc.Status,
                    DueDate = nc.DueDate,
                    CreatedDate = nc.CreatedDate
                }).ToList();

            var itemResultsList = (inspection.ItemResults ?? new List<InspectionItemResult>())
                .Select(ir => new InspectionItemResultViewModel
                {
                    Id = ir.Id,
                    ItemLabel = ir.TemplateItem != null ? ir.TemplateItem.ItemLabel : "Checklist Item",
                    Category = ir.TemplateItem != null ? ir.TemplateItem.Category : "",
                    Result = ir.Result,
                    Notes = ir.Notes
                }).ToList();

            bool hasCritical = nonConformancesList.Any(nc => nc.Severity == "Critical");

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
                ItemResults = itemResultsList,
                NonConformances = nonConformancesList,
                HasCriticalNonConformance = hasCritical,
                SignedOffBy = inspection.SignedOffBy,
                SignedOffDate = inspection.SignedOffDate,
                SignatureDataPath = inspection.SignatureDataPath,
                MeasurementNotes = inspection.MeasurementNotes,
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
                    && (inspection.ActionRequired || (inspection.Score.HasValue && inspection.Score.Value < 80) || inspection.Status != "Reviewed" || hasCritical)
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
                .Include(i => i.PerformedBy)
                .Include(i => i.NonConformances);

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
                    IsPassed = i.IsPassed,
                    HasCriticalNonConformance = i.NonConformances != null && i.NonConformances.Any(nc => nc.Severity == "Critical")
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
        // Request Variation & Non-Conformance Actions
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Project Manager, Site Manager")]
        public ActionResult RequestVariationFromNonConformance(int nonConformanceId)
        {
            var nc = db.NonConformances
                .Include(n => n.SiteInspection)
                .Include(n => n.SiteInspection.Project)
                .FirstOrDefault(n => n.Id == nonConformanceId);

            if (nc == null || nc.SiteInspection == null)
            {
                TempData["ErrorMessage"] = "Non-conformance not found.";
                return RedirectToAction("Index");
            }

            if (!UserCanViewInspection(nc.SiteInspection))
            {
                TempData["ErrorMessage"] = "You do not have permission to access this inspection.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("RequestVariation", new { inspectionId = nc.SiteInspectionId, prefillNonConformanceId = nc.Id });
        }

        [Authorize(Roles = "Admin, Project Manager, Site Manager, Client")]
        public ActionResult RequestVariation(int? inspectionId = null)
        {
            var model = new RequestVariationViewModel
            {
                Projects = GetProjectSelectListForUser(),
                VariationTypes = GetVariationTypeSelectList(null),
                IsClientInitiated = User.IsInRole("Client")
            };

            int? prefillNcId = null;
            if (Request.QueryString["prefillNonConformanceId"] != null && int.TryParse(Request.QueryString["prefillNonConformanceId"], out int parsedNcId))
            {
                prefillNcId = parsedNcId;
            }

            if (inspectionId.HasValue)
            {
                var inspection = db.SiteInspections
                    .Include(i => i.Project)
                    .Include(i => i.NonConformances)
                    .FirstOrDefault(i => i.Id == inspectionId.Value);

                if (inspection != null && UserCanViewInspection(inspection))
                {
                    model.SiteInspectionId = inspection.Id;
                    model.ProjectId = inspection.ProjectId;
                    model.RelatedInspectionSummary = $"{inspection.InspectionType} on {inspection.InspectionDate:dd MMM yyyy} – {inspection.InspectionArea}";

                    if (prefillNcId.HasValue)
                    {
                        var nc = inspection.NonConformances?.FirstOrDefault(n => n.Id == prefillNcId.Value);
                        if (nc != null)
                        {
                            model.NonConformanceId = nc.Id;
                            model.VariationType = "Scope Change";
                            model.Description = $"Non-conformance {nc.ReferenceNumber}: {nc.Description}";
                            model.Reason = "Critical/quality non-conformance found during inspection";
                            model.PrefillSummary = $"Pre-filled from Non-Conformance {nc.ReferenceNumber} ({nc.Severity} severity)";
                        }
                    }

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
            model.IsClientInitiated = User.IsInRole("Client");

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

                    if (model.SiteInspectionId.HasValue)
                    {
                        var insp = db.SiteInspections.Find(model.SiteInspectionId.Value);
                        if (insp == null || insp.ProjectId != model.ProjectId)
                        {
                            ModelState.AddModelError("SiteInspectionId", "Linked inspection does not belong to the selected project.");
                            return View(RepopulateRequestVariation(model));
                        }
                    }

                    string status;
                    if (model.IsDraft)
                    {
                        status = "Draft";
                    }
                    else if (model.IsClientInitiated)
                    {
                        status = "PendingValidation";
                    }
                    else
                    {
                        status = "Requested";
                    }

                    var variation = new ProjectVariation
                    {
                        ProjectId = model.ProjectId,
                        VariationType = model.VariationType,
                        Description = model.Description,
                        Reason = model.Reason,
                        ImpactOnScope = model.ImpactOnScope,
                        ImpactOnQuality = model.ImpactOnQuality,
                        ImpactOnResources = model.ImpactOnResources,
                        ImpactOnCost = model.ImpactOnCost,
                        ImpactOnTimelineDays = model.ImpactOnTimelineDays,
                        RequestedBy = User.Identity.Name,
                        RequestDate = DateTime.Now,
                        Status = status,
                        SiteInspectionId = model.SiteInspectionId,
                        NonConformanceId = model.NonConformanceId
                    };

                    db.ProjectVariations.Add(variation);
                    await db.SaveChangesAsync();

                    // Generate Year-based Reference Number
                    try
                    {
                        variation.VariationNumber = GenerateVariationNumberForYear(variation.RequestDate.Year);
                        await db.SaveChangesAsync();
                    }
                    catch (Exception numEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Number generation failed: {numEx.Message}");
                        variation.VariationNumber = $"VAR-{variation.RequestDate.Year}-{variation.Id:D4}";
                        await db.SaveChangesAsync();
                    }

                    if (model.SupportingDocuments != null && model.SupportingDocuments.Any())
                    {
                        var paths = new List<string>();
                        foreach (var doc in model.SupportingDocuments)
                        {
                            if (doc != null && doc.ContentLength > 0)
                            {
                                try
                                {
                                    var folder = Server.MapPath($"~/Uploads/Variations/{variation.Id}");
                                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                                    var ext = Path.GetExtension(doc.FileName);
                                    var fileName = Guid.NewGuid().ToString("N") + ext;
                                    var physicalPath = Path.Combine(folder, fileName);
                                    doc.SaveAs(physicalPath);
                                    paths.Add($"/Uploads/Variations/{variation.Id}/{fileName}");
                                }
                                catch (Exception docEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Supporting doc upload failed: {docEx.Message}");
                                }
                            }
                        }
                        if (paths.Any())
                        {
                            variation.SupportingDocumentsPath = string.Join(";", paths);
                            await db.SaveChangesAsync();
                        }
                    }

                    if (!model.IsDraft)
                    {
                        var pm = db.Staff.Find(project.ProjectManagerId);
                        if (model.IsClientInitiated)
                        {
                            if (pm != null && !string.IsNullOrEmpty(pm.UserId))
                            {
                                await NotifyStakeholder(
                                    pm.UserId,
                                    "Variation pending your validation",
                                    $"Variation {variation.VariationNumber} ({variation.VariationType}) submitted by client on '{project.ProjectName}' requires validation.",
                                    "Project",
                                    $"/Quality/VariationDetails/{variation.Id}"
                                );
                            }
                        }
                        else
                        {
                            if (pm != null && !string.IsNullOrEmpty(pm.UserId) && pm.UserId != User.Identity.GetUserId())
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
                                if (adminId != User.Identity.GetUserId())
                                {
                                    await CreateNotification(adminId, "Variation Requested",
                                        $"Variation {variation.VariationNumber} requested on '{project.ProjectName}'.",
                                        "Project", $"/Quality/VariationDetails/{variation.Id}");
                                }
                            }
                        }
                    }

                    TempData["SuccessMessage"] = model.IsDraft ? $"Variation {variation.VariationNumber} saved as draft." : $"Variation {variation.VariationNumber} submitted successfully.";
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Project Manager")]
        public async Task<ActionResult> ValidateVariation(int id, string validationNotes)
        {
            var variation = db.ProjectVariations
                .Include(v => v.Project)
                .Include(v => v.Project.Client)
                .FirstOrDefault(v => v.Id == id);

            if (variation == null)
            {
                TempData["ErrorMessage"] = "Variation not found.";
                return RedirectToAction("VariationQueue");
            }

            if (!UserCanViewVariation(variation))
            {
                TempData["ErrorMessage"] = "You do not have permission to validate this variation.";
                return RedirectToAction("VariationQueue");
            }

            if (variation.Status != "PendingValidation")
            {
                TempData["ErrorMessage"] = "This variation is not awaiting validation.";
                return RedirectToAction("VariationDetails", new { id });
            }

            variation.Status = "Requested";
            string stamp = $"[{DateTime.Now:yyyy-MM-dd HH:mm} by {User.Identity.Name}] Validated & Forwarded: {validationNotes}";
            variation.Reason = string.IsNullOrWhiteSpace(variation.Reason) ? stamp : variation.Reason + "\n" + stamp;

            await db.SaveChangesAsync();

            if (variation.Project?.Client != null && !string.IsNullOrEmpty(variation.Project.Client.UserId))
            {
                await NotifyStakeholder(
                    variation.Project.Client.UserId,
                    "Variation Validated",
                    $"Your variation {variation.VariationNumber} on '{variation.Project.ProjectName}' has passed validation and is now awaiting approval.",
                    "Project",
                    $"/Quality/VariationDetails/{variation.Id}"
                );
            }

            TempData["SuccessMessage"] = "Variation validated and forwarded for approval.";
            return RedirectToAction("VariationDetails", new { id });
        }

        // ------------------------------------------------------------------
        // Variation Queue / Details
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Project Manager, Client, Finance Officer, Site Manager")]
        public ActionResult VariationQueue(string status = null, int? projectId = null, string type = null)
        {
            var staff = ResolveCurrentStaff();
            var userId = User.Identity.GetUserId();
            var client = db.Clients.FirstOrDefault(c => c.UserId == userId);

            IQueryable<ProjectVariation> query = db.ProjectVariations
                .Include(v => v.Project);

            if (User.IsInRole("Admin") || User.IsInRole("Finance Officer"))
            {
                // all
            }
            else if (User.IsInRole("Project Manager") && staff != null)
            {
                var pmIds = db.Projects.Where(p => p.ProjectManagerId == staff.Id).Select(p => p.Id).ToList();
                query = query.Where(v => pmIds.Contains(v.ProjectId));
            }
            else if (User.IsInRole("Site Manager") && staff != null)
            {
                var smIds = db.Projects.Where(p => p.SiteManagerId == staff.Id).Select(p => p.Id).ToList();
                query = query.Where(v => smIds.Contains(v.ProjectId));
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
                    ImpactOnTimelineDays = v.ImpactOnTimelineDays,
                    IsPendingValidation = v.Status == "PendingValidation",
                    IsPendingFinanceApproval = v.Status == "PendingFinanceApproval",
                    IsConditionalApproval = v.Status == "ConditionalApproval"
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
                    new SelectListItem { Value = "Draft", Text = "Draft" },
                    new SelectListItem { Value = "PendingValidation", Text = "Pending Validation" },
                    new SelectListItem { Value = "Requested", Text = "Requested" },
                    new SelectListItem { Value = "Under Review", Text = "Under Review" },
                    new SelectListItem { Value = "PendingFinanceApproval", Text = "Pending Finance Approval" },
                    new SelectListItem { Value = "ConditionalApproval", Text = "Conditional Approval" },
                    new SelectListItem { Value = "Approved", Text = "Approved" },
                    new SelectListItem { Value = "Rejected", Text = "Rejected" },
                    new SelectListItem { Value = "Implemented", Text = "Implemented" }
                },
                VariationTypes = GetVariationTypeSelectList(type)
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin, Project Manager, Client, Finance Officer, SiteManager")]
        public ActionResult VariationDetails(int id)
        {
            var variation = db.ProjectVariations
                .Include(v => v.Project)
                .Include(v => v.Project.Client)
                .Include(v => v.Project.ProjectManager)
                .Include(v => v.Approvals)
                .Include(v => v.BaselineSnapshots)
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

            var snapshotsList = (variation.BaselineSnapshots ?? Enumerable.Empty<ProjectBaselineSnapshot>())
                .OrderByDescending(s => s.SnapshottedDate)
                .Select(s => new BaselineSnapshotViewModel
                {
                    Id = s.Id,
                    PreviousBudgetEstimated = s.PreviousBudgetEstimated,
                    PreviousBudgetAllocated = s.PreviousBudgetAllocated,
                    PreviousBudgetRemaining = s.PreviousBudgetRemaining,
                    PreviousEstimatedEndDate = s.PreviousEstimatedEndDate,
                    NewBudgetEstimated = s.NewBudgetEstimated,
                    NewBudgetAllocated = s.NewBudgetAllocated,
                    NewBudgetRemaining = s.NewBudgetRemaining,
                    NewEstimatedEndDate = s.NewEstimatedEndDate,
                    SnapshottedBy = s.SnapshottedBy,
                    SnapshottedDate = s.SnapshottedDate
                }).ToList();

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
                ImpactOnQuality = variation.ImpactOnQuality,
                ImpactOnResources = variation.ImpactOnResources,
                ImpactOnCost = variation.ImpactOnCost,
                ImpactOnTimelineDays = variation.ImpactOnTimelineDays,
                ApprovedCostCap = variation.ApprovedCostCap,
                IsConditionalApproval = variation.Status == "ConditionalApproval",
                FinanceOfficerApproved = variation.FinanceOfficerApproved ?? false,
                FinanceOfficerApprovedBy = variation.FinanceOfficerApprovedBy,
                FinanceOfficerApprovalDate = variation.FinanceOfficerApprovalDate,
                VariationOrderPdfPath = variation.VariationOrderPdfPath,
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
                        ApprovalDate = a.ApprovalDate ?? DateTime.MinValue,
                        ApprovalLevel = a.ApprovalLevel
                    }).ToList(),
                BaselineSnapshots = snapshotsList,
                CanApprove = (User.IsInRole("Admin") || User.IsInRole("Project Manager"))
                    && (variation.Status == "Requested" || variation.Status == "Under Review"),
                CanValidate = (User.IsInRole("Admin") || User.IsInRole("Project Manager")) && variation.Status == "PendingValidation",
                CanFinanceApprove = User.IsInRole("Finance Officer") && variation.Status == "PendingFinanceApproval",
                CanDownloadVariationOrder = !string.IsNullOrEmpty(variation.VariationOrderPdfPath)
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

            if (!User.IsInRole("Admin"))
            {
                var staff = ResolveCurrentStaff();
                if (staff == null || variation.Project?.ProjectManagerId != staff.Id)
                {
                    TempData["ErrorMessage"] = "You may only approve variations on projects you manage.";
                    return RedirectToAction("VariationDetails", new { id });
                }
            }

            string cpWarning = null;
            if (variation.ImpactOnTimelineDays.HasValue && variation.ImpactOnTimelineDays.Value > 0 && variation.Project?.EstimatedEndDate.HasValue == true)
            {
                var currentEnd = variation.Project.EstimatedEndDate.Value;
                var newEnd = currentEnd.AddDays(variation.ImpactOnTimelineDays.Value);
                cpWarning = $"Approving this variation will extend the project end date by {variation.ImpactOnTimelineDays.Value} day(s), from {currentEnd:dd MMM yyyy} to {newEnd:dd MMM yyyy}.";
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
                ImplementationDate = DateTime.Today,
                CriticalPathWarning = cpWarning
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

            if (model.Decision == "Reject" && string.IsNullOrWhiteSpace(model.Comments))
            {
                ModelState.AddModelError("Comments", "A rejection reason is required.");
            }

            if (model.Decision == "Approve" && model.ImpactOnTimelineDays.HasValue && model.ImpactOnTimelineDays.Value > 0)
            {
                if (string.IsNullOrWhiteSpace(model.Comments) && !model.ConfirmedCriticalPath)
                {
                    ModelState.AddModelError("ConfirmedCriticalPath", "Please confirm you have reviewed the schedule impact.");
                }
            }

            if (model.Decision == "Approve" && model.IsConditionalApproval)
            {
                if (!model.ApprovedCostCap.HasValue || model.ApprovedCostCap.Value <= 0)
                {
                    ModelState.AddModelError("ApprovedCostCap", "An approved cost cap greater than zero is required for conditional approval.");
                }
                else if (model.ImpactOnCost.HasValue && model.ApprovedCostCap.Value > model.ImpactOnCost.Value)
                {
                    ModelState.AddModelError("ApprovedCostCap", "The approved cost cap cannot exceed the requested cost impact.");
                }
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
                        decimal effectiveCost = model.IsConditionalApproval ? model.ApprovedCostCap.Value : (variation.ImpactOnCost ?? 0m);

                        // Check Finance Officer Threshold
                        decimal threshold = 100000m;
                        if (decimal.TryParse(ConfigurationManager.AppSettings["VariationFinanceApprovalThreshold"], out decimal customThreshold))
                        {
                            threshold = customThreshold;
                        }

                        bool requiresFinanceApproval = effectiveCost > threshold && !User.IsInRole("Finance Officer");

                        if (requiresFinanceApproval)
                        {
                            variation.Status = "PendingFinanceApproval";
                            variation.ApprovedBy = User.Identity.Name;
                            variation.ApprovalDate = DateTime.Now;
                            if (model.IsConditionalApproval)
                            {
                                variation.ApprovedCostCap = model.ApprovedCostCap;
                            }
                            variation.ImplementationDate = model.ImplementationDate ?? DateTime.Now;
                            variation.ImplementationNotes = model.Comments;

                            var approval = new VariationApproval
                            {
                                VariationId = variation.Id,
                                ApproverName = User.Identity.Name,
                                ApproverEmail = User.Identity.Name,
                                ApprovalStatus = "Pending",
                                Comments = model.Comments,
                                ApprovalDate = DateTime.Now,
                                ApprovalLevel = 2
                            };
                            db.VariationApprovals.Add(approval);
                            await db.SaveChangesAsync();

                            var financeOfficers = db.Users.ToList()
                                .Where(u => UserManager.IsInRole(u.Id, "Finance Officer"))
                                .Select(u => u.Id)
                                .ToList();
                            foreach (var foId in financeOfficers)
                            {
                                await CreateNotification(foId, "Variation awaiting your approval",
                                    $"{variation.VariationNumber} on '{variation.Project?.ProjectName}' exceeds R{threshold:N0} and requires finance approval.",
                                    "Project", $"/Quality/VariationDetails/{variation.Id}");
                            }

                            TempData["SuccessMessage"] = "Variation approved by PM and forwarded to Finance Officer due to high cost impact.";
                            return RedirectToAction("VariationDetails", new { id = variation.Id });
                        }
                        else
                        {
                            variation.Status = model.IsConditionalApproval ? "ConditionalApproval" : "Approved";
                            variation.ApprovedBy = User.Identity.Name;
                            variation.ApprovalDate = DateTime.Now;
                            if (model.IsConditionalApproval)
                            {
                                variation.ApprovedCostCap = model.ApprovedCostCap;
                            }
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

                            if (variation.Project != null)
                            {
                                RecordBaselineSnapshot(variation);

                                if (variation.Project.Budget != null)
                                {
                                    var delta = effectiveCost;
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

                            await db.SaveChangesAsync();

                            // Generate Variation Order PDF
                            try
                            {
                                var pdfBytes = HandoverPdfGenerator.GenerateVariationOrder(variation, variation.Project, variation.Project?.Client);
                                var folder = Server.MapPath("~/Uploads/VariationOrders");
                                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                                var fileName = $"VariationOrder_{variation.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                                var physicalPath = Path.Combine(folder, fileName);
                                System.IO.File.WriteAllBytes(physicalPath, pdfBytes);
                                variation.VariationOrderPdfPath = $"/Uploads/VariationOrders/{fileName}";
                                await db.SaveChangesAsync();
                            }
                            catch (Exception pdfEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Variation Order PDF generation failed: {pdfEx.Message}");
                            }
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
                        await db.SaveChangesAsync();
                    }

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
                        if (adminId != User.Identity.GetUserId())
                        {
                            await CreateNotification(adminId,
                                model.Decision == "Approve" ? "Variation Approved" : "Variation Rejected",
                                $"Variation {variation.VariationNumber} was {model.Decision.ToLower()}d by {User.Identity.Name}.",
                                "Project", $"/Quality/VariationDetails/{variation.Id}");
                        }
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Finance Officer")]
        public async Task<ActionResult> FinanceApproveVariation(int id, string financeNotes)
        {
            var variation = db.ProjectVariations
                .Include(v => v.Project)
                .Include(v => v.Project.Budget)
                .Include(v => v.Project.Client)
                .FirstOrDefault(v => v.Id == id);

            if (variation == null || variation.Status != "PendingFinanceApproval")
            {
                TempData["ErrorMessage"] = "Variation not found or not awaiting finance approval.";
                return RedirectToAction("VariationQueue");
            }

            decimal effectiveCost = variation.ApprovedCostCap ?? variation.ImpactOnCost ?? 0m;

            variation.Status = "Approved";
            variation.FinanceOfficerApproved = true;
            variation.FinanceOfficerApprovedBy = User.Identity.Name;
            variation.FinanceOfficerApprovalDate = DateTime.Now;

            var approval = new VariationApproval
            {
                VariationId = variation.Id,
                ApproverName = User.Identity.Name,
                ApproverEmail = User.Identity.Name,
                ApprovalStatus = "Approved",
                Comments = financeNotes,
                ApprovalDate = DateTime.Now,
                ApprovalLevel = 2
            };
            db.VariationApprovals.Add(approval);

            if (variation.Project != null)
            {
                RecordBaselineSnapshot(variation);

                if (variation.Project.Budget != null)
                {
                    variation.Project.Budget.EstimatedCost += effectiveCost;
                    variation.Project.Budget.AllocatedAmount += effectiveCost;
                    variation.Project.Budget.RemainingAmount += effectiveCost;
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

            await db.SaveChangesAsync();

            // Generate Variation Order PDF
            try
            {
                var pdfBytes = HandoverPdfGenerator.GenerateVariationOrder(variation, variation.Project, variation.Project?.Client);
                var folder = Server.MapPath("~/Uploads/VariationOrders");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                var fileName = $"VariationOrder_{variation.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                var physicalPath = Path.Combine(folder, fileName);
                System.IO.File.WriteAllBytes(physicalPath, pdfBytes);
                variation.VariationOrderPdfPath = $"/Uploads/VariationOrders/{fileName}";
                await db.SaveChangesAsync();
            }
            catch (Exception pdfEx)
            {
                System.Diagnostics.Debug.WriteLine($"Variation Order PDF generation failed: {pdfEx.Message}");
            }

            // Notify PM, Client, Admins
            if (variation.Project != null)
            {
                var pm = db.Staff.Find(variation.Project.ProjectManagerId);
                if (pm != null && !string.IsNullOrEmpty(pm.UserId))
                {
                    await NotifyStakeholder(pm.UserId, "Finance Approved Variation", $"Variation {variation.VariationNumber} approved by Finance.", "Project", $"/Quality/VariationDetails/{variation.Id}");
                }
                if (variation.Project.Client != null && !string.IsNullOrEmpty(variation.Project.Client.UserId))
                {
                    await NotifyStakeholder(variation.Project.Client.UserId, "Variation Approved", $"Variation {variation.VariationNumber} has been approved.", "Project", $"/Quality/VariationDetails/{variation.Id}");
                }
            }

            TempData["SuccessMessage"] = "Variation approved by Finance and baseline updated.";
            return RedirectToAction("VariationDetails", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Finance Officer")]
        public async Task<ActionResult> FinanceRejectVariation(int id, string financeNotes)
        {
            var variation = db.ProjectVariations
                .Include(v => v.Project)
                .Include(v => v.Project.Client)
                .FirstOrDefault(v => v.Id == id);

            if (variation == null || variation.Status != "PendingFinanceApproval")
            {
                TempData["ErrorMessage"] = "Variation not found or not awaiting finance approval.";
                return RedirectToAction("VariationQueue");
            }

            variation.Status = "Rejected";
            variation.RejectionReason = financeNotes;
            variation.FinanceOfficerApproved = false;
            variation.FinanceOfficerApprovedBy = User.Identity.Name;
            variation.FinanceOfficerApprovalDate = DateTime.Now;

            var approval = new VariationApproval
            {
                VariationId = variation.Id,
                ApproverName = User.Identity.Name,
                ApproverEmail = User.Identity.Name,
                ApprovalStatus = "Rejected",
                Comments = financeNotes,
                ApprovalDate = DateTime.Now,
                ApprovalLevel = 2
            };
            db.VariationApprovals.Add(approval);
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Variation rejected by Finance.";
            return RedirectToAction("VariationDetails", new { id });
        }

        [Authorize(Roles = "Admin, Project Manager, Site Manager, Client, Finance Officer")]
        public ActionResult DownloadVariationOrder(int id)
        {
            var variation = db.ProjectVariations
                .Include(v => v.Project)
                .FirstOrDefault(v => v.Id == id);

            if (variation == null || !UserCanViewVariation(variation))
            {
                TempData["ErrorMessage"] = "Variation not found or access denied.";
                return RedirectToAction("VariationQueue");
            }

            if (string.IsNullOrEmpty(variation.VariationOrderPdfPath))
            {
                TempData["ErrorMessage"] = "Variation order PDF is not available.";
                return RedirectToAction("VariationDetails", new { id });
            }

            var physicalPath = Server.MapPath("~" + variation.VariationOrderPdfPath);
            if (!System.IO.File.Exists(physicalPath))
            {
                TempData["ErrorMessage"] = "PDF file not found on server.";
                return RedirectToAction("VariationDetails", new { id });
            }

            var bytes = System.IO.File.ReadAllBytes(physicalPath);
            return File(bytes, "application/pdf", $"VariationOrder_{variation.VariationNumber}.pdf");
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private string GenerateVariationNumberForYear(int year)
        {
            string prefix = $"VAR-{year}-";
            var lastVar = db.ProjectVariations
                .Where(v => v.VariationNumber != null && v.VariationNumber.StartsWith(prefix))
                .OrderByDescending(v => v.Id)
                .FirstOrDefault();

            int nextNum = 1;
            if (lastVar != null && lastVar.VariationNumber.Length >= prefix.Length + 4)
            {
                string suffix = lastVar.VariationNumber.Substring(prefix.Length);
                if (int.TryParse(suffix, out int parsedNum))
                {
                    nextNum = parsedNum + 1;
                }
            }
            return $"{prefix}{nextNum:D4}";
        }

        private void RecordBaselineSnapshot(ProjectVariation variation)
        {
            if (variation?.Project?.Budget == null) return;
            var budget = variation.Project.Budget;

            decimal effectiveCost = variation.ApprovedCostCap ?? variation.ImpactOnCost ?? 0m;
            int timelineDays = variation.ImpactOnTimelineDays ?? 0;

            decimal prevEst = budget.EstimatedCost;
            decimal prevAlloc = budget.AllocatedAmount;
            decimal prevRem = budget.RemainingAmount;
            DateTime? prevEnd = variation.Project.EstimatedEndDate;

            decimal newEst = prevEst + effectiveCost;
            decimal newAlloc = prevAlloc + effectiveCost;
            decimal newRem = prevRem + effectiveCost;
            DateTime? newEnd = prevEnd.HasValue && timelineDays > 0 ? prevEnd.Value.AddDays(timelineDays) : prevEnd;

            var snapshot = new ProjectBaselineSnapshot
            {
                ProjectId = variation.ProjectId,
                VariationId = variation.Id,
                PreviousBudgetEstimated = prevEst,
                PreviousBudgetAllocated = prevAlloc,
                PreviousBudgetRemaining = prevRem,
                PreviousEstimatedEndDate = prevEnd,
                NewBudgetEstimated = newEst,
                NewBudgetAllocated = newAlloc,
                NewBudgetRemaining = newRem,
                NewEstimatedEndDate = newEnd,
                SnapshottedBy = User.Identity.Name,
                SnapshottedDate = DateTime.Now
            };

            db.ProjectBaselineSnapshots.Add(snapshot);
        }

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
            if (User.IsInRole("Admin") || User.IsInRole("Finance Officer")) return true;
            var staff = ResolveCurrentStaff();
            var userId = User.Identity.GetUserId();
            var client = db.Clients.FirstOrDefault(c => c.UserId == userId);

            if (User.IsInRole("Project Manager") && staff != null)
                return variation.Project != null && variation.Project.ProjectManagerId == staff.Id;
            if (User.IsInRole("Site Manager") && staff != null)
                return variation.Project != null && variation.Project.SiteManagerId == staff.Id;
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

            if (User.IsInRole("Admin") || User.IsInRole("Finance Officer"))
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
                case "Draft": return "secondary";
                case "PendingValidation": return "warning";
                case "Requested": return "warning";
                case "Under Review": return "info";
                case "PendingFinanceApproval": return "primary";
                case "ConditionalApproval": return "info";
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
            model.TemplateItems = db.InspectionTemplateItems
                .Where(t => t.IsActive)
                .OrderBy(t => t.InspectionType)
                .ThenBy(t => t.Order)
                .ToList()
                .Select(t => new InspectionTemplateItemViewModel
                {
                    Id = t.Id,
                    InspectionType = t.InspectionType,
                    ItemLabel = t.ItemLabel,
                    Category = t.Category,
                    Order = t.Order
                }).ToList();
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