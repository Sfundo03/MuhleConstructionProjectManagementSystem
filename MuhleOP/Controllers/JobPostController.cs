using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using MuhleOP.Models;
using MuhleOP.ViewModels;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace MuhleOP.Controllers
{
    [Authorize]
    public class JobPostController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        [Authorize(Roles = "Admin")]
        public ActionResult Index()
        {
            System.Diagnostics.Debug.WriteLine("=== JOB POST INDEX LOADED ===");

            var jobPosts = db.JobPosts
                .OrderByDescending(j => j.DatePosted)
                .ToList();

            var viewModels = jobPosts.Select(j => new JobPostViewModel
            {
                Id = j.Id,
                JobTitle = j.JobTitle,
                JobDescription = j.JobDescription,
                Requirements = j.Requirements,
                ClosingDate = j.ClosingDate,
                JobType = j.JobType,
                ExperienceLevel = j.ExperienceLevel,
                Location = j.Location,
                SalaryRange = j.SalaryRange,
                NumberOfOpenings = j.NumberOfOpenings,
                Department = j.Department,
                IsPublished = j.IsPublished,
                DatePosted = j.DatePosted,
                PostedBy = j.PostedBy,
                ApplicationCount = db.JobApplications.Count(a => a.JobPostId == j.Id)
            }).ToList();

            return View(viewModels);
        }

        // GET: JobPost/AllApplications
        [Authorize(Roles = "Admin")]
        public ActionResult AllApplications()
        {
            System.Diagnostics.Debug.WriteLine("=== ALL APPLICATIONS LOADED ===");

            try
            {
                var allApplications = db.JobApplications
                    .OrderByDescending(a => a.ApplicationDate)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Found {allApplications.Count} total applications");

                var viewModels = new List<ApplicationReviewViewModel>();

                foreach (var app in allApplications)
                {
                    var candidate = db.Candidates.Find(app.CandidateId);
                    var jobPost = db.JobPosts.Find(app.JobPostId);

                    if (candidate == null || jobPost == null) continue;

                    var viewModel = new ApplicationReviewViewModel
                    {
                        ApplicationId = app.Id,
                        JobPostId = jobPost.Id,
                        JobTitle = jobPost.JobTitle,
                        CandidateId = candidate.Id,
                        CandidateFullName = candidate.FullName,
                        CandidateEmail = candidate.Email,
                        CandidatePhone = candidate.PhoneNumber,
                        CandidateNationality = candidate.Nationality,
                        CandidateDateOfBirth = candidate.DateOfBirth,
                        CandidateGender = candidate.Gender,
                        CandidateCurrentAddress = candidate.CurrentAddress,
                        IdDocumentPath = candidate.IdDocumentPath,
                        QualificationsPath = candidate.QualificationsPath,
                        CvPath = candidate.CvPath,
                        LicensePath = candidate.LicensePath,
                        ProfilePicturePath = candidate.ProfilePicturePath,
                        CoverLetter = app.CoverLetter,
                        ApplicationDate = app.ApplicationDate,
                        Status = app.Status,
                        RankingScore = app.RankingScore,
                        SkillsMatchPercentage = app.SkillsMatchPercentage,
                        HasMatric = app.HasMatric,
                        HasLicense = app.HasLicense,
                        QualificationLevel = app.QualificationLevel,
                        IsShortlisted = app.IsShortlisted,
                        InterviewScheduled = app.InterviewScheduled,
                        InterviewDateTime = app.InterviewDateTime,
                        InterviewNotes = app.InterviewNotes,
                        ExtractedSkills = new List<string>(),
                        ExtractedQualifications = new List<string>(),
                        YearsOfExperience = 0,
                        HighestQualification = app.QualificationLevel,
                        HasGrade12 = app.HasMatric,
                        HasDriversLicense = app.HasLicense
                    };

                    viewModels.Add(viewModel);
                }

                // Sort by ranking score (highest first)
                viewModels = viewModels.OrderByDescending(v => v.RankingScore).ToList();

                ViewBag.TotalApplications = viewModels.Count;
                ViewBag.ShortlistedCount = viewModels.Count(v => v.IsShortlisted);
                ViewBag.InterviewScheduledCount = viewModels.Count(v => v.InterviewScheduled);

                return View(viewModels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! EXCEPTION in AllApplications: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Details(int id)
        {
            System.Diagnostics.Debug.WriteLine($"=== JOB POST DETAILS LOADED - Job ID: {id} ===");

            var jobPost = db.JobPosts.Find(id);
            if (jobPost == null)
            {
                return HttpNotFound();
            }

            var viewModel = new JobPostViewModel
            {
                Id = jobPost.Id,
                JobTitle = jobPost.JobTitle,
                JobDescription = jobPost.JobDescription,
                Requirements = jobPost.Requirements,
                ClosingDate = jobPost.ClosingDate,
                JobType = jobPost.JobType,
                ExperienceLevel = jobPost.ExperienceLevel,
                Location = jobPost.Location,
                SalaryRange = jobPost.SalaryRange,
                NumberOfOpenings = jobPost.NumberOfOpenings,
                Department = jobPost.Department,
                IsPublished = jobPost.IsPublished,
                DatePosted = jobPost.DatePosted,
                PostedBy = jobPost.PostedBy,
                ApplicationCount = db.JobApplications.Count(a => a.JobPostId == jobPost.Id)
            };

            return View(viewModel);
        }

        // GET: JobPost/ReviewApplications/5
        [Authorize(Roles = "Admin")]
        public ActionResult ReviewApplications(int id)
        {
            System.Diagnostics.Debug.WriteLine($"=== REVIEW APPLICATIONS LOADED - Job ID: {id} ===");

            try
            {
                var jobPost = db.JobPosts.Find(id);
                if (jobPost == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Job post not found - ID: {id}");
                    return HttpNotFound();
                }

                var applications = db.JobApplications
                    .Where(a => a.JobPostId == id)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Found {applications.Count} applications for job: {jobPost.JobTitle}");

                var viewModels = new List<ApplicationReviewViewModel>();

                foreach (var app in applications)
                {
                    var candidate = db.Candidates.Find(app.CandidateId);
                    if (candidate == null) continue;

                    // Extract and rank the candidate
                    var rankedData = RankCandidate(candidate, jobPost);

                    // Update application with ranking data
                    app.RankingScore = rankedData.RankingScore;
                    app.SkillsMatchPercentage = rankedData.SkillsMatchPercentage;
                    app.HasMatric = rankedData.HasGrade12;
                    app.HasLicense = rankedData.HasDriversLicense;
                    app.QualificationLevel = rankedData.HighestQualification;

                    // Update interview scheduled status
                    app.InterviewScheduled = app.InterviewDateTime.HasValue;

                    var viewModel = new ApplicationReviewViewModel
                    {
                        ApplicationId = app.Id,
                        JobPostId = jobPost.Id,
                        JobTitle = jobPost.JobTitle,
                        CandidateId = candidate.Id,
                        CandidateFullName = candidate.FullName,
                        CandidateEmail = candidate.Email,
                        CandidatePhone = candidate.PhoneNumber,
                        CandidateNationality = candidate.Nationality,
                        CandidateDateOfBirth = candidate.DateOfBirth,
                        CandidateGender = candidate.Gender,
                        CandidateCurrentAddress = candidate.CurrentAddress,
                        IdDocumentPath = candidate.IdDocumentPath,
                        QualificationsPath = candidate.QualificationsPath,
                        CvPath = candidate.CvPath,
                        LicensePath = candidate.LicensePath,
                        ProfilePicturePath = candidate.ProfilePicturePath,
                        CoverLetter = app.CoverLetter,
                        ApplicationDate = app.ApplicationDate,
                        Status = app.Status,
                        RankingScore = rankedData.RankingScore,
                        SkillsMatchPercentage = rankedData.SkillsMatchPercentage,
                        HasMatric = rankedData.HasGrade12,
                        HasLicense = rankedData.HasDriversLicense,
                        QualificationLevel = rankedData.HighestQualification,
                        IsShortlisted = app.IsShortlisted,
                        InterviewScheduled = app.InterviewScheduled,
                        InterviewDateTime = app.InterviewDateTime,
                        InterviewNotes = app.InterviewNotes,
                        ExtractedSkills = rankedData.ExtractedSkills,
                        ExtractedQualifications = rankedData.ExtractedQualifications,
                        YearsOfExperience = rankedData.YearsOfExperience,
                        HighestQualification = rankedData.HighestQualification,
                        HasGrade12 = rankedData.HasGrade12,
                        HasDriversLicense = rankedData.HasDriversLicense
                    };

                    viewModels.Add(viewModel);
                }

                // Sort by ranking score (highest first)
                viewModels = viewModels.OrderByDescending(v => v.RankingScore).ToList();

                System.Diagnostics.Debug.WriteLine($"Returning {viewModels.Count} ranked applications");
                ViewBag.JobTitle = jobPost.JobTitle;
                ViewBag.JobPostId = jobPost.Id;
                return View(viewModels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! EXCEPTION in ReviewApplications: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        // GET: JobPost/CandidateDetails/5
        [Authorize(Roles = "Admin")]
        public ActionResult CandidateDetails(int id)
        {
            System.Diagnostics.Debug.WriteLine($"=== CANDIDATE DETAILS LOADED - Candidate ID: {id} ===");

            try
            {
                var candidate = db.Candidates.Find(id);
                if (candidate == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Candidate not found - ID: {id}");
                    return HttpNotFound();
                }

                // Get all applications for this candidate
                var applications = db.JobApplications
                    .Where(a => a.CandidateId == candidate.Id)
                    .OrderByDescending(a => a.ApplicationDate)
                    .ToList();

                var applicationHistory = applications.Select(app => new ApplicationHistoryViewModel
                {
                    ApplicationId = app.Id,
                    JobTitle = app.JobPost?.JobTitle ?? "Unknown",
                    ApplicationDate = app.ApplicationDate,
                    Status = app.Status,
                    RankingScore = app.RankingScore,
                    SkillsMatchPercentage = app.SkillsMatchPercentage,
                    IsShortlisted = app.IsShortlisted,
                    InterviewScheduled = app.InterviewScheduled,
                    InterviewDateTime = app.InterviewDateTime,
                    InterviewNotes = app.InterviewNotes
                }).ToList();

                var viewModel = new CandidateDetailsViewModel
                {
                    Id = candidate.Id,
                    FirstName = candidate.FirstName,
                    LastName = candidate.LastName,
                    FullName = candidate.FullName,
                    Email = candidate.Email,
                    PhoneNumber = candidate.PhoneNumber,
                    DateOfBirth = candidate.DateOfBirth,
                    Gender = candidate.Gender,
                    Nationality = candidate.Nationality,
                    CurrentAddress = candidate.CurrentAddress,
                    IdDocumentPath = candidate.IdDocumentPath,
                    QualificationsPath = candidate.QualificationsPath,
                    CvPath = candidate.CvPath,
                    LicensePath = candidate.LicensePath,
                    ProfilePicturePath = candidate.ProfilePicturePath,
                    RegistrationDate = candidate.RegistrationDate,
                    IsActive = candidate.IsActive,
                    ApplicationHistory = applicationHistory
                };

                System.Diagnostics.Debug.WriteLine($"Candidate details loaded for: {candidate.FullName}");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! EXCEPTION in CandidateDetails: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        // POST: JobPost/ShortlistCandidates
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ShortlistCandidates(BulkShortlistViewModel model)
        {
            System.Diagnostics.Debug.WriteLine($"=== SHORTLIST CANDIDATES POST STARTED ===");
            System.Diagnostics.Debug.WriteLine($"JobPostId: {model.JobPostId}");
            System.Diagnostics.Debug.WriteLine($"ApplicationIds: {(model.ApplicationIds != null ? string.Join(", ", model.ApplicationIds) : "NULL")}");

            try
            {
                if (model.ApplicationIds == null || !model.ApplicationIds.Any())
                {
                    System.Diagnostics.Debug.WriteLine("No applications selected for shortlisting");
                    TempData["ErrorMessage"] = "Please select at least one candidate to shortlist.";
                    return RedirectToAction("ReviewApplications", new { id = model.JobPostId });
                }

                foreach (var appId in model.ApplicationIds)
                {
                    var application = await db.JobApplications.FindAsync(appId);
                    if (application != null)
                    {
                        application.IsShortlisted = true;
                        application.Status = "Shortlisted";
                        System.Diagnostics.Debug.WriteLine($"Application {appId} shortlisted");

                        // Create status history
                        var history = new ApplicationStatusHistory
                        {
                            ApplicationId = application.Id,
                            Status = "Shortlisted",
                            Notes = "Candidate was shortlisted for interview",
                            ChangedBy = User.Identity.Name
                        };
                        db.ApplicationStatusHistories.Add(history);

                        // Create notification for candidate
                        await CreateNotificationForCandidate(
                            application.CandidateId,
                            "Application Shortlisted",
                            $"Your application for '{application.JobPost?.JobTitle ?? "the position"}' has been shortlisted!",
                            "Status",
                            $"/Candidate/ApplicationStatus/{application.Id}"
                        );
                    }
                }

                await db.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"Successfully shortlisted {model.ApplicationIds.Count} candidates");

                TempData["SuccessMessage"] = $"{model.ApplicationIds.Count} candidate(s) have been shortlisted successfully!";
                return RedirectToAction("ReviewApplications", new { id = model.JobPostId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! EXCEPTION in ShortlistCandidates: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "An error occurred while shortlisting candidates: " + ex.Message;
                return RedirectToAction("ReviewApplications", new { id = model.JobPostId });
            }
        }

        // GET: JobPost/ScheduleInterview/5
        [Authorize(Roles = "Admin")]
        public ActionResult ScheduleInterview(int id)
        {
            System.Diagnostics.Debug.WriteLine($"=== SCHEDULE INTERVIEW LOADED - Application ID: {id} ===");

            try
            {
                var application = db.JobApplications.Find(id);
                if (application == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Application not found - ID: {id}");
                    return HttpNotFound();
                }

                var candidate = db.Candidates.Find(application.CandidateId);
                var jobPost = db.JobPosts.Find(application.JobPostId);

                if (candidate == null || jobPost == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Candidate or JobPost not found");
                    return HttpNotFound();
                }

                var viewModel = new ScheduleInterviewViewModel
                {
                    ApplicationId = application.Id,
                    CandidateId = candidate.Id,
                    CandidateFullName = candidate.FullName,
                    JobTitle = jobPost.JobTitle,
                    InterviewDateTime = application.InterviewDateTime ?? DateTime.Now.AddDays(3).Date.AddHours(10),
                    InterviewNotes = application.InterviewNotes,
                    InterviewLocation = "Muhle Building Office",
                    InterviewType = "In-person"
                };

                System.Diagnostics.Debug.WriteLine($"Schedule interview prepared for: {candidate.FullName}");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! EXCEPTION in ScheduleInterview: {ex.Message}");
                throw;
            }
        }

        // POST: JobPost/ScheduleInterview
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ScheduleInterview(ScheduleInterviewViewModel model)
        {
            System.Diagnostics.Debug.WriteLine($"=== SCHEDULE INTERVIEW POST STARTED ===");
            System.Diagnostics.Debug.WriteLine($"ApplicationId: {model.ApplicationId}");
            System.Diagnostics.Debug.WriteLine($"InterviewDateTime: {model.InterviewDateTime}");
            System.Diagnostics.Debug.WriteLine($"InterviewType: {model.InterviewType}");

            if (ModelState.IsValid)
            {
                try
                {
                    var application = await db.JobApplications.FindAsync(model.ApplicationId);
                    if (application == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Application not found - ID: {model.ApplicationId}");
                        return HttpNotFound();
                    }

                    application.InterviewDateTime = model.InterviewDateTime;
                    application.InterviewNotes = model.InterviewNotes;
                    application.InterviewScheduled = true;
                    application.Status = "Interviewed";

                    // Create status history
                    var history = new ApplicationStatusHistory
                    {
                        ApplicationId = application.Id,
                        Status = "Interviewed",
                        Notes = $"Interview scheduled for {model.InterviewDateTime:yyyy-MM-dd HH:mm}",
                        ChangedBy = User.Identity.Name
                    };
                    db.ApplicationStatusHistories.Add(history);

                    await db.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"Interview scheduled for application {model.ApplicationId}");

                    // Create notification for candidate
                    await CreateNotificationForCandidate(
                        application.CandidateId,
                        "Interview Scheduled",
                        $"Your interview for '{model.JobTitle}' has been scheduled for {model.InterviewDateTime:dd MMM yyyy HH:mm}",
                        "Interview",
                        $"/Candidate/ApplicationStatus/{application.Id}"
                    );

                    // Send interview notification email
                    SendInterviewNotification(application);

                    TempData["SuccessMessage"] = $"Interview scheduled successfully for {model.CandidateFullName} on {model.InterviewDateTime:yyyy-MM-dd HH:mm}";
                    return RedirectToAction("ReviewApplications", new { id = application.JobPostId });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"!!! EXCEPTION in ScheduleInterview POST: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                    ModelState.AddModelError("", "Error scheduling interview: " + ex.Message);
                }
            }

            // If we got this far, something failed
            var app = db.JobApplications.Find(model.ApplicationId);
            if (app != null)
            {
                var candidate = db.Candidates.Find(app.CandidateId);
                if (candidate != null)
                {
                    model.CandidateFullName = candidate.FullName;
                }
                var jobPost = db.JobPosts.Find(app.JobPostId);
                if (jobPost != null)
                {
                    model.JobTitle = jobPost.JobTitle;
                }
            }

            return View(model);
        }

        // POST: JobPost/UpdateApplicationStatus
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> UpdateApplicationStatus(int applicationId, string status, string notes)
        {
            try
            {
                var application = await db.JobApplications.FindAsync(applicationId);
                if (application == null)
                {
                    return Json(new { success = false, message = "Application not found" });
                }

                var oldStatus = application.Status;
                application.Status = status;

                // Update specific status flags
                if (status == "Shortlisted")
                {
                    application.IsShortlisted = true;
                }
                else if (status == "Interviewed")
                {
                    application.InterviewScheduled = true;
                }
                else if (status == "Hired")
                {
                    // Additional logic for hired status
                }
                else if (status == "Rejected")
                {
                    // Additional logic for rejected status
                }

                // Create status history
                var history = new ApplicationStatusHistory
                {
                    ApplicationId = applicationId,
                    Status = status,
                    Notes = string.IsNullOrEmpty(notes) ? $"Status changed from {oldStatus} to {status}" : notes,
                    ChangedBy = User.Identity.Name
                };
                db.ApplicationStatusHistories.Add(history);

                await db.SaveChangesAsync();

                // Get candidate info
                var candidate = db.Candidates.Find(application.CandidateId);
                if (candidate != null)
                {
                    // Create notification for candidate
                    await CreateNotificationForCandidate(
                        candidate.Id,
                        $"Application {status}",
                        $"Your application for '{application.JobPost?.JobTitle ?? "the position"}' has been updated to: {status}",
                        "Status",
                        $"/Candidate/ApplicationStatus/{applicationId}"
                    );
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Ranking Method
        private ApplicationReviewViewModel RankCandidate(Candidate candidate, JobPost jobPost)
        {
            System.Diagnostics.Debug.WriteLine($"=== RANKING CANDIDATE STARTED ===");
            System.Diagnostics.Debug.WriteLine($"Candidate: {candidate.FullName}");
            System.Diagnostics.Debug.WriteLine($"Job: {jobPost.JobTitle}");

            var result = new ApplicationReviewViewModel
            {
                ExtractedSkills = new List<string>(),
                ExtractedQualifications = new List<string>(),
                YearsOfExperience = 0,
                HighestQualification = "None",
                HasGrade12 = false,
                HasDriversLicense = false,
                SkillsMatchPercentage = 0,
                RankingScore = 0
            };

            try
            {
                // Extract text from CV if file exists
                string cvText = "";
                if (!string.IsNullOrEmpty(candidate.CvPath))
                {
                    string cvFullPath = Server.MapPath(candidate.CvPath);
                    System.Diagnostics.Debug.WriteLine($"CV path: {cvFullPath}");

                    if (System.IO.File.Exists(cvFullPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"CV found at: {cvFullPath}");
                        cvText = ExtractTextFromFile(cvFullPath);
                        System.Diagnostics.Debug.WriteLine($"CV text extracted: {(string.IsNullOrEmpty(cvText) ? "EMPTY" : cvText.Substring(0, Math.Min(500, cvText.Length)))}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"CV file not found: {cvFullPath}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No CV path found for candidate");
                }

                // Extract skills from CV
                var skills = ExtractSkills(cvText);
                result.ExtractedSkills = skills;
                System.Diagnostics.Debug.WriteLine($"Extracted {skills.Count} skills: {string.Join(", ", skills.Take(5))}");

                // Extract qualifications from CV
                var qualifications = ExtractQualifications(cvText);
                result.ExtractedQualifications = qualifications;
                System.Diagnostics.Debug.WriteLine($"Extracted {qualifications.Count} qualifications");

                // Extract experience
                result.YearsOfExperience = ExtractYearsOfExperience(cvText);
                System.Diagnostics.Debug.WriteLine($"Years of experience: {result.YearsOfExperience}");

                // Check for matric/Grade 12
                result.HasGrade12 = CheckForMatric(cvText);
                System.Diagnostics.Debug.WriteLine($"Has Matric/Grade 12: {result.HasGrade12}");

                // Check for driver's license
                result.HasDriversLicense = CheckForLicense(candidate.LicensePath, cvText);
                System.Diagnostics.Debug.WriteLine($"Has Driver's License: {result.HasDriversLicense}");

                // Determine highest qualification
                result.HighestQualification = DetermineHighestQualification(cvText);
                System.Diagnostics.Debug.WriteLine($"Highest Qualification: {result.HighestQualification}");

                // Calculate skills match percentage
                var jobRequirements = ExtractSkills(jobPost.Requirements);
                result.SkillsMatchPercentage = CalculateSkillsMatch(skills, jobRequirements);
                System.Diagnostics.Debug.WriteLine($"Skills Match Percentage: {result.SkillsMatchPercentage}%");

                // Calculate overall ranking score
                result.RankingScore = CalculateRankingScore(
                    result.SkillsMatchPercentage,
                    result.YearsOfExperience,
                    result.HighestQualification,
                    result.HasGrade12,
                    result.HasDriversLicense
                );
                System.Diagnostics.Debug.WriteLine($"Ranking Score: {result.RankingScore:F2}");

                // Check if candidate has matric (for ranking)
                result.HasMatric = result.HasGrade12;
                System.Diagnostics.Debug.WriteLine($"Has Matric (for ranking): {result.HasMatric}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! EXCEPTION in RankCandidate: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                // Return default values on error
            }

            System.Diagnostics.Debug.WriteLine($"=== RANKING CANDIDATE COMPLETED ===");
            return result;
        }

        private string ExtractTextFromFile(string filePath)
        {
            try
            {
                string extension = System.IO.Path.GetExtension(filePath).ToLower();
                System.Diagnostics.Debug.WriteLine($"Extracting text from: {filePath} (Extension: {extension})");

                if (extension == ".txt")
                {
                    return System.IO.File.ReadAllText(filePath);
                }
                else if (extension == ".pdf")
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("Using iTextSharp to extract PDF text...");
                        StringBuilder text = new StringBuilder();
                        using (PdfReader reader = new PdfReader(filePath))
                        {
                            for (int page = 1; page <= reader.NumberOfPages; page++)
                            {
                                string pageText = PdfTextExtractor.GetTextFromPage(reader, page);
                                if (!string.IsNullOrEmpty(pageText))
                                {
                                    text.Append(pageText);
                                    text.Append(" ");
                                }
                            }
                        }
                        string result = text.ToString().Trim();
                        System.Diagnostics.Debug.WriteLine($"PDF extracted {result.Length} characters");
                        return result;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"iTextSharp extraction failed: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                        return "";
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Unsupported file type: {extension}. Only .pdf and .txt are supported.");
                    return "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting text: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return "";
            }
        }

        private List<string> ExtractSkills(string text)
        {
            var skills = new List<string>();
            if (string.IsNullOrEmpty(text)) return skills;

            string lowerText = text.ToLower();

            // Common construction and management skills
            var skillKeywords = new List<string>
            {
                "project management", "construction", "safety", "compliance", "team leadership",
                "budgeting", "scheduling", "quality control", "risk management", "communication",
                "problem solving", "decision making", "planning", "organization", "leadership",
                "ms office", "excel", "word", "project planning", "site management", "supervision",
                "contract management", "procurement", "cost control", "safety compliance",
                "building codes", "blueprint reading", "estimation", "negotiation", "reporting",
                "civil engineering", "structural", "architectural", "mechanical", "electrical",
                "plumbing", "hvac", "environmental", "health and safety", "risk assessment",
                "project coordination", "stakeholder management", "time management", "teamwork",
                "critical thinking", "attention to detail", "multitasking", "adaptability",
                "autocad", "revit", "microsoft project", "primavera", "microsoft office",
                "project management", "construction management", "site supervision"
            };

            foreach (var skill in skillKeywords)
            {
                if (lowerText.Contains(skill))
                {
                    skills.Add(skill);
                }
            }

            return skills.Distinct().ToList();
        }

        private List<string> ExtractQualifications(string text)
        {
            var qualifications = new List<string>();
            if (string.IsNullOrEmpty(text)) return qualifications;

            string lowerText = text.ToLower();

            var qualificationKeywords = new List<string>
            {
                "bachelor", "degree", "diploma", "honours", "masters", "phd",
                "certificate", "certification", "btch", "bcom", "ba", "bsc",
                "higher certificate", "advanced diploma", "postgraduate",
                "national diploma", "graduate", "undergraduate", "postgraduate",
                "b eng", "bsc eng", "btch eng", "ndip", "certificate in"
            };

            foreach (var qual in qualificationKeywords)
            {
                if (lowerText.Contains(qual))
                {
                    qualifications.Add(qual);
                }
            }

            return qualifications.Distinct().ToList();
        }

        private int ExtractYearsOfExperience(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            string lowerText = text.ToLower();

            // Look for patterns like "5 years", "5+ years", "5 years experience"
            var regex = new Regex(@"(\d+)\s*\+\s*years|\b(\d+)\s*years\b");
            var matches = regex.Matches(lowerText);

            int maxYears = 0;
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                int years;
                if (int.TryParse(match.Groups[1].Value, out years) || int.TryParse(match.Groups[2].Value, out years))
                {
                    if (years > maxYears) maxYears = years;
                }
            }

            return maxYears;
        }

        private bool CheckForMatric(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            string lowerText = text.ToLower();
            return lowerText.Contains("matric") ||
                   lowerText.Contains("grade 12") ||
                   lowerText.Contains("grade12") ||
                   lowerText.Contains("secondary school") ||
                   lowerText.Contains("high school diploma");
        }

        private bool CheckForLicense(string licensePath, string cvText)
        {
            // Check if license file exists
            if (!string.IsNullOrEmpty(licensePath))
            {
                string fullPath = Server.MapPath(licensePath);
                if (System.IO.File.Exists(fullPath))
                {
                    return true;
                }
            }

            // Check CV text for license keywords
            if (!string.IsNullOrEmpty(cvText))
            {
                string lowerText = cvText.ToLower();
                if (lowerText.Contains("driver") && lowerText.Contains("license"))
                {
                    return true;
                }
                if (lowerText.Contains("driving licence"))
                {
                    return true;
                }
                if (lowerText.Contains("code") && (lowerText.Contains(" b ") || lowerText.Contains(" c ") || lowerText.Contains(" eb ")))
                {
                    return true;
                }
                if (lowerText.Contains("driver's license") || lowerText.Contains("drivers license"))
                {
                    return true;
                }
            }

            return false;
        }

        private string DetermineHighestQualification(string text)
        {
            if (string.IsNullOrEmpty(text)) return "None";

            string lowerText = text.ToLower();

            if (lowerText.Contains("phd") || lowerText.Contains("doctorate")) return "PhD";
            if (lowerText.Contains("masters") || lowerText.Contains("master's") || lowerText.Contains("master")) return "Masters";
            if (lowerText.Contains("honours") || lowerText.Contains("hons")) return "Honours";
            if (lowerText.Contains("bachelor") || lowerText.Contains("degree") || lowerText.Contains("bsc") || lowerText.Contains("ba") || lowerText.Contains("bcom") || lowerText.Contains("b eng"))
                return "Degree";
            if (lowerText.Contains("advanced diploma")) return "Advanced Diploma";
            if (lowerText.Contains("national diploma") || lowerText.Contains("diploma")) return "Diploma";
            if (lowerText.Contains("certificate") || lowerText.Contains("certification")) return "Certificate";
            if (lowerText.Contains("matric") || lowerText.Contains("grade 12") || lowerText.Contains("grade12") || lowerText.Contains("secondary school"))
                return "Matric/Grade 12";

            return "None";
        }

        private int CalculateSkillsMatch(List<string> candidateSkills, List<string> jobSkills)
        {
            if (jobSkills == null || jobSkills.Count == 0) return 0;
            if (candidateSkills == null || candidateSkills.Count == 0) return 0;

            var matchingSkills = candidateSkills.Intersect(jobSkills, StringComparer.OrdinalIgnoreCase);
            int matchCount = matchingSkills.Count();

            return (int)Math.Round((double)matchCount / jobSkills.Count * 100);
        }

        private double CalculateRankingScore(int skillsMatch, int yearsExperience, string qualification, bool hasMatric, bool hasLicense)
        {
            double score = 0;

            // Skills match (max 40 points)
            score += skillsMatch * 0.4;

            // Experience (max 20 points)
            if (yearsExperience >= 10) score += 20;
            else if (yearsExperience >= 7) score += 18;
            else if (yearsExperience >= 5) score += 15;
            else if (yearsExperience >= 3) score += 10;
            else if (yearsExperience >= 1) score += 5;

            // Qualification (max 25 points)
            switch (qualification.ToLower())
            {
                case "phd": score += 25; break;
                case "masters": score += 22; break;
                case "honours": score += 19; break;
                case "degree": score += 15; break;
                case "advanced diploma": score += 12; break;
                case "diploma": score += 10; break;
                case "certificate": score += 5; break;
                case "matric/grade 12": score += 3; break;
                default: score += 0; break;
            }

            // Matric/Grade 12 (max 10 points)
            if (hasMatric) score += 10;

            // Driver's License (max 5 points)
            if (hasLicense) score += 5;

            return Math.Round(score, 2);
        }

        // GET: JobPost/Create
        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: JobPost/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Create(JobPostViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var jobPost = new JobPost
                {
                    JobTitle = viewModel.JobTitle,
                    JobDescription = viewModel.JobDescription,
                    Requirements = viewModel.Requirements,
                    ClosingDate = viewModel.ClosingDate,
                    JobType = viewModel.JobType,
                    ExperienceLevel = viewModel.ExperienceLevel,
                    Location = viewModel.Location,
                    SalaryRange = viewModel.SalaryRange,
                    NumberOfOpenings = viewModel.NumberOfOpenings,
                    Department = viewModel.Department,
                    IsPublished = viewModel.IsPublished,
                    DatePosted = DateTime.Now,
                    PostedBy = User.Identity.Name
                };

                db.JobPosts.Add(jobPost);
                await db.SaveChangesAsync();

                TempData["SuccessMessage"] = "Job post created successfully!";
                return RedirectToAction("Index");
            }

            return View(viewModel);
        }

        // GET: JobPost/Edit/5
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int id)
        {
            var jobPost = db.JobPosts.Find(id);
            if (jobPost == null)
            {
                return HttpNotFound();
            }

            var viewModel = new JobPostViewModel
            {
                Id = jobPost.Id,
                JobTitle = jobPost.JobTitle,
                JobDescription = jobPost.JobDescription,
                Requirements = jobPost.Requirements,
                ClosingDate = jobPost.ClosingDate,
                JobType = jobPost.JobType,
                ExperienceLevel = jobPost.ExperienceLevel,
                Location = jobPost.Location,
                SalaryRange = jobPost.SalaryRange,
                NumberOfOpenings = jobPost.NumberOfOpenings,
                Department = jobPost.Department,
                IsPublished = jobPost.IsPublished,
                DatePosted = jobPost.DatePosted,
                PostedBy = jobPost.PostedBy
            };

            return View(viewModel);
        }

        // POST: JobPost/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Edit(JobPostViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var jobPost = await db.JobPosts.FindAsync(viewModel.Id);
                if (jobPost == null)
                {
                    return HttpNotFound();
                }

                jobPost.JobTitle = viewModel.JobTitle;
                jobPost.JobDescription = viewModel.JobDescription;
                jobPost.Requirements = viewModel.Requirements;
                jobPost.ClosingDate = viewModel.ClosingDate;
                jobPost.JobType = viewModel.JobType;
                jobPost.ExperienceLevel = viewModel.ExperienceLevel;
                jobPost.Location = viewModel.Location;
                jobPost.SalaryRange = viewModel.SalaryRange;
                jobPost.NumberOfOpenings = viewModel.NumberOfOpenings;
                jobPost.Department = viewModel.Department;
                jobPost.IsPublished = viewModel.IsPublished;

                await db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Job post updated successfully!";
                return RedirectToAction("Index");
            }

            return View(viewModel);
        }

        // GET: JobPost/Delete/5
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id)
        {
            var jobPost = db.JobPosts.Find(id);
            if (jobPost == null)
            {
                return HttpNotFound();
            }

            var viewModel = new JobPostViewModel
            {
                Id = jobPost.Id,
                JobTitle = jobPost.JobTitle,
                JobDescription = jobPost.JobDescription,
                Requirements = jobPost.Requirements,
                ClosingDate = jobPost.ClosingDate,
                JobType = jobPost.JobType,
                ExperienceLevel = jobPost.ExperienceLevel,
                Location = jobPost.Location,
                SalaryRange = jobPost.SalaryRange,
                NumberOfOpenings = jobPost.NumberOfOpenings,
                Department = jobPost.Department,
                IsPublished = jobPost.IsPublished,
                DatePosted = jobPost.DatePosted,
                PostedBy = jobPost.PostedBy
            };

            return View(viewModel);
        }

        // POST: JobPost/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var jobPost = await db.JobPosts.FindAsync(id);
            if (jobPost != null)
            {
                var hasApplications = db.JobApplications.Any(a => a.JobPostId == id);
                if (hasApplications)
                {
                    TempData["ErrorMessage"] = "Cannot delete this job post because it has applications.";
                    return RedirectToAction("Index");
                }

                db.JobPosts.Remove(jobPost);
                await db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Job post deleted successfully!";
            }

            return RedirectToAction("Index");
        }

        private async Task CreateNotificationForCandidate(int candidateId, string title, string message, string type, string link)
        {
            try
            {
                var candidate = db.Candidates.Find(candidateId);
                if (candidate == null) return;

                var notification = new Notification
                {
                    UserId = candidate.UserId,
                    Title = title,
                    Message = message,
                    Type = type,
                    Link = link,
                    CreatedDate = DateTime.Now,
                    IsRead = false
                };

                db.Notifications.Add(notification);
                await db.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"Notification created for candidate {candidate.FullName}: {title}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating notification: {ex.Message}");
            }
        }

        private void SendInterviewNotification(JobApplication application)
        {
            try
            {
                var candidate = db.Candidates.Find(application.CandidateId);
                if (candidate == null) return;

                System.Diagnostics.Debug.WriteLine($"Sending interview notification to: {candidate.Email}");
                // Email implementation would go here
                System.Diagnostics.Debug.WriteLine($"Interview scheduled for {candidate.FullName} on {application.InterviewDateTime}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending interview notification: {ex.Message}");
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