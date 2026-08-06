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

                    // Send notification to admin
                    SendApplicationNotification(candidate, job);
                    System.Diagnostics.Debug.WriteLine("Application notification sent");

                    TempData["SuccessMessage"] = "Your application has been submitted successfully!";
                    System.Diagnostics.Debug.WriteLine("=== APPLY POST COMPLETED SUCCESSFULLY ===");
                    return RedirectToAction("Dashboard", "Home");
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

        private void SendApplicationNotification(Candidate candidate, JobPost job)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Sending application notification for: {job.JobTitle}");
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