using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MuhleOP.Models;
using MuhleOP.ViewModels;
using System.Collections.Generic;

namespace MuhleOP.Controllers
{
    [AllowAnonymous]
    public class CandidateController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private ApplicationUserManager _userManager;

        public CandidateController()
        {
        }

        public CandidateController(ApplicationUserManager userManager)
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

        // GET: Candidate/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: Candidate/Register - Use Case 2: Candidate Registration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(CandidateRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
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

                    var result = await UserManager.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        var candidate = new Candidate
                        {
                            FirstName = model.FirstName,
                            LastName = model.LastName,
                            Email = model.Email,
                            PhoneNumber = model.PhoneNumber,
                            DateOfBirth = model.DateOfBirth,
                            Gender = model.Gender,
                            Nationality = model.Nationality,
                            CurrentAddress = model.CurrentAddress,
                            UserId = user.Id,
                            RegistrationDate = DateTime.Now,
                            IsActive = true
                        };

                        // Save documents
                        string uploadFolder = Server.MapPath("~/Uploads/Candidates/" + user.Id + "/");
                        if (!Directory.Exists(uploadFolder))
                        {
                            Directory.CreateDirectory(uploadFolder);
                        }

                        if (model.IdDocument != null && model.IdDocument.ContentLength > 0)
                        {
                            string fileName = "ID_" + Guid.NewGuid().ToString() + Path.GetExtension(model.IdDocument.FileName);
                            string filePath = Path.Combine(uploadFolder, fileName);
                            model.IdDocument.SaveAs(filePath);
                            candidate.IdDocumentPath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                        }

                        if (model.Qualifications != null && model.Qualifications.ContentLength > 0)
                        {
                            string fileName = "Qualifications_" + Guid.NewGuid().ToString() + Path.GetExtension(model.Qualifications.FileName);
                            string filePath = Path.Combine(uploadFolder, fileName);
                            model.Qualifications.SaveAs(filePath);
                            candidate.QualificationsPath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                        }

                        if (model.Cv != null && model.Cv.ContentLength > 0)
                        {
                            string fileName = "CV_" + Guid.NewGuid().ToString() + Path.GetExtension(model.Cv.FileName);
                            string filePath = Path.Combine(uploadFolder, fileName);
                            model.Cv.SaveAs(filePath);
                            candidate.CvPath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                        }

                        if (model.License != null && model.License.ContentLength > 0)
                        {
                            string fileName = "License_" + Guid.NewGuid().ToString() + Path.GetExtension(model.License.FileName);
                            string filePath = Path.Combine(uploadFolder, fileName);
                            model.License.SaveAs(filePath);
                            candidate.LicensePath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                        }

                        if (model.ProfilePicture != null && model.ProfilePicture.ContentLength > 0)
                        {
                            string fileName = "Profile_" + Guid.NewGuid().ToString() + Path.GetExtension(model.ProfilePicture.FileName);
                            string filePath = Path.Combine(uploadFolder, fileName);
                            model.ProfilePicture.SaveAs(filePath);
                            candidate.ProfilePicturePath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                        }

                        db.Candidates.Add(candidate);
                        await db.SaveChangesAsync();

                        // Assign Candidate role
                        if (!UserManager.IsInRole(user.Id, "Candidate"))
                        {
                            await UserManager.AddToRoleAsync(user.Id, "Candidate");
                        }

                        await CreateNotification(
                            user.Id,
                            "Welcome to MuhleOps!",
                            $"Welcome {candidate.FullName}! Your account has been successfully created. You can now browse and apply for jobs.",
                            "System",
                            null
                        );

                        await SignInAsync(user, isPersistent: false);

