using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MuhleOP.Models;
using MuhleOP.ViewModels;
using System;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MuhleOP.Controllers
{
    [Authorize]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private ApplicationUserManager _userManager;

        public StaffController()
        {
        }

        public StaffController(ApplicationUserManager userManager)
        {
            UserManager = userManager;
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

        // GET: Staff/Index - View all staff
        [Authorize(Roles = "Admin")]
        public ActionResult Index()
        {
            var staffMembers = db.Staff
                .OrderByDescending(s => s.HireDate)
                .ToList();

            var viewModels = staffMembers.Select(s => new StaffViewModel
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                FullName = s.FullName,
                Email = s.Email,
                Role = s.Role,
                PhoneNumber = s.PhoneNumber,
                EmployeeId = s.EmployeeId,
                Department = s.Department,
                HireDate = s.HireDate,
                IsActive = s.IsActive,
                ProfilePicturePath = s.ProfilePicturePath
            }).ToList();

            var viewModel = new StaffListViewModel
            {
                StaffMembers = viewModels,
                TotalStaff = viewModels.Count,
                ActiveStaff = viewModels.Count(s => s.IsActive),
                InactiveStaff = viewModels.Count(s => !s.IsActive)
            };

            return View(viewModel);
        }

        // GET: Staff/Details/5
        [Authorize(Roles = "Admin")]
        public ActionResult Details(int id)
        {
            var staff = db.Staff.Find(id);
            if (staff == null)
            {
                return HttpNotFound();
            }

            var viewModel = new StaffViewModel
            {
                Id = staff.Id,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                FullName = staff.FullName,
                Email = staff.Email,
                Role = staff.Role,
                PhoneNumber = staff.PhoneNumber,
                EmployeeId = staff.EmployeeId,
                Department = staff.Department,
                HireDate = staff.HireDate,
                IsActive = staff.IsActive,
                ProfilePicturePath = staff.ProfilePicturePath
            };

            return View(viewModel);
        }

        // GET: Staff/Hire - Use Case 5: Hire Staff
        [Authorize(Roles = "Admin")]
        public ActionResult Hire()
        {
            return View();
        }

        // POST: Staff/Hire - Use Case 5: Hire Staff
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Hire(CreateStaffViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if user already exists
                    var existingUser = await UserManager.FindByEmailAsync(model.Email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Email", "A user with this email already exists.");
                        return View(model);
                    }

                    // Generate random password
                    string password = GenerateRandomPassword();

                    // Create user account
                    var user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        EmailConfirmed = true,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        CreatedDate = DateTime.Now,
                        IsActive = true
                    };

                    var result = await UserManager.CreateAsync(user, password);

                    if (result.Succeeded)
                    {
                        // Assign role
                        var roleResult = await UserManager.AddToRoleAsync(user.Id, model.Role);

                        // Create staff record
                        var staff = new Staff
                        {
                            FirstName = model.FirstName,
                            LastName = model.LastName,
                            Email = model.Email,
                            Role = model.Role,
                            PhoneNumber = model.PhoneNumber,
                            Department = model.Department,
                            UserId = user.Id,
                            HireDate = DateTime.Now,
                            IsActive = true
                        };

                        db.Staff.Add(staff);
                        await db.SaveChangesAsync();

                        // Check if hiring from candidate
                        if (!string.IsNullOrEmpty(Request.Form["ApplicationId"]))
                        {
                            int applicationId = int.Parse(Request.Form["ApplicationId"]);
                            var application = await db.JobApplications.FindAsync(applicationId);
                            if (application != null)
                            {
                                application.IsHired = true;
                                application.Status = "Hired";
                                application.HireDate = DateTime.Now;
                                application.HiredBy = User.Identity.Name;

                                // Update status history
                                var history = new ApplicationStatusHistory
                                {
                                    ApplicationId = application.Id,
                                    Status = "Hired",
                                    Notes = $"Candidate hired as {model.Role}",
                                    ChangedBy = User.Identity.Name
                                };
                                db.ApplicationStatusHistories.Add(history);

                                await db.SaveChangesAsync();

                                // Notify candidate
                                await CreateNotificationForCandidate(
                                    application.CandidateId,
                                    "You've Been Hired!",
                                    $"Congratulations! You have been hired for the position of '{application.JobPost?.JobTitle ?? "the position"}' as a {model.Role}. Welcome to the team! Your login credentials have been sent to your email.",
                                    "System",
                                    $"/Candidate/ApplicationStatus/{application.Id}"
                                );
                            }
                        }

                        // Notify Admin
                        await CreateNotification(
                            User.Identity.GetUserId(),
                            "New Staff Member Hired",
                            $"You have successfully hired {model.FirstName} {model.LastName} as a {model.Role}.",
                            "System",
                            "/Staff/Index"
                        );

                        TempData["SuccessMessage"] = $"{model.FirstName} {model.LastName} has been hired successfully! Login details have been sent to their email.";
                        return RedirectToAction("Index");
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while hiring staff: " + ex.Message);
                }
            }

            return View(model);
        }

        // GET: Staff/HireFromCandidate/5 - Use Case 5: Hire Candidate
        [Authorize(Roles = "Admin")]
        public ActionResult HireFromCandidate(int id, int? applicationId)
        {
            var candidate = db.Candidates.Find(id);
            if (candidate == null)
            {
                TempData["ErrorMessage"] = "Candidate not found.";
                return RedirectToAction("Index", "JobPost");
            }

            var viewModel = new CreateStaffViewModel
            {
                FirstName = candidate.FirstName,
                LastName = candidate.LastName,
                Email = candidate.Email,
                PhoneNumber = candidate.PhoneNumber
            };

            ViewBag.CandidateId = id;
            ViewBag.ApplicationId = applicationId;
            return View("Hire", viewModel);
        }

        // GET: Staff/HireManagement - list of candidates who passed their
        // interview and are waiting for their staff login account to be created.
        [Authorize(Roles = "Admin")]
        public ActionResult HireManagement()
        {
            // Candidates who passed the interview but don't have a Staff
            // record yet (i.e. no login account was created for them).
            var existingStaffUserIds = db.Staff.Select(s => s.UserId).ToList();

            var readyApplications = db.JobApplications
                .Where(a => a.InterviewResult == "Passed" && a.IsHired)
                .ToList()
                .Where(a => a.Candidate != null && !existingStaffUserIds.Contains(a.Candidate.UserId))
                .OrderByDescending(a => a.InterviewAssessedDate)
                .ToList();

            var viewModel = new HireManagementListViewModel
            {
                ReadyToHire = readyApplications.Select(a => new HireManagementListItemViewModel
                {
                    ApplicationId = a.Id,
                    CandidateId = a.CandidateId,
                    CandidateFullName = a.Candidate?.FullName,
                    CandidateEmail = a.Candidate?.Email,
                    CandidatePhone = a.Candidate?.PhoneNumber,
                    ProfilePicturePath = a.Candidate?.ProfilePicturePath,
                    JobPostId = a.JobPostId,
                    JobTitle = a.JobPost?.JobTitle,
                    Department = a.JobPost?.Department,
                    SystemRoleName = a.JobPost?.SystemRole?.Name ?? a.JobPost?.JobTitle,
                    InterviewAssessedDate = a.InterviewAssessedDate,
                    HiredBy = a.HiredBy
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: Staff/HireManagementDetails/5 (applicationId)
        [Authorize(Roles = "Admin")]
        public ActionResult HireManagementDetails(int applicationId)
        {
            var application = db.JobApplications.Find(applicationId);
            if (application == null)
            {
                TempData["ErrorMessage"] = "Application not found.";
                return RedirectToAction("HireManagement");
            }

            if (application.InterviewResult != "Passed" || !application.IsHired)
            {
                TempData["ErrorMessage"] = "This candidate has not passed the interview yet.";
                return RedirectToAction("HireManagement");
            }

            var candidate = db.Candidates.Find(application.CandidateId);
            var jobPost = db.JobPosts.Find(application.JobPostId);

            if (candidate == null || jobPost == null)
            {
                TempData["ErrorMessage"] = "Candidate or job post not found.";
                return RedirectToAction("HireManagement");
            }

            if (db.Staff.Any(s => s.UserId == candidate.UserId))
            {
                TempData["ErrorMessage"] = $"{candidate.FullName} already has a staff account.";
                return RedirectToAction("HireManagement");
            }

            var viewModel = new HireManagementDetailsViewModel
            {
                ApplicationId = application.Id,
                CandidateId = candidate.Id,
                CandidateFullName = candidate.FullName,
                CandidateEmail = candidate.Email,
                CandidatePhone = candidate.PhoneNumber,
                ProfilePicturePath = candidate.ProfilePicturePath,
                JobPostId = jobPost.Id,
                JobTitle = jobPost.JobTitle,
                Department = jobPost.Department,
                SystemRoleName = jobPost.SystemRole?.Name ?? jobPost.JobTitle
            };

            return View(viewModel);
        }

        // POST: Staff/CreateAccountFromApplication - Use Case 5: Hire Management
        // Fully automated: no form fields are entered by the admin. The
        // candidate's own name/email/phone are reused, the account is put in
        // the role tied to the job post they applied for, a random password is
        // generated, and a welcome email is sent to the candidate.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> CreateAccountFromApplication(int applicationId)
        {
            var application = db.JobApplications.Find(applicationId);
            if (application == null)
            {
                TempData["ErrorMessage"] = "Application not found.";
                return RedirectToAction("HireManagement");
            }

            if (application.InterviewResult != "Passed" || !application.IsHired)
            {
                TempData["ErrorMessage"] = "This candidate has not passed the interview yet.";
                return RedirectToAction("HireManagement");
            }

            var candidate = db.Candidates.Find(application.CandidateId);
            var jobPost = db.JobPosts.Find(application.JobPostId);

            if (candidate == null || jobPost == null)
            {
                TempData["ErrorMessage"] = "Candidate or job post not found.";
                return RedirectToAction("HireManagement");
            }

            if (db.Staff.Any(s => s.UserId == candidate.UserId))
            {
                TempData["ErrorMessage"] = $"{candidate.FullName} already has a staff account.";
                return RedirectToAction("HireManagement");
            }

            var user = await UserManager.FindByIdAsync(candidate.UserId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "This candidate's login account could not be found.";
                return RedirectToAction("HireManagement");
            }

            // The role for the new account is the system role tied to the job
            // post the candidate applied for - never a manual admin choice.
            string roleName = jobPost.SystemRole?.Name ?? jobPost.JobTitle;
            string newPassword = GenerateRandomPassword();

            try
            {
                // Reset the candidate's existing login to a fresh generated
                // password (their email stays the same - it's the same account).
                var removeResult = await UserManager.RemovePasswordAsync(user.Id);
                if (!removeResult.Succeeded && !removeResult.Errors.Any(e => e.Contains("does not have a password")))
                {
                    TempData["ErrorMessage"] = "Could not reset the candidate's password: " + string.Join(", ", removeResult.Errors);
                    return RedirectToAction("HireManagementDetails", new { applicationId });
                }

                var addPasswordResult = await UserManager.AddPasswordAsync(user.Id, newPassword);
                if (!addPasswordResult.Succeeded)
                {
                    TempData["ErrorMessage"] = "Could not set a new password: " + string.Join(", ", addPasswordResult.Errors);
                    return RedirectToAction("HireManagementDetails", new { applicationId });
                }

                // Swap the "Candidate" role for their new staff role.
                if (await UserManager.IsInRoleAsync(user.Id, "Candidate"))
                {
                    await UserManager.RemoveFromRoleAsync(user.Id, "Candidate");
                }

                if (!string.IsNullOrEmpty(roleName) && !await UserManager.IsInRoleAsync(user.Id, roleName))
                {
                    await UserManager.AddToRoleAsync(user.Id, roleName);
                }

                user.IsActive = true;
                await UserManager.UpdateAsync(user);

                var staff = new Staff
                {
                    FirstName = candidate.FirstName,
                    LastName = candidate.LastName,
                    Email = candidate.Email,
                    Role = roleName,
                    PhoneNumber = candidate.PhoneNumber,
                    Department = jobPost.Department,
                    UserId = candidate.UserId,
                    ProfilePicturePath = candidate.ProfilePicturePath,
                    HireDate = DateTime.Now,
                    IsActive = true
                };

                db.Staff.Add(staff);

                application.IsHired = true;
                application.Status = "Hired";
                application.HireDate = DateTime.Now;
                application.HiredBy = User.Identity.Name;

                await db.SaveChangesAsync();

                db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
                {
                    ApplicationId = application.Id,
                    Status = "Hired",
                    Notes = $"Staff login account created for role '{roleName}'. Credentials emailed to {candidate.Email}.",
                    ChangedBy = User.Identity.Name
                });

                await db.SaveChangesAsync();

                // Welcome email with the new login credentials
                SendHireWelcomeEmail(candidate.Email, candidate.FullName, roleName, jobPost.JobTitle, newPassword);

                // In-app notifications
                await CreateNotificationForCandidate(
                    candidate.Id,
                    "Welcome to the Team!",
                    $"Your staff account has been created for the role of '{roleName}'. Your login email is {candidate.Email} - check your inbox for your new password.",
                    "System",
                    "/Staff/Index"
                );

                await CreateNotification(
                    User.Identity.GetUserId(),
                    "Staff Account Created",
                    $"A staff account for '{roleName}' was created for {candidate.FullName} and credentials were emailed to them.",
                    "System",
                    "/Staff/Index"
                );

                TempData["SuccessMessage"] = $"{candidate.FullName}'s staff account has been created and login credentials were emailed to {candidate.Email}.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the account: " + ex.Message;
                return RedirectToAction("HireManagementDetails", new { applicationId });
            }
        }

        // Sends the "you've been hired" email containing the auto-generated
        // password. Reads SMTP settings from web.config appSettings:
        //   SmtpHost, SmtpPort, SmtpUseSsl, SmtpUsername, SmtpPassword, SmtpFromEmail, SmtpFromName
        private void SendHireWelcomeEmail(string toEmail, string candidateName, string roleName, string jobTitle, string password)
        {
            try
            {
                string host = ConfigurationManager.AppSettings["SmtpHost"];
                if (string.IsNullOrEmpty(host))
                {
                    System.Diagnostics.Debug.WriteLine("SMTP not configured - skipping welcome email send.");
                    return;
                }

                int port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
                bool useSsl = bool.Parse(ConfigurationManager.AppSettings["SmtpUseSsl"] ?? "true");
                string smtpUser = ConfigurationManager.AppSettings["SmtpUsername"];
                string smtpPass = ConfigurationManager.AppSettings["SmtpPassword"];
                string fromEmail = ConfigurationManager.AppSettings["SmtpFromEmail"] ?? smtpUser;
                string fromName = ConfigurationManager.AppSettings["SmtpFromName"] ?? "MuhleOP";

                string subject = "Welcome to the Team - Your Staff Account is Ready";
                string body = $@"
                    <div style='font-family:Segoe UI,Arial,sans-serif;max-width:600px;margin:auto;'>
                        <h2 style='color:#F5A623;'>Welcome, {candidateName}!</h2>
                        <p>Congratulations - you have been hired as a <strong>{roleName}</strong>{(string.IsNullOrEmpty(jobTitle) ? "" : $" for the position of <strong>{jobTitle}</strong>")}.</p>
                        <p>A staff account has been created for you. Please use the credentials below to log in:</p>
                        <table style='border-collapse:collapse;margin:16px 0;'>
                            <tr><td style='padding:4px 12px 4px 0;'><strong>Email:</strong></td><td>{toEmail}</td></tr>
                            <tr><td style='padding:4px 12px 4px 0;'><strong>Password:</strong></td><td>{password}</td></tr>
                        </table>
                        <p>For your security, please log in and change your password as soon as possible.</p>
                        <p>We're excited to have you on the team!</p>
                        <p style='color:#888;font-size:12px;margin-top:24px;'>This is an automated message, please do not reply to this email.</p>
                    </div>";

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(fromEmail, fromName);
                    message.To.Add(toEmail);
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
                // Don't let an email failure block the hire - the account was
                // already created successfully. Just log it.
                System.Diagnostics.Debug.WriteLine($"Error sending welcome email: {ex.Message}");
            }
        }

        // POST: Staff/ToggleActive/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ToggleActive(int id)
        {
            var staff = db.Staff.Find(id);
            if (staff == null)
            {
                return Json(new { success = false, message = "Staff member not found" });
            }

            staff.IsActive = !staff.IsActive;

            // Update user status
            var user = await UserManager.FindByIdAsync(staff.UserId);
            if (user != null)
            {
                user.IsActive = staff.IsActive;
                await UserManager.UpdateAsync(user);
            }

            await db.SaveChangesAsync();

            // Notify Admin
            await CreateNotification(
                User.Identity.GetUserId(),
                $"Staff {(staff.IsActive ? "Activated" : "Deactivated")}",
                $"{staff.FullName} has been {(staff.IsActive ? "activated" : "deactivated")}.",
                "System",
                "/Staff/Index"
            );

            return Json(new { success = true, isActive = staff.IsActive });
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            var password = new StringBuilder();
            for (int i = 0; i < 12; i++)
            {
                password.Append(chars[random.Next(chars.Length)]);
            }
            return password.ToString();
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

        private async Task CreateNotificationForCandidate(int candidateId, string title, string message, string type, string link)
        {
            try
            {
                var candidate = db.Candidates.Find(candidateId);
                if (candidate == null) return;

                await CreateNotification(candidate.UserId, title, message, type, link);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating notification for candidate: {ex.Message}");
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