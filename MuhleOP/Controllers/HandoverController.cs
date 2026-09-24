using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MuhleOP.Helpers;
using MuhleOP.Models;
using MuhleOP.ViewModels;

namespace MuhleOP.Controllers
{
    [Authorize]
    public class HandoverController : Controller
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
        // Index — Dashboard
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Project Manager, Site Manager")]
        public ActionResult Index()
        {
            var handovers = db.ProjectHandovers
                .Include(h => h.Project)
                .Include(h => h.Project.Client)
                .Include(h => h.Documents)
                .OrderByDescending(h => h.CreatedDate)
                .ToList();

            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var viewModel = new HandoverIndexViewModel
            {
                Items = handovers.Select(h => new HandoverListItemViewModel
                {
                    Id = h.Id,
                    ProjectId = h.ProjectId,
                    ProjectName = h.Project != null ? h.Project.ProjectName : "Unknown",
                    ProjectCode = h.Project != null ? h.Project.ProjectCode : null,
                    ClientName = h.Project?.Client != null ? h.Project.Client.CompanyName : null,
                    HandoverType = h.HandoverType,
                    Status = h.Status,
                    StatusBadgeColor = GetStatusBadgeColor(h.Status),
                    ClientSignOffStatus = h.ClientSignOffStatus,
                    ClientSignOffBadgeColor = GetClientSignOffBadgeColor(h.ClientSignOffStatus),
                    HandoverDate = h.HandoverDate,
                    DocumentCount = h.Documents != null ? h.Documents.Count : 0,
                    CreatedBy = h.CreatedBy
                }).ToList(),
                PendingCount = handovers.Count(h => h.Status == "Initiated" || h.Status == "In Progress"),
                AwaitingClientSignOffCount = handovers.Count(h => h.ClientSignOffStatus == "Pending" && h.Status != "Closed"),
                CompletedThisMonthCount = handovers.Count(h =>
                    (h.Status == "Completed" || h.Status == "Closed") &&
                    h.ClientSignOffDate.HasValue &&
                    h.ClientSignOffDate.Value >= startOfMonth),
                TotalDocumentsCount = handovers.Sum(h => h.Documents != null ? h.Documents.Count : 0)
            };

            return View(viewModel);
        }

