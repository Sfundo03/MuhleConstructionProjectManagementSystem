using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MuhleOP.Models;
using MuhleOP.ViewModels;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MuhleOP.Controllers
{
    [AllowAnonymous]
    public class CandidateController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private ApplicationUserManager _userManager;

        public CandidateController()
        {
            System.Diagnostics.Debug.WriteLine("========== CandidateController Constructor Called ==========");
        }

        public CandidateController(ApplicationUserManager userManager)
        {
            System.Diagnostics.Debug.WriteLine("========== CandidateController Constructor (with UserManager) Called ==========");
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
            System.Diagnostics.Debug.WriteLine(">>> Candidate/Register GET called");
            return View();
        }

        // POST: Candidate/Register - Use Case 2: Candidate Registration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(CandidateRegisterViewModel model)
        {
            System.Diagnostics.Debug.WriteLine(">>> Candidate/Register POST called");

            if (ModelState.IsValid)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(">>> ModelState is valid, creating user...");

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
                        System.Diagnostics.Debug.WriteLine($">>> User created successfully: {user.Id}");

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
                            System.Diagnostics.Debug.WriteLine($">>> Created upload folder: {uploadFolder}");
                        }

                        if (model.IdDocument != null && model.IdDocument.ContentLength > 0)
                        {
                            string fileName = "ID_" + Guid.NewGuid().ToString() + Path.GetExtension(model.IdDocument.FileName);
                            string filePath = Path.Combine(uploadFolder, fileName);
                            model.IdDocument.SaveAs(filePath);
                            candidate.IdDocumentPath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                            System.Diagnostics.Debug.WriteLine($">>> Saved ID document: {fileName}");
                        }

                        if (model.Qualifications != null && model.Qualifications.ContentLength > 0)
                        {
                            string fileName = "Qualifications_" + Guid.NewGuid().ToString() + Path.GetExtension(model.Qualifications.FileName);
                            string filePath = Path.Combine(uploadFolder, fileName);
                            model.Qualifications.SaveAs(filePath);
                            candidate.QualificationsPath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                            System.Diagnostics.Debug.WriteLine($">>> Saved Qualifications document: {fileName}");
                        }

                        if (model.Cv != null && model.Cv.ContentLength > 0)
                        {
                            string fileName = "CV_" + Guid.NewGuid().ToString() + Path.GetExtension(model.Cv.FileName);
                            string filePath = Path.Combine(uploadFolder, fileName);
                            model.Cv.SaveAs(filePath);
                            candidate.CvPath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                            System.Diagnostics.Debug.WriteLine($">>> Saved CV: {fileName}");
                        }

                        if (model.License != null && model.License.ContentLength > 0)
                        {
                            string fileName = "License_" + Guid.NewGuid().ToString() + Path.GetExtension(model.License.FileName);
                            string filePath = Path.Combine(uploadFolder, fileName);
                            model.License.SaveAs(filePath);
                            candidate.LicensePath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                            System.Diagnostics.Debug.WriteLine($">>> Saved License: {fileName}");
                        }

                        if (model.ProfilePicture != null && model.ProfilePicture.ContentLength > 0)
                        {
                            string fileName = "Profile_" + Guid.NewGuid().ToString() + Path.GetExtension(model.ProfilePicture.FileName);
                            string filePath = Path.Combine(uploadFolder, fileName);
                            model.ProfilePicture.SaveAs(filePath);
                            candidate.ProfilePicturePath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                            System.Diagnostics.Debug.WriteLine($">>> Saved Profile Picture: {fileName}");
                        }

                        db.Candidates.Add(candidate);
                        await db.SaveChangesAsync();
                        System.Diagnostics.Debug.WriteLine($">>> Candidate saved with ID: {candidate.Id}");

                        // Assign Candidate role
                        if (!UserManager.IsInRole(user.Id, "Candidate"))
                        {
                            await UserManager.AddToRoleAsync(user.Id, "Candidate");
                            System.Diagnostics.Debug.WriteLine($">>> Assigned Candidate role to user: {user.Id}");
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
                        System.Diagnostics.Debug.WriteLine($">>> Registration error: {error}");
                        ModelState.AddModelError("", error);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($">>> Registration EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                    ModelState.AddModelError("", "Registration failed: " + ex.Message);
                }
            }

            return View(model);
        }

        // GET: Candidate/BrowseJobs - Use Case 2: Browse Jobs
        [Authorize]
        public ActionResult BrowseJobs()
        {
            System.Diagnostics.Debug.WriteLine(">>> Candidate/BrowseJobs called");

            try
            {
                var jobs = db.JobPosts
                    .Where(j => j.IsPublished && !j.IsClosed && j.ClosingDate >= DateTime.Now)
                    .OrderByDescending(j => j.DatePosted)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($">>> Found {jobs.Count} available jobs");

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
                System.Diagnostics.Debug.WriteLine($">>> BrowseJobs EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading jobs: " + ex.Message;
                return View(new List<JobPostSummaryViewModel>());
            }
        }

        // GET: Candidate/JobDetails/5 - Use Case 2: View Job Details
        [Authorize]
        public ActionResult JobDetails(int id)
        {
            System.Diagnostics.Debug.WriteLine($">>> Candidate/JobDetails called with id: {id}");

            try
            {
                var job = db.JobPosts.Find(id);
                if (job == null || !job.IsPublished || job.IsClosed)
                {
                    System.Diagnostics.Debug.WriteLine($">>> Job not found or not available: Id={id}, Published={job?.IsPublished}, Closed={job?.IsClosed}");
                    return HttpNotFound();
                }

                var user = UserManager.FindById(User.Identity.GetUserId());
                var candidate = db.Candidates.FirstOrDefault(c => c.UserId == user.Id);
                bool hasApplied = false;
                if (candidate != null)
                {
                    hasApplied = db.JobApplications.Any(a => a.JobPostId == id && a.CandidateId == candidate.Id);
                    System.Diagnostics.Debug.WriteLine($">>> Candidate has applied: {hasApplied}");
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
                System.Diagnostics.Debug.WriteLine($">>> JobDetails EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading job details: " + ex.Message;
                return RedirectToAction("BrowseJobs");
            }
        }

        // GET: Candidate/Apply/5 - Use Case 2: Apply for Job
        [Authorize]
        public ActionResult Apply(int id)
        {
            System.Diagnostics.Debug.WriteLine($">>> Candidate/Apply called with id: {id}");

            try
            {
                var job = db.JobPosts.Find(id);
                if (job == null || !job.IsPublished || job.IsClosed || job.ClosingDate < DateTime.Now)
                {
                    System.Diagnostics.Debug.WriteLine($">>> Job not available for application");
                    TempData["ErrorMessage"] = "This job is no longer available.";
                    return RedirectToAction("BrowseJobs");
                }

                var user = UserManager.FindById(User.Identity.GetUserId());
                var candidate = db.Candidates.FirstOrDefault(c => c.UserId == user.Id);

                if (candidate == null)
                {
                    System.Diagnostics.Debug.WriteLine($">>> Candidate not found, redirecting to Register");
                    return RedirectToAction("Register");
                }

                // Check if already applied
                var existingApplication = db.JobApplications
                    .FirstOrDefault(a => a.JobPostId == id && a.CandidateId == candidate.Id);

                if (existingApplication != null)
                {
                    System.Diagnostics.Debug.WriteLine($">>> Candidate already applied for this job");
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
                System.Diagnostics.Debug.WriteLine($">>> Apply EXCEPTION: {ex.Message}\n{ex.StackTrace}");
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
            System.Diagnostics.Debug.WriteLine($">>> Candidate/Apply POST called for JobPostId: {model.JobPostId}");

            if (ModelState.IsValid)
            {
                try
                {
                    var user = UserManager.FindById(User.Identity.GetUserId());
                    var candidate = db.Candidates.FirstOrDefault(c => c.UserId == user.Id);

                    if (candidate == null)
                    {
                        System.Diagnostics.Debug.WriteLine($">>> Candidate not found");
                        return RedirectToAction("Register");
                    }

                    var job = db.JobPosts.Find(model.JobPostId);

                    if (job == null || !job.IsPublished || job.IsClosed)
                    {
                        System.Diagnostics.Debug.WriteLine($">>> Job not available");
                        TempData["ErrorMessage"] = "Job not found or no longer available.";
                        return RedirectToAction("BrowseJobs");
                    }

                    // Check for duplicate application
                    var existingApplication = db.JobApplications
                        .FirstOrDefault(a => a.JobPostId == model.JobPostId && a.CandidateId == candidate.Id);

                    if (existingApplication != null)
                    {
                        System.Diagnostics.Debug.WriteLine($">>> Duplicate application detected");
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
                    System.Diagnostics.Debug.WriteLine($">>> Application created with ID: {application.Id}");

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

                    // Notify Admin
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
                    System.Diagnostics.Debug.WriteLine($">>> Apply POST EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                    ModelState.AddModelError("", "Error submitting application: " + ex.Message);
                }
            }

            return View(model);
        }

        // GET: Candidate/ApplicationSuccess
        [Authorize]
        public ActionResult ApplicationSuccess()
        {
            System.Diagnostics.Debug.WriteLine(">>> Candidate/ApplicationSuccess called");

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
            System.Diagnostics.Debug.WriteLine(">>> Candidate/AllApplications called");

            try
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine(">>> User not found");
                    return RedirectToAction("Login", "Account");
                }

                var candidate = db.Candidates.FirstOrDefault(c => c.UserId == user.Id);
                if (candidate == null)
                {
                    System.Diagnostics.Debug.WriteLine(">>> Candidate not found");
                    return RedirectToAction("Register");
                }

                var applications = db.JobApplications
                    .Where(a => a.CandidateId == candidate.Id)
                    .OrderByDescending(a => a.ApplicationDate)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($">>> Found {applications.Count} applications for candidate");

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
                    QuizScore = app.QuizScore,
                    QuizPassed = app.QuizPassed,
                    QuizCompleted = app.QuizCompletedDate.HasValue,
                    QuizCompletedDate = app.QuizCompletedDate,
                    StatusHistory = new List<ApplicationStatusHistoryViewModel>()
                }).ToList();

                return View(viewModels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> AllApplications EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading applications: " + ex.Message;
                return View(new List<ApplicationStatusViewModel>());
            }
        }

        // GET: Candidate/ApplicationStatus/5 - Use Case 2: View Application Status
        [Authorize]
        public ActionResult ApplicationStatus(int? id)
        {
            System.Diagnostics.Debug.WriteLine($">>> Candidate/ApplicationStatus called with id: {id}");

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
                    QuizScore = application.QuizScore,
                    QuizPassed = application.QuizPassed,
                    QuizCompleted = application.QuizCompletedDate.HasValue,
                    QuizCompletedDate = application.QuizCompletedDate,
                    StatusHistory = statusHistory
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> ApplicationStatus EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading application status: " + ex.Message;
                return RedirectToAction("AllApplications");
            }
        }

        // GET: Candidate/GetNotifications
        [Authorize]
        public JsonResult GetNotifications()
        {
            System.Diagnostics.Debug.WriteLine(">>> Candidate/GetNotifications called");

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
                System.Diagnostics.Debug.WriteLine($">>> GetNotifications EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Candidate/MarkNotificationRead
        [HttpPost]
        [Authorize]
        public async Task<JsonResult> MarkNotificationRead(int id)
        {
            System.Diagnostics.Debug.WriteLine($">>> Candidate/MarkNotificationRead called with id: {id}");

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
                System.Diagnostics.Debug.WriteLine($">>> MarkNotificationRead EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Candidate/MarkAllNotificationsRead
        [HttpPost]
        [Authorize]
        public async Task<JsonResult> MarkAllNotificationsRead()
        {
            System.Diagnostics.Debug.WriteLine(">>> Candidate/MarkAllNotificationsRead called");

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
                System.Diagnostics.Debug.WriteLine($">>> MarkAllNotificationsRead EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Candidate/Dashboard
        [Authorize]
        public ActionResult Dashboard()
        {
            System.Diagnostics.Debug.WriteLine(">>> Candidate/Dashboard called - redirecting to Home/Dashboard");
            return RedirectToAction("Dashboard", "Home");
        }

        // ===== QUIZ METHODS =====

        // GET: Candidate/MyQuizzes
        [Authorize]
        public ActionResult MyQuizzes()
        {
            System.Diagnostics.Debug.WriteLine(">>> Candidate/MyQuizzes called");

            try
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine(">>> User not found");
                    return RedirectToAction("Login", "Account");
                }

                var candidate = db.Candidates.FirstOrDefault(c => c.UserId == user.Id);
                if (candidate == null)
                {
                    System.Diagnostics.Debug.WriteLine(">>> Candidate not found");
                    return RedirectToAction("Register", "Candidate");
                }

                System.Diagnostics.Debug.WriteLine($">>> Candidate found: {candidate.FullName} (ID: {candidate.Id})");

                // Get all applications with quiz attempts
                var applications = db.JobApplications
                    .Where(a => a.CandidateId == candidate.Id)
                    .OrderByDescending(a => a.ApplicationDate)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($">>> Found {applications.Count} applications for candidate");

                var quizAttempts = new List<QuizAttemptViewModel>();
                foreach (var app in applications)
                {
                    System.Diagnostics.Debug.WriteLine($">>> Processing Application ID: {app.Id}, JobPostId: {app.JobPostId}, Status: {app.Status}");

                    var attempts = db.QuizAttempts
                        .Where(qa => qa.ApplicationId == app.Id)
                        .OrderByDescending(qa => qa.StartTime)
                        .ToList();

                    System.Diagnostics.Debug.WriteLine($">>> Found {attempts.Count} quiz attempts for application {app.Id}");

                    foreach (var attempt in attempts)
                    {
                        var quiz = db.Quizzes.Find(attempt.QuizId);
                        var jobPost = db.JobPosts.Find(app.JobPostId);
                        quizAttempts.Add(new QuizAttemptViewModel
                        {
                            AttemptId = attempt.Id,
                            QuizId = attempt.QuizId,
                            QuizTitle = quiz?.Title ?? "Unknown Quiz",
                            ApplicationId = app.Id,
                            JobTitle = jobPost?.JobTitle ?? "Unknown Position",
                            StartTime = attempt.StartTime,
                            EndTime = attempt.EndTime,
                            Score = attempt.Score,
                            Passed = attempt.Passed,
                            Status = attempt.Status
                        });

                        System.Diagnostics.Debug.WriteLine($">>> Added attempt: QuizId={attempt.QuizId}, Status={attempt.Status}, Score={attempt.Score}");
                    }

                    // If this application has never had a quiz attempt, but the candidate
                    // was invited to one (interview scheduled + quiz created for the job
                    // post, and not already completed), surface it so they can start it.
                    if (!attempts.Any() && app.InterviewScheduled && !app.QuizCompletedDate.HasValue)
                    {
                        System.Diagnostics.Debug.WriteLine($">>> No attempts found but interview is scheduled for application {app.Id}");

                        var invitedQuiz = db.Quizzes.FirstOrDefault(q => q.JobPostId == app.JobPostId && q.IsActive);
                        if (invitedQuiz != null)
                        {
                            System.Diagnostics.Debug.WriteLine($">>> Found active quiz for JobPostId {app.JobPostId}: QuizId={invitedQuiz.Id}, Title={invitedQuiz.Title}");

                            var jobPost = db.JobPosts.Find(app.JobPostId);
                            quizAttempts.Add(new QuizAttemptViewModel
                            {
                                AttemptId = 0,
                                QuizId = invitedQuiz.Id,
                                QuizTitle = invitedQuiz.Title,
                                ApplicationId = app.Id,
                                JobTitle = jobPost?.JobTitle ?? "Unknown Position",
                                StartTime = DateTime.MinValue,
                                EndTime = null,
                                Score = null,
                                Passed = null,
                                Status = "Available"
                            });

                            System.Diagnostics.Debug.WriteLine($">>> Added available quiz: {invitedQuiz.Title}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($">>> No active quiz found for JobPostId {app.JobPostId}");
                        }
                    }
                }

                // Show newest / most actionable items first
                quizAttempts = quizAttempts
                    .OrderBy(qa => qa.Status == "Available" ? 0 : qa.Status == "InProgress" ? 1 : 2)
                    .ThenByDescending(qa => qa.StartTime)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($">>> Total quiz attempts displayed: {quizAttempts.Count}");
                ViewBag.HasAttempts = quizAttempts.Any();
                return View(quizAttempts);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> MyQuizzes EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading quizzes: " + ex.Message;
                return View(new List<QuizAttemptViewModel>());
            }
        }

        // ===== DEBUG METHODS =====

        // GET: Candidate/DebugTakeQuiz/5
        // Read-only diagnostic - does NOT create any attempt or quiz.
        [Authorize]
        public JsonResult DebugTakeQuiz(int id)
        {
            System.Diagnostics.Debug.WriteLine($"========== DEBUG: DebugTakeQuiz called with ApplicationId: {id} ==========");

            try
            {
                var userId = User.Identity.GetUserId();
                System.Diagnostics.Debug.WriteLine($">>> DebugTakeQuiz: UserId = {userId}");

                var user = UserManager.FindById(userId);
                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine(">>> DebugTakeQuiz: User not found");
                    return Json(new
                    {
                        success = false,
                        step = "identity",
                        message = "Could not resolve the logged-in ApplicationUser from User.Identity."
                    }, JsonRequestBehavior.AllowGet);
                }
                System.Diagnostics.Debug.WriteLine($">>> DebugTakeQuiz: User found: {user.UserName}");

                var candidate = db.Candidates.FirstOrDefault(c => c.UserId == user.Id);
                if (candidate == null)
                {
                    System.Diagnostics.Debug.WriteLine(">>> DebugTakeQuiz: Candidate not found");
                    return Json(new
                    {
                        success = false,
                        step = "candidate",
                        message = "No Candidate record is linked to this logged-in user.",
                        userId = user.Id,
                        userName = user.UserName
                    }, JsonRequestBehavior.AllowGet);
                }
                System.Diagnostics.Debug.WriteLine($">>> DebugTakeQuiz: Candidate found: {candidate.FullName} (ID: {candidate.Id})");

                var application = db.JobApplications.Find(id);
                if (application == null)
                {
                    System.Diagnostics.Debug.WriteLine($">>> DebugTakeQuiz: Application not found for ID: {id}");
                    return Json(new
                    {
                        success = false,
                        step = "application",
                        message = "No JobApplication exists with Id = " + id,
                        requestedApplicationId = id
                    }, JsonRequestBehavior.AllowGet);
                }
                System.Diagnostics.Debug.WriteLine($">>> DebugTakeQuiz: Application found: ID={application.Id}, JobPostId={application.JobPostId}, Status={application.Status}");

                if (application.CandidateId != candidate.Id)
                {
                    System.Diagnostics.Debug.WriteLine($">>> DebugTakeQuiz: Application belongs to different candidate. AppCandidateId={application.CandidateId}, LoggedInCandidateId={candidate.Id}");
                    return Json(new
                    {
                        success = false,
                        step = "ownership",
                        message = "This application belongs to a different candidate.",
                        applicationCandidateId = application.CandidateId,
                        loggedInCandidateId = candidate.Id
                    }, JsonRequestBehavior.AllowGet);
                }

                var jobPost = db.JobPosts.Find(application.JobPostId);
                if (jobPost == null)
                {
                    System.Diagnostics.Debug.WriteLine($">>> DebugTakeQuiz: JobPost not found for JobPostId: {application.JobPostId}");
                    return Json(new
                    {
                        success = false,
                        step = "jobpost",
                        message = "JobPost not found for JobPostId = " + application.JobPostId
                    }, JsonRequestBehavior.AllowGet);
                }
                System.Diagnostics.Debug.WriteLine($">>> DebugTakeQuiz: JobPost found: {jobPost.JobTitle} (ID: {jobPost.Id})");

                // Get all quizzes for this job post with actual question counts
                System.Diagnostics.Debug.WriteLine($">>> DebugTakeQuiz: Checking quizzes for JobPostId: {jobPost.Id}");

                var allQuizzesForJobPost = db.Quizzes
                    .Where(q => q.JobPostId == jobPost.Id)
                    .Select(q => new
                    {
                        q.Id,
                        q.Title,
                        q.IsActive,
                        q.NumberOfQuestions,
                        ActualQuestionCountInDb = db.QuizQuestions.Count(qq => qq.QuizId == q.Id)
                    })
                    .ToList();

                System.Diagnostics.Debug.WriteLine($">>> DebugTakeQuiz: Found {allQuizzesForJobPost.Count} quizzes for this job post");
                foreach (var q in allQuizzesForJobPost)
                {
                    System.Diagnostics.Debug.WriteLine($">>>   Quiz ID: {q.Id}, Title: {q.Title}, IsActive: {q.IsActive}, Questions: {q.ActualQuestionCountInDb}");
                }

                var activeQuiz = allQuizzesForJobPost.FirstOrDefault(q => q.IsActive);
                System.Diagnostics.Debug.WriteLine($">>> DebugTakeQuiz: Active quiz: {(activeQuiz != null ? $"ID={activeQuiz.Id}, Questions={activeQuiz.ActualQuestionCountInDb}" : "None")}");

                // Get existing attempts
                System.Diagnostics.Debug.WriteLine($">>> DebugTakeQuiz: Checking attempts for ApplicationId: {application.Id}");

                var existingAttempts = db.QuizAttempts
                    .Where(a => a.ApplicationId == application.Id)
                    .OrderByDescending(a => a.StartTime)
                    .Select(a => new
                    {
                        a.Id,
                        a.QuizId,
                        a.Status,
                        a.Score,
                        a.Passed,
                        a.StartTime,
                        a.EndTime
                    })
                    .ToList();

                System.Diagnostics.Debug.WriteLine($">>> DebugTakeQuiz: Found {existingAttempts.Count} attempts");
                foreach (var a in existingAttempts)
                {
                    System.Diagnostics.Debug.WriteLine($">>>   Attempt ID: {a.Id}, QuizId: {a.QuizId}, Status: {a.Status}, Score: {a.Score}");
                }

                // Generate diagnosis
                string diagnosis;
                string recommendedAction;

                if (allQuizzesForJobPost.Count == 0)
                {
                    diagnosis = "No Quiz row exists at all for this JobPost. TakeQuiz should auto-create one via CreateAutoQuizForCandidate.";
                    recommendedAction = "Run DebugFixQuiz or reschedule the interview to trigger quiz creation.";
                }
                else if (activeQuiz == null)
                {
                    diagnosis = "Quiz row(s) exist for this JobPost, but none has IsActive = true.";
                    recommendedAction = "Run SQL: UPDATE Quizzes SET IsActive = 1 WHERE JobPostId = " + jobPost.Id + " OR delete stale quiz rows.";
                }
                else if (activeQuiz.ActualQuestionCountInDb == 0)
                {
                    diagnosis = "Active quiz found (Id=" + activeQuiz.Id + ") but it has 0 rows in QuizQuestions.";
                    recommendedAction = "Go to Manage Quiz for this job post and click 'Generate Questions', then Save/Update. Or run DebugFixQuiz.";
                }
                else if (existingAttempts.Any(a => a.Status == "Completed"))
                {
                    diagnosis = "This application already has a Completed attempt, so TakeQuiz redirects straight to QuizResult.";
                    recommendedAction = "This is expected behavior. Candidate should view their results at /Candidate/QuizResult/" + existingAttempts.First(a => a.Status == "Completed").Id;
                }
                else
                {
                    diagnosis = "Everything looks fine: an active quiz with questions exists and there's no completed attempt blocking it.";
                    recommendedAction = "TakeQuiz should work. Check the TakeQuiz view for rendering issues.";
                }

                System.Diagnostics.Debug.WriteLine($">>> DebugTakeQuiz: Diagnosis: {diagnosis}");
                System.Diagnostics.Debug.WriteLine($">>> DebugTakeQuiz: Recommended Action: {recommendedAction}");

                return Json(new
                {
                    success = true,
                    diagnosis,
                    recommendedAction,
                    applicationId = application.Id,
                    applicationCandidateId = application.CandidateId,
                    applicationJobPostId = application.JobPostId,
                    applicationStatus = application.Status,
                    applicationInterviewScheduled = application.InterviewScheduled,
                    applicationQuizCompletedDate = application.QuizCompletedDate,
                    applicationQuizScore = application.QuizScore,
                    applicationQuizPassed = application.QuizPassed,
                    jobPostTitle = jobPost.JobTitle,
                    allQuizzesForJobPost = allQuizzesForJobPost,
                    activeQuiz = activeQuiz,
                    existingAttempts = existingAttempts
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> DebugTakeQuiz EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                return Json(new
                {
                    success = false,
                    step = "exception",
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Candidate/TakeQuiz/5
        [Authorize]
        public async Task<ActionResult> TakeQuiz(int id)
        {
            System.Diagnostics.Debug.WriteLine($"========== TakeQuiz called with ApplicationId: {id} ==========");

            try
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine(">>> TakeQuiz: User not found");
                    return RedirectToAction("Login", "Account");
                }
                System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: User found: {user.UserName}");

                var candidate = db.Candidates.FirstOrDefault(c => c.UserId == user.Id);
                if (candidate == null)
                {
                    System.Diagnostics.Debug.WriteLine(">>> TakeQuiz: Candidate not found");
                    return RedirectToAction("Register", "Candidate");
                }
                System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: Candidate found: {candidate.FullName} (ID: {candidate.Id})");

                var application = db.JobApplications.Find(id);
                if (application == null || application.CandidateId != candidate.Id)
                {
                    System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: Application not found or permission denied. ApplicationId={id}, CandidateId={candidate.Id}");
                    TempData["ErrorMessage"] = "Application not found or you don't have permission.";
                    return RedirectToAction("Dashboard", "Home");
                }
                System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: Application found: ID={application.Id}, JobPostId={application.JobPostId}");

                // Check if already completed
                var existingAttempt = db.QuizAttempts
                    .Where(qa => qa.ApplicationId == id)
                    .OrderByDescending(qa => qa.StartTime)
                    .FirstOrDefault();

                if (existingAttempt != null && existingAttempt.Status == "Completed")
                {
                    System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: Attempt already completed. Redirecting to QuizResult. AttemptId={existingAttempt.Id}");
                    return RedirectToAction("QuizResult", new { id = existingAttempt.Id });
                }

                // Get or create quiz for this application
                var jobPost = db.JobPosts.Find(application.JobPostId);
                if (jobPost == null)
                {
                    System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: JobPost not found for JobPostId: {application.JobPostId}");
                    TempData["ErrorMessage"] = "Job post not found.";
                    return RedirectToAction("AllApplications");
                }
                System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: JobPost found: {jobPost.JobTitle} (ID: {jobPost.Id})");

                // Check if quiz exists - use a direct query to avoid lazy loading issues
                System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: Checking for active quiz for JobPostId: {jobPost.Id}");

                var quiz = db.Quizzes
                    .Where(q => q.JobPostId == jobPost.Id && q.IsActive)
                    .Select(q => new { Quiz = q, QuestionCount = db.QuizQuestions.Count(qq => qq.QuizId == q.Id) })
                    .FirstOrDefault();

                Quiz fullQuiz = null;

                if (quiz != null)
                {
                    System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: Found active quiz: ID={quiz.Quiz.Id}, Title={quiz.Quiz.Title}, QuestionCount={quiz.QuestionCount}");

                    if (quiz.QuestionCount > 0)
                    {
                        // Load full quiz with questions
                        fullQuiz = db.Quizzes.Include(q => q.Questions).FirstOrDefault(q => q.Id == quiz.Quiz.Id);
                        System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: Loaded full quiz with {fullQuiz?.Questions?.Count ?? 0} questions");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: Quiz has 0 questions. Will generate questions.");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: No active quiz found. Will auto-create one.");
                }

                if (fullQuiz == null || !fullQuiz.Questions.Any())
                {
                    // Auto-generate quiz if none exists or has no questions
                    System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: Creating/updating quiz for JobPostId: {jobPost.Id}");
                    fullQuiz = await CreateAutoQuizForCandidate(jobPost);

                    if (fullQuiz == null || !fullQuiz.Questions.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: Failed to create quiz or no questions generated.");
                        TempData["ErrorMessage"] = "Quiz not available for this position. Please contact the administrator.";
                        return RedirectToAction("AllApplications");
                    }
                    System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: Quiz created/updated with {fullQuiz.Questions.Count} questions");
                }

                // Check if there's an in-progress attempt
                var inProgressAttempt = db.QuizAttempts
                    .FirstOrDefault(qa => qa.ApplicationId == id && qa.Status == "InProgress");

                if (inProgressAttempt != null)
                {
                    System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: Found in-progress attempt. Resuming. AttemptId={inProgressAttempt.Id}");
                    // Resume the in-progress attempt
                    return RedirectToAction("ResumeQuiz", new { id = inProgressAttempt.Id });
                }

                // Create new attempt
                System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: Creating new quiz attempt for QuizId: {fullQuiz.Id}");
                var attempt = new QuizAttempt
                {
                    QuizId = fullQuiz.Id,
                    ApplicationId = application.Id,
                    StartTime = DateTime.Now,
                    Status = "InProgress"
                };

                db.QuizAttempts.Add(attempt);
                await db.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: Attempt created with ID: {attempt.Id}");

                // Prepare quiz view - limit to 10 questions only
                var allQuestions = fullQuiz.Questions.OrderBy(q => q.Order).ToList();
                var questions = allQuestions.Take(10).ToList();
                System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: Preparing view with {questions.Count} questions (limited to 10)");

                var viewModel = new QuizViewModel
                {
                    QuizId = fullQuiz.Id,
                    Title = fullQuiz.Title,
                    Description = fullQuiz.Description,
                    TimeLimitMinutes = 20, // Changed to 20 minutes
                    PassingScore = fullQuiz.PassingScore,
                    NumberOfQuestions = questions.Count,
                    ApplicationId = application.Id,
                    JobTitle = jobPost?.JobTitle ?? "Position",
                    CandidateName = candidate.FullName,
                    Questions = questions.Select(q => new QuizQuestionViewModel
                    {
                        Id = q.Id,
                        Question = q.Question,
                        Options = JsonConvert.DeserializeObject<List<string>>(q.OptionsJson ?? "[]"),
                        Points = q.Points
                    }).ToList()
                };

                ViewBag.AttemptId = attempt.Id;
                ViewBag.QuizTitle = fullQuiz.Title;
                ViewBag.TimeLimitMinutes = 20; // Changed to 20 minutes
                ViewBag.QuestionCount = questions.Count;
                ViewBag.PassingScore = fullQuiz.PassingScore;

                System.Diagnostics.Debug.WriteLine($">>> TakeQuiz: View prepared successfully. Returning view.");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> TakeQuiz EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading quiz: " + ex.Message;
                return RedirectToAction("AllApplications");
            }
        }

        // POST: Candidate/SubmitQuiz
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SubmitQuiz(int attemptId, string answers)
        {
            System.Diagnostics.Debug.WriteLine($"========== SubmitQuiz called for AttemptId: {attemptId} ==========");

            // The client sends the answers as a JSON-encoded string (e.g. {"5":"A","6":"B"}).
            // The default MVC model binder cannot bind a JSON string onto a Dictionary<int,string>,
            // so we deserialize it ourselves instead of relying on a bound view model.
            Dictionary<int, string> parsedAnswers;
            try
            {
                parsedAnswers = string.IsNullOrWhiteSpace(answers)
                    ? new Dictionary<int, string>()
                    : JsonConvert.DeserializeObject<Dictionary<int, string>>(answers);
            }
            catch (Exception parseEx)
            {
                System.Diagnostics.Debug.WriteLine($">>> SubmitQuiz: Failed to parse answers JSON: {parseEx.Message}");
                return Json(new { success = false, message = "Invalid answers submitted." });
            }

            try
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine(">>> SubmitQuiz: User not authenticated");
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var candidate = db.Candidates.FirstOrDefault(c => c.UserId == user.Id);
                if (candidate == null)
                {
                    System.Diagnostics.Debug.WriteLine(">>> SubmitQuiz: Candidate not found");
                    return Json(new { success = false, message = "Candidate not found" });
                }

                var attempt = db.QuizAttempts.Find(attemptId);
                if (attempt == null)
                {
                    System.Diagnostics.Debug.WriteLine($">>> SubmitQuiz: Attempt not found for ID: {attemptId}");
                    return Json(new { success = false, message = "Quiz attempt not found" });
                }

                var application = db.JobApplications.Find(attempt.ApplicationId);
                if (application == null || application.CandidateId != candidate.Id)
                {
                    System.Diagnostics.Debug.WriteLine($">>> SubmitQuiz: Permission denied. ApplicationId={attempt.ApplicationId}, CandidateId={candidate.Id}");
                    return Json(new { success = false, message = "Permission denied" });
                }

                if (attempt.Status != "InProgress")
                {
                    System.Diagnostics.Debug.WriteLine($">>> SubmitQuiz: Attempt already completed. Status={attempt.Status}");
                    return Json(new { success = false, message = "Quiz already completed" });
                }

                var quiz = db.Quizzes.Include(q => q.Questions).FirstOrDefault(q => q.Id == attempt.QuizId);
                if (quiz == null)
                {
                    System.Diagnostics.Debug.WriteLine($">>> SubmitQuiz: Quiz not found for QuizId: {attempt.QuizId}");
                    return Json(new { success = false, message = "Quiz not found" });
                }

                // Only use the first 10 questions for scoring
                var questions = quiz.Questions.OrderBy(q => q.Order).Take(10).ToList();
                int totalQuestions = questions.Count;

                // Calculate score
                int correctCount = 0;
                var answersJson = new Dictionary<int, string>();

                System.Diagnostics.Debug.WriteLine($">>> SubmitQuiz: Processing {totalQuestions} questions");

                foreach (var question in questions)
                {
                    string userAnswer = null;
                    if (parsedAnswers != null && parsedAnswers.ContainsKey(question.Id))
                    {
                        userAnswer = parsedAnswers[question.Id];
                        answersJson[question.Id] = userAnswer;
                        System.Diagnostics.Debug.WriteLine($">>> SubmitQuiz: Question {question.Id}: User Answer='{userAnswer}', Correct='{question.CorrectAnswer}'");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($">>> SubmitQuiz: Question {question.Id}: No answer provided");
                    }

                    if (!string.IsNullOrEmpty(userAnswer) && userAnswer == question.CorrectAnswer)
                    {
                        correctCount++;
                    }
                }

                int score = (int)Math.Round((double)correctCount / totalQuestions * 100);
                bool passed = score >= quiz.PassingScore;

                System.Diagnostics.Debug.WriteLine($">>> SubmitQuiz: Score={score}%, Passed={passed}, Correct={correctCount}/{totalQuestions}");

                // Update attempt
                attempt.EndTime = DateTime.Now;
                attempt.Score = score;
                attempt.Passed = passed;
                attempt.Status = "Completed";
                attempt.AnswersJson = JsonConvert.SerializeObject(answersJson);

                await db.SaveChangesAsync();

                // Update application
                application.QuizScore = score;
                application.QuizPassed = passed;
                application.QuizCompletedDate = DateTime.Now;

                await db.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($">>> SubmitQuiz: Application updated. QuizScore={score}, QuizPassed={passed}");

                // Create status history
                var history = new ApplicationStatusHistory
                {
                    ApplicationId = application.Id,
                    Status = passed ? "Quiz Passed" : "Quiz Failed",
                    Notes = $"Quiz completed with score: {score}%. Passed: {(passed ? "Yes" : "No")}",
                    ChangedBy = candidate.FullName,
                    ChangeDate = DateTime.Now
                };
                db.ApplicationStatusHistories.Add(history);
                await db.SaveChangesAsync();

                // Create notification for candidate
                string notificationTitle = passed ? "Quiz Completed - Passed!" : "Quiz Completed - Failed";
                string notificationMessage = passed
                    ? $"Congratulations! You passed the quiz for '{application.JobPost?.JobTitle ?? "the position"}' with a score of {score}%."
                    : $"You did not pass the quiz for '{application.JobPost?.JobTitle ?? "the position"}'. Your score was {score}%. The passing score is {quiz.PassingScore}%.";

                await CreateNotification(
                    user.Id,
                    notificationTitle,
                    notificationMessage,
                    "Quiz",
                    $"/Candidate/QuizResult/{attempt.Id}"
                );

                // Notify admin if passed
                if (passed)
                {
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
                            "Candidate Passed Quiz",
                            $"{candidate.FullName} passed the quiz for '{application.JobPost?.JobTitle ?? "the position"}' with a score of {score}%.",
                            "Quiz",
                            $"/JobPost/ReviewApplications/{application.JobPostId}"
                        );
                    }
                }

                System.Diagnostics.Debug.WriteLine($">>> SubmitQuiz: Completed successfully. Result: {(passed ? "PASSED" : "FAILED")}");

                return Json(new
                {
                    success = true,
                    passed = passed,
                    score = score,
                    passingScore = quiz.PassingScore,
                    attemptId = attempt.Id,
                    message = passed ? "Quiz completed! You passed!" : "Quiz completed. You did not pass."
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> SubmitQuiz EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                return Json(new { success = false, message = "Error submitting quiz: " + ex.Message });
            }
        }

        // GET: Candidate/QuizResult/5
        [Authorize]
        public ActionResult QuizResult(int id)
        {
            System.Diagnostics.Debug.WriteLine($">>> QuizResult called with AttemptId: {id}");

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
                    return RedirectToAction("Register", "Candidate");
                }

                var attempt = db.QuizAttempts.Find(id);
                if (attempt == null)
                {
                    System.Diagnostics.Debug.WriteLine($">>> QuizResult: Attempt not found for ID: {id}");
                    TempData["ErrorMessage"] = "Quiz attempt not found.";
                    return RedirectToAction("AllApplications");
                }

                var application = db.JobApplications.Find(attempt.ApplicationId);
                if (application == null || application.CandidateId != candidate.Id)
                {
                    System.Diagnostics.Debug.WriteLine($">>> QuizResult: Permission denied. ApplicationId={attempt.ApplicationId}, CandidateId={candidate.Id}");
                    TempData["ErrorMessage"] = "Permission denied.";
                    return RedirectToAction("AllApplications");
                }

                var quiz = db.Quizzes.Include(q => q.Questions).FirstOrDefault(q => q.Id == attempt.QuizId);
                if (quiz == null)
                {
                    System.Diagnostics.Debug.WriteLine($">>> QuizResult: Quiz not found for QuizId: {attempt.QuizId}");
                    TempData["ErrorMessage"] = "Quiz not found.";
                    return RedirectToAction("AllApplications");
                }

                var jobPost = db.JobPosts.Find(application.JobPostId);
                // Only show first 10 questions in results
                var questions = quiz.Questions.OrderBy(q => q.Order).Take(10).ToList();
                var answers = JsonConvert.DeserializeObject<Dictionary<int, string>>(attempt.AnswersJson ?? "{}");

                var viewModel = new QuizResultViewModel
                {
                    QuizId = quiz.Id,
                    QuizTitle = quiz.Title,
                    ApplicationId = application.Id,
                    JobTitle = jobPost?.JobTitle ?? "Unknown Position",
                    Score = attempt.Score ?? 0,
                    TotalQuestions = questions.Count,
                    CorrectAnswers = questions.Count(q => answers.ContainsKey(q.Id) && answers[q.Id] == q.CorrectAnswer),
                    Passed = attempt.Passed ?? false,
                    PassingScore = quiz.PassingScore,
                    StartTime = attempt.StartTime,
                    EndTime = attempt.EndTime ?? DateTime.Now,
                    TimeTakenSeconds = (int)((attempt.EndTime ?? DateTime.Now) - attempt.StartTime).TotalSeconds,
                    Questions = questions.Select(q => new QuizQuestionViewModel
                    {
                        Id = q.Id,
                        Question = q.Question,
                        Options = JsonConvert.DeserializeObject<List<string>>(q.OptionsJson ?? "[]"),
                        SelectedAnswer = answers.ContainsKey(q.Id) ? answers[q.Id] : null,
                        CorrectAnswer = q.CorrectAnswer,
                        Points = q.Points,
                        IsCorrect = answers.ContainsKey(q.Id) && answers[q.Id] == q.CorrectAnswer
                    }).ToList()
                };

                System.Diagnostics.Debug.WriteLine($">>> QuizResult: Returning result for AttemptId: {id}, Score: {viewModel.Score}%");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> QuizResult EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading quiz result: " + ex.Message;
                return RedirectToAction("AllApplications");
            }
        }

        // GET: Candidate/ResumeQuiz/5
        [Authorize]
        public ActionResult ResumeQuiz(int id)
        {
            System.Diagnostics.Debug.WriteLine($">>> ResumeQuiz called with AttemptId: {id}");

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
                    return RedirectToAction("Register", "Candidate");
                }

                var attempt = db.QuizAttempts.Find(id);
                if (attempt == null || attempt.Status != "InProgress")
                {
                    System.Diagnostics.Debug.WriteLine($">>> ResumeQuiz: Attempt not found or not in progress. Id={id}, Status={attempt?.Status ?? "null"}");
                    TempData["ErrorMessage"] = "Quiz attempt not found or already completed.";
                    return RedirectToAction("AllApplications");
                }

                var application = db.JobApplications.Find(attempt.ApplicationId);
                if (application == null || application.CandidateId != candidate.Id)
                {
                    System.Diagnostics.Debug.WriteLine($">>> ResumeQuiz: Permission denied. ApplicationId={attempt.ApplicationId}, CandidateId={candidate.Id}");
                    TempData["ErrorMessage"] = "Permission denied.";
                    return RedirectToAction("AllApplications");
                }

                var quiz = db.Quizzes.Include(q => q.Questions).FirstOrDefault(q => q.Id == attempt.QuizId);
                if (quiz == null)
                {
                    System.Diagnostics.Debug.WriteLine($">>> ResumeQuiz: Quiz not found for QuizId: {attempt.QuizId}");
                    TempData["ErrorMessage"] = "Quiz not found.";
                    return RedirectToAction("AllApplications");
                }

                var jobPost = db.JobPosts.Find(application.JobPostId);
                // Only use first 10 questions
                var questions = quiz.Questions.OrderBy(q => q.Order).Take(10).ToList();
                var answers = JsonConvert.DeserializeObject<Dictionary<int, string>>(attempt.AnswersJson ?? "{}");

                var viewModel = new QuizViewModel
                {
                    QuizId = quiz.Id,
                    Title = quiz.Title,
                    Description = quiz.Description,
                    TimeLimitMinutes = 20, // Changed to 20 minutes
                    PassingScore = quiz.PassingScore,
                    NumberOfQuestions = questions.Count,
                    ApplicationId = application.Id,
                    JobTitle = jobPost?.JobTitle ?? "Position",
                    CandidateName = candidate.FullName,
                    Questions = questions.Select(q => new QuizQuestionViewModel
                    {
                        Id = q.Id,
                        Question = q.Question,
                        Options = JsonConvert.DeserializeObject<List<string>>(q.OptionsJson ?? "[]"),
                        SelectedAnswer = answers.ContainsKey(q.Id) ? answers[q.Id] : null,
                        Points = q.Points
                    }).ToList()
                };

                ViewBag.AttemptId = attempt.Id;
                ViewBag.QuizTitle = quiz.Title;
                ViewBag.TimeLimitMinutes = 20; // Changed to 20 minutes
                ViewBag.QuestionCount = questions.Count;
                ViewBag.PassingScore = quiz.PassingScore;
                ViewBag.ElapsedSeconds = (int)(DateTime.Now - attempt.StartTime).TotalSeconds;

                System.Diagnostics.Debug.WriteLine($">>> ResumeQuiz: Returning quiz for AttemptId: {id}, Elapsed: {ViewBag.ElapsedSeconds}s");
                return View("TakeQuiz", viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> ResumeQuiz EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "Error resuming quiz: " + ex.Message;
                return RedirectToAction("AllApplications");
            }
        }

        private async Task<Quiz> CreateAutoQuizForCandidate(JobPost jobPost)
        {
            System.Diagnostics.Debug.WriteLine($"========== CreateAutoQuizForCandidate called for JobPostId: {jobPost.Id}, Title: {jobPost.JobTitle} ==========");

            try
            {
                // Check if quiz already exists
                System.Diagnostics.Debug.WriteLine($">>> CreateAutoQuizForCandidate: Checking for existing quiz for JobPostId: {jobPost.Id}");

                var existingQuiz = db.Quizzes.Include(q => q.Questions)
                    .FirstOrDefault(q => q.JobPostId == jobPost.Id);

                if (existingQuiz != null)
                {
                    System.Diagnostics.Debug.WriteLine($">>> CreateAutoQuizForCandidate: Found existing quiz: ID={existingQuiz.Id}, Title={existingQuiz.Title}");

                    // Check if quiz already has questions
                    if (existingQuiz.Questions != null && existingQuiz.Questions.Any())
                    {
                        // Check if there are already 10 questions - if so, use existing
                        if (existingQuiz.Questions.Count >= 10)
                        {
                            System.Diagnostics.Debug.WriteLine($">>> CreateAutoQuizForCandidate: Quiz already has {existingQuiz.Questions.Count} questions. Returning existing quiz.");
                            return existingQuiz;
                        }
                        else
                        {
                            // Has some questions but less than 10 - generate more to reach 10
                            System.Diagnostics.Debug.WriteLine($">>> CreateAutoQuizForCandidate: Quiz has {existingQuiz.Questions.Count} questions. Need to generate {10 - existingQuiz.Questions.Count} more.");

                            var additionalQuestions = GenerateSampleQuestions(
                                jobPost.JobTitle,
                                jobPost.Requirements,
                                10 - existingQuiz.Questions.Count);

                            foreach (var q in additionalQuestions)
                            {
                                // Make sure the order is correct
                                var maxOrder = existingQuiz.Questions.Any() ? existingQuiz.Questions.Max(qq => qq.Order) : 0;
                                var question = new QuizQuestion
                                {
                                    QuizId = existingQuiz.Id,
                                    Question = q.Question,
                                    QuestionType = "MultipleChoice",
                                    OptionsJson = JsonConvert.SerializeObject(q.Options),
                                    CorrectAnswer = q.CorrectAnswer,
                                    Points = 1,
                                    Order = maxOrder + q.Order
                                };
                                db.QuizQuestions.Add(question);
                                existingQuiz.Questions.Add(question);
                            }
                            await db.SaveChangesAsync();
                            System.Diagnostics.Debug.WriteLine($">>> CreateAutoQuizForCandidate: Added {additionalQuestions.Count} questions to existing quiz. Total now: {existingQuiz.Questions.Count}");
                            return existingQuiz;
                        }
                    }

                    // Quiz shell exists but has no questions - generate 10 new ones
                    System.Diagnostics.Debug.WriteLine($">>> CreateAutoQuizForCandidate: Quiz exists with no questions. Generating 10 questions now.");
                    var fallbackQuestions = GenerateSampleQuestions(
                        jobPost.JobTitle,
                        jobPost.Requirements,
                        10);

                    System.Diagnostics.Debug.WriteLine($">>> CreateAutoQuizForCandidate: Generated {fallbackQuestions.Count} questions");

                    foreach (var q in fallbackQuestions)
                    {
                        var question = new QuizQuestion
                        {
                            QuizId = existingQuiz.Id,
                            Question = q.Question,
                            QuestionType = "MultipleChoice",
                            OptionsJson = JsonConvert.SerializeObject(q.Options),
                            CorrectAnswer = q.CorrectAnswer,
                            Points = 1,
                            Order = q.Order
                        };
                        db.QuizQuestions.Add(question);
                        existingQuiz.Questions.Add(question);
                    }
                    await db.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($">>> CreateAutoQuizForCandidate: Added 10 questions to existing quiz.");
                    return existingQuiz;
                }

                System.Diagnostics.Debug.WriteLine($">>> CreateAutoQuizForCandidate: No existing quiz found. Creating new quiz.");

                var quiz = new Quiz
                {
                    Title = $"Quiz for {jobPost.JobTitle}",
                    Description = $"This quiz assesses candidates for the {jobPost.JobTitle} position.",
                    JobPostId = jobPost.Id,
                    SystemRoleId = jobPost.SystemRoleId,
                    TimeLimitMinutes = 20, // Changed to 20 minutes
                    PassingScore = 60,
                    NumberOfQuestions = 10,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                };

                db.Quizzes.Add(quiz);
                await db.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($">>> CreateAutoQuizForCandidate: Quiz created with ID: {quiz.Id}");

                // Generate exactly 10 questions
                var questions = GenerateSampleQuestions(jobPost.JobTitle, jobPost.Requirements, 10);
                System.Diagnostics.Debug.WriteLine($">>> CreateAutoQuizForCandidate: Generated {questions.Count} questions");

                foreach (var q in questions)
                {
                    var question = new QuizQuestion
                    {
                        QuizId = quiz.Id,
                        Question = q.Question,
                        QuestionType = "MultipleChoice",
                        OptionsJson = JsonConvert.SerializeObject(q.Options),
                        CorrectAnswer = q.CorrectAnswer,
                        Points = 1,
                        Order = q.Order
                    };
                    db.QuizQuestions.Add(question);
                }
                await db.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($">>> CreateAutoQuizForCandidate: Added {questions.Count} questions to quiz ID: {quiz.Id}");

                return quiz;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> CreateAutoQuizForCandidate EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private List<QuizQuestionViewModel> GenerateSampleQuestions(string jobTitle, string requirements, int count = 10)
        {
            System.Diagnostics.Debug.WriteLine($">>> GenerateSampleQuestions called for: {jobTitle}, count: {count}");

            var questions = new List<QuizQuestionViewModel>();
            var random = new Random();

            // Generic construction/project management questions
            var questionBank = new List<Tuple<string, List<string>, string>>
            {
                // Safety Questions
                Tuple.Create("What is the most important safety regulation on a construction site?",
                    new List<string> { "Wearing hard hats", "Following all safety protocols", "Wearing safety boots", "Using safety harnesses" },
                    "Following all safety protocols"),
                Tuple.Create("Which of the following is a critical safety measure in construction?",
                    new List<string> { "Regular safety inspections", "Emergency response planning", "Personal protective equipment", "All of the above" },
                    "All of the above"),
                Tuple.Create("What is the purpose of a risk assessment in construction?",
                    new List<string> { "To identify potential hazards", "To eliminate all risks", "To increase costs", "To delay the project" },
                    "To identify potential hazards"),
                Tuple.Create("Which document outlines safety procedures on a construction site?",
                    new List<string> { "Site safety plan", "Project charter", "Budget report", "Schedule plan" },
                    "Site safety plan"),
                Tuple.Create("What is the primary cause of accidents on construction sites?",
                    new List<string> { "Human error", "Equipment failure", "Weather conditions", "Poor lighting" },
                    "Human error"),

                // Construction Management
                Tuple.Create("What is the critical path method (CPM) used for?",
                    new List<string> { "Scheduling projects", "Budgeting projects", "Quality control", "Safety management" },
                    "Scheduling projects"),
                Tuple.Create("What is a Gantt chart primarily used for?",
                    new List<string> { "Project scheduling", "Budget tracking", "Risk assessment", "Quality control" },
                    "Project scheduling"),
                Tuple.Create("What is the purpose of a project charter?",
                    new List<string> { "To authorize the project", "To track project costs", "To manage resources", "To evaluate risks" },
                    "To authorize the project"),
                Tuple.Create("Which of the following is a project constraint?",
                    new List<string> { "Scope", "Time", "Cost", "All of the above" },
                    "All of the above"),
                Tuple.Create("What does SWOT analysis stand for?",
                    new List<string> { "Strengths, Weaknesses, Opportunities, Threats", "Strategy, Work, Operations, Time", "Schedule, Work, Outcomes, Targets", "Safety, Welfare, Operations, Training" },
                    "Strengths, Weaknesses, Opportunities, Threats"),

                // Quality Management
                Tuple.Create("What is quality assurance (QA) in construction?",
                    new List<string> { "Process-oriented approach", "Product-oriented approach", "Cost-oriented approach", "Time-oriented approach" },
                    "Process-oriented approach"),
                Tuple.Create("What is quality control (QC) in construction?",
                    new List<string> { "Product-oriented approach", "Process-oriented approach", "Cost-oriented approach", "Time-oriented approach" },
                    "Product-oriented approach"),

                // Contract Management
                Tuple.Create("What is the purpose of a construction contract?",
                    new List<string> { "To formalize the agreement", "To increase costs", "To delay work", "To confuse parties" },
                    "To formalize the agreement"),
                Tuple.Create("What is a variation order in construction?",
                    new List<string> { "Changes to the project scope", "Changes to project costs", "Changes to project schedule", "All of the above" },
                    "All of the above"),

                // Project Management
                Tuple.Create("What is the project lifecycle?",
                    new List<string> { "Initiation, Planning, Execution, Closure", "Start, Middle, End", "Research, Design, Build, Test", "Proposal, Approval, Work, Review" },
                    "Initiation, Planning, Execution, Closure"),
                Tuple.Create("What is stakeholder management?",
                    new List<string> { "Managing project stakeholders", "Managing project costs", "Managing project schedule", "Managing project quality" },
                    "Managing project stakeholders"),
                Tuple.Create("What is the triple constraint of project management?",
                    new List<string> { "Scope, Time, Cost", "Quality, Speed, Price", "People, Process, Technology", "Planning, Execution, Control" },
                    "Scope, Time, Cost"),
                Tuple.Create("What is a milestone in project management?",
                    new List<string> { "A significant event", "A minor task", "A budget item", "A quality check" },
                    "A significant event"),
                Tuple.Create("What is resource allocation?",
                    new List<string> { "Assigning resources to tasks", "Managing project costs", "Creating project schedule", "Defining project scope" },
                    "Assigning resources to tasks"),
                Tuple.Create("What is risk management in construction?",
                    new List<string> { "Identifying and mitigating risks", "Increasing risks", "Ignoring risks", "Transferring all risks" },
                    "Identifying and mitigating risks"),
                Tuple.Create("What is the purpose of project documentation?",
                    new List<string> { "Record keeping and communication", "Increasing costs", "Delaying work", "Creating confusion" },
                    "Record keeping and communication"),
                Tuple.Create("What is continuous improvement in construction?",
                    new List<string> { "Ongoing process improvement", "Making things worse", "Doing nothing", "Working slower" },
                    "Ongoing process improvement"),
                Tuple.Create("What is the role of a project manager?",
                    new List<string> { "Leading the project to completion", "Working on site", "Managing finances", "Designing buildings" },
                    "Leading the project to completion"),
                Tuple.Create("What is the purpose of project reporting?",
                    new List<string> { "Communicating project status", "Creating more work", "Confusing stakeholders", "Increasing costs" },
                    "Communicating project status"),

                // Industry-Specific Questions based on job title
                Tuple.Create("What is the most important consideration when managing a construction project?",
                    new List<string> { "Safety", "Cost", "Schedule", "Quality" },
                    "Safety"),
                Tuple.Create("What is the purpose of a site inspection?",
                    new List<string> { "To check progress and quality", "To delay the project", "To increase costs", "To confuse workers" },
                    "To check progress and quality"),
                Tuple.Create("What is the importance of communication in construction?",
                    new List<string> { "It is essential for success", "It is not important", "It is only for managers", "It is optional" },
                    "It is essential for success"),
                Tuple.Create("What is the role of a site manager?",
                    new List<string> { "Managing daily site operations", "Designing buildings", "Managing finances", "HR management" },
                    "Managing daily site operations"),
                Tuple.Create("What is the difference between a project manager and a site manager?",
                    new List<string> { "Project manager oversees entire project, site manager oversees daily operations", "They are the same role", "Site manager is more senior", "Project manager works on site" },
                    "Project manager oversees entire project, site manager oversees daily operations"),
            };

            // Filter questions based on job title keywords
            var filteredQuestions = questionBank;
            if (!string.IsNullOrEmpty(jobTitle))
            {
                var keywords = jobTitle.ToLower().Split(' ');
                filteredQuestions = questionBank.Where(q =>
                    keywords.Any(k => q.Item1.ToLower().Contains(k) || q.Item2.Any(o => o.ToLower().Contains(k)))
                ).ToList();

                if (filteredQuestions.Count < count)
                {
                    filteredQuestions = questionBank;
                }
            }

            // Ensure we have exactly 'count' questions
            var selectedQuestions = filteredQuestions.OrderBy(x => random.Next()).Take(count).ToList();

            // If not enough questions, add more from the bank
            if (selectedQuestions.Count < count && questionBank.Count > selectedQuestions.Count)
            {
                var additional = questionBank.Except(selectedQuestions).OrderBy(x => random.Next()).Take(count - selectedQuestions.Count);
                selectedQuestions.AddRange(additional);
            }

            for (int i = 0; i < selectedQuestions.Count && i < count; i++)
            {
                var q = selectedQuestions[i];
                var options = q.Item2.OrderBy(x => random.Next()).ToList();
                questions.Add(new QuizQuestionViewModel
                {
                    Id = i + 1,
                    Question = q.Item1,
                    Options = options,
                    CorrectAnswer = q.Item3,
                    Points = 1,
                    Order = i + 1
                });
            }

            System.Diagnostics.Debug.WriteLine($">>> GenerateSampleQuestions: Generated {questions.Count} questions");
            return questions;
        }

        private async Task CreateNotification(string userId, string title, string message, string type, string link)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($">>> CreateNotification: userId={userId}, title={title}, type={type}");

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

                System.Diagnostics.Debug.WriteLine($">>> CreateNotification: Notification created with ID: {notification.Id}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> CreateNotification EXCEPTION: {ex.Message}\n{ex.StackTrace}");
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
            System.Diagnostics.Debug.WriteLine($">>> SignInAsync called for user: {user.UserName}");

            var authenticationManager = HttpContext.GetOwinContext().Authentication;
            var identity = await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
            authenticationManager.SignIn(new Microsoft.Owin.Security.AuthenticationProperties { IsPersistent = isPersistent }, identity);

            System.Diagnostics.Debug.WriteLine($">>> SignInAsync completed for user: {user.UserName}");
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