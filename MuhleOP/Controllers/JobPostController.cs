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

        // GET: JobPost/Index - View all job posts
        [Authorize(Roles = "Admin")]
        public ActionResult Index()
        {
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
                IsClosed = j.IsClosed,
                IsAutoClosed = j.IsAutoClosed,
                DatePosted = j.DatePosted,
                PostedBy = j.PostedBy,
                ApplicationCount = db.JobApplications.Count(a => a.JobPostId == j.Id),
                AcceptedCount = j.AcceptedApplicantsCount
            }).ToList();

            return View(viewModels);
        }

        // GET: JobPost/Create - Use Case 1: Create Vacancy
        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: JobPost/Create - Use Case 1: Create Vacancy
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Create(JobPostViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var existingJob = db.JobPosts.FirstOrDefault(j => j.JobTitle == viewModel.JobTitle && !j.IsClosed);
                if (existingJob != null)
                {
                    ModelState.AddModelError("JobTitle", "A job post with this title already exists and is active.");
                    return View(viewModel);
                }

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
                    IsClosed = false,
                    IsAutoClosed = false,
                    DatePosted = DateTime.Now,
                    PostedBy = User.Identity.Name,
                    AcceptedApplicantsCount = 0
                };

                db.JobPosts.Add(jobPost);
                await db.SaveChangesAsync();

                await CreateNotification(
                    User.Identity.GetUserId(),
                    "Job Post Created",
                    $"Job post '{jobPost.JobTitle}' has been created successfully.",
                    "System",
                    $"/JobPost/Details/{jobPost.Id}"
                );

                TempData["SuccessMessage"] = "Job post created successfully!";
                return RedirectToAction("Index");
            }

            return View(viewModel);
        }

        // GET: JobPost/Details/5
        [Authorize(Roles = "Admin")]
        public ActionResult Details(int id)
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
                IsClosed = jobPost.IsClosed,
                IsAutoClosed = jobPost.IsAutoClosed,
                DatePosted = jobPost.DatePosted,
                PostedBy = jobPost.PostedBy,
                ApplicationCount = db.JobApplications.Count(a => a.JobPostId == jobPost.Id),
                AcceptedCount = jobPost.AcceptedApplicantsCount
            };

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
                IsClosed = jobPost.IsClosed,
                IsAutoClosed = jobPost.IsAutoClosed,
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

            var hasApplications = db.JobApplications.Any(a => a.JobPostId == id);
            if (hasApplications)
            {
                TempData["ErrorMessage"] = "Cannot delete this job post because it has applications.";
                return RedirectToAction("Index");
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
                IsClosed = jobPost.IsClosed,
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

        // POST: JobPost/CloseJobPost
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> CloseJobPost(int id)
        {
            try
            {
                var jobPost = await db.JobPosts.FindAsync(id);
                if (jobPost == null)
                {
                    return Json(new { success = false, message = "Job post not found" }, JsonRequestBehavior.AllowGet);
                }

                jobPost.IsClosed = !jobPost.IsClosed;
                if (jobPost.IsClosed)
                {
                    jobPost.IsAutoClosed = false;
                    jobPost.AutoCloseDate = DateTime.Now;

                    await CreateNotification(
                        User.Identity.GetUserId(),
                        "Job Post Closed",
                        $"Job post '{jobPost.JobTitle}' has been closed.",
                        "System",
                        $"/JobPost/Details/{jobPost.Id}"
                    );
                }
                else
                {
                    await CreateNotification(
                        User.Identity.GetUserId(),
                        "Job Post Reopened",
                        $"Job post '{jobPost.JobTitle}' has been reopened for applications.",
                        "System",
                        $"/JobPost/Details/{jobPost.Id}"
                    );
                }

                await db.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    isClosed = jobPost.IsClosed,
                    message = $"Job post {(jobPost.IsClosed ? "closed" : "reopened")} successfully"
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: JobPost/ReviewApplications/5
        [Authorize(Roles = "Admin")]
        public ActionResult ReviewApplications(int id)
        {
            try
            {
                var jobPost = db.JobPosts.Find(id);
                if (jobPost == null)
                {
                    return HttpNotFound();
                }

                var applications = db.JobApplications
                    .Where(a => a.JobPostId == id)
                    .ToList();

                var viewModels = new List<ApplicationReviewViewModel>();

                foreach (var app in applications)
                {
                    var candidate = db.Candidates.Find(app.CandidateId);
                    if (candidate == null) continue;

                    var rankedData = RankCandidate(candidate, jobPost);

                    app.RankingScore = rankedData.RankingScore;
                    app.SkillsMatchPercentage = rankedData.SkillsMatchPercentage;
                    app.HasMatric = rankedData.HasGrade12;
                    app.HasLicense = rankedData.HasDriversLicense;
                    app.QualificationLevel = rankedData.HighestQualification;
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
                        InterviewResult = app.InterviewResult ?? "Pending",
                        InterviewFeedback = app.InterviewFeedback,
                        IsHired = app.IsHired,
                        IsInterviewCompleted = app.InterviewResult != "Pending",
                        ExtractedSkills = rankedData.ExtractedSkills,
                        ExtractedQualifications = rankedData.ExtractedQualifications,
                        YearsOfExperience = rankedData.YearsOfExperience,
                        HighestQualification = rankedData.HighestQualification,
                        HasGrade12 = rankedData.HasGrade12,
                        HasDriversLicense = rankedData.HasDriversLicense
                    };

                    viewModels.Add(viewModel);
                }

                viewModels = viewModels.OrderByDescending(v => v.RankingScore).ToList();

                ViewBag.JobTitle = jobPost.JobTitle;
                ViewBag.JobPostId = jobPost.Id;
                ViewBag.TotalApplications = viewModels.Count;
                ViewBag.ShortlistedCount = viewModels.Count(v => v.IsShortlisted);
                ViewBag.InterviewScheduledCount = viewModels.Count(v => v.InterviewScheduled);

                return View(viewModels);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading applications: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: JobPost/ShortlistCandidates
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ShortlistCandidates(BulkShortlistViewModel model)
        {
            try
            {
                if (model.ApplicationIds == null || !model.ApplicationIds.Any())
                {
                    TempData["ErrorMessage"] = "Please select at least one candidate to shortlist.";
                    return RedirectToAction("ReviewApplications", new { id = model.JobPostId });
                }

                int shortlistedCount = 0;
                foreach (var appId in model.ApplicationIds)
                {
                    var application = await db.JobApplications.FindAsync(appId);
                    if (application != null && !application.IsShortlisted && application.InterviewResult == "Pending")
                    {
                        application.IsShortlisted = true;
                        application.Status = "Shortlisted";
                        application.ShortlistDate = DateTime.Now;

                        var history = new ApplicationStatusHistory
                        {
                            ApplicationId = application.Id,
                            Status = "Shortlisted",
                            Notes = "Candidate was shortlisted for interview based on CV ranking.",
                            ChangedBy = User.Identity.Name
                        };
                        db.ApplicationStatusHistories.Add(history);

                        await CreateNotificationForCandidate(
                            application.CandidateId,
                            "Application Shortlisted",
                            $"Your application for '{application.JobPost?.JobTitle ?? "the position"}' has been shortlisted!",
                            "Status",
                            $"/Candidate/ApplicationStatus/{application.Id}"
                        );

                        shortlistedCount++;
                    }
                }

                await db.SaveChangesAsync();

                var jobPost = await db.JobPosts.FindAsync(model.JobPostId);
                if (jobPost != null)
                {
                    jobPost.AcceptedApplicantsCount += shortlistedCount;

                    if (jobPost.AcceptedApplicantsCount >= jobPost.NumberOfOpenings && !jobPost.IsClosed)
                    {
                        jobPost.IsClosed = true;
                        jobPost.IsAutoClosed = true;
                        jobPost.AutoCloseDate = DateTime.Now;

                        await CreateNotification(
                            User.Identity.GetUserId(),
                            "Job Post Auto-Closed",
                            $"Job post '{jobPost.JobTitle}' has been automatically closed because all positions have been filled.",
                            "System",
                            $"/JobPost/Details/{jobPost.Id}"
                        );
                    }

                    await db.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"{shortlistedCount} candidate(s) have been shortlisted successfully!";
                return RedirectToAction("ReviewApplications", new { id = model.JobPostId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while shortlisting candidates: " + ex.Message;
                return RedirectToAction("ReviewApplications", new { id = model.JobPostId });
            }
        }

        // GET: JobPost/ScheduleInterview/5
        [Authorize(Roles = "Admin")]
        public ActionResult ScheduleInterview(int id)
        {
            try
            {
                var application = db.JobApplications.Find(id);
                if (application == null)
                {
                    return HttpNotFound();
                }

                if (application.InterviewScheduled || application.InterviewResult != "Pending")
                {
                    TempData["ErrorMessage"] = "This candidate already has an interview scheduled or the interview has been completed.";
                    return RedirectToAction("ReviewApplications", new { id = application.JobPostId });
                }

                var candidate = db.Candidates.Find(application.CandidateId);
                var jobPost = db.JobPosts.Find(application.JobPostId);

                if (candidate == null || jobPost == null)
                {
                    return HttpNotFound();
                }

                var viewModel = new ScheduleInterviewViewModel
                {
                    ApplicationId = application.Id,
                    CandidateId = candidate.Id,
                    CandidateFullName = candidate.FullName,
                    JobTitle = jobPost.JobTitle,
                    InterviewDateTime = DateTime.Now.AddDays(3).Date.AddHours(10),
                    InterviewNotes = application.InterviewNotes,
                    InterviewLocation = "Muhle Building Office",
                    InterviewType = "In-person"
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error scheduling interview: " + ex.Message;
                return RedirectToAction("ReviewApplications", new { id = id });
            }
        }

        // POST: JobPost/ScheduleInterview
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ScheduleInterview(ScheduleInterviewViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var application = await db.JobApplications.FindAsync(model.ApplicationId);
                    if (application == null)
                    {
                        return HttpNotFound();
                    }

                    if (application.InterviewScheduled || application.InterviewResult != "Pending")
                    {
                        TempData["ErrorMessage"] = "This candidate already has an interview scheduled or the interview has been completed.";
                        return RedirectToAction("ReviewApplications", new { id = application.JobPostId });
                    }

                    application.InterviewDateTime = model.InterviewDateTime;
                    application.InterviewNotes = model.InterviewNotes;
                    application.InterviewScheduled = true;
                    application.Status = "Interview Scheduled";

                    var history = new ApplicationStatusHistory
                    {
                        ApplicationId = application.Id,
                        Status = "Interview Scheduled",
                        Notes = $"Interview scheduled for {model.InterviewDateTime:yyyy-MM-dd HH:mm} at {model.InterviewLocation}",
                        ChangedBy = User.Identity.Name
                    };
                    db.ApplicationStatusHistories.Add(history);

                    await db.SaveChangesAsync();

                    await CreateNotificationForCandidate(
                        application.CandidateId,
                        "Interview Scheduled",
                        $"Your interview for '{model.JobTitle}' has been scheduled for {model.InterviewDateTime:dd MMM yyyy HH:mm} at {model.InterviewLocation}.",
                        "Interview",
                        $"/Candidate/ApplicationStatus/{application.Id}"
                    );

                    TempData["SuccessMessage"] = $"Interview scheduled successfully for {model.CandidateFullName} on {model.InterviewDateTime:yyyy-MM-dd HH:mm}";
                    return RedirectToAction("ReviewApplications", new { id = application.JobPostId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error scheduling interview: " + ex.Message);
                }
            }

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

        // GET: JobPost/AssessInterview/5
        [Authorize(Roles = "Admin")]
        public ActionResult AssessInterview(int id)
        {
            try
            {
                var application = db.JobApplications.Find(id);
                if (application == null)
                {
                    return HttpNotFound();
                }

                if (application.InterviewResult != "Pending")
                {
                    TempData["ErrorMessage"] = "This interview has already been assessed.";
                    return RedirectToAction("ReviewApplications", new { id = application.JobPostId });
                }

                var candidate = db.Candidates.Find(application.CandidateId);
                var jobPost = db.JobPosts.Find(application.JobPostId);

                if (candidate == null || jobPost == null)
                {
                    return HttpNotFound();
                }

                var viewModel = new AssessInterviewViewModel
                {
                    ApplicationId = application.Id,
                    CandidateId = candidate.Id,
                    CandidateFullName = candidate.FullName,
                    CandidateEmail = candidate.Email,
                    JobTitle = jobPost.JobTitle,
                    InterviewDateTime = application.InterviewDateTime,
                    InterviewNotes = application.InterviewNotes,
                    InterviewResult = application.InterviewResult ?? "Pending",
                    InterviewFeedback = application.InterviewFeedback
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading interview assessment: " + ex.Message;
                return RedirectToAction("ReviewApplications", new { id = 0 });
            }
        }

        // POST: JobPost/AssessInterview
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AssessInterview(AssessInterviewViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var application = await db.JobApplications.FindAsync(model.ApplicationId);
                    if (application == null)
                    {
                        return HttpNotFound();
                    }

                    if (application.InterviewResult != "Pending")
                    {
                        TempData["ErrorMessage"] = "This interview has already been assessed.";
                        return RedirectToAction("ReviewApplications", new { id = application.JobPostId });
                    }

                    application.InterviewResult = model.InterviewResult;
                    application.InterviewFeedback = model.InterviewFeedback;
                    application.InterviewAssessedBy = User.Identity.Name;
                    application.InterviewAssessedDate = DateTime.Now;

                    if (model.InterviewResult == "Passed")
                    {
                        application.Status = "Passed Interview";
                        application.IsHired = true;
                    }
                    else if (model.InterviewResult == "Failed")
                    {
                        application.Status = "Failed Interview";
                        application.IsHired = false;
                    }

                    await db.SaveChangesAsync();

                    var history = new ApplicationStatusHistory
                    {
                        ApplicationId = application.Id,
                        Status = application.Status,
                        Notes = $"Interview assessment: {model.InterviewResult}. Feedback: {model.InterviewFeedback}",
                        ChangedBy = User.Identity.Name
                    };
                    db.ApplicationStatusHistories.Add(history);
                    await db.SaveChangesAsync();

                    await CreateNotificationForCandidate(
                        application.CandidateId,
                        $"Interview {model.InterviewResult}!",
                        $"Your interview for '{application.JobPost?.JobTitle ?? "the position"}' has been assessed as: {model.InterviewResult}.",
                        "Interview",
                        $"/Candidate/ApplicationStatus/{application.Id}"
                    );

                    if (model.InterviewResult == "Passed")
                    {
                        await CreateNotificationForCandidate(
                            application.CandidateId,
                            "Congratulations! You've Been Selected!",
                            $"You have passed the interview for '{application.JobPost?.JobTitle ?? "the position"}'. You will be hired soon!",
                            "System",
                            $"/Candidate/ApplicationStatus/{application.Id}"
                        );

                        TempData["SuccessMessage"] = $"Interview assessment completed for {model.CandidateFullName}. Result: Passed!";
                        return RedirectToAction("HireCandidate", new { id = application.Id });
                    }

                    TempData["SuccessMessage"] = $"Interview assessment completed for {model.CandidateFullName}. Result: {model.InterviewResult}";
                    return RedirectToAction("ReviewApplications", new { id = application.JobPostId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error saving interview assessment: " + ex.Message);
                }
            }

            return View(model);
        }

        // GET: JobPost/HireCandidate/5
        [Authorize(Roles = "Admin")]
        public ActionResult HireCandidate(int id)
        {
            try
            {
                var application = db.JobApplications.Find(id);
                if (application == null)
                {
                    TempData["ErrorMessage"] = "Application not found.";
                    return RedirectToAction("Index");
                }

                var candidate = db.Candidates.Find(application.CandidateId);
                var jobPost = db.JobPosts.Find(application.JobPostId);

                if (candidate == null || jobPost == null)
                {
                    TempData["ErrorMessage"] = "Candidate or job not found.";
                    return RedirectToAction("ReviewApplications", new { id = application.JobPostId });
                }

                if (application.InterviewResult != "Passed")
                {
                    TempData["ErrorMessage"] = "This candidate has not passed the interview.";
                    return RedirectToAction("ReviewApplications", new { id = application.JobPostId });
                }

                if (application.IsHired)
                {
                    TempData["ErrorMessage"] = "This candidate has already been hired.";
                    return RedirectToAction("ReviewApplications", new { id = application.JobPostId });
                }

                return RedirectToAction("HireFromCandidate", "Staff", new { id = candidate.Id, applicationId = application.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: JobPost/CandidateDetails/5
        [Authorize(Roles = "Admin")]
        public ActionResult CandidateDetails(int id)
        {
            try
            {
                var candidate = db.Candidates.Find(id);
                if (candidate == null)
                {
                    return HttpNotFound();
                }

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
                    InterviewNotes = app.InterviewNotes,
                    InterviewResult = app.InterviewResult ?? "Pending",
                    InterviewFeedback = app.InterviewFeedback,
                    IsHired = app.IsHired,
                    IsInterviewCompleted = app.InterviewResult != "Pending"
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

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading candidate details: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: JobPost/AllApplications
        [Authorize(Roles = "Admin")]
        public ActionResult AllApplications()
        {
            try
            {
                var allApplications = db.JobApplications
                    .OrderByDescending(a => a.ApplicationDate)
                    .ToList();

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
                        InterviewResult = app.InterviewResult ?? "Pending",
                        InterviewFeedback = app.InterviewFeedback,
                        IsHired = app.IsHired,
                        IsInterviewCompleted = app.InterviewResult != "Pending",
                        ExtractedSkills = new List<string>(),
                        ExtractedQualifications = new List<string>(),
                        YearsOfExperience = 0,
                        HighestQualification = app.QualificationLevel,
                        HasGrade12 = app.HasMatric,
                        HasDriversLicense = app.HasLicense
                    };

                    viewModels.Add(viewModel);
                }

                viewModels = viewModels.OrderByDescending(v => v.RankingScore).ToList();

                ViewBag.TotalApplications = viewModels.Count;
                ViewBag.ShortlistedCount = viewModels.Count(v => v.IsShortlisted);
                ViewBag.InterviewScheduledCount = viewModels.Count(v => v.InterviewScheduled);

                return View(viewModels);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading applications: " + ex.Message;
                return View(new List<ApplicationReviewViewModel>());
            }
        }

        #region Helper Methods

        private ApplicationReviewViewModel RankCandidate(Candidate candidate, JobPost jobPost)
        {
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
                string cvText = "";
                if (!string.IsNullOrEmpty(candidate.CvPath))
                {
                    string cvFullPath = Server.MapPath(candidate.CvPath);
                    if (System.IO.File.Exists(cvFullPath))
                    {
                        cvText = ExtractTextFromFile(cvFullPath);
                    }
                }

                var skills = ExtractSkills(cvText);
                result.ExtractedSkills = skills;

                var qualifications = ExtractQualifications(cvText);
                result.ExtractedQualifications = qualifications;

                result.YearsOfExperience = ExtractYearsOfExperience(cvText);
                result.HasGrade12 = CheckForMatric(cvText);
                result.HasDriversLicense = CheckForLicense(candidate.LicensePath, cvText);
                result.HighestQualification = DetermineHighestQualification(cvText);

                var jobRequirements = ExtractSkills(jobPost.Requirements);
                result.SkillsMatchPercentage = CalculateSkillsMatch(skills, jobRequirements);

                result.RankingScore = CalculateRankingScore(
                    result.SkillsMatchPercentage,
                    result.YearsOfExperience,
                    result.HighestQualification,
                    result.HasGrade12,
                    result.HasDriversLicense
                );

                result.HasMatric = result.HasGrade12;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error ranking candidate: {ex.Message}");
            }

            return result;
        }

        private string ExtractTextFromFile(string filePath)
        {
            try
            {
                string extension = System.IO.Path.GetExtension(filePath).ToLower();

                if (extension == ".txt")
                {
                    return System.IO.File.ReadAllText(filePath);
                }
                else if (extension == ".pdf")
                {
                    try
                    {
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
                        return text.ToString().Trim();
                    }
                    catch
                    {
                        return "";
                    }
                }
                else
                {
                    return "";
                }
            }
            catch
            {
                return "";
            }
        }

        private List<string> ExtractSkills(string text)
        {
            var skills = new List<string>();
            if (string.IsNullOrEmpty(text)) return skills;

            string lowerText = text.ToLower();

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
                "autocad", "revit", "microsoft project", "primavera", "microsoft office"
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

            var regex = new Regex(@"(\d+)\s*\+\s*years|\b(\d+)\s*years\b");
            var matches = regex.Matches(lowerText);

            int maxYears = 0;
            foreach (Match match in matches)
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
            if (!string.IsNullOrEmpty(licensePath))
            {
                string fullPath = Server.MapPath(licensePath);
                if (System.IO.File.Exists(fullPath))
                {
                    return true;
                }
            }

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

            score += skillsMatch * 0.4;

            if (yearsExperience >= 10) score += 20;
            else if (yearsExperience >= 7) score += 18;
            else if (yearsExperience >= 5) score += 15;
            else if (yearsExperience >= 3) score += 10;
            else if (yearsExperience >= 1) score += 5;

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

            if (hasMatric) score += 10;
            if (hasLicense) score += 5;

            return Math.Round(score, 2);
        }

        #endregion

        #region Notification Helpers

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

        #endregion

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