        // ------------------------------------------------------------------
        // Initiate Handover
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Project Manager, Site Manager")]
        public ActionResult InitiateHandover()
        {
            var activeHandoverProjectIds = db.ProjectHandovers
                .Where(h => h.Status != "Closed")
                .Select(h => h.ProjectId)
                .Distinct()
                .ToList();

            var eligibleProjects = db.Projects
                .Where(p => p.IsActive &&
                            (p.Status == "Completed" || p.Status == "In Progress" || p.Status == "Initiated") &&
                            !activeHandoverProjectIds.Contains(p.Id))
                .OrderBy(p => p.ProjectName)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.ProjectName + " (" + p.ProjectCode + ")"
                })
                .ToList();

            var model = new InitiateHandoverViewModel
            {
                HandoverType = "Final",
                WarrantyPeriodDays = 365,
                WarrantyStartDate = DateTime.Today,
                Projects = eligibleProjects,
                HandoverTypes = GetHandoverTypeSelectList("Final")
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Project Manager, Site Manager")]
        public async Task<ActionResult> InitiateHandover(InitiateHandoverViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var project = db.Projects
                        .Include(p => p.Client)
                        .FirstOrDefault(p => p.Id == model.ProjectId);

                    if (project == null)
                    {
                        ModelState.AddModelError("ProjectId", "Selected project was not found.");
                        return View(RepopulateInitiateDropdowns(model));
                    }

                    var hasActive = db.ProjectHandovers.Any(h =>
                        h.ProjectId == model.ProjectId && h.Status != "Closed");

                    if (hasActive)
                    {
                        ModelState.AddModelError("ProjectId", "This project already has an active handover.");
                        return View(RepopulateInitiateDropdowns(model));
                    }

                    var handover = new ProjectHandover
                    {
                        ProjectId = model.ProjectId,
                        HandoverDate = DateTime.Now,
                        HandoverType = model.HandoverType ?? "Final",
                        FinalInspectionDate = model.FinalInspectionDate,
                        FinalInspectionFindings = model.FinalInspectionFindings,
                        ClientSignOffStatus = "Pending",
                        ProjectManagerSignOff = false,
                        Status = "Initiated",
                        CompletionNotes = model.CompletionNotes,
                        LessonsLearned = model.LessonsLearned,
                        Recommendations = model.Recommendations,
                        WarrantyPeriodDays = model.WarrantyPeriodDays,
                        WarrantyStartDate = model.WarrantyStartDate,
                        CreatedBy = User.Identity.Name,
                        CreatedDate = DateTime.Now
                    };

                    if (model.WarrantyPeriodDays.HasValue && model.WarrantyPeriodDays.Value > 0 && model.WarrantyStartDate.HasValue)
                    {
                        handover.WarrantyEndDate = model.WarrantyStartDate.Value.AddDays(model.WarrantyPeriodDays.Value);
                    }

                    db.ProjectHandovers.Add(handover);
                    await db.SaveChangesAsync();

                    // Optional certificate upload
                    if (model.CertificateFile != null && model.CertificateFile.ContentLength > 0)
                    {
                        try
                        {
                            var folder = Server.MapPath($"~/Uploads/Handovers/{handover.Id}");
                            if (!Directory.Exists(folder))
                                Directory.CreateDirectory(folder);

                            var ext = Path.GetExtension(model.CertificateFile.FileName);
                            var fileName = "Certificate_" + Guid.NewGuid().ToString("N") + ext;
                            var physicalPath = Path.Combine(folder, fileName);
                            model.CertificateFile.SaveAs(physicalPath);

                            handover.CertificatePath = $"/Uploads/Handovers/{handover.Id}/{fileName}";
                            await db.SaveChangesAsync();
                        }
                        catch (Exception certEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Certificate upload failed: {certEx.Message}");
                        }
                    }

                    // Notify client if linked
                    if (project.Client != null && !string.IsNullOrEmpty(project.Client.UserId))
                    {
                        await NotifyStakeholder(
                            project.Client.UserId,
                            "Project Handover Initiated",
                            $"A handover has been initiated for project '{project.ProjectName}'. You will be invited to sign off shortly.",
                            "Project",
                            $"/Handover/ClientSignOff/{handover.Id}"
                        );
                    }

                    TempData["SuccessMessage"] = $"Handover for '{project.ProjectName}' initiated successfully.";
                    return RedirectToAction("HandoverDetails", new { id = handover.Id });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error initiating handover: {ex.Message}");
                    ModelState.AddModelError("", "Error initiating handover: " + ex.Message);
                }
            }

            return View(RepopulateInitiateDropdowns(model));
        }

        // ------------------------------------------------------------------
        // Handover Details
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Project Manager, Site Manager, Client")]
        public ActionResult HandoverDetails(int id)
        {
            var handover = db.ProjectHandovers
                .Include(h => h.Project)
                .Include(h => h.Project.Client)
                .Include(h => h.Project.ProjectManager)
                .Include(h => h.Documents)
                .FirstOrDefault(h => h.Id == id);

            if (handover == null)
            {
                TempData["ErrorMessage"] = "Handover not found.";
                return RedirectToAction("Index");
            }

            // Client ownership check
            if (User.IsInRole("Client") && !User.IsInRole("Admin"))
            {
                var client = ResolveCurrentClient();
                if (client == null || handover.Project == null || handover.Project.ClientId != client.Id)
                {
                    TempData["ErrorMessage"] = "You do not have access to this handover.";
                    return RedirectToAction("Dashboard", "Home");
                }
            }

            var viewModel = new HandoverDetailsViewModel
            {
                Id = handover.Id,
                ProjectId = handover.ProjectId,
                ProjectName = handover.Project != null ? handover.Project.ProjectName : "Unknown",
                ProjectCode = handover.Project != null ? handover.Project.ProjectCode : null,
                ProjectStatus = handover.Project != null ? handover.Project.Status : null,
                ClientName = handover.Project?.Client != null ? handover.Project.Client.CompanyName : null,
                ProjectManagerName = handover.Project?.ProjectManager != null ? handover.Project.ProjectManager.FullName : null,
                HandoverDate = handover.HandoverDate,
                HandoverType = handover.HandoverType,
                FinalInspectionDate = handover.FinalInspectionDate,
                FinalInspectionFindings = handover.FinalInspectionFindings,
                ClientSignOffStatus = handover.ClientSignOffStatus,
                ClientSignOffDate = handover.ClientSignOffDate,
                ClientSignOffNotes = handover.ClientSignOffNotes,
                ClientInvitedToSignOff = handover.ClientInvitedToSignOff,
                ClientInvitedDate = handover.ClientInvitedDate,
                ProjectManagerSignOff = handover.ProjectManagerSignOff,
                ProjectManagerSignOffDate = handover.ProjectManagerSignOffDate,
                Status = handover.Status,
                StatusBadgeColor = GetStatusBadgeColor(handover.Status),
                ClientSignOffBadgeColor = GetClientSignOffBadgeColor(handover.ClientSignOffStatus),
                CompletionNotes = handover.CompletionNotes,
                LessonsLearned = handover.LessonsLearned,
                Recommendations = handover.Recommendations,
                ClientFeedback = handover.ClientFeedback,
                CertificatePath = handover.CertificatePath,
                SignedHandoverPdfPath = handover.SignedHandoverPdfPath,
                WarrantyPeriodDays = handover.WarrantyPeriodDays,
                WarrantyStartDate = handover.WarrantyStartDate,
                WarrantyEndDate = handover.WarrantyEndDate,
                CreatedBy = handover.CreatedBy,
                CreatedDate = handover.CreatedDate,
                Documents = (handover.Documents ?? Enumerable.Empty<HandoverDocument>())
                    .OrderByDescending(d => d.UploadDate)
                    .Select(d => new HandoverDocumentListItemViewModel
                    {
                        Id = d.Id,
                        DocumentName = d.DocumentName,
                        DocumentType = d.DocumentType,
                        Description = d.Description,
                        FilePath = d.FilePath,
                        FileSizeBytes = d.FileSizeBytes,
                        UploadDate = d.UploadDate,
                        UploadedBy = d.UploadedBy,
                        IsApproved = d.IsApproved
                    }).ToList()
            };

            return View(viewModel);
        }

        // ------------------------------------------------------------------
        // Invite Client to Sign Off
        // ------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Project Manager, Site Manager")]
        public async Task<ActionResult> InviteClientToSignOff(int id)
        {
            try
            {
                var handover = db.ProjectHandovers
                    .Include(h => h.Project)
                    .Include(h => h.Project.Client)
                    .FirstOrDefault(h => h.Id == id);

                if (handover == null)
                {
                    TempData["ErrorMessage"] = "Handover not found.";
                    return RedirectToAction("Index");
                }

                if (handover.ClientSignOffStatus == "Signed")
                {
                    TempData["ErrorMessage"] = "Client has already signed off.";
                    return RedirectToAction("HandoverDetails", new { id });
                }

                handover.ClientInvitedToSignOff = true;
                handover.ClientInvitedDate = DateTime.Now;
                handover.Status = "In Progress";
                handover.LastModifiedDate = DateTime.Now;
                handover.CompletionNotes = (handover.CompletionNotes ?? "") +
                    $" [{DateTime.Now:yyyy-MM-dd HH:mm} by {User.Identity.Name}] Client invited to sign off.";

                await db.SaveChangesAsync();

                if (handover.Project?.Client != null && !string.IsNullOrEmpty(handover.Project.Client.UserId))
                {
                    await NotifyStakeholder(
                        handover.Project.Client.UserId,
                        "Action Required: Project Sign-off",
                        $"Please review and sign off the handover for project '{handover.Project.ProjectName}'.",
                        "Project",
                        $"/Handover/ClientSignOff/{handover.Id}"
                    );
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"InviteClientToSignOff: No Client UserId for handover {id}");
                }

                if (handover.Project?.Client != null && !string.IsNullOrEmpty(handover.Project.Client.ContactEmail))
                {
                    SendClientInvitedToSignOffEmail(handover.Project.Client, handover.Project);
                }

                TempData["SuccessMessage"] = "Client has been invited to sign off.";
                return RedirectToAction("HandoverDetails", new { id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error inviting client: {ex.Message}");
                TempData["ErrorMessage"] = "Error inviting client to sign off.";
                return RedirectToAction("HandoverDetails", new { id });
            }
        }

        // ------------------------------------------------------------------
        // Upload Document
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Project Manager, Site Manager")]
        public ActionResult UploadDocument(int id)
        {
            var handover = db.ProjectHandovers
                .Include(h => h.Project)
                .FirstOrDefault(h => h.Id == id);

            if (handover == null)
            {
                TempData["ErrorMessage"] = "Handover not found.";
                return RedirectToAction("Index");
            }

            if (handover.Status == "Closed")
            {
                TempData["ErrorMessage"] = "Cannot upload documents to a closed handover.";
                return RedirectToAction("HandoverDetails", new { id });
            }

            var model = new UploadHandoverDocumentViewModel
            {
                HandoverId = handover.Id,
                ProjectName = handover.Project != null ? handover.Project.ProjectName : "Unknown",
                DocumentTypes = GetDocumentTypeSelectList(null)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Project Manager, Site Manager")]
        public async Task<ActionResult> UploadDocument(UploadHandoverDocumentViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var handover = db.ProjectHandovers
                        .Include(h => h.Documents)
                        .FirstOrDefault(h => h.Id == model.HandoverId);

                    if (handover == null)
                    {
                        TempData["ErrorMessage"] = "Handover not found.";
                        return RedirectToAction("Index");
                    }

                    if (handover.Status == "Closed")
                    {
                        TempData["ErrorMessage"] = "Cannot upload documents to a closed handover.";
                        return RedirectToAction("HandoverDetails", new { id = model.HandoverId });
                    }

                    if (model.File == null || model.File.ContentLength == 0)
                    {
                        ModelState.AddModelError("File", "A file is required.");
                        model.DocumentTypes = GetDocumentTypeSelectList(model.DocumentType);
                        return View(model);
                    }

                    var folder = Server.MapPath($"~/Uploads/Handovers/{handover.Id}");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    var ext = Path.GetExtension(model.File.FileName);
                    var safeName = Path.GetFileNameWithoutExtension(model.File.FileName);
                    if (string.IsNullOrWhiteSpace(safeName)) safeName = "document";
                    var fileName = safeName + "_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ext;
                    var physicalPath = Path.Combine(folder, fileName);
                    model.File.SaveAs(physicalPath);

                    var relativePath = $"/Uploads/Handovers/{handover.Id}/{fileName}";

                    var doc = new HandoverDocument
                    {
                        HandoverId = handover.Id,
                        DocumentName = model.DocumentName,
                        DocumentType = model.DocumentType,
                        Description = model.Description,
                        FilePath = relativePath,
                        FileSizeBytes = model.File.ContentLength,
                        UploadDate = DateTime.Now,
                        UploadedBy = User.Identity.Name,
                        IsApproved = false,
                        Notes = model.Notes
                    };

                    db.HandoverDocuments.Add(doc);

                    if (handover.Status == "Initiated")
                    {
                        handover.Status = "In Progress";
                        handover.LastModifiedDate = DateTime.Now;
                    }

                    await db.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Document '{model.DocumentName}' uploaded successfully.";
                    return RedirectToAction("HandoverDetails", new { id = handover.Id });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error uploading handover document: {ex.Message}");
                    ModelState.AddModelError("", "Error uploading document: " + ex.Message);
                }
            }

            model.DocumentTypes = GetDocumentTypeSelectList(model.DocumentType);
            return View(model);
        }

        // ------------------------------------------------------------------
        // Client Sign-Off
        // ------------------------------------------------------------------

        [Authorize(Roles = "Client")]
        public ActionResult ClientSignOff(int id)
        {
            var handover = db.ProjectHandovers
                .Include(h => h.Project)
                .Include(h => h.Project.Client)
                .Include(h => h.Documents)
                .FirstOrDefault(h => h.Id == id);

            if (handover == null)
            {
                TempData["ErrorMessage"] = "Handover not found.";
                return RedirectToAction("Dashboard", "Home");
            }

            var client = ResolveCurrentClient();
            if (client == null || handover.Project == null || handover.Project.ClientId != client.Id)
            {
                TempData["ErrorMessage"] = "You do not have access to this handover.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (!handover.ClientInvitedToSignOff)
            {
                TempData["ErrorMessage"] = "The project manager has not yet invited you to sign off.";
                return RedirectToAction("HandoverDetails", new { id });
            }

            if (handover.ClientSignOffStatus == "Signed")
            {
                TempData["SuccessMessage"] = "You have already signed off this handover.";
                return RedirectToAction("HandoverDetails", new { id });
            }

            var model = new ClientSignOffViewModel
            {
                HandoverId = handover.Id,
                ProjectName = handover.Project.ProjectName,
                ProjectCode = handover.Project.ProjectCode,
                HandoverType = handover.HandoverType,
                CompletionNotes = handover.CompletionNotes,
                FinalInspectionFindings = handover.FinalInspectionFindings,
                LessonsLearned = handover.LessonsLearned,
                Recommendations = handover.Recommendations,
                WarrantyPeriodDays = handover.WarrantyPeriodDays,
                WarrantyStartDate = handover.WarrantyStartDate,
                WarrantyEndDate = handover.WarrantyEndDate,
                ClientInvitedDate = handover.ClientInvitedDate,
                Documents = (handover.Documents ?? Enumerable.Empty<HandoverDocument>())
                    .OrderByDescending(d => d.UploadDate)
                    .Select(d => new HandoverDocumentListItemViewModel
                    {
                        Id = d.Id,
                        DocumentName = d.DocumentName,
                        DocumentType = d.DocumentType,
                        Description = d.Description,
                        FilePath = d.FilePath,
                        FileSizeBytes = d.FileSizeBytes,
                        UploadDate = d.UploadDate,
                        UploadedBy = d.UploadedBy,
                        IsApproved = d.IsApproved
                    }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client")]
        public async Task<ActionResult> ClientSignOff(ClientSignOffViewModel model)
        {
            if (!model.AcceptTerms)
            {
                ModelState.AddModelError("AcceptTerms", "You must accept the project as completed to sign off.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var handover = db.ProjectHandovers
                        .Include(h => h.Project)
                        .Include(h => h.Project.Client)
                        .Include(h => h.Project.ProjectManager)
                        .Include(h => h.Project.SiteManager)
                        .Include(h => h.Documents)
                        .FirstOrDefault(h => h.Id == model.HandoverId);

                    if (handover == null)
                    {
                        TempData["ErrorMessage"] = "Handover not found.";
                        return RedirectToAction("Dashboard", "Home");
                    }

                    var client = ResolveCurrentClient();
                    if (client == null || handover.Project == null || handover.Project.ClientId != client.Id)
                    {
                        TempData["ErrorMessage"] = "You do not have access to this handover.";
                        return RedirectToAction("Dashboard", "Home");
                    }

                    if (handover.ClientSignOffStatus == "Signed")
                    {
                        TempData["SuccessMessage"] = "You have already signed off this handover.";
                        return RedirectToAction("HandoverDetails", new { id = model.HandoverId });
                    }

                    handover.ClientSignOffStatus = "Signed";
                    handover.ClientSignOffDate = DateTime.Now;
                    handover.ClientSignOffNotes = model.ClientSignOffNotes;
                    handover.ClientFeedback = model.ClientFeedback;
                    handover.LastModifiedDate = DateTime.Now;

                    await db.SaveChangesAsync();

                    var currentUserId = User.Identity.GetUserId();

                    // Generate the signed handover PDF (never abort sign-off on failure)
                    byte[] signedPdfBytes = null;
                    try
                    {
                        var signOffMeta = new HandoverSignOffMeta
                        {
                            SignerName = client.PrimaryContactName,
                            SignerEmail = client.ContactEmail,
                            SignedAt = DateTime.Now,
                            IpAddress = Request.UserHostAddress,
                            UserAgent = Request.UserAgent,
                            Feedback = handover.ClientFeedback,
                            SignOffNotes = handover.ClientSignOffNotes
                        };

                        signedPdfBytes = HandoverPdfGenerator.Generate(
                            handover, handover.Project, client, handover.Documents, signOffMeta);

                        var pdfFolder = Server.MapPath("~/Uploads/SignedHandovers/");
                        if (!Directory.Exists(pdfFolder))
                        {
                            Directory.CreateDirectory(pdfFolder);
                        }

                        var pdfFileName = "SignedHandover_" + handover.Id + "_"
                            + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";
                        var pdfFilePath = Path.Combine(pdfFolder, pdfFileName);
                        System.IO.File.WriteAllBytes(pdfFilePath, signedPdfBytes);
                        handover.SignedHandoverPdfPath = "/Uploads/SignedHandovers/" + pdfFileName;

                        await db.SaveChangesAsync();
                    }
                    catch (Exception pdfEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error generating signed handover PDF: {pdfEx.Message}");
                        signedPdfBytes = null;
                        handover.SignedHandoverPdfPath = null;
                    }

                    // In-app notifications
                    if (handover.Project?.ProjectManager != null &&
                        !string.IsNullOrEmpty(handover.Project.ProjectManager.UserId) &&
                        handover.Project.ProjectManager.UserId != currentUserId)
                    {
                        await NotifyStakeholder(
                            handover.Project.ProjectManager.UserId,
                            "Client Signed Off",
                            $"Client has signed off the handover for project '{handover.Project.ProjectName}'.",
                            "Project",
                            $"/Handover/HandoverDetails/{handover.Id}"
                        );
                    }

                    if (handover.Project?.SiteManager != null &&
                        !string.IsNullOrEmpty(handover.Project.SiteManager.UserId) &&
                        handover.Project.SiteManager.UserId != currentUserId)
                    {
                        await NotifyStakeholder(
                            handover.Project.SiteManager.UserId,
                            "Client Signed Off",
                            $"Client has signed off the handover for project '{handover.Project.ProjectName}'.",
                            "Project",
                            $"/Handover/HandoverDetails/{handover.Id}"
                        );
                    }

                    await NotifyAdmins(
                        "Client Signed Off",
                        $"Client has signed off the handover for project '{handover.Project?.ProjectName}'.",
                        $"/Handover/HandoverDetails/{handover.Id}",
                        skipUserId: currentUserId
                    );

                    await NotifyFinanceOfficers(
                        "Client Signed Off",
                        $"Client has signed off the handover for project '{handover.Project?.ProjectName}'.",
                        $"/Handover/HandoverDetails/{handover.Id}",
                        skipUserId: currentUserId
                    );

                    if (!string.IsNullOrEmpty(client.UserId) && client.UserId != currentUserId)
                    {
                        await NotifyStakeholder(
                            client.UserId,
                            "Sign-off Recorded",
                            $"Your sign-off for project '{handover.Project?.ProjectName}' has been recorded.",
                            "Project",
                            $"/Handover/HandoverDetails/{handover.Id}"
                        );
                    }

                    // Email PM + Client, PDF attached when available
                    var signOffRecipientEmails = new List<string>();
                    if (handover.Project?.ProjectManager?.UserId != null)
                    {
                        var pmUser = db.Users.FirstOrDefault(u => u.Id == handover.Project.ProjectManager.UserId);
                        if (pmUser != null && !string.IsNullOrEmpty(pmUser.Email))
                        {
                            signOffRecipientEmails.Add(pmUser.Email);
                        }
                    }
                    if (!string.IsNullOrEmpty(client.ContactEmail))
                    {
                        signOffRecipientEmails.Add(client.ContactEmail);
                    }

                    SendSignedHandoverEmail(
                        signOffRecipientEmails.Distinct().ToList(),
                        handover, client, signedPdfBytes);

                    // Try to auto-close
                    bool canAutoClose =
                        (handover.Documents != null && handover.Documents.Any())
                        && !string.IsNullOrEmpty(handover.SignedHandoverPdfPath);

                    if (canAutoClose)
                    {
                        handover.Status = "Closed";
                        handover.LastModifiedDate = DateTime.Now;

                        if (handover.Project != null)
                        {
                            handover.Project.Status = "Closed";
                            handover.Project.IsActive = false;
                            handover.Project.ActualEndDate = DateTime.Now;
                            handover.Project.LastModifiedDate = DateTime.Now;
                            handover.Project.LastModifiedBy = User.Identity.Name;
                        }

                        var allocations = db.ProjectResourceAllocations
                            .Where(a => a.ProjectId == handover.ProjectId && a.IsActive)
                            .ToList();
                        foreach (var a in allocations)
                        {
                            a.IsActive = false;
                            a.EndDate = DateTime.Now;
                        }

                        var assignments = db.EquipmentAssignments
                            .Where(a => a.ProjectId == handover.ProjectId && a.IsActive)
                            .ToList();
                        foreach (var a in assignments)
                        {
                            a.IsActive = false;
                            a.ActualReturnDate = DateTime.Now;
                        }

                        await db.SaveChangesAsync();

                        var projectName = handover.Project != null ? handover.Project.ProjectName : "Project";

                        if (handover.Project?.ProjectManager != null &&
                            !string.IsNullOrEmpty(handover.Project.ProjectManager.UserId) &&
                            handover.Project.ProjectManager.UserId != currentUserId)
                        {
                            await NotifyStakeholder(
                                handover.Project.ProjectManager.UserId,
                                "Project Closed",
                                $"Project '{projectName}' has been closed.",
                                "Project",
                                $"/Handover/HandoverDetails/{handover.Id}"
                            );
                        }

                        if (handover.Project?.SiteManager != null &&
                            !string.IsNullOrEmpty(handover.Project.SiteManager.UserId) &&
                            handover.Project.SiteManager.UserId != currentUserId)
                        {
                            await NotifyStakeholder(
                                handover.Project.SiteManager.UserId,
                                "Project Closed",
                                $"Project '{projectName}' has been closed.",
                                "Project",
                                $"/Handover/HandoverDetails/{handover.Id}"
                            );
                        }

                        await NotifyAdmins(
                            "Project Closed",
                            $"Project '{projectName}' has been closed.",
                            $"/Handover/HandoverDetails/{handover.Id}",
                            skipUserId: currentUserId
                        );

                        await NotifyFinanceOfficers(
                            "Project Closed",
                            $"Project '{projectName}' has been closed.",
                            $"/Handover/HandoverDetails/{handover.Id}",
                            skipUserId: currentUserId
                        );

                        if (!string.IsNullOrEmpty(client.UserId) && client.UserId != currentUserId)
                        {
                            await NotifyStakeholder(
                                client.UserId,
                                "Project Closed",
                                $"Project '{projectName}' has been closed.",
                                "Project",
                                $"/Handover/HandoverDetails/{handover.Id}"
                            );
                        }

                        var closedRecipientEmails = new List<string>();
                        if (handover.Project?.ProjectManager?.UserId != null)
                        {
                            var pmUser = db.Users.FirstOrDefault(u => u.Id == handover.Project.ProjectManager.UserId);
                            if (pmUser != null && !string.IsNullOrEmpty(pmUser.Email))
                            {
                                closedRecipientEmails.Add(pmUser.Email);
                            }
                        }
                        if (!string.IsNullOrEmpty(client.ContactEmail))
                        {
                            closedRecipientEmails.Add(client.ContactEmail);
                        }

                        SendProjectClosedEmail(closedRecipientEmails.Distinct().ToList(), projectName);

                        TempData["SuccessMessage"] = "Client has signed off. Project has been closed and all stakeholders notified.";
                    }
                    else
                    {
                        handover.Status = "Completed";
                        if (handover.Project != null)
                        {
                            handover.Project.Status = "Completed";
                        }

                        await db.SaveChangesAsync();

                        bool documentsMissing = handover.Documents == null || !handover.Documents.Any();
                        bool pdfMissing = string.IsNullOrEmpty(handover.SignedHandoverPdfPath);

                        string reason;
                        if (documentsMissing && pdfMissing)
                        {
                            reason = "no handover documents were uploaded and the signed PDF could not be generated";
                        }
                        else if (documentsMissing)
                        {
                            reason = "no handover documents were uploaded";
                        }
                        else
                        {
                            reason = "the signed PDF could not be generated";
                        }

                        if (handover.Project?.ProjectManager != null &&
                            !string.IsNullOrEmpty(handover.Project.ProjectManager.UserId) &&
                            handover.Project.ProjectManager.UserId != currentUserId)
                        {
                            await NotifyStakeholder(
                                handover.Project.ProjectManager.UserId,
                                "Sign-off recorded — manual close required",
                                $"Client has signed off the handover for project '{handover.Project.ProjectName}', but the project could not be auto-closed because {reason}. Please review and close manually.",
                                "Project",
                                $"/Handover/HandoverDetails/{handover.Id}"
                            );
                        }

                        await NotifyAdmins(
                            "Sign-off recorded — manual close required",
                            $"Client has signed off the handover for project '{handover.Project?.ProjectName}', but the project could not be auto-closed because {reason}. Please review and close manually.",
                            $"/Handover/HandoverDetails/{handover.Id}",
                            skipUserId: currentUserId
                        );

                        TempData["SuccessMessage"] = $"Client has signed off. Project could not be auto-closed — manual closure required ({reason}).";
                    }

                    return RedirectToAction("HandoverDetails", new { id = handover.Id });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error during client sign-off: {ex.Message}");
                    ModelState.AddModelError("", "Error recording sign-off: " + ex.Message);
                }
            }

            // Repopulate for redisplay
            var ho = db.ProjectHandovers
                .Include(h => h.Project)
                .Include(h => h.Documents)
                .FirstOrDefault(h => h.Id == model.HandoverId);

            if (ho != null)
            {
                model.ProjectName = ho.Project?.ProjectName;
                model.ProjectCode = ho.Project?.ProjectCode;
                model.HandoverType = ho.HandoverType;
                model.CompletionNotes = ho.CompletionNotes;
                model.FinalInspectionFindings = ho.FinalInspectionFindings;
                model.LessonsLearned = ho.LessonsLearned;
                model.Recommendations = ho.Recommendations;
                model.WarrantyPeriodDays = ho.WarrantyPeriodDays;
                model.WarrantyStartDate = ho.WarrantyStartDate;
                model.WarrantyEndDate = ho.WarrantyEndDate;
                model.ClientInvitedDate = ho.ClientInvitedDate;
                model.Documents = (ho.Documents ?? Enumerable.Empty<HandoverDocument>())
                    .OrderByDescending(d => d.UploadDate)
                    .Select(d => new HandoverDocumentListItemViewModel
                    {
                        Id = d.Id,
                        DocumentName = d.DocumentName,
                        DocumentType = d.DocumentType,
                        Description = d.Description,
                        FilePath = d.FilePath,
                        FileSizeBytes = d.FileSizeBytes,
                        UploadDate = d.UploadDate,
                        UploadedBy = d.UploadedBy,
                        IsApproved = d.IsApproved
                    }).ToList();
            }

            return View(model);
        }

        // ------------------------------------------------------------------
        // Close Project
        // ------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Project Manager")]
        public async Task<ActionResult> CloseProject(int id)
        {
            try
            {
                var handover = db.ProjectHandovers
                    .Include(h => h.Project)
                    .Include(h => h.Project.Client)
                    .Include(h => h.Documents)
                    .FirstOrDefault(h => h.Id == id);

                if (handover == null)
                {
                    TempData["ErrorMessage"] = "Handover not found.";
                    return RedirectToAction("Index");
                }

                if (handover.ClientSignOffStatus != "Signed")
                {
                    TempData["ErrorMessage"] = "Client must sign off before the project can be closed.";
                    return RedirectToAction("HandoverDetails", new { id });
                }

                if (handover.Documents == null || !handover.Documents.Any())
                {
                    TempData["ErrorMessage"] = "At least one handover document is required before closing.";
                    return RedirectToAction("HandoverDetails", new { id });
                }

                if (handover.Status == "Closed")
                {
                    TempData["ErrorMessage"] = "This handover is already closed.";
                    return RedirectToAction("HandoverDetails", new { id });
                }

                // Close handover
                handover.Status = "Closed";
                handover.LastModifiedDate = DateTime.Now;

                // Close project
                if (handover.Project != null)
                {
                    handover.Project.Status = "Closed";
                    handover.Project.ActualEndDate = DateTime.Now;
                    handover.Project.IsActive = false;
                    handover.Project.LastModifiedDate = DateTime.Now;
                    handover.Project.LastModifiedBy = User.Identity.Name;
                }

                // Deactivate resource allocations
                var allocations = db.ProjectResourceAllocations
                    .Where(a => a.ProjectId == handover.ProjectId && a.IsActive)
                    .ToList();
                foreach (var a in allocations)
                {
                    a.IsActive = false;
                    a.EndDate = DateTime.Now;
                }

                // Close equipment assignments
                var assignments = db.EquipmentAssignments
                    .Where(a => a.ProjectId == handover.ProjectId && a.IsActive)
                    .ToList();
                foreach (var a in assignments)
                {
                    a.IsActive = false;
                    a.ActualReturnDate = DateTime.Now;
                }

                await db.SaveChangesAsync();

                var projectName = handover.Project != null ? handover.Project.ProjectName : "Project";

                // Notify Client
                if (handover.Project?.Client != null && !string.IsNullOrEmpty(handover.Project.Client.UserId))
                {
                    await NotifyStakeholder(
                        handover.Project.Client.UserId,
                        "Project Closed",
                        $"Project '{projectName}' has been closed.",
                        "Project",
                        $"/Handover/HandoverDetails/{handover.Id}"
                    );
                }

                // Notify Admins
                await NotifyAdmins(
                    "Project Closed",
                    $"Project '{projectName}' has been closed.",
                    $"/Handover/HandoverDetails/{handover.Id}",
                    skipUserId: User.Identity.GetUserId()
                );

                TempData["SuccessMessage"] = $"Project '{projectName}' has been closed successfully.";
                return RedirectToAction("HandoverDetails", new { id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing project: {ex.Message}");
                TempData["ErrorMessage"] = "Error closing project.";
                return RedirectToAction("HandoverDetails", new { id });
            }
        }

        // ------------------------------------------------------------------
        // Download Document
        // ------------------------------------------------------------------

        [Authorize(Roles = "Admin, Project Manager, Site Manager, Client")]
        public ActionResult DownloadDocument(int id)
        {
            var doc = db.HandoverDocuments
                .Include(d => d.Handover)
                .Include(d => d.Handover.Project)
                .FirstOrDefault(d => d.Id == id);

            if (doc == null || string.IsNullOrEmpty(doc.FilePath))
            {
                TempData["ErrorMessage"] = "Document not found.";
                return RedirectToAction("Index");
            }

            // Client ownership check
            if (User.IsInRole("Client") && !User.IsInRole("Admin"))
            {
                var client = ResolveCurrentClient();
                if (client == null || doc.Handover?.Project == null || doc.Handover.Project.ClientId != client.Id)
                {
                    TempData["ErrorMessage"] = "You do not have access to this document.";
                    return RedirectToAction("Dashboard", "Home");
                }
            }

            var physicalPath = Server.MapPath("~" + doc.FilePath);
            if (!System.IO.File.Exists(physicalPath))
            {
                TempData["ErrorMessage"] = "File is missing on the server.";
                if (doc.HandoverId > 0)
                    return RedirectToAction("HandoverDetails", new { id = doc.HandoverId });
                return RedirectToAction("Index");
            }

            var mime = MimeMapping.GetMimeMapping(doc.FilePath);
            var downloadName = doc.DocumentName;
            if (string.IsNullOrWhiteSpace(Path.GetExtension(downloadName)))
            {
                downloadName += Path.GetExtension(doc.FilePath);
            }

            return File(physicalPath, mime, downloadName);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private Staff ResolveCurrentStaff()
        {
            var userId = User.Identity.GetUserId();
            return db.Staff.FirstOrDefault(s => s.UserId == userId && s.IsActive);
        }

        private Client ResolveCurrentClient()
        {
            var userId = User.Identity.GetUserId();
            return db.Clients.FirstOrDefault(c => c.UserId == userId && c.IsActive);
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
                    await CreateNotification(adminId, title, message, "Project", link);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error notifying admins: {ex.Message}");
            }
        }

        private async Task NotifyFinanceOfficers(string title, string message, string link, string skipUserId = null)
        {
            try
            {
                var financeOfficerUserIds = db.Staff
                    .Where(s => s.Role == "Finance Officer" && s.IsActive && s.UserId != skipUserId)
                    .Select(s => s.UserId)
                    .ToList();

                foreach (var userId in financeOfficerUserIds)
                {
                    await CreateNotification(userId, title, message, "Project", link);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error notifying finance officers: {ex.Message}");
            }
        }

        // Reads SMTP settings from web.config appSettings, same convention as
        // ClientController. Returns null if SMTP isn't configured (email
        // sending is then skipped and just logged, so nothing blocks the workflow).
        private SmtpClient BuildSmtpClient(out string fromEmail, out string fromName)
        {
            fromEmail = null;
            fromName = null;
            string host = ConfigurationManager.AppSettings["SmtpHost"];
            if (string.IsNullOrEmpty(host)) return null;

            int port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
            bool useSsl = bool.Parse(ConfigurationManager.AppSettings["SmtpUseSsl"] ?? "true");
            string smtpUser = ConfigurationManager.AppSettings["SmtpUsername"];
            string smtpPass = ConfigurationManager.AppSettings["SmtpPassword"];
            fromEmail = ConfigurationManager.AppSettings["SmtpFromEmail"] ?? smtpUser;
            fromName = ConfigurationManager.AppSettings["SmtpFromName"] ?? "MuhleOP";

            var client = new SmtpClient(host, port) { EnableSsl = useSsl };
            if (!string.IsNullOrEmpty(smtpUser))
            {
                client.Credentials = new System.Net.NetworkCredential(smtpUser, smtpPass);
            }
            return client;
        }

        private void SendClientInvitedToSignOffEmail(Client client, Project project)
        {
            try
            {
                using (var smtp = BuildSmtpClient(out string fromEmail, out string fromName))
                {
                    if (smtp == null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"SMTP not configured - sign-off invite email not sent to {client.ContactEmail}.");
                        return;
                    }

                    string body = $@"
                        <div style='font-family:Segoe UI,Arial,sans-serif;max-width:600px;margin:auto;'>
                            <h2 style='color:#F5A623;'>Project Sign-off Requested</h2>
                            <p>Dear {client.PrimaryContactName},</p>
                            <p>The handover for <strong>{project.ProjectName}</strong> is ready for your review and sign-off.</p>
                            <p>Please log in to MuhleOps and open the sign-off page to review the handover documents and confirm the project as completed.</p>
                            <p style='color:#888;font-size:12px;margin-top:24px;'>This is an automated message, please do not reply to this email.</p>
                        </div>";

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(fromEmail, fromName);
                        message.To.Add(client.ContactEmail);
                        message.Subject = $"Project Sign-off Requested - {project.ProjectName}";
                        message.Body = body;
                        message.IsBodyHtml = true;
                        smtp.Send(message);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending sign-off invite email: {ex.Message}");
            }
        }

        private void SendSignedHandoverEmail(List<string> recipientEmails, ProjectHandover handover, Client client, byte[] pdfBytes)
        {
            if (recipientEmails == null || !recipientEmails.Any()) return;

            try
            {
                using (var smtp = BuildSmtpClient(out string fromEmail, out string fromName))
                {
                    if (smtp == null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "SMTP not configured - signed handover email not sent.");
                        return;
                    }

                    var projectName = handover.Project != null ? handover.Project.ProjectName : "Project";

                    string pdfNote = pdfBytes == null
                        ? "<p style='color:#c0392b;'>The signed handover PDF could not be generated automatically and can be regenerated by the Project Manager.</p>"
                        : "<p>The signed handover certificate is attached to this email as a PDF for your records.</p>";

                    string body = $@"
                        <div style='font-family:Segoe UI,Arial,sans-serif;max-width:600px;margin:auto;'>
                            <h2 style='color:#F5A623;'>Handover Signed Off</h2>
                            <p>The handover for <strong>{projectName}</strong> has been signed off by {client.CompanyName} on {handover.ClientSignOffDate:dd MMM yyyy HH:mm}.</p>
                            {pdfNote}
                            <p style='color:#888;font-size:12px;margin-top:24px;'>This is an automated message, please do not reply to this email.</p>
                        </div>";

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(fromEmail, fromName);
                        foreach (var email in recipientEmails)
                        {
                            message.To.Add(email);
                        }
                        message.Subject = $"Handover Signed - {projectName}";
                        message.Body = body;
                        message.IsBodyHtml = true;

                        if (pdfBytes != null)
                        {
                            using (var stream = new MemoryStream(pdfBytes))
                            {
                                var attachment = new Attachment(stream, $"SignedHandover_{handover.Id}.pdf", "application/pdf");
                                message.Attachments.Add(attachment);
                                smtp.Send(message);
                            }
                        }
                        else
                        {
                            smtp.Send(message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending signed handover email: {ex.Message}");
            }
        }

        private void SendProjectClosedEmail(List<string> recipientEmails, string projectName)
        {
            if (recipientEmails == null || !recipientEmails.Any()) return;

            try
            {
                using (var smtp = BuildSmtpClient(out string fromEmail, out string fromName))
                {
                    if (smtp == null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "SMTP not configured - project closed email not sent.");
                        return;
                    }

                    string body = $@"
                        <div style='font-family:Segoe UI,Arial,sans-serif;max-width:600px;margin:auto;'>
                            <h2 style='color:#F5A623;'>Project Closed</h2>
                            <p>Project <strong>{projectName}</strong> has been closed following client sign-off.</p>
                            <p style='color:#888;font-size:12px;margin-top:24px;'>This is an automated message, please do not reply to this email.</p>
                        </div>";

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(fromEmail, fromName);
                        foreach (var email in recipientEmails)
                        {
                            message.To.Add(email);
                        }
                        message.Subject = $"Project Closed - {projectName}";
                        message.Body = body;
                        message.IsBodyHtml = true;
                        smtp.Send(message);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending project closed email: {ex.Message}");
            }
        }

        private List<SelectListItem> GetHandoverTypeSelectList(string selected)
        {
            var items = new[] { "Final", "Partial", "Provisional" };
            return items.Select(c => new SelectListItem { Value = c, Text = c, Selected = c == selected }).ToList();
        }

        private List<SelectListItem> GetDocumentTypeSelectList(string selected)
        {
            var items = new[]
            {
                "As-Built Drawings",
                "Operation Manuals",
                "Maintenance Manuals",
                "Warranty Certificates",
                "Compliance Certificates",
                "Other"
            };
            return items.Select(c => new SelectListItem { Value = c, Text = c, Selected = c == selected }).ToList();
        }

        private static string GetStatusBadgeColor(string status)
        {
            switch (status)
            {
                case "Initiated": return "info";
                case "In Progress": return "warning";
                case "Completed": return "primary";
                case "Closed": return "success";
                default: return "secondary";
            }
        }

        private static string GetClientSignOffBadgeColor(string status)
        {
            switch (status)
            {
                case "Pending": return "warning";
                case "Signed": return "success";
                case "Rejected": return "danger";
                default: return "secondary";
            }
        }

        private InitiateHandoverViewModel RepopulateInitiateDropdowns(InitiateHandoverViewModel model)
        {
            var activeHandoverProjectIds = db.ProjectHandovers
                .Where(h => h.Status != "Closed")
                .Select(h => h.ProjectId)
                .Distinct()
                .ToList();

            model.Projects = db.Projects
                .Where(p => p.IsActive &&
                            (p.Status == "Completed" || p.Status == "In Progress" || p.Status == "Initiated") &&
                            !activeHandoverProjectIds.Contains(p.Id))
                .OrderBy(p => p.ProjectName)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.ProjectName + " (" + p.ProjectCode + ")",
                    Selected = p.Id == model.ProjectId
                })
                .ToList();

            model.HandoverTypes = GetHandoverTypeSelectList(model.HandoverType);
            return model;
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