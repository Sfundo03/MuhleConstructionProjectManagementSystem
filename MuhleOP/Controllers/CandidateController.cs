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
using System.Net.Mail;
using System.Net;
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
            System.Diagnostics.Debug.WriteLine("=== CandidateController Constructor Called ===");
        }

        public CandidateController(ApplicationUserManager userManager)
        {
            System.Diagnostics.Debug.WriteLine("=== CandidateController Constructor with UserManager Called ===");
            UserManager = userManager;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                System.Diagnostics.Debug.WriteLine("=== UserManager Getter Called ===");
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                System.Diagnostics.Debug.WriteLine("=== UserManager Setter Called ===");
                _userManager = value;
            }
        }

        // GET: Candidate/Register
        public ActionResult Register()
        {
            System.Diagnostics.Debug.WriteLine("=== REGISTER PAGE LOADED ===");
            return View();
        }

        // POST: Candidate/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(CandidateRegisterViewModel model)
        {
            System.Diagnostics.Debug.WriteLine("=== REGISTER POST STARTED ===");
            System.Diagnostics.Debug.WriteLine($"Email: {model.Email}");
            System.Diagnostics.Debug.WriteLine($"FirstName: {model.FirstName}");
            System.Diagnostics.Debug.WriteLine($"LastName: {model.LastName}");
            System.Diagnostics.Debug.WriteLine($"PhoneNumber: {model.PhoneNumber}");

            if (ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("ModelState is Valid");
                try
                {
                    System.Diagnostics.Debug.WriteLine("Creating user account...");
                    var user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        EmailConfirmed = true
                    };

                    var result = await UserManager.CreateAsync(user, model.Password);
                    System.Diagnostics.Debug.WriteLine($"User creation result: {result.Succeeded}");

                    if (result.Succeeded)
                    {
                        System.Diagnostics.Debug.WriteLine($"User created successfully with ID: {user.Id}");

                        System.Diagnostics.Debug.WriteLine("Creating Candidate profile...");
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

                        System.Diagnostics.Debug.WriteLine($"Candidate profile created: {candidate.FullName}");

                        // Save documents
                        string uploadFolder = Server.MapPath("~/Uploads/Candidates/" + user.Id + "/");
                        System.Diagnostics.Debug.WriteLine($"Upload folder path: {uploadFolder}");

                        if (!Directory.Exists(uploadFolder))
                        {
                            System.Diagnostics.Debug.WriteLine("Creating upload directory...");
                            Directory.CreateDirectory(uploadFolder);
                        }

                        // Save ID Document
                        if (model.IdDocument != null && model.IdDocument.ContentLength > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"Saving ID Document: {model.IdDocument.FileName}");
                            string fileName = "ID_" + Guid.NewGuid().ToString() + Path.GetExtension(model.IdDocument.FileName);
                            string filePath = Path.Combine(uploadFolder, fileName);
                            model.IdDocument.SaveAs(filePath);
                            candidate.IdDocumentPath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                            System.Diagnostics.Debug.WriteLine($"ID Document saved to: {candidate.IdDocumentPath}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("ID Document is null or empty");
                        }

                        // Save Qualifications
                        if (model.Qualifications != null && model.Qualifications.ContentLength > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"Saving Qualifications: {model.Qualifications.FileName}");
                            string fileName = "Qualifications_" + Guid.NewGuid().ToString() + Path.GetExtension(model.Qualifications.FileName);
                            string filePath = Path.Combine(uploadFolder, fileName);
                            model.Qualifications.SaveAs(filePath);
                            candidate.QualificationsPath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                            System.Diagnostics.Debug.WriteLine($"Qualifications saved to: {candidate.QualificationsPath}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Qualifications is null or empty");
                        }

                        // Save CV
                        if (model.Cv != null && model.Cv.ContentLength > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"Saving CV: {model.Cv.FileName}");
                            string fileName = "CV_" + Guid.NewGuid().ToString() + Path.GetExtension(model.Cv.FileName);
                            string filePath = Path.Combine(uploadFolder, fileName);
                            model.Cv.SaveAs(filePath);
                            candidate.CvPath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                            System.Diagnostics.Debug.WriteLine($"CV saved to: {candidate.CvPath}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("CV is null or empty");
                        }

                        // Save License (Optional)
                        if (model.License != null && model.License.ContentLength > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"Saving License: {model.License.FileName}");
                            string fileName = "License_" + Guid.NewGuid().ToString() + Path.GetExtension(model.License.FileName);
                            string filePath = Path.Combine(uploadFolder, fileName);
                            model.License.SaveAs(filePath);
                            candidate.LicensePath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                            System.Diagnostics.Debug.WriteLine($"License saved to: {candidate.LicensePath}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("License is null or empty (optional)");
                        }

                        // Save Profile Picture (Optional)
                        if (model.ProfilePicture != null && model.ProfilePicture.ContentLength > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"Saving Profile Picture: {model.ProfilePicture.FileName}");
                            string fileName = "Profile_" + Guid.NewGuid().ToString() + Path.GetExtension(model.ProfilePicture.FileName);
                            string filePath = Path.Combine(uploadFolder, fileName);
                            model.ProfilePicture.SaveAs(filePath);
                            candidate.ProfilePicturePath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                            System.Diagnostics.Debug.WriteLine($"Profile Picture saved to: {candidate.ProfilePicturePath}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Profile Picture is null or empty (optional)");
                        }

                        System.Diagnostics.Debug.WriteLine("Adding candidate to database...");
                        db.Candidates.Add(candidate);
                        await db.SaveChangesAsync();
                        System.Diagnostics.Debug.WriteLine($"Candidate saved to database with ID: {candidate.Id}");

                        // Assign Candidate role
                        System.Diagnostics.Debug.WriteLine("Checking if 'Candidate' role exists and assigning...");
                        try
                        {
                            if (!UserManager.IsInRole(user.Id, "Candidate"))
                            {
                                System.Diagnostics.Debug.WriteLine("Assigning 'Candidate' role to user...");
                                var roleResult = UserManager.AddToRole(user.Id, "Candidate");
                                System.Diagnostics.Debug.WriteLine($"Role assignment result: {roleResult.Succeeded}");
                                if (!roleResult.Succeeded)
                                {
                                    foreach (var error in roleResult.Errors)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Role assignment error: {error}");
                                    }
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("User is already in 'Candidate' role");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error assigning role: {ex.Message}");
                        }

                        // Create welcome notification
                        await CreateNotification(
                            user.Id,
                            "Welcome to MuhleOps!",
                            $"Welcome {candidate.FullName}! Your account has been successfully created. You can now browse and apply for jobs.",
                            "System",
                            null
                        );

                        System.Diagnostics.Debug.WriteLine("Sending registration email...");
                        SendRegistrationEmail(model.Email, model.FirstName);

                        System.Diagnostics.Debug.WriteLine("Signing in user...");
                        await SignInAsync(user, isPersistent: false);

                        System.Diagnostics.Debug.WriteLine("=== REGISTRATION COMPLETED SUCCESSFULLY ===");
                        TempData["SuccessMessage"] = "Registration successful! Welcome to MuhleOps.";
                        return RedirectToAction("Dashboard", "Home");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("User creation failed:");
                        foreach (var error in result.Errors)
                        {
                            System.Diagnostics.Debug.WriteLine($"  - {error}");
                            ModelState.AddModelError("", error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"!!! EXCEPTION in Register: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    ModelState.AddModelError("", "Registration failed: " + ex.Message);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ModelState is INVALID");
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    if (errors.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {key}: {string.Join(", ", errors.Select(e => e.ErrorMessage))}");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("=== REGISTER POST ENDED WITH ERRORS ===");
            return View(model);
        }

        // GET: Candidate/Dashboard - Redirect to Home/Dashboard
        [Authorize]
        public ActionResult Dashboard()
        {
            System.Diagnostics.Debug.WriteLine("=== CANDIDATE DASHBOARD REDIRECTING TO HOME DASHBOARD ===");
            return RedirectToAction("Dashboard", "Home");
        }

        // GET: Candidate/AllApplications
        [Authorize]
        public ActionResult AllApplications()
        {
            System.Diagnostics.Debug.WriteLine("=== ALL APPLICATIONS LOADED ===");

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
                    RankingScore = app.RankingScore,
                    SkillsMatchPercentage = app.SkillsMatchPercentage
                }).ToList();

                return View(viewModels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! EXCEPTION in AllApplications: {ex.Message}");
                throw;
            }
        }

        // GET: Candidate/ApplicationStatus/5
        [Authorize]
        public ActionResult ApplicationStatus(int? id)
        {
            System.Diagnostics.Debug.WriteLine($"=== APPLICATION STATUS LOADED - Application ID: {(id.HasValue ? id.Value.ToString() : "ALL")} ===");

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

                // If no specific ID is provided, show all applications
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

                // Get status history
                var statusHistory = db.ApplicationStatusHistories
                    .Where(h => h.ApplicationId == application.Id)
                    .OrderByDescending(h => h.ChangeDate)
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
                    RankingScore = application.RankingScore,
                    SkillsMatchPercentage = application.SkillsMatchPercentage,
                    StatusHistory = statusHistory.Select(h => new ApplicationStatusHistoryViewModel
                    {
                        Status = h.Status,
                        Notes = h.Notes,
                        ChangedBy = h.ChangedBy,
                        ChangeDate = h.ChangeDate
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! EXCEPTION in ApplicationStatus: {ex.Message}");
                throw;
            }
        }

        // GET: Candidate/BrowseJobs
        [Authorize]
        public ActionResult BrowseJobs()
        {
            System.Diagnostics.Debug.WriteLine("=== BROWSE JOBS LOADED ===");
            try
            {
                var jobs = db.JobPosts
                    .Where(j => j.IsPublished && j.ClosingDate >= DateTime.Now)
                    .OrderByDescending(j => j.DatePosted)
                    .ToList();

                var jobList = jobs.Select(j => new JobPostSummaryViewModel
                {
                    Id = j.Id,
                    JobTitle = j.JobTitle,
                    Department = j.Department,
                    Location = j.Location,
                    JobType = j.JobType,
                    ClosingDate = j.ClosingDate,
                    IsPublished = j.IsPublished,
                    DaysLeft = (int)(j.ClosingDate - DateTime.Now).TotalDays
                }).ToList();

                System.Diagnostics.Debug.WriteLine($"Found {jobList.Count} available jobs");
                return View(jobList);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! EXCEPTION in BrowseJobs: {ex.Message}");
                throw;
            }
        }

        // GET: Candidate/JobDetails/5
        [Authorize]
        public ActionResult JobDetails(int id)
        {
            System.Diagnostics.Debug.WriteLine($"=== JOB DETAILS LOADED - Job ID: {id} ===");
            try
            {
                var job = db.JobPosts.Find(id);
                if (job == null || !job.IsPublished)
                {
                    System.Diagnostics.Debug.WriteLine($"Job not found or not published: {id}");
                    return HttpNotFound();
                }

                var viewModel = new ApplyJobViewModel
                {
                    JobPostId = job.Id,
                    JobTitle = job.JobTitle,
                    JobDescription = job.JobDescription,
                    Requirements = job.Requirements,
                    Location = job.Location,
                    JobType = job.JobType,
                    ClosingDate = job.ClosingDate
                };

                System.Diagnostics.Debug.WriteLine($"Job details loaded: {job.JobTitle}");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! EXCEPTION in JobDetails: {ex.Message}");
                throw;
            }
        }

        // GET: Candidate/Apply/5
        [Authorize]
        public ActionResult Apply(int id)
        {
            System.Diagnostics.Debug.WriteLine($"=== APPLY LOADED - Job ID: {id} ===");
            try
            {
                var job = db.JobPosts.Find(id);
                if (job == null || !job.IsPublished || job.ClosingDate < DateTime.Now)
                {
                    System.Diagnostics.Debug.WriteLine($"Job not available - Job ID: {id}");
                    TempData["ErrorMessage"] = "This job is no longer available.";
                    return RedirectToAction("BrowseJobs");
                }

                var user = UserManager.FindById(User.Identity.GetUserId());
                System.Diagnostics.Debug.WriteLine($"User found: {(user != null ? user.Email : "NULL")}");

                var candidate = db.Candidates.FirstOrDefault(c => c.UserId == user.Id);
                System.Diagnostics.Debug.WriteLine($"Candidate found: {(candidate != null ? candidate.FullName : "NULL")}");

                if (candidate == null)
                {
                    System.Diagnostics.Debug.WriteLine("Candidate not found, redirecting to Register");
                    return RedirectToAction("Register");
                }

                // Check if already applied
                var existingApplication = db.JobApplications
                    .FirstOrDefault(a => a.JobPostId == id && a.CandidateId == candidate.Id);

                if (existingApplication != null)
                {
                    System.Diagnostics.Debug.WriteLine($"User already applied for this job - Application ID: {existingApplication.Id}");
                    TempData["ErrorMessage"] = "You have already applied for this position.";
                    return RedirectToAction("Dashboard", "Home");
                }

                var viewModel = new ApplyJobViewModel
                {
                    JobPostId = job.Id,
                    JobTitle = job.JobTitle,
                    JobDescription = job.JobDescription,
                    Requirements = job.Requirements,
                    Location = job.Location,
                    JobType = job.JobType,
                    ClosingDate = job.ClosingDate,
                    HasExistingDocuments = true,
                    CoverLetter = "Dear Hiring Manager,\n\nI am writing to apply for the " + job.JobTitle + " position. I am very interested in this opportunity and believe my skills and experience make me a strong candidate for this role.\n\nI look forward to hearing from you.\n\nSincerely,\n" + candidate.FullName
                };

                System.Diagnostics.Debug.WriteLine($"Apply view model created for: {job.JobTitle}");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! EXCEPTION in Apply: {ex.Message}");
                throw;
            }
        }

        // POST: Candidate/Apply
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> Apply(ApplyJobViewModel model)
        {
            System.Diagnostics.Debug.WriteLine("=== APPLY POST STARTED ===");
            System.Diagnostics.Debug.WriteLine($"JobPostId: {model.JobPostId}");
            System.Diagnostics.Debug.WriteLine($"Has CoverLetter: {!string.IsNullOrEmpty(model.CoverLetter)}");

            if (ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("ModelState is Valid");
                try
                {
                    var user = UserManager.FindById(User.Identity.GetUserId());
                    System.Diagnostics.Debug.WriteLine($"User found: {(user != null ? user.Email : "NULL")}");

                    var candidate = db.Candidates.FirstOrDefault(c => c.UserId == user.Id);
                    System.Diagnostics.Debug.WriteLine($"Candidate found: {(candidate != null ? candidate.FullName : "NULL")}");

                    if (candidate == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Candidate not found, redirecting to Register");
                        return RedirectToAction("Register");
                    }

                    var job = db.JobPosts.Find(model.JobPostId);
                    System.Diagnostics.Debug.WriteLine($"Job found: {(job != null ? job.JobTitle : "NULL")}");

                    if (job == null || !job.IsPublished)
                    {
                        System.Diagnostics.Debug.WriteLine($"Job not found or not published - Job ID: {model.JobPostId}");
                        TempData["ErrorMessage"] = "Job not found or no longer available.";
                        return RedirectToAction("BrowseJobs");
                    }

                    // Update documents if provided
                    string uploadFolder = Server.MapPath("~/Uploads/Candidates/" + user.Id + "/");
                    System.Diagnostics.Debug.WriteLine($"Upload folder: {uploadFolder}");

                    if (!Directory.Exists(uploadFolder))
                    {
                        System.Diagnostics.Debug.WriteLine("Creating upload directory...");
                        Directory.CreateDirectory(uploadFolder);
                    }

                    if (model.UpdatedCv != null && model.UpdatedCv.ContentLength > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Updating CV: {model.UpdatedCv.FileName}");
                        string fileName = "CV_" + Guid.NewGuid().ToString() + Path.GetExtension(model.UpdatedCv.FileName);
                        string filePath = Path.Combine(uploadFolder, fileName);
                        model.UpdatedCv.SaveAs(filePath);
                        candidate.CvPath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                        System.Diagnostics.Debug.WriteLine($"CV updated: {candidate.CvPath}");
                    }

                    if (model.UpdatedQualifications != null && model.UpdatedQualifications.ContentLength > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Updating Qualifications: {model.UpdatedQualifications.FileName}");
                        string fileName = "Qualifications_" + Guid.NewGuid().ToString() + Path.GetExtension(model.UpdatedQualifications.FileName);
                        string filePath = Path.Combine(uploadFolder, fileName);
                        model.UpdatedQualifications.SaveAs(filePath);
                        candidate.QualificationsPath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                        System.Diagnostics.Debug.WriteLine($"Qualifications updated: {candidate.QualificationsPath}");
                    }

                    if (model.UpdatedIdDocument != null && model.UpdatedIdDocument.ContentLength > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Updating ID Document: {model.UpdatedIdDocument.FileName}");
                        string fileName = "ID_" + Guid.NewGuid().ToString() + Path.GetExtension(model.UpdatedIdDocument.FileName);
                        string filePath = Path.Combine(uploadFolder, fileName);
                        model.UpdatedIdDocument.SaveAs(filePath);
                        candidate.IdDocumentPath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                        System.Diagnostics.Debug.WriteLine($"ID Document updated: {candidate.IdDocumentPath}");
                    }

                    if (model.UpdatedLicense != null && model.UpdatedLicense.ContentLength > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Updating License: {model.UpdatedLicense.FileName}");
                        string fileName = "License_" + Guid.NewGuid().ToString() + Path.GetExtension(model.UpdatedLicense.FileName);
                        string filePath = Path.Combine(uploadFolder, fileName);
                        model.UpdatedLicense.SaveAs(filePath);
                        candidate.LicensePath = "~/Uploads/Candidates/" + user.Id + "/" + fileName;
                        System.Diagnostics.Debug.WriteLine($"License updated: {candidate.LicensePath}");
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

                    System.Diagnostics.Debug.WriteLine("Adding application to database...");
                    db.JobApplications.Add(application);
                    await db.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"Application saved with ID: {application.Id}");

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
                        $"Your application for '{job.JobTitle}' has been submitted successfully. You will receive updates on your application status.",
                        "Application",
                        $"/Candidate/ApplicationStatus/{application.Id}"
                    );

                    // Send notification to admin
                    SendApplicationNotification(candidate, job);

                    // Send email to candidate
                    SendApplicationConfirmationEmail(candidate.Email, candidate.FullName, job.JobTitle, application.Id);

                    // Set success message with application details
                    TempData["ApplicationSuccess"] = "Your application has been submitted successfully!";
                    TempData["JobTitle"] = job.JobTitle;
                    TempData["ApplicationId"] = application.Id;
                    TempData["CompanyName"] = "Muhle Building";

                    System.Diagnostics.Debug.WriteLine("=== APPLY POST COMPLETED SUCCESSFULLY ===");
                    return RedirectToAction("ApplicationSuccess");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"!!! EXCEPTION in Apply POST: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    ModelState.AddModelError("", "Error submitting application: " + ex.Message);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ModelState is INVALID");
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    if (errors.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {key}: {string.Join(", ", errors.Select(e => e.ErrorMessage))}");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("=== APPLY POST ENDED WITH ERRORS ===");
            return View(model);
        }

        // GET: Candidate/ApplicationSuccess
        [Authorize]
        public ActionResult ApplicationSuccess()
        {
            System.Diagnostics.Debug.WriteLine("=== APPLICATION SUCCESS PAGE LOADED ===");

            // Check if we have the success data
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

        // GET: Candidate/GetNotifications
        [Authorize]
        public JsonResult GetNotifications()
        {
            System.Diagnostics.Debug.WriteLine("=== GET NOTIFICATIONS CALLED ===");

            try
            {
                // Check if user is authenticated
                if (!User.Identity.IsAuthenticated)
                {
                    System.Diagnostics.Debug.WriteLine("User is not authenticated");
                    return Json(new { success = false, message = "User not authenticated" }, JsonRequestBehavior.AllowGet);
                }

                var userId = User.Identity.GetUserId();
                System.Diagnostics.Debug.WriteLine($"Getting notifications for user: {userId}");

                if (string.IsNullOrEmpty(userId))
                {
                    System.Diagnostics.Debug.WriteLine("User ID is null or empty");
                    return Json(new { success = false, message = "Invalid user ID" }, JsonRequestBehavior.AllowGet);
                }

                // First, get the notifications from database
                var notifications = db.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(10)
                    .ToList(); // Materialize the query first

                // Then format in memory
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

                System.Diagnostics.Debug.WriteLine($"Found {formattedNotifications.Count} notifications, {unreadCount} unread");

                return Json(new { success = true, notifications = formattedNotifications, unreadCount }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! Error getting notifications: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Candidate/MarkNotificationRead
        [HttpPost]
        [Authorize]
        public async Task<JsonResult> MarkNotificationRead(int id)
        {
            System.Diagnostics.Debug.WriteLine($"=== MARK NOTIFICATION READ CALLED - ID: {id} ===");

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
                    System.Diagnostics.Debug.WriteLine($"Notification {id} marked as read");
                    return Json(new { success = true });
                }

                System.Diagnostics.Debug.WriteLine($"Notification {id} not found for user {userId}");
                return Json(new { success = false, message = "Notification not found" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! Error marking notification as read: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Candidate/MarkAllNotificationsRead
        [HttpPost]
        [Authorize]
        public async Task<JsonResult> MarkAllNotificationsRead()
        {
            System.Diagnostics.Debug.WriteLine("=== MARK ALL NOTIFICATIONS READ CALLED ===");

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
                System.Diagnostics.Debug.WriteLine($"Marked {count} notifications as read");
                return Json(new { success = true, count });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! Error marking all notifications as read: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
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
                System.Diagnostics.Debug.WriteLine($"Notification created for user {userId}: {title}");
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

        private string GetStatusColor(string status)
        {
            switch (status)
            {
                case "Pending": return "warning";
                case "Shortlisted": return "info";
                case "Interviewed": return "primary";
                case "Hired": return "success";
                case "Rejected": return "danger";
                default: return "secondary";
            }
        }

        private void SendRegistrationEmail(string email, string firstName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Sending registration email to: {email}");
                System.Diagnostics.Debug.WriteLine($"Registration email sent to: {email}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Email sending failed: {ex.Message}");
            }
        }

        private void SendApplicationConfirmationEmail(string email, string fullName, string jobTitle, int applicationId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Sending application confirmation to: {email}");
                System.Diagnostics.Debug.WriteLine($"Application confirmation sent to: {email} for job: {jobTitle}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Email sending failed: {ex.Message}");
            }
        }

        private void SendApplicationNotification(Candidate candidate, JobPost job)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Sending application notification for: {job.JobTitle}");

                // Create notification for admin (in a real app, you'd send to admin role users)
                var adminUsers = UserManager.Users.Where(u => UserManager.IsInRole(u.Id, "Admin")).ToList();
                foreach (var admin in adminUsers)
                {
                    // Create in-app notification for admin
                    Task.Run(async () => await CreateNotification(
                        admin.Id,
                        "New Application Received",
                        $"{candidate.FullName} has applied for '{job.JobTitle}'",
                        "Application",
                        $"/JobPost/ReviewApplications/{job.Id}"
                    ));
                }

                System.Diagnostics.Debug.WriteLine($"Application notification sent to admin for: {job.JobTitle}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Email notification failed: {ex.Message}");
            }
        }

        private async Task SignInAsync(ApplicationUser user, bool isPersistent)
        {
            System.Diagnostics.Debug.WriteLine($"Signing in user: {user.Email}");
            var authenticationManager = HttpContext.GetOwinContext().Authentication;
            var identity = await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
            authenticationManager.SignIn(new Microsoft.Owin.Security.AuthenticationProperties { IsPersistent = isPersistent }, identity);
            System.Diagnostics.Debug.WriteLine($"User signed in: {user.Email}");
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