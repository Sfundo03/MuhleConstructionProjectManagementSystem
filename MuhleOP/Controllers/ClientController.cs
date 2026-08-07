using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MuhleOP.Models;
using MuhleOP.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                    // Check if client already exists
                    var existingClient = db.Clients
                        .FirstOrDefault(c => c.RegistrationNumber == model.RegistrationNumber || c.ContactEmail == model.ContactEmail);

                    if (existingClient != null)
                    {
                        ModelState.AddModelError("", "A client with this registration number or email already exists.");
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
                        SendWelcomeEmail(model.ContactEmail, model.PrimaryContactName, password);

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
                ContractFilePath = c.ContractFilePath
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
                Status = "Draft"
            };

            ViewBag.ClientName = client.CompanyName;
            return View(viewModel);
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
                        Status = model.Status,
                        IsSigned = false,
                        CreatedBy = User.Identity.Name,
                        CreatedDate = DateTime.Now,
                        ContractFilePath = contractFilePath
                    };

                    db.Contracts.Add(contract);
                    await db.SaveChangesAsync();

                    // Create notification for client
                    await CreateNotification(
                        client.UserId,
                        "New Contract Created",
                        $"A new contract for '{model.ProjectName}' has been created. Please review and sign.",
                        "System",
                        $"/Client/SignContract/{contract.Id}"
                    );

                    // Notify Admin
                    await CreateNotification(
                        User.Identity.GetUserId(),
                        "Contract Created",
                        $"Contract for '{model.ProjectName}' has been created successfully.",
                        "System",
                        $"/Client/ContractDetails/{contract.Id}"
                    );

                    TempData["SuccessMessage"] = $"Contract for '{model.ProjectName}' created successfully!";
                    return RedirectToAction("Contracts", new { id = model.ClientId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error creating contract: " + ex.Message);
                }
            }

            ViewBag.ClientName = db.Clients.Find(model.ClientId)?.CompanyName;
            return View(model);
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
                ContractFilePath = contract.ContractFilePath
            };

            // Check if current user is the client
            var user = UserManager.FindById(User.Identity.GetUserId());
            bool isClient = client != null && client.UserId == user?.Id;

            ViewBag.IsClient = isClient;
            ViewBag.CanSign = isClient && !contract.IsSigned && contract.Status != "Terminated";

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
                    contract.Status = "Signed";
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

                    // Create notification for Admin
                    var adminUsers = UserManager.Users.Where(u => UserManager.IsInRole(u.Id, "Admin")).ToList();
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
                    var pmUsers = UserManager.Users.Where(u => UserManager.IsInRole(u.Id, "Project Manager")).ToList();
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

        // GET: Client/SendContract/5 - Send contract for signature
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

            var client = db.Clients.Find(contract.ClientId);

            // Update status
            contract.Status = "Sent";
            await db.SaveChangesAsync();

            // Notify client
            await CreateNotification(
                client.UserId,
                "Contract Ready for Signature",
                $"The contract for '{contract.ProjectName}' is ready for your signature. Please review and sign.",
                "System",
                $"/Client/SignContract/{contract.Id}"
            );

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

        private void SendWelcomeEmail(string email, string name, string password)
        {
            // Email sending logic - simplified for now
            System.Diagnostics.Debug.WriteLine($"Welcome email sent to {email} for {name} with password: {password}");
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