                        TempData["SuccessMessage"] = "Registration successful! Welcome to MuhleOps.";
                        return RedirectToAction("Dashboard", "Home");
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Registration failed: " + ex.Message);
                }
            }

            return View(model);
        }

        // GET: Candidate/BrowseJobs - Use Case 2: Browse Jobs
        [Authorize]
        public ActionResult BrowseJobs()
        {
            try
            {
                var jobs = db.JobPosts
                    .Where(j => j.IsPublished && !j.IsClosed && j.ClosingDate >= DateTime.Now)
                    .OrderByDescending(j => j.DatePosted)
                    .ToList();

                var user = UserManager.FindById(User.Identity.GetUserId());
                var candidate = db.Candidates.FirstOrDefault(c => c.UserId == user.Id);

                var jobList = jobs.Select(j => new JobPostSummaryViewModel
                {
                    Id = j.Id,
                    JobTitle = j.JobTitle,
                    Department = j.Department,
                    Location = j.Location,
                    JobType = j.JobType,
                    ClosingDate = j.ClosingDate,
                    IsPublished = j.IsPublished,
                    IsClosed = j.IsClosed,
                    DaysLeft = (int)(j.ClosingDate - DateTime.Now).TotalDays,
                    ApplicationCount = db.JobApplications.Count(a => a.JobPostId == j.Id),
                    HasApplied = candidate != null && db.JobApplications.Any(a => a.JobPostId == j.Id && a.CandidateId == candidate.Id)
                }).ToList();

                return View(jobList);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading jobs: " + ex.Message;
                return View(new List<JobPostSummaryViewModel>());
            }
        }

        // GET: Candidate/JobDetails/5 - Use Case 2: View Job Details
        [Authorize]
        public ActionResult JobDetails(int id)
        {
            try
            {
                var job = db.JobPosts.Find(id);
                if (job == null || !job.IsPublished || job.IsClosed)
                {
                    return HttpNotFound();
                }

                var user = UserManager.FindById(User.Identity.GetUserId());
                var candidate = db.Candidates.FirstOrDefault(c => c.UserId == user.Id);
                bool hasApplied = false;
                if (candidate != null)
                {
                    hasApplied = db.JobApplications.Any(a => a.JobPostId == id && a.CandidateId == candidate.Id);
                }

                var viewModel = new ApplyJobViewModel
                {
                    JobPostId = job.Id,
                    JobTitle = job.JobTitle,
                    JobDescription = job.JobDescription,
                    Requirements = job.Requirements,
                    Location = job.Location,
                    JobType = job.JobType,
                    SalaryRange = job.SalaryRange,
                    ClosingDate = job.ClosingDate,
                    HasApplied = hasApplied
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading job details: " + ex.Message;
                return RedirectToAction("BrowseJobs");
            }
        }

        // GET: Candidate/Apply/5 - Use Case 2: Apply for Job
        [Authorize]
        public ActionResult Apply(int id)
        {
            try
            {
                var job = db.JobPosts.Find(id);
                if (job == null || !job.IsPublished || job.IsClosed || job.ClosingDate < DateTime.Now)
                {
                    TempData["ErrorMessage"] = "This job is no longer available.";
                    return RedirectToAction("BrowseJobs");
                }

                var user = UserManager.FindById(User.Identity.GetUserId());
                var candidate = db.Candidates.FirstOrDefault(c => c.UserId == user.Id);

                if (candidate == null)
                {
                    return RedirectToAction("Register");
                }

                // Check if already applied
                var existingApplication = db.JobApplications
                    .FirstOrDefault(a => a.JobPostId == id && a.CandidateId == candidate.Id);

                if (existingApplication != null)
                {
                    TempData["ErrorMessage"] = "You have already applied for this position.";
                    return RedirectToAction("BrowseJobs");
                }

                var viewModel = new ApplyJobViewModel
                {
                    JobPostId = job.Id,
                    JobTitle = job.JobTitle,
                    JobDescription = job.JobDescription,
                    Requirements = job.Requirements,
                    Location = job.Location,
                    JobType = job.JobType,
                    SalaryRange = job.SalaryRange,
                    ClosingDate = job.ClosingDate,
                    HasExistingDocuments = true,
                    HasApplied = false,
                    CoverLetter = "Dear Hiring Manager,\n\nI am writing to apply for the " + job.JobTitle + " position. I am very interested in this opportunity and believe my skills and experience make me a strong candidate for this role.\n\nI look forward to hearing from you.\n\nSincerely,\n" + candidate.FullName
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading application: " + ex.Message;
                return RedirectToAction("BrowseJobs");
            }
        }

        // POST: Candidate/Apply - Use Case 2: Submit Job Application
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> Apply(ApplyJobViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = UserManager.FindById(User.Identity.GetUserId());
                    var candidate = db.Candidates.FirstOrDefault(c => c.UserId == user.Id);

                    if (candidate == null)
                    {
                        return RedirectToAction("Register");
                    }

                    var job = db.JobPosts.Find(model.JobPostId);

                    if (job == null || !job.IsPublished || job.IsClosed)
                    {
                        TempData["ErrorMessage"] = "Job not found or no longer available.";
                        return RedirectToAction("BrowseJobs");
                    }

                    // Check for duplicate application
                    var existingApplication = db.JobApplications
                        .FirstOrDefault(a => a.JobPostId == model.JobPostId && a.CandidateId == candidate.Id);

                    if (existingApplication != null)
                    {
                        TempData["ErrorMessage"] = "You have already applied for this position.";
                        return RedirectToAction("BrowseJobs");
                    }

                    // Update documents if provided
                    string uploadFolder = Server.MapPath("~/Uploads/Candidates/" + user.Id + "/");
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    if (model.UpdatedCv != null && model.UpdatedCv.ContentLength > 0)
                    {
                        string fileName = "CV_" + Guid.NewGuid().ToString() + Path.GetExtension(model.UpdatedCv.FileName);
                        string filePath = Path.Combine(uploadFolder, fileName);
                        model.UpdatedCv.SaveAs(filePath);
                        candidate.CvPath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                    }

                    if (model.UpdatedQualifications != null && model.UpdatedQualifications.ContentLength > 0)
                    {
                        string fileName = "Qualifications_" + Guid.NewGuid().ToString() + Path.GetExtension(model.UpdatedQualifications.FileName);
                        string filePath = Path.Combine(uploadFolder, fileName);
                        model.UpdatedQualifications.SaveAs(filePath);
                        candidate.QualificationsPath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                    }

                    if (model.UpdatedIdDocument != null && model.UpdatedIdDocument.ContentLength > 0)
                    {
                        string fileName = "ID_" + Guid.NewGuid().ToString() + Path.GetExtension(model.UpdatedIdDocument.FileName);
                        string filePath = Path.Combine(uploadFolder, fileName);
                        model.UpdatedIdDocument.SaveAs(filePath);
                        candidate.IdDocumentPath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                    }

                    if (model.UpdatedLicense != null && model.UpdatedLicense.ContentLength > 0)
                    {
                        string fileName = "License_" + Guid.NewGuid().ToString() + Path.GetExtension(model.UpdatedLicense.FileName);
                        string filePath = Path.Combine(uploadFolder, fileName);
                        model.UpdatedLicense.SaveAs(filePath);
                        candidate.LicensePath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                    }

                    // Create application
                    var application = new JobApplication
                    {
                        JobPostId = model.JobPostId,
                        CandidateId = candidate.Id,
                        CoverLetter = model.CoverLetter,
                        ApplicationDate = DateTime.Now,
                        Status = "Pending"
                    };

                    db.JobApplications.Add(application);
                    await db.SaveChangesAsync();

                    // Create status history
                    var statusHistory = new ApplicationStatusHistory
                    {
                        ApplicationId = application.Id,
                        Status = "Pending",
                        Notes = "Application submitted successfully",
                        ChangedBy = candidate.FullName
                    };
                    db.ApplicationStatusHistories.Add(statusHistory);
                    await db.SaveChangesAsync();

                    // Create notification for candidate
                    await CreateNotification(
                        user.Id,
                        "Application Submitted!",
                        $"Your application for '{job.JobTitle}' has been submitted successfully.",
                        "Application",
                        $"/Candidate/ApplicationStatus/{application.Id}"
                    );

                    // Notify Admin - FIXED: Get admin users properly
                    var adminUsers = UserManager.Users.ToList();
                    var adminUserIds = new List<string>();
                    foreach (var adminUser in adminUsers)
                    {
                        if (UserManager.IsInRole(adminUser.Id, "Admin"))
                        {
                            adminUserIds.Add(adminUser.Id);
                        }
                    }

                    foreach (var adminId in adminUserIds)
                    {
                        await CreateNotification(
                            adminId,
                            "New Application Received",
                            $"{candidate.FullName} has applied for '{job.JobTitle}'",
                            "Application",
                            $"/JobPost/ReviewApplications/{job.Id}"
                        );
                    }

                    TempData["ApplicationSuccess"] = "Your application has been submitted successfully!";
                    TempData["JobTitle"] = job.JobTitle;
                    TempData["ApplicationId"] = application.Id;
                    TempData["CompanyName"] = "Muhle Building";

                    return RedirectToAction("ApplicationSuccess");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error submitting application: " + ex.Message);
                }
            }

            return View(model);
        }

        // GET: Candidate/ApplicationSuccess
        [Authorize]
        public ActionResult ApplicationSuccess()
        {
            if (TempData["ApplicationSuccess"] == null)
            {
                return RedirectToAction("Dashboard", "Home");
            }

            ViewBag.SuccessMessage = TempData["ApplicationSuccess"];
            ViewBag.JobTitle = TempData["JobTitle"];
            ViewBag.ApplicationId = TempData["ApplicationId"];
            ViewBag.CompanyName = TempData["CompanyName"];

            return View();
        }

        // GET: Candidate/AllApplications - Use Case 2: View All Applications
        [Authorize]
        public ActionResult AllApplications()
        {
            try
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var candidate = db.Candidates.FirstOrDefault(c => c.UserId == user.Id);
                if (candidate == null)
                {
                    return RedirectToAction("Register");
                }

                var applications = db.JobApplications
                    .Where(a => a.CandidateId == candidate.Id)
                    .OrderByDescending(a => a.ApplicationDate)
                    .ToList();

                var viewModels = applications.Select(app => new ApplicationStatusViewModel
                {
                    ApplicationId = app.Id,
                    JobTitle = app.JobPost?.JobTitle ?? "Unknown",
                    Status = app.Status,
                    ApplicationDate = app.ApplicationDate,
                    IsShortlisted = app.IsShortlisted,
                    InterviewScheduled = app.InterviewScheduled,
                    InterviewDateTime = app.InterviewDateTime,
                    InterviewResult = app.InterviewResult ?? "Pending",
                    IsHired = app.IsHired,
                    IsInterviewCompleted = app.InterviewResult != "Pending",
                    RankingScore = app.RankingScore,
                    SkillsMatchPercentage = app.SkillsMatchPercentage,
                    StatusHistory = new List<ApplicationStatusHistoryViewModel>()
                }).ToList();

                return View(viewModels);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading applications: " + ex.Message;
                return View(new List<ApplicationStatusViewModel>());
            }
        }

        // GET: Candidate/ApplicationStatus/5 - Use Case 2: View Application Status
        [Authorize]
        public ActionResult ApplicationStatus(int? id)
        {
            try
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var candidate = db.Candidates.FirstOrDefault(c => c.UserId == user.Id);
                if (candidate == null)
                {
                    return RedirectToAction("Register");
                }

                if (!id.HasValue)
                {
                    return RedirectToAction("AllApplications");
                }

                var application = db.JobApplications.Find(id.Value);
                if (application == null)
                {
                    TempData["ErrorMessage"] = "Application not found.";
                    return RedirectToAction("AllApplications");
                }

                if (application.CandidateId != candidate.Id)
                {
                    TempData["ErrorMessage"] = "You do not have permission to view this application.";
                    return RedirectToAction("AllApplications");
                }

                var job = db.JobPosts.Find(application.JobPostId);

                var statusHistory = db.ApplicationStatusHistories
                    .Where(h => h.ApplicationId == application.Id)
                    .OrderByDescending(h => h.ChangeDate)
                    .Select(h => new ApplicationStatusHistoryViewModel
                    {
                        Status = h.Status,
                        Notes = h.Notes,
                        ChangedBy = h.ChangedBy,
                        ChangeDate = h.ChangeDate
                    })
                    .ToList();

                var viewModel = new ApplicationStatusViewModel
                {
                    ApplicationId = application.Id,
                    JobTitle = job?.JobTitle ?? "Unknown",
                    Status = application.Status,
                    ApplicationDate = application.ApplicationDate,
                    CoverLetter = application.CoverLetter,
                    IsShortlisted = application.IsShortlisted,
                    InterviewScheduled = application.InterviewScheduled,
                    InterviewDateTime = application.InterviewDateTime,
                    InterviewNotes = application.InterviewNotes,
                    InterviewResult = application.InterviewResult ?? "Pending",
                    InterviewFeedback = application.InterviewFeedback,
                    IsHired = application.IsHired,
                    IsInterviewCompleted = application.InterviewResult != "Pending",
                    RankingScore = application.RankingScore,
                    SkillsMatchPercentage = application.SkillsMatchPercentage,
                    StatusHistory = statusHistory
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading application status: " + ex.Message;
                return RedirectToAction("AllApplications");
            }
        }

        // GET: Candidate/GetNotifications
        [Authorize]
        public JsonResult GetNotifications()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "User not authenticated" }, JsonRequestBehavior.AllowGet);
                }

                var userId = User.Identity.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Invalid user ID" }, JsonRequestBehavior.AllowGet);
                }

                var notifications = db.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(10)
                    .ToList();

                var formattedNotifications = notifications.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    type = n.Type,
                    isRead = n.IsRead,
                    link = n.Link,
                    createdDate = n.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
                    timeAgo = GetTimeAgo(n.CreatedDate)
                }).ToList();

                var unreadCount = db.Notifications.Count(n => n.UserId == userId && !n.IsRead);

                return Json(new { success = true, notifications = formattedNotifications, unreadCount }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Candidate/MarkNotificationRead
        [HttpPost]
        [Authorize]
        public async Task<JsonResult> MarkNotificationRead(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var userId = User.Identity.GetUserId();
                var notification = db.Notifications.FirstOrDefault(n => n.Id == id && n.UserId == userId);

                if (notification != null)
                {
                    notification.IsRead = true;
                    notification.ReadDate = DateTime.Now;
                    await db.SaveChangesAsync();
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Notification not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Candidate/MarkAllNotificationsRead
        [HttpPost]
        [Authorize]
        public async Task<JsonResult> MarkAllNotificationsRead()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var userId = User.Identity.GetUserId();
                var notifications = db.Notifications.Where(n => n.UserId == userId && !n.IsRead);
                var count = notifications.Count();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                    notification.ReadDate = DateTime.Now;
                }

                await db.SaveChangesAsync();
                return Json(new { success = true, count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Candidate/Dashboard
        [Authorize]
        public ActionResult Dashboard()
        {
            return RedirectToAction("Dashboard", "Home");
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

        private string GetTimeAgo(DateTime date)
        {
            var timeSpan = DateTime.Now - date;
            if (timeSpan.TotalSeconds < 60)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{Math.Floor(timeSpan.TotalMinutes)} minutes ago";
            if (timeSpan.TotalHours < 24)
                return $"{Math.Floor(timeSpan.TotalHours)} hours ago";
            if (timeSpan.TotalDays < 7)
                return $"{Math.Floor(timeSpan.TotalDays)} days ago";
            return date.ToString("yyyy-MM-dd");
        }

        private async Task SignInAsync(ApplicationUser user, bool isPersistent)
        {
            var authenticationManager = HttpContext.GetOwinContext().Authentication;
            var identity = await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
            authenticationManager.SignIn(new Microsoft.Owin.Security.AuthenticationProperties { IsPersistent = isPersistent }, identity);
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