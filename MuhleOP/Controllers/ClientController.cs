using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MuhleOP.Helpers;
using MuhleOP.Models;
using MuhleOP.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MuhleOP.Controllers
{
    [Authorize]
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private ApplicationUserManager _userManager;

        public ClientController()
        {
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        // GET: Client/Index - View all clients
        [Authorize(Roles = "Admin, Project Manager")]
        public ActionResult Index()
        {
            var clients = db.Clients
                .OrderByDescending(c => c.RegistrationDate)
                .ToList();

            var viewModels = clients.Select(c => new ClientViewModel
            {
                Id = c.Id,
                CompanyName = c.CompanyName,
                RegistrationNumber = c.RegistrationNumber,
                TaxIdNumber = c.TaxIdNumber,
                PrimaryContactName = c.PrimaryContactName,
                ContactEmail = c.ContactEmail,
                ContactPhone = c.ContactPhone,
                AlternativePhone = c.AlternativePhone,
                PhysicalAddress = c.PhysicalAddress,
                PostalAddress = c.PostalAddress,
                Website = c.Website,
                Industry = c.Industry,
                CompanySize = c.CompanySize,
                BillingInformation = c.BillingInformation,
                PaymentTerms = c.PaymentTerms,
                CreditLimit = c.CreditLimit,
                IsActive = c.IsActive,
                RegistrationDate = c.RegistrationDate,
                RegisteredBy = c.RegisteredBy,
                UserId = c.UserId,
                ContractCount = db.Contracts.Count(ct => ct.ClientId == c.Id),
                ProjectCount = db.Projects.Count(p => p.ClientId == c.Id),
                DisplayName = c.DisplayName
            }).ToList();

            return View(viewModels);
        }

        // GET: Client/Details/5
        [Authorize(Roles = "Admin, Project Manager")]
        public ActionResult Details(int id)
        {
            var client = db.Clients.Find(id);
            if (client == null)
            {
                return HttpNotFound();
            }

            var contracts = db.Contracts
                .Where(c => c.ClientId == client.Id)
                .OrderByDescending(c => c.CreatedDate)
                .ToList();

            var projects = db.Projects
                .Where(p => p.ClientId == client.Id)
                .OrderByDescending(p => p.CreatedDate)
                .ToList();

            var viewModel = new ClientViewModel
            {
                Id = client.Id,
                CompanyName = client.CompanyName,
                RegistrationNumber = client.RegistrationNumber,
                TaxIdNumber = client.TaxIdNumber,
                PrimaryContactName = client.PrimaryContactName,
                ContactEmail = client.ContactEmail,
                ContactPhone = client.ContactPhone,
                AlternativePhone = client.AlternativePhone,
                PhysicalAddress = client.PhysicalAddress,
                PostalAddress = client.PostalAddress,
                Website = client.Website,
                Industry = client.Industry,
                CompanySize = client.CompanySize,
                BillingInformation = client.BillingInformation,
                PaymentTerms = client.PaymentTerms,
                CreditLimit = client.CreditLimit,
                IsActive = client.IsActive,
                RegistrationDate = client.RegistrationDate,
                RegisteredBy = client.RegisteredBy,
                UserId = client.UserId,
                ContractCount = contracts.Count,
                ProjectCount = projects.Count,
                DisplayName = client.DisplayName
            };

            ViewBag.Contracts = contracts;
            ViewBag.Projects = projects;

            return View(viewModel);
        }

        // GET: Client/Register - Use Case 6: Register Client
        [Authorize(Roles = "Admin")]
        public ActionResult Register()
        {
            return View();
        }

        // POST: Client/Register - Use Case 6: Register Client
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Register(ClientRegistrationViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if client already exists - by registration number,
                    // by client contact email, or by an existing login account
                    // using that email (e.g. the contact is already a Staff
                    // member or Candidate in the system).
                    var existingClient = db.Clients
                        .FirstOrDefault(c => c.RegistrationNumber == model.RegistrationNumber || c.ContactEmail == model.ContactEmail);

                    if (existingClient != null)
                    {
                        if (existingClient.RegistrationNumber == model.RegistrationNumber)
                        {
                            ModelState.AddModelError("RegistrationNumber", "A client with this registration number is already registered.");
                        }
                        else
                        {
                            ModelState.AddModelError("ContactEmail", "A client with this contact email is already registered.");
                        }
                        return View(model);
                    }

                    var existingUser = await UserManager.FindByEmailAsync(model.ContactEmail);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("ContactEmail", "A login account with this email already exists in the system.");
                        return View(model);
                    }

                    // Create user account for client contact
                    var user = new ApplicationUser
                    {
                        UserName = model.ContactEmail,
                        Email = model.ContactEmail,
                        EmailConfirmed = true,
                        FirstName = model.PrimaryContactName.Split(' ').FirstOrDefault(),
                        LastName = model.PrimaryContactName.Contains(" ") ?
                            model.PrimaryContactName.Substring(model.PrimaryContactName.IndexOf(' ') + 1) :
                            model.PrimaryContactName,
                        CreatedDate = DateTime.Now,
                        IsActive = true
                    };

                    string password = GenerateRandomPassword();
                    var result = await UserManager.CreateAsync(user, password);

                    if (result.Succeeded)
                    {
                        // Create client
                        var client = new Client
                        {
                            CompanyName = model.CompanyName,
                            RegistrationNumber = model.RegistrationNumber,
                            TaxIdNumber = model.TaxIdNumber,
                            PrimaryContactName = model.PrimaryContactName,
                            ContactEmail = model.ContactEmail,
                            ContactPhone = model.ContactPhone,
                            AlternativePhone = model.AlternativePhone,
                            PhysicalAddress = model.PhysicalAddress,
                            PostalAddress = model.PostalAddress,
                            Website = model.Website,
                            Industry = model.Industry,
                            CompanySize = model.CompanySize,
                            BillingInformation = model.BillingInformation,
                            PaymentTerms = model.PaymentTerms,
                            CreditLimit = model.CreditLimit,
                            UserId = user.Id,
                            RegistrationDate = DateTime.Now,
                            IsActive = true
                        };

                        db.Clients.Add(client);
                        await db.SaveChangesAsync();

                        // Assign Client role if exists, otherwise just store
                        if (!UserManager.IsInRole(user.Id, "Client"))
                        {
                            try
                            {
                                await UserManager.AddToRoleAsync(user.Id, "Client");
                            }
                            catch
                            {
                                // Role might not exist, continue
                            }
                        }

                        // Send welcome email with login credentials
                        SendWelcomeEmail(model.ContactEmail, model.PrimaryContactName, model.CompanyName, password);

                        // Notify Admin
                        await CreateNotification(
                            User.Identity.GetUserId(),
                            "New Client Registered",
                            $"Client '{model.CompanyName}' has been successfully registered. Login credentials have been sent to {model.ContactEmail}.",
                            "System",
                            $"/Client/Details/{client.Id}"
                        );

                        // Notify client
                        await CreateNotification(
                            user.Id,
                            "Welcome to MuhleOps!",
                            $"Your company '{model.CompanyName}' has been registered. You can now view contracts and projects.",
                            "System",
                            "/Client/Details"
                        );

                        TempData["SuccessMessage"] = $"Client '{model.CompanyName}' registered successfully! Login details sent to {model.ContactEmail}.";
                        return RedirectToAction("Index");
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error registering client: " + ex.Message);
                }
            }

            return View(model);
        }

        // GET: Client/Contracts/5 - View all contracts for a client
        [Authorize(Roles = "Admin, Project Manager")]
        public ActionResult Contracts(int id)
        {
            var client = db.Clients.Find(id);
            if (client == null)
            {
                return HttpNotFound();
            }

            var contracts = db.Contracts
                .Where(c => c.ClientId == id)
                .OrderByDescending(c => c.CreatedDate)
                .ToList();

            var viewModels = contracts.Select(c => new ContractViewModel
            {
                Id = c.Id,
                ContractNumber = c.ContractNumber,
                ClientId = c.ClientId,
                ClientName = client.CompanyName,
                ProjectName = c.ProjectName,
                ContractType = c.ContractType,
                ContractValue = c.ContractValue,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                ProjectScope = c.ProjectScope,
                Deliverables = c.Deliverables,
                PaymentTerms = c.PaymentTerms,
                PaymentSchedule = c.PaymentSchedule,
                Milestones = c.Milestones,
                Timeline = c.Timeline,
                PenaltyClauses = c.PenaltyClauses,
                TerminationConditions = c.TerminationConditions,
                SpecialConditions = c.SpecialConditions,
                Status = c.Status,
                IsSigned = c.IsSigned,
                SignatureDate = c.SignatureDate,
                SignedDocumentPath = c.SignedDocumentPath,
                CreatedDate = c.CreatedDate,
                CreatedBy = c.CreatedBy,
                ContractFilePath = c.ContractFilePath,
                ReviewedBy = c.ReviewedBy,
                ReviewedDate = c.ReviewedDate,
                ReviewNotes = c.ReviewNotes,
                GeneratedPdfPath = c.GeneratedPdfPath,
                SignedPdfPath = c.SignedPdfPath,
                SentForReviewDate = c.SentForReviewDate,
                SentToClientDate = c.SentToClientDate
            }).ToList();

            ViewBag.ClientName = client.CompanyName;
            return View(viewModels);
        }

        // GET: Client/CreateContract/5 - Use Case 7: Create Contract
        [Authorize(Roles = "Admin, Project Manager")]
        public ActionResult CreateContract(int clientId)
        {
            var client = db.Clients.Find(clientId);
            if (client == null)
            {
                TempData["ErrorMessage"] = "Client not found.";
                return RedirectToAction("Index");
            }

            var viewModel = new CreateContractViewModel
            {
                ClientId = clientId,
                Status = ContractStatus.Draft
            };

            ViewBag.ClientName = client.CompanyName;
            return View(viewModel);
        }

        // POST: Client/GenerateContractDefaults - AJAX call from the Create Contract
        // form's "Auto-Generate" button. Fills text fields from offline templates;
        // the admin can still edit every field before submitting.
        [HttpPost]
        [Authorize(Roles = "Admin, Project Manager")]
        public ActionResult GenerateContractDefaults(int clientId, string projectName, string contractType,
            decimal contractValue, DateTime startDate, DateTime endDate)
        {
            var client = db.Clients.Find(clientId);
            if (client == null)
            {
                return Json(new { success = false, message = "Client not found." });
            }

            if (endDate < startDate)
            {
                return Json(new { success = false, message = "End date must be after start date." });
            }

            var defaults = ContractTemplateGenerator.Generate(client.CompanyName, projectName, contractType,
                contractValue, startDate, endDate);

            return Json(new
            {
                success = true,
                defaults.ProjectScope,
                defaults.Deliverables,
                defaults.PaymentTerms,
                defaults.PaymentSchedule,
                defaults.Milestones,
                defaults.Timeline,
                defaults.PenaltyClauses,
                defaults.TerminationConditions
            });
        }

        // POST: Client/CreateContract - Use Case 7: Create Contract
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Project Manager")]
        public async Task<ActionResult> CreateContract(CreateContractViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var client = db.Clients.Find(model.ClientId);
                    if (client == null)
                    {
                        ModelState.AddModelError("", "Client not found.");
                        return View(model);
                    }

                    // Validate dates
                    if (model.EndDate < model.StartDate)
                    {
                        ModelState.AddModelError("EndDate", "End date must be after start date.");
                        return View(model);
                    }

                    // Check for duplicate contract
                    var existingContract = db.Contracts
                        .FirstOrDefault(c => c.ClientId == model.ClientId && c.ProjectName == model.ProjectName && c.Status != "Terminated");

                    if (existingContract != null)
                    {
                        ModelState.AddModelError("ProjectName", "A contract with this project name already exists for this client.");
                        return View(model);
                    }

                    // Save contract file if uploaded
                    string contractFilePath = null;
                    if (model.ContractFile != null && model.ContractFile.ContentLength > 0)
                    {
                        string uploadFolder = Server.MapPath("~/Uploads/Contracts/");
                        if (!Directory.Exists(uploadFolder))
                        {
                            Directory.CreateDirectory(uploadFolder);
                        }

                        string fileName = "Contract_" + model.ClientId + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + Path.GetExtension(model.ContractFile.FileName);
                        string filePath = Path.Combine(uploadFolder, fileName);
                        model.ContractFile.SaveAs(filePath);
                        contractFilePath = "~/Uploads/Contracts/" + fileName;
                    }

                    var contract = new Contract
                    {
                        ClientId = model.ClientId,
                        ProjectName = model.ProjectName,
                        ContractType = model.ContractType,
                        ContractValue = model.ContractValue,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate,
                        ProjectScope = model.ProjectScope,
                        Deliverables = model.Deliverables,
                        PaymentTerms = model.PaymentTerms,
                        PaymentSchedule = model.PaymentSchedule,
                        Milestones = model.Milestones,
                        Timeline = model.Timeline,
                        PenaltyClauses = model.PenaltyClauses,
                        TerminationConditions = model.TerminationConditions,
                        SpecialConditions = model.SpecialConditions,
                        IsSigned = false,
                        CreatedBy = User.Identity.Name,
                        CreatedDate = DateTime.Now,
                        ContractFilePath = contractFilePath
                    };

                    // Completeness check - all key clauses must be filled in
                    // (auto-generated or hand-typed) before it can go anywhere.
                    var missingFields = ValidateContractCompleteness(contract);
                    if (missingFields.Any())
                    {
                        ModelState.AddModelError("", "Please complete the following before submitting: " + string.Join(", ", missingFields));
                        ViewBag.ClientName = client.CompanyName;
                        return View(model);
                    }

                    contract.Status = ContractStatus.PendingReview;
                    contract.SentForReviewDate = DateTime.Now;

                    db.Contracts.Add(contract);
                    await db.SaveChangesAsync();

                    // Generate the draft/preview PDF now so it's downloadable immediately
                    GenerateAndSaveDraftPdf(contract, client);
                    await db.SaveChangesAsync();

                    // Notify every Project Manager that a contract is waiting on their review
                    var pmUsers = UserManager.Users.ToList().Where(u => UserManager.IsInRole(u.Id, "Project Manager")).ToList();
                    foreach (var pm in pmUsers)
                    {
                        await CreateNotification(
                            pm.Id,
                            "Contract Awaiting Your Review",
                            $"A new contract for '{model.ProjectName}' ({client.CompanyName}) needs your review before it can be sent to the client.",
                            "System",
                            $"/Client/ReviewContract/{contract.Id}"
                        );
                    }

                    // Notify Admin (confirmation)
                    await CreateNotification(
                        User.Identity.GetUserId(),
                        "Contract Submitted for Review",
                        $"Contract for '{model.ProjectName}' has been created and sent to the Project Manager for review.",
                        "System",
                        $"/Client/ContractDetails/{contract.Id}"
                    );

                    TempData["SuccessMessage"] = $"Contract for '{model.ProjectName}' created and sent to the Project Manager for review!";
                    return RedirectToAction("ContractDetails", new { id = contract.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error creating contract: " + ex.Message);
                }
            }

            ViewBag.ClientName = db.Clients.Find(model.ClientId)?.CompanyName;
            return View(model);
        }

        // GET: Client/ReviewContract/5 - Project Manager reviews/edits and finalizes
        // a contract that was submitted by the Admin, then sends it to the client.
        [Authorize(Roles = "Project Manager, Admin")]
        public ActionResult ReviewContract(int id)
        {
            var contract = db.Contracts.Find(id);
            if (contract == null)
            {
                TempData["ErrorMessage"] = "Contract not found.";
                return RedirectToAction("Index");
            }

            if (contract.Status != ContractStatus.PendingReview)
            {
                TempData["ErrorMessage"] = "This contract is not currently awaiting review.";
                return RedirectToAction("ContractDetails", new { id = contract.Id });
            }

            var client = db.Clients.Find(contract.ClientId);

            var viewModel = new ReviewContractViewModel
            {
                ContractId = contract.Id,
                ContractNumber = contract.ContractNumber,
                ClientId = contract.ClientId,
                ClientName = client?.CompanyName,
                ProjectName = contract.ProjectName,
                ContractType = contract.ContractType,
                ContractValue = contract.ContractValue,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                ProjectScope = contract.ProjectScope,
                Deliverables = contract.Deliverables,
                PaymentTerms = contract.PaymentTerms,
                PaymentSchedule = contract.PaymentSchedule,
                Milestones = contract.Milestones,
                Timeline = contract.Timeline,
                PenaltyClauses = contract.PenaltyClauses,
                TerminationConditions = contract.TerminationConditions,
                SpecialConditions = contract.SpecialConditions,
                Status = contract.Status,
                CreatedBy = contract.CreatedBy,
                CreatedDate = contract.CreatedDate
            };

            return View(viewModel);
        }

        // POST: Client/ReviewContract - Project Manager finalizes and sends to client
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Project Manager, Admin")]
        public async Task<ActionResult> ReviewContract(ReviewContractViewModel model)
        {
            var contract = db.Contracts.Find(model.ContractId);
            if (contract == null)
            {
                TempData["ErrorMessage"] = "Contract not found.";
                return RedirectToAction("Index");
            }

            if (contract.Status != ContractStatus.PendingReview)
            {
                TempData["ErrorMessage"] = "This contract is not currently awaiting review.";
                return RedirectToAction("ContractDetails", new { id = contract.Id });
            }

            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date.");
            }

            if (!ModelState.IsValid)
            {
                model.ClientName = db.Clients.Find(model.ClientId)?.CompanyName;
                return View(model);
            }

            try
            {
                var client = db.Clients.Find(contract.ClientId);

                // Apply the PM's (possibly edited) final wording
                contract.ProjectName = model.ProjectName;
                contract.ContractType = model.ContractType;
                contract.ContractValue = model.ContractValue;
                contract.StartDate = model.StartDate;
                contract.EndDate = model.EndDate;
                contract.ProjectScope = model.ProjectScope;
                contract.Deliverables = model.Deliverables;
                contract.PaymentTerms = model.PaymentTerms;
                contract.PaymentSchedule = model.PaymentSchedule;
                contract.Milestones = model.Milestones;
                contract.Timeline = model.Timeline;
                contract.PenaltyClauses = model.PenaltyClauses;
                contract.TerminationConditions = model.TerminationConditions;
                contract.SpecialConditions = model.SpecialConditions;

                var missingFields = ValidateContractCompleteness(contract);
                if (missingFields.Any())
                {
                    ModelState.AddModelError("", "Please complete the following before sending: " + string.Join(", ", missingFields));
                    model.ClientName = client?.CompanyName;
                    return View(model);
                }

                contract.ReviewedBy = User.Identity.Name;
                contract.ReviewedDate = DateTime.Now;
                contract.ReviewNotes = model.ReviewNotes;
                contract.Status = ContractStatus.Sent;
                contract.SentToClientDate = DateTime.Now;

                // Regenerate the draft PDF with the PM's final wording
                GenerateAndSaveDraftPdf(contract, client);

                await db.SaveChangesAsync();

                // Notify + email the client
                await CreateNotification(
                    client.UserId,
                    "Contract Ready for Signature",
                    $"The contract for '{contract.ProjectName}' has been finalized and is ready for your signature.",
                    "System",
                    $"/Client/SignContract/{contract.Id}"
                );
                SendContractReadyEmail(client, contract);

                // Notify Admin
                var adminUsers = UserManager.Users.ToList().Where(u => UserManager.IsInRole(u.Id, "Admin")).ToList();
                foreach (var admin in adminUsers)
                {
                    await CreateNotification(
                        admin.Id,
                        "Contract Finalized & Sent to Client",
                        $"Contract for '{contract.ProjectName}' was reviewed by {contract.ReviewedBy} and sent to {client.CompanyName} for signature.",
                        "System",
                        $"/Client/ContractDetails/{contract.Id}"
                    );
                }

                TempData["SuccessMessage"] = $"Contract for '{contract.ProjectName}' finalized and sent to {client.CompanyName} for signature.";
                return RedirectToAction("ContractDetails", new { id = contract.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error finalizing contract: " + ex.Message);
                model.ClientName = db.Clients.Find(model.ClientId)?.CompanyName;
                return View(model);
            }
        }

        // GET: Client/DownloadContractPdf/5 - available before AND after signing
        [Authorize(Roles = "Admin, Project Manager, Client")]
        public ActionResult DownloadContractPdf(int id)
        {
            var contract = db.Contracts.Find(id);
            if (contract == null)
            {
                TempData["ErrorMessage"] = "Contract not found.";
                return RedirectToAction("Index", "Home");
            }

            var client = db.Clients.Find(contract.ClientId);

            // If the current user is the Client, make sure it's *their* contract
            if (User.IsInRole("Client"))
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                var myClient = db.Clients.FirstOrDefault(c => c.UserId == user.Id);
                if (myClient == null || myClient.Id != contract.ClientId)
                {
                    TempData["ErrorMessage"] = "You are not authorized to view this contract.";
                    return RedirectToAction("Index", "Home");
                }
            }

            byte[] pdfBytes;
            string fileName = $"{contract.ContractNumber}.pdf";

            if (contract.IsSigned && !string.IsNullOrEmpty(contract.SignedPdfPath))
            {
                var signedPath = Server.MapPath(contract.SignedPdfPath);
                if (System.IO.File.Exists(signedPath))
                {
                    pdfBytes = System.IO.File.ReadAllBytes(signedPath);
                    fileName = $"{contract.ContractNumber}_Signed.pdf";
                    return File(pdfBytes, "application/pdf", fileName);
                }
            }

            // Otherwise generate the current draft/preview on the fly so it always
            // reflects the latest saved wording.
            pdfBytes = ContractPdfGenerator.Generate(contract, client);
            return File(pdfBytes, "application/pdf", fileName);
        }

        // GET: Client/ContractDetails/5
        [Authorize(Roles = "Admin, Project Manager, Client")]
        public ActionResult ContractDetails(int id)
        {
            var contract = db.Contracts.Find(id);
            if (contract == null)
            {
                return HttpNotFound();
            }

            var client = db.Clients.Find(contract.ClientId);

            var viewModel = new ContractViewModel
            {
                Id = contract.Id,
                ContractNumber = contract.ContractNumber,
                ClientId = contract.ClientId,
                ClientName = client?.CompanyName ?? "Unknown",
                ProjectName = contract.ProjectName,
                ContractType = contract.ContractType,
                ContractValue = contract.ContractValue,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                ProjectScope = contract.ProjectScope,
                Deliverables = contract.Deliverables,
                PaymentTerms = contract.PaymentTerms,
                PaymentSchedule = contract.PaymentSchedule,
                Milestones = contract.Milestones,
                Timeline = contract.Timeline,
                PenaltyClauses = contract.PenaltyClauses,
                TerminationConditions = contract.TerminationConditions,
                SpecialConditions = contract.SpecialConditions,
                Status = contract.Status,
                IsSigned = contract.IsSigned,
                SignatureDate = contract.SignatureDate,
                SignedDocumentPath = contract.SignedDocumentPath,
                CreatedDate = contract.CreatedDate,
                CreatedBy = contract.CreatedBy,
                ContractFilePath = contract.ContractFilePath,
                ReviewedBy = contract.ReviewedBy,
                ReviewedDate = contract.ReviewedDate,
                ReviewNotes = contract.ReviewNotes,
                GeneratedPdfPath = contract.GeneratedPdfPath,
                SignedPdfPath = contract.SignedPdfPath,
                SentForReviewDate = contract.SentForReviewDate,
                SentToClientDate = contract.SentToClientDate
            };

            // Check if current user is the client
            var user = UserManager.FindById(User.Identity.GetUserId());
            bool isClient = client != null && client.UserId == user?.Id;

            ViewBag.IsClient = isClient;
            ViewBag.CanSign = isClient && !contract.IsSigned && contract.Status != ContractStatus.Terminated;
            ViewBag.CanReview = (User.IsInRole("Project Manager") || User.IsInRole("Admin")) && contract.Status == ContractStatus.PendingReview;

            return View(viewModel);
        }

        // GET: Client/SignContract/5 - Use Case 8: Sign Contract
        [Authorize(Roles = "Client")]
        public ActionResult SignContract(int id)
        {
            var contract = db.Contracts.Find(id);
            if (contract == null)
            {
                TempData["ErrorMessage"] = "Contract not found.";
                return RedirectToAction("Index", "Home");
            }

            // Verify that the current user is the client
            var user = UserManager.FindById(User.Identity.GetUserId());
            var client = db.Clients.FirstOrDefault(c => c.UserId == user.Id);

            if (client == null || client.Id != contract.ClientId)
            {
                TempData["ErrorMessage"] = "You are not authorized to sign this contract.";
                return RedirectToAction("Index", "Home");
            }

            if (contract.IsSigned)
            {
                TempData["ErrorMessage"] = "This contract has already been signed.";
                return RedirectToAction("ContractDetails", new { id = contract.Id });
            }

            var viewModel = new SignContractViewModel
            {
                ContractId = contract.Id,
                ContractNumber = contract.ContractNumber,
                ProjectName = contract.ProjectName,
                ClientName = client.CompanyName,
                ContractValue = contract.ContractValue,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                ProjectScope = contract.ProjectScope,
                PaymentTerms = contract.PaymentTerms,
                Status = contract.Status,
                SignerName = client.PrimaryContactName,
                SignerEmail = client.ContactEmail
            };

            return View(viewModel);
        }

        // POST: Client/SignContract - Use Case 8: Sign Contract
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client")]
        public async Task<ActionResult> SignContract(SignContractViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var contract = db.Contracts.Find(model.ContractId);
                    if (contract == null)
                    {
                        TempData["ErrorMessage"] = "Contract not found.";
                        return RedirectToAction("Index", "Home");
                    }

                    // Verify that the current user is the client
                    var user = UserManager.FindById(User.Identity.GetUserId());
                    var client = db.Clients.FirstOrDefault(c => c.UserId == user.Id);

                    if (client == null || client.Id != contract.ClientId)
                    {
                        TempData["ErrorMessage"] = "You are not authorized to sign this contract.";
                        return RedirectToAction("Index", "Home");
                    }

                    if (contract.IsSigned)
                    {
                        TempData["ErrorMessage"] = "This contract has already been signed.";
                        return RedirectToAction("ContractDetails", new { id = contract.Id });
                    }

                    if (!model.AcceptTerms)
                    {
                        ModelState.AddModelError("AcceptTerms", "You must accept the terms and conditions to sign.");
                        return View(model);
                    }

                    // Save signature
                    string signatureFolder = Server.MapPath("~/Uploads/Signatures/");
                    if (!Directory.Exists(signatureFolder))
                    {
                        Directory.CreateDirectory(signatureFolder);
                    }

                    string signatureFileName = "Signature_" + contract.Id + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".png";
                    string signatureFilePath = Path.Combine(signatureFolder, signatureFileName);

                    // Save base64 signature as image
                    if (!string.IsNullOrEmpty(model.SignatureData))
                    {
                        string base64Data = model.SignatureData.Replace("data:image/png;base64,", "").Replace("data:image/jpeg;base64,", "");
                        byte[] imageBytes = Convert.FromBase64String(base64Data);
                        System.IO.File.WriteAllBytes(signatureFilePath, imageBytes);
                    }

                    // Update contract
                    contract.IsSigned = true;
                    contract.SignatureDate = DateTime.Now;
                    contract.Status = ContractStatus.Signed;
                    contract.SignedDocumentPath = "~/Uploads/Signatures/" + signatureFileName;

                    // Add signature record
                    var signature = new ContractSignature
                    {
                        ContractId = contract.Id,
                        SignerName = model.SignerName ?? client.PrimaryContactName,
                        SignerEmail = model.SignerEmail ?? client.ContactEmail,
                        SignatureData = model.SignatureData,
                        SignatureType = "Electronic",
                        SignedDate = DateTime.Now,
                        IsValid = true,
                        IpAddress = Request.UserHostAddress,
                        UserAgent = Request.UserAgent
                    };

                    db.ContractSignatures.Add(signature);
                    await db.SaveChangesAsync();

                    // Generate the final signed PDF (contract text + signature image stamped in)
                    byte[] signatureBytesForPdf = null;
                    if (!string.IsNullOrEmpty(model.SignatureData))
                    {
                        string b64 = model.SignatureData.Replace("data:image/png;base64,", "").Replace("data:image/jpeg;base64,", "");
                        signatureBytesForPdf = Convert.FromBase64String(b64);
                    }
                    byte[] signedPdfBytes = ContractPdfGenerator.Generate(contract, client, signatureBytesForPdf, signature);

                    string pdfFolder = Server.MapPath("~/Uploads/SignedContracts/");
                    if (!Directory.Exists(pdfFolder))
                    {
                        Directory.CreateDirectory(pdfFolder);
                    }
                    string pdfFileName = "SignedContract_" + contract.Id + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";
                    string pdfFilePath = Path.Combine(pdfFolder, pdfFileName);
                    System.IO.File.WriteAllBytes(pdfFilePath, signedPdfBytes);
                    contract.SignedPdfPath = "~/Uploads/SignedContracts/" + pdfFileName;

                    await db.SaveChangesAsync();

                    // Create notification for Admin
                    var adminUsers = UserManager.Users.ToList().Where(u => UserManager.IsInRole(u.Id, "Admin")).ToList();
                    foreach (var admin in adminUsers)
                    {
                        await CreateNotification(
                            admin.Id,
                            "Contract Signed",
                            $"Contract '{contract.ProjectName}' has been signed by {client.CompanyName}.",
                            "System",
                            $"/Client/ContractDetails/{contract.Id}"
                        );
                    }

                    // Notify Project Managers
                    var pmUsers = UserManager.Users.ToList().Where(u => UserManager.IsInRole(u.Id, "Project Manager")).ToList();
                    foreach (var pm in pmUsers)
                    {
                        await CreateNotification(
                            pm.Id,
                            "Contract Signed",
                            $"Contract '{contract.ProjectName}' has been signed by {client.CompanyName}.",
                            "System",
                            $"/Client/ContractDetails/{contract.Id}"
                        );
                    }

                    // Notify client
                    await CreateNotification(
                        client.UserId,
                        "Contract Signed Successfully",
                        $"You have successfully signed the contract for '{contract.ProjectName}'. The project can now be initiated.",
                        "System",
                        $"/Client/ContractDetails/{contract.Id}"
                    );

                    // Email the signed PDF (attached) to PM(s), Admin(s) and the Client
                    var recipientEmails = new List<string> { client.ContactEmail };
                    recipientEmails.AddRange(adminUsers.Where(u => !string.IsNullOrEmpty(u.Email)).Select(u => u.Email));
                    recipientEmails.AddRange(pmUsers.Where(u => !string.IsNullOrEmpty(u.Email)).Select(u => u.Email));
                    SendSignedContractEmail(recipientEmails.Distinct().ToList(), contract, client, signedPdfBytes, pdfFileName);

                    TempData["SuccessMessage"] = $"Contract for '{contract.ProjectName}' has been signed successfully!";
                    return RedirectToAction("ContractDetails", new { id = contract.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error signing contract: " + ex.Message);
                }
            }

            return View(model);
        }

        // GET: Client/SendContract/5 - Resend the signature reminder to the client.
        // NOTE: the Draft -> PendingReview -> Sent transition now happens through
        // CreateContract (Admin) and ReviewContract (Project Manager); this action
        // only re-notifies the client for a contract that's already in "Sent" status.
        [Authorize(Roles = "Admin, Project Manager")]
        public async Task<ActionResult> SendContract(int id)
        {
            var contract = db.Contracts.Find(id);
            if (contract == null)
            {
                TempData["ErrorMessage"] = "Contract not found.";
                return RedirectToAction("Index");
            }

            if (contract.IsSigned)
            {
                TempData["ErrorMessage"] = "This contract has already been signed.";
                return RedirectToAction("ContractDetails", new { id = contract.Id });
            }

            if (contract.Status == ContractStatus.PendingReview)
            {
                TempData["ErrorMessage"] = "This contract is still awaiting Project Manager review. It will be sent to the client once reviewed.";
                return RedirectToAction("ContractDetails", new { id = contract.Id });
            }

            if (contract.Status == ContractStatus.Draft)
            {
                TempData["ErrorMessage"] = "This contract hasn't been submitted for review yet.";
                return RedirectToAction("ContractDetails", new { id = contract.Id });
            }

            var client = db.Clients.Find(contract.ClientId);

            contract.Status = ContractStatus.Sent;
            if (contract.SentToClientDate == null)
            {
                contract.SentToClientDate = DateTime.Now;
            }
            await db.SaveChangesAsync();

            // Notify + email client
            await CreateNotification(
                client.UserId,
                "Contract Ready for Signature",
                $"The contract for '{contract.ProjectName}' is ready for your signature. Please review and sign.",
                "System",
                $"/Client/SignContract/{contract.Id}"
            );
            SendContractReadyEmail(client, contract);

            TempData["SuccessMessage"] = $"Contract sent to {client.CompanyName} for signature.";
            return RedirectToAction("ContractDetails", new { id = contract.Id });
        }

        // POST: Client/ToggleActive/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ToggleActive(int id)
        {
            var client = db.Clients.Find(id);
            if (client == null)
            {
                return Json(new { success = false, message = "Client not found" });
            }

            client.IsActive = !client.IsActive;

            // Update user status
            var user = await UserManager.FindByIdAsync(client.UserId);
            if (user != null)
            {
                user.IsActive = client.IsActive;
                await UserManager.UpdateAsync(user);
            }

            await db.SaveChangesAsync();

            // Notify Admin
            await CreateNotification(
                User.Identity.GetUserId(),
                $"Client {(client.IsActive ? "Activated" : "Deactivated")}",
                $"{client.CompanyName} has been {(client.IsActive ? "activated" : "deactivated")}.",
                "System",
                "/Client/Index"
            );

            return Json(new { success = true, isActive = client.IsActive });
        }

        // Checks that every clause required for a legally usable contract has
        // been filled in (auto-generated or hand-typed). Returns the list of
        // missing field labels, or an empty list if complete.
        private List<string> ValidateContractCompleteness(Contract contract)
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(contract.ProjectName)) missing.Add("Project Name");
            if (contract.ContractValue <= 0) missing.Add("Contract Value");
            if (contract.StartDate == default(DateTime)) missing.Add("Start Date");
            if (contract.EndDate == default(DateTime)) missing.Add("End Date");
            if (contract.EndDate < contract.StartDate) missing.Add("End Date must be after Start Date");
            if (string.IsNullOrWhiteSpace(contract.ProjectScope)) missing.Add("Project Scope");
            if (string.IsNullOrWhiteSpace(contract.Deliverables)) missing.Add("Deliverables");
            if (string.IsNullOrWhiteSpace(contract.PaymentTerms)) missing.Add("Payment Terms");
            if (string.IsNullOrWhiteSpace(contract.PaymentSchedule)) missing.Add("Payment & Invoicing Schedule");
            if (string.IsNullOrWhiteSpace(contract.Milestones)) missing.Add("Milestones");
            if (string.IsNullOrWhiteSpace(contract.Timeline)) missing.Add("Project Timeline");
            if (string.IsNullOrWhiteSpace(contract.PenaltyClauses)) missing.Add("Penalty Clauses");
            if (string.IsNullOrWhiteSpace(contract.TerminationConditions)) missing.Add("Termination Conditions");
            return missing;
        }

        // Generates the current draft/preview PDF and saves it to disk,
        // updating contract.GeneratedPdfPath. Call SaveChangesAsync afterwards.
        private void GenerateAndSaveDraftPdf(Contract contract, Client client)
        {
            try
            {
                byte[] pdfBytes = ContractPdfGenerator.Generate(contract, client);
                string folder = Server.MapPath("~/Uploads/ContractPdfs/");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                string fileName = "Contract_" + contract.Id + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";
                string filePath = Path.Combine(folder, fileName);
                System.IO.File.WriteAllBytes(filePath, pdfBytes);
                contract.GeneratedPdfPath = "~/Uploads/ContractPdfs/" + fileName;
            }
            catch (Exception ex)
            {
                // Don't block the workflow if PDF generation fails - the contract
                // record is already saved; DownloadContractPdf can regenerate on demand.
                System.Diagnostics.Debug.WriteLine($"Error generating contract PDF: {ex.Message}");
            }
        }

        // Reads SMTP settings from web.config appSettings, same convention as
        // SendWelcomeEmail below. Returns null if SMTP isn't configured (email
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

        private void SendContractReadyEmail(Client client, Contract contract)
        {
            try
            {
                using (var smtp = BuildSmtpClient(out string fromEmail, out string fromName))
                {
                    if (smtp == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"SMTP not configured - contract-ready email not sent to {client.ContactEmail}.");
                        return;
                    }

                    string body = $@"
                        <div style='font-family:Segoe UI,Arial,sans-serif;max-width:600px;margin:auto;'>
                            <h2 style='color:#F5A623;'>Contract Ready for Your Signature</h2>
                            <p>Dear {client.PrimaryContactName},</p>
                            <p>The contract for <strong>{contract.ProjectName}</strong> (Contract No. {contract.ContractNumber}) has been
                            reviewed and finalized. Please log in to MuhleOps to review and sign it.</p>
                            <p><strong>Contract Value:</strong> {contract.ContractValue:C}<br/>
                            <strong>Start Date:</strong> {contract.StartDate:dd MMM yyyy}<br/>
                            <strong>End Date:</strong> {contract.EndDate:dd MMM yyyy}</p>
                            <p>You can download a copy of the contract for review before signing directly from the system.</p>
                            <p style='color:#888;font-size:12px;margin-top:24px;'>This is an automated message, please do not reply to this email.</p>
                        </div>";

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(fromEmail, fromName);
                        message.To.Add(client.ContactEmail);
                        message.Subject = $"Contract Ready for Signature - {contract.ContractNumber}";
                        message.Body = body;
                        message.IsBodyHtml = true;
                        smtp.Send(message);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending contract-ready email: {ex.Message}");
            }
        }

        private void SendSignedContractEmail(List<string> recipientEmails, Contract contract, Client client, byte[] pdfBytes, string pdfFileName)
        {
            if (recipientEmails == null || !recipientEmails.Any()) return;

            try
            {
                using (var smtp = BuildSmtpClient(out string fromEmail, out string fromName))
                {
                    if (smtp == null)
                    {
                        System.Diagnostics.Debug.WriteLine("SMTP not configured - signed contract email not sent.");
                        return;
                    }

                    string body = $@"
                        <div style='font-family:Segoe UI,Arial,sans-serif;max-width:600px;margin:auto;'>
                            <h2 style='color:#F5A623;'>Contract Signed</h2>
                            <p>The contract <strong>{contract.ProjectName}</strong> (Contract No. {contract.ContractNumber}) for
                            {client.CompanyName} has been signed on {contract.SignatureDate:dd MMM yyyy HH:mm}.</p>
                            <p>The signed contract is attached to this email as a PDF for your records.</p>
                            <p style='color:#888;font-size:12px;margin-top:24px;'>This is an automated message, please do not reply to this email.</p>
                        </div>";

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(fromEmail, fromName);
                        foreach (var email in recipientEmails)
                        {
                            message.To.Add(email);
                        }
                        message.Subject = $"Contract Signed - {contract.ContractNumber}";
                        message.Body = body;
                        message.IsBodyHtml = true;

                        using (var stream = new MemoryStream(pdfBytes))
                        {
                            var attachment = new Attachment(stream, pdfFileName, "application/pdf");
                            message.Attachments.Add(attachment);
                            smtp.Send(message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending signed contract email: {ex.Message}");
            }
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            var password = new char[12];
            for (int i = 0; i < 12; i++)
            {
                password[i] = chars[random.Next(chars.Length)];
            }
            return new string(password);
        }

        // Sends the "registration successful" email containing the
        // auto-generated login password. Reads SMTP settings from
        // web.config appSettings:
        //   SmtpHost, SmtpPort, SmtpUseSsl, SmtpUsername, SmtpPassword, SmtpFromEmail, SmtpFromName
        private void SendWelcomeEmail(string email, string contactName, string companyName, string password)
        {
            try
            {
                string host = ConfigurationManager.AppSettings["SmtpHost"];
                if (string.IsNullOrEmpty(host))
                {
                    System.Diagnostics.Debug.WriteLine($"SMTP not configured - welcome email not sent to {email} (password: {password}).");
                    return;
                }

                int port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
                bool useSsl = bool.Parse(ConfigurationManager.AppSettings["SmtpUseSsl"] ?? "true");
                string smtpUser = ConfigurationManager.AppSettings["SmtpUsername"];
                string smtpPass = ConfigurationManager.AppSettings["SmtpPassword"];
                string fromEmail = ConfigurationManager.AppSettings["SmtpFromEmail"] ?? smtpUser;
                string fromName = ConfigurationManager.AppSettings["SmtpFromName"] ?? "MuhleOP";

                string subject = "Welcome to MuhleOps - Your Client Account is Ready";
                string body = $@"
                    <div style='font-family:Segoe UI,Arial,sans-serif;max-width:600px;margin:auto;'>
                        <h2 style='color:#F5A623;'>Welcome, {contactName}!</h2>
                        <p><strong>{companyName}</strong> has been successfully registered with MuhleOps.</p>
                        <p>A login account has been created for you. Please use the credentials below to sign in:</p>
                        <table style='border-collapse:collapse;margin:16px 0;'>
                            <tr><td style='padding:4px 12px 4px 0;'><strong>Email:</strong></td><td>{email}</td></tr>
                            <tr><td style='padding:4px 12px 4px 0;'><strong>Password:</strong></td><td>{password}</td></tr>
                        </table>
                        <p>For your security, please log in and change your password as soon as possible.</p>
                        <p>Once logged in, you'll be able to view your contracts and track project progress.</p>
                        <p style='color:#888;font-size:12px;margin-top:24px;'>This is an automated message, please do not reply to this email.</p>
                    </div>";

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(fromEmail, fromName);
                    message.To.Add(email);
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;

                    using (var client = new SmtpClient(host, port))
                    {
                        client.EnableSsl = useSsl;
                        if (!string.IsNullOrEmpty(smtpUser))
                        {
                            client.Credentials = new System.Net.NetworkCredential(smtpUser, smtpPass);
                        }
                        client.Send(message);
                    }
                }
            }
            catch (Exception ex)
            {
                // Don't let an email failure block registration - the account
                // was already created successfully. Just log it.
                System.Diagnostics.Debug.WriteLine($"Error sending client welcome email: {ex.Message}");
            }
        }

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