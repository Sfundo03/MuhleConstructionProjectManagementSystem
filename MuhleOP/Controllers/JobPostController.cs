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
using Newtonsoft.Json;

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
                AcceptedCount = j.AcceptedApplicantsCount,
                SystemRoleId = j.SystemRoleId,
                SystemRoleName = j.SystemRole != null ? j.SystemRole.Name : "Unknown"
            }).ToList();

            return View(viewModels);
        }

        // GET: JobPost/Create - Use Case 1: Create Vacancy
        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            var viewModel = new JobPostViewModel();
            viewModel.AvailableRoles = GetSystemRoles();
            viewModel.ClosingDate = DateTime.Now.AddDays(30);
            viewModel.NumberOfOpenings = 1;
            return View(viewModel);
        }

        // POST: JobPost/Create - Use Case 1: Create Vacancy
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Create(JobPostViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate job post with same role
                var existingJob = db.JobPosts.FirstOrDefault(j =>
                    j.SystemRoleId == viewModel.SystemRoleId &&
                    !j.IsClosed &&
                    j.IsPublished);

                if (existingJob != null)
                {
                    ModelState.AddModelError("SystemRoleId", "A job post for this role already exists and is active.");
                    viewModel.AvailableRoles = GetSystemRoles();
                    return View(viewModel);
                }

                // Get the role name from the database
                var role = db.Roles.Find(viewModel.SystemRoleId);
                if (role == null)
                {
                    ModelState.AddModelError("SystemRoleId", "Selected role does not exist.");
                    viewModel.AvailableRoles = GetSystemRoles();
                    return View(viewModel);
                }

                // Auto-generate job title from role name
                string jobTitle = $"{role.Name} Position";

                var jobPost = new JobPost
                {
                    JobTitle = jobTitle,
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
                    AcceptedApplicantsCount = 0,
                    SystemRoleId = viewModel.SystemRoleId
                };

                db.JobPosts.Add(jobPost);
                await db.SaveChangesAsync();

                await CreateNotification(
                    User.Identity.GetUserId(),
                    "Job Post Created",
                    $"Job post '{jobPost.JobTitle}' has been created successfully for role {role.Name}.",
                    "System",
                    $"/JobPost/Details/{jobPost.Id}"
                );

                TempData["SuccessMessage"] = $"Job post created successfully for role: {role.Name}!";
                return RedirectToAction("Index");
            }

            viewModel.AvailableRoles = GetSystemRoles();
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

            var roleName = jobPost.SystemRole != null ? jobPost.SystemRole.Name : "Unknown";

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
                AcceptedCount = jobPost.AcceptedApplicantsCount,
                SystemRoleId = jobPost.SystemRoleId,
                SystemRoleName = roleName
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
                PostedBy = jobPost.PostedBy,
                SystemRoleId = jobPost.SystemRoleId
            };
            viewModel.AvailableRoles = GetSystemRoles();

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

                // Check for duplicate if role changed
                if (jobPost.SystemRoleId != viewModel.SystemRoleId)
                {
                    var existingJob = db.JobPosts.FirstOrDefault(j =>
                        j.SystemRoleId == viewModel.SystemRoleId &&
                        j.Id != viewModel.Id &&
                        !j.IsClosed &&
                        j.IsPublished);

                    if (existingJob != null)
                    {
                        ModelState.AddModelError("SystemRoleId", "A job post for this role already exists and is active.");
                        viewModel.AvailableRoles = GetSystemRoles();
                        return View(viewModel);
                    }

                    // Update job title if role changed
                    var role = db.Roles.Find(viewModel.SystemRoleId);
                    if (role != null)
                    {
                        jobPost.JobTitle = $"{role.Name} Position";
                        jobPost.SystemRoleId = viewModel.SystemRoleId;
                    }
                }

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

            viewModel.AvailableRoles = GetSystemRoles();
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
                        HasDriversLicense = rankedData.HasDriversLicense,
                        QuizScore = app.QuizScore,
                        QuizPassed = app.QuizPassed,
                        QuizCompleted = app.QuizCompletedDate.HasValue,
                        QuizCompletedDate = app.QuizCompletedDate
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
                    InterviewType = "In-person",
                    SendQuizInvitation = true,
                    QuizTimeLimit = 20, // Changed to 20 minutes
                    QuizPassingScore = 60,
                    QuizQuestionCount = 10
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

                    // Send interview notification to candidate
                    await CreateNotificationForCandidate(
                        application.CandidateId,
                        "Interview Scheduled",
                        $"Your interview for '{model.JobTitle}' has been scheduled for {model.InterviewDateTime:dd MMM yyyy HH:mm} at {model.InterviewLocation}. Please check your email for details.",
                        "Interview",
                        $"/Candidate/ApplicationStatus/{application.Id}"
                    );

                    // Send quiz invitation if enabled
                    if (model.SendQuizInvitation)
                    {
                        var jobPost = db.JobPosts.Find(application.JobPostId);
                        var quiz = db.Quizzes.FirstOrDefault(q => q.JobPostId == jobPost.Id && q.IsActive);

                        if (quiz == null)
                        {
                            // Auto-generate quiz if none exists - with 20 minute time limit
                            quiz = await CreateAutoQuiz(jobPost, model.QuizQuestionCount, 20, model.QuizPassingScore);
                        }

                        if (quiz != null)
                        {
                            // Create notification for quiz
                            await CreateNotificationForCandidate(
                                application.CandidateId,
                                "Quiz Invitation",
                                $"You have been invited to complete a quiz for the '{jobPost.JobTitle}' position. The quiz has {quiz.NumberOfQuestions} questions and you need to score at least {quiz.PassingScore}% to pass. Click the 'Take Quiz' button in your dashboard to start.",
                                "Quiz",
                                $"/Candidate/TakeQuiz/{application.Id}"
                            );

                            // Also create a notification for the admin confirming quiz was sent
                            await CreateNotification(
                                User.Identity.GetUserId(),
                                "Quiz Invitation Sent",
                                $"Quiz invitation sent to {model.CandidateFullName} for the '{jobPost.JobTitle}' position. Quiz ID: {quiz.Id}, Questions: {quiz.NumberOfQuestions}",
                                "System",
                                $"/JobPost/ReviewApplications/{jobPost.Id}"
                            );

                            TempData["QuizInvited"] = true;
                            TempData["QuizId"] = quiz.Id;
                            TempData["QuizQuestions"] = quiz.NumberOfQuestions;
                        }
                        else
                        {
                            // Log error if quiz creation failed
                            System.Diagnostics.Debug.WriteLine($"ERROR: Failed to create quiz for JobPostId: {jobPost?.Id}, Title: {jobPost?.JobTitle}");
                            TempData["QuizCreationError"] = "Failed to create quiz. Please check logs.";
                        }
                    }

                    TempData["SuccessMessage"] = $"Interview scheduled successfully for {model.CandidateFullName} on {model.InterviewDateTime:yyyy-MM-dd HH:mm}" +
                                                 (model.SendQuizInvitation ? " and quiz invitation sent." : ".");

                    return RedirectToAction("ReviewApplications", new { id = application.JobPostId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error scheduling interview: " + ex.Message);
                    System.Diagnostics.Debug.WriteLine($"EXCEPTION in ScheduleInterview POST: {ex.Message}\n{ex.StackTrace}");
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

                var latestAttempt = db.QuizAttempts
                    .Where(a => a.ApplicationId == application.Id && a.Status == "Completed")
                    .OrderByDescending(a => a.EndTime)
                    .FirstOrDefault();

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
                    InterviewFeedback = application.InterviewFeedback,
                    QuizCompleted = application.QuizCompletedDate.HasValue,
                    QuizScore = application.QuizScore,
                    QuizPassed = application.QuizPassed,
                    QuizCompletedDate = application.QuizCompletedDate,
                    QuizAttemptId = latestAttempt?.Id
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
                        $"Your interview for '{application.JobPost?.JobTitle ?? "the position"}' has been assessed as: {model.InterviewResult}. " +
                        (model.InterviewResult == "Passed" ? "Congratulations! You will be hired soon." : "We appreciate your interest."),
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

        // GET: JobPost/ViewQuizResult/5
        // "id" here is the JobApplication Id - shows the admin the candidate's
        // autograded quiz result (score, pass/fail, and a per-question breakdown)
        [Authorize(Roles = "Admin")]
        public ActionResult ViewQuizResult(int id)
        {
            try
            {
                var application = db.JobApplications.Find(id);
                if (application == null)
                {
                    TempData["ErrorMessage"] = "Application not found.";
                    return RedirectToAction("AllApplications");
                }

                var candidate = db.Candidates.Find(application.CandidateId);
                var jobPost = db.JobPosts.Find(application.JobPostId);

                var attempt = db.QuizAttempts
                    .Where(a => a.ApplicationId == application.Id && a.Status == "Completed")
                    .OrderByDescending(a => a.EndTime)
                    .FirstOrDefault();

                if (attempt == null)
                {
                    TempData["ErrorMessage"] = "This candidate has not completed a quiz yet.";
                    return RedirectToAction("AssessInterview", new { id = application.Id });
                }

                var quiz = db.Quizzes.Find(attempt.QuizId);
                if (quiz == null)
                {
                    TempData["ErrorMessage"] = "Quiz not found.";
                    return RedirectToAction("AssessInterview", new { id = application.Id });
                }

                // Only show first 10 questions
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

                ViewBag.CandidateFullName = candidate?.FullName ?? "Unknown Candidate";
                ViewBag.CandidateEmail = candidate?.Email;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading quiz result: " + ex.Message;
                return RedirectToAction("AllApplications");
            }
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
                    IsInterviewCompleted = app.InterviewResult != "Pending",
                    QuizCompleted = app.QuizCompletedDate.HasValue,
                    QuizScore = app.QuizScore,
                    QuizPassed = app.QuizPassed,
                    QuizCompletedDate = app.QuizCompletedDate
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
                        HasDriversLicense = app.HasLicense,
                        QuizScore = app.QuizScore,
                        QuizPassed = app.QuizPassed,
                        QuizCompleted = app.QuizCompletedDate.HasValue,
                        QuizCompletedDate = app.QuizCompletedDate
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

        // ===== QUIZ MANAGEMENT =====

        // GET: JobPost/ManageQuiz/5
        [Authorize(Roles = "Admin")]
        public ActionResult ManageQuiz(int id)
        {
            try
            {
                var jobPost = db.JobPosts.Find(id);
                if (jobPost == null)
                {
                    return HttpNotFound();
                }

                var quiz = db.Quizzes.FirstOrDefault(q => q.JobPostId == id);

                var viewModel = new QuizViewModel();
                if (quiz != null)
                {
                    viewModel.QuizId = quiz.Id;
                    viewModel.Title = quiz.Title;
                    viewModel.Description = quiz.Description;
                    viewModel.TimeLimitMinutes = quiz.TimeLimitMinutes;
                    viewModel.PassingScore = quiz.PassingScore;
                    viewModel.NumberOfQuestions = quiz.NumberOfQuestions;
                    viewModel.JobTitle = jobPost.JobTitle;
                    viewModel.Questions = quiz.Questions.OrderBy(q => q.Order).Select(q => new QuizQuestionViewModel
                    {
                        Id = q.Id,
                        Question = q.Question,
                        Options = JsonConvert.DeserializeObject<List<string>>(q.OptionsJson ?? "[]"),
                        CorrectAnswer = q.CorrectAnswer,
                        Points = q.Points,
                        Order = q.Order
                    }).ToList();
                }

                ViewBag.JobPostId = id;
                ViewBag.JobTitle = jobPost.JobTitle;
                ViewBag.HasQuiz = quiz != null;
                ViewBag.ApplicationCount = db.JobApplications.Count(a => a.JobPostId == id);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading quiz management: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: JobPost/CreateQuiz
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateQuiz(QuizViewModel model)
        {
            try
            {
                var jobPost = db.JobPosts.Find(model.QuizId); // Using QuizId as JobPostId
                if (jobPost == null)
                {
                    TempData["ErrorMessage"] = "Job post not found.";
                    return RedirectToAction("Index");
                }

                // Check if quiz already exists
                var existingQuiz = db.Quizzes.FirstOrDefault(q => q.JobPostId == jobPost.Id);
                if (existingQuiz != null)
                {
                    TempData["ErrorMessage"] = "A quiz already exists for this job post. Please edit the existing quiz.";
                    return RedirectToAction("ManageQuiz", new { id = jobPost.Id });
                }

                var quiz = new Quiz
                {
                    Title = model.Title ?? $"Quiz for {jobPost.JobTitle}",
                    Description = model.Description ?? $"This quiz assesses candidates for the {jobPost.JobTitle} position.",
                    JobPostId = jobPost.Id,
                    SystemRoleId = jobPost.SystemRoleId,
                    TimeLimitMinutes = model.TimeLimitMinutes,
                    PassingScore = model.PassingScore,
                    NumberOfQuestions = model.NumberOfQuestions,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = User.Identity.Name
                };

                db.Quizzes.Add(quiz);
                await db.SaveChangesAsync();

                // Add questions
                if (model.Questions != null && model.Questions.Any())
                {
                    int order = 1;
                    foreach (var q in model.Questions)
                    {
                        var question = new QuizQuestion
                        {
                            QuizId = quiz.Id,
                            Question = q.Question,
                            QuestionType = "MultipleChoice",
                            OptionsJson = JsonConvert.SerializeObject(q.Options ?? new List<string>()),
                            CorrectAnswer = q.CorrectAnswer,
                            Points = q.Points > 0 ? q.Points : 1,
                            Order = order
                        };
                        db.QuizQuestions.Add(question);
                        order++;
                    }
                    await db.SaveChangesAsync();
                }
                else
                {
                    // Generate sample questions if none provided
                    var sampleQuestions = GenerateSampleQuestions(jobPost.JobTitle, jobPost.Requirements, model.NumberOfQuestions);
                    foreach (var q in sampleQuestions)
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
                }

                TempData["SuccessMessage"] = "Quiz created successfully!";
                return RedirectToAction("ManageQuiz", new { id = jobPost.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error creating quiz: " + ex.Message;
                return RedirectToAction("ManageQuiz", new { id = model.QuizId });
            }
        }

        // POST: JobPost/UpdateQuiz
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateQuiz(QuizViewModel model)
        {
            try
            {
                var quiz = await db.Quizzes.FindAsync(model.QuizId);
                if (quiz == null)
                {
                    TempData["ErrorMessage"] = "Quiz not found.";
                    return RedirectToAction("Index");
                }

                quiz.Title = model.Title;
                quiz.Description = model.Description;
                quiz.TimeLimitMinutes = model.TimeLimitMinutes;
                quiz.PassingScore = model.PassingScore;
                quiz.NumberOfQuestions = model.NumberOfQuestions;

                await db.SaveChangesAsync();

                // Update questions
                if (model.Questions != null && model.Questions.Any())
                {
                    // Remove existing questions
                    var existingQuestions = db.QuizQuestions.Where(q => q.QuizId == quiz.Id).ToList();
                    db.QuizQuestions.RemoveRange(existingQuestions);
                    await db.SaveChangesAsync();

                    // Add new questions
                    int order = 1;
                    foreach (var q in model.Questions)
                    {
                        var question = new QuizQuestion
                        {
                            QuizId = quiz.Id,
                            Question = q.Question,
                            QuestionType = "MultipleChoice",
                            OptionsJson = JsonConvert.SerializeObject(q.Options ?? new List<string>()),
                            CorrectAnswer = q.CorrectAnswer,
                            Points = q.Points > 0 ? q.Points : 1,
                            Order = order
                        };
                        db.QuizQuestions.Add(question);
                        order++;
                    }
                    await db.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Quiz updated successfully!";
                return RedirectToAction("ManageQuiz", new { id = quiz.JobPostId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating quiz: " + ex.Message;
                return RedirectToAction("ManageQuiz", new { id = model.QuizId });
            }
        }

        // POST: JobPost/GenerateQuestions
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> GenerateQuestions(int jobPostId, int count)
        {
            try
            {
                var jobPost = db.JobPosts.Find(jobPostId);
                if (jobPost == null)
                {
                    return Json(new { success = false, message = "Job post not found" });
                }

                var questions = GenerateSampleQuestions(jobPost.JobTitle, jobPost.Requirements, count);

                // Get existing quiz to update
                var quiz = db.Quizzes.FirstOrDefault(q => q.JobPostId == jobPostId);
                if (quiz != null)
                {
                    // Remove existing questions
                    var existingQuestions = db.QuizQuestions.Where(q => q.QuizId == quiz.Id).ToList();
                    db.QuizQuestions.RemoveRange(existingQuestions);
                    await db.SaveChangesAsync();

                    // Add new questions
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
                }

                return Json(new
                {
                    success = true,
                    questions = questions.Select(q => new
                    {
                        q.Question,
                        q.Options,
                        q.CorrectAnswer,
                        q.Order
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<Quiz> CreateAutoQuiz(JobPost jobPost, int questionCount, int timeLimit, int passingScore)
        {
            try
            {
                // Check if quiz already exists
                var existingQuiz = db.Quizzes.FirstOrDefault(q => q.JobPostId == jobPost.Id);
                if (existingQuiz != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Quiz already exists for JobPostId: {jobPost.Id}, QuizId: {existingQuiz.Id}");
                    return existingQuiz;
                }

                System.Diagnostics.Debug.WriteLine($"Creating new quiz for JobPostId: {jobPost.Id}, Title: {jobPost.JobTitle}");

                var quiz = new Quiz
                {
                    Title = $"Quiz for {jobPost.JobTitle}",
                    Description = $"This quiz assesses candidates for the {jobPost.JobTitle} position.",
                    JobPostId = jobPost.Id,
                    SystemRoleId = jobPost.SystemRoleId,
                    TimeLimitMinutes = 20, // Changed to 20 minutes
                    PassingScore = passingScore,
                    NumberOfQuestions = questionCount,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                };

                db.Quizzes.Add(quiz);
                await db.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"Quiz created with ID: {quiz.Id}");

                // Generate questions based on job requirements
                var questions = GenerateSampleQuestions(jobPost.JobTitle, jobPost.Requirements, questionCount);
                System.Diagnostics.Debug.WriteLine($"Generated {questions.Count} questions");

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
                System.Diagnostics.Debug.WriteLine($"Added {questions.Count} questions to quiz ID: {quiz.Id}");

                return quiz;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating auto quiz: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private List<QuizQuestionViewModel> GenerateSampleQuestions(string jobTitle, string requirements, int count = 10)
        {
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

            // Ensure we have enough questions
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

            return questions;
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

        private List<SelectListItem> GetSystemRoles()
        {
            var roles = db.Roles
                .Where(r => r.Name != "Admin" && r.Name != "Client" && r.Name != "Candidate")
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem
                {
                    Value = r.Id,
                    Text = r.Name
                })
                .ToList();

            roles.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- Select a System Role --"
            });

            return roles;
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

        #region Debug Methods

        // GET: JobPost/DebugQuiz
        [Authorize(Roles = "Admin")]
        public ActionResult DebugQuiz(int? jobPostId = null)
        {
            try
            {
                var debugInfo = new
                {
                    TotalQuizzes = db.Quizzes.Count(),
                    TotalQuestions = db.QuizQuestions.Count(),
                    TotalAttempts = db.QuizAttempts.Count(),
                    TotalApplications = db.JobApplications.Count(),
                    TotalCandidates = db.Candidates.Count()
                };

                // Get all job posts with quiz status
                var jobPosts = db.JobPosts
                    .Where(j => !jobPostId.HasValue || j.Id == jobPostId.Value)
                    .OrderByDescending(j => j.DatePosted)
                    .ToList();

                var jobPostDebug = new List<object>();
                foreach (var job in jobPosts)
                {
                    var quiz = db.Quizzes.FirstOrDefault(q => q.JobPostId == job.Id && q.IsActive);
                    var applications = db.JobApplications.Where(a => a.JobPostId == job.Id).ToList();
                    var attempts = db.QuizAttempts
                        .Where(qa => applications.Select(a => a.Id).Contains(qa.ApplicationId))
                        .ToList();

                    jobPostDebug.Add(new
                    {
                        JobPostId = job.Id,
                        JobTitle = job.JobTitle,
                        IsPublished = job.IsPublished,
                        IsClosed = job.IsClosed,
                        HasQuiz = quiz != null,
                        QuizId = quiz?.Id ?? 0,
                        QuizTitle = quiz?.Title ?? "No Quiz",
                        QuizActive = quiz?.IsActive ?? false,
                        QuizQuestions = quiz?.Questions.Count ?? 0,
                        QuizPassingScore = quiz?.PassingScore ?? 0,
                        QuizTimeLimit = quiz?.TimeLimitMinutes ?? 0,
                        ApplicationCount = applications.Count,
                        AttemptCount = attempts.Count,
                        PassedCount = attempts.Count(a => a.Passed == true),
                        FailedCount = attempts.Count(a => a.Passed == false),
                        InProgressCount = attempts.Count(a => a.Status == "InProgress")
                    });
                }

                ViewBag.DebugInfo = debugInfo;
                ViewBag.JobPostDebug = jobPostDebug;
                ViewBag.TargetJobPostId = jobPostId;

                return View("DebugQuiz");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Debug error: " + ex.Message;
                return View("DebugQuiz");
            }
        }

        // GET: JobPost/DebugCandidate
        [Authorize(Roles = "Admin")]
        public ActionResult DebugCandidate(string userId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    userId = User.Identity.GetUserId();
                }

                var candidate = db.Candidates.FirstOrDefault(c => c.UserId == userId);
                if (candidate == null)
                {
                    ViewBag.Error = "Candidate not found for user ID: " + userId;
                    return View("DebugCandidate");
                }

                var viewModel = new
                {
                    CandidateId = candidate.Id,
                    CandidateName = candidate.FullName,
                    CandidateEmail = candidate.Email,
                    UserId = candidate.UserId,
                    TotalApplications = db.JobApplications.Count(a => a.CandidateId == candidate.Id)
                };

                // Get all applications for this candidate
                var applications = db.JobApplications
                    .Where(a => a.CandidateId == candidate.Id)
                    .OrderByDescending(a => a.ApplicationDate)
                    .ToList();

                var appDebug = new List<object>();
                foreach (var app in applications)
                {
                    var jobPost = db.JobPosts.Find(app.JobPostId);
                    var quiz = db.Quizzes.FirstOrDefault(q => q.JobPostId == app.JobPostId && q.IsActive);
                    var quizAttempt = db.QuizAttempts
                        .Where(qa => qa.ApplicationId == app.Id)
                        .OrderByDescending(qa => qa.StartTime)
                        .FirstOrDefault();

                    appDebug.Add(new
                    {
                        ApplicationId = app.Id,
                        JobTitle = jobPost?.JobTitle ?? "Unknown",
                        Status = app.Status,
                        ApplicationDate = app.ApplicationDate,
                        InterviewScheduled = app.InterviewScheduled,
                        InterviewDateTime = app.InterviewDateTime,
                        IsShortlisted = app.IsShortlisted,
                        QuizScore = app.QuizScore,
                        QuizPassed = app.QuizPassed,
                        QuizCompletedDate = app.QuizCompletedDate,
                        HasQuiz = quiz != null,
                        QuizId = quiz?.Id ?? 0,
                        QuizTitle = quiz?.Title ?? "No Quiz",
                        QuizActive = quiz?.IsActive ?? false,
                        QuizQuestions = quiz?.Questions.Count ?? 0,
                        HasAttempt = quizAttempt != null,
                        AttemptId = quizAttempt?.Id ?? 0,
                        AttemptStatus = quizAttempt?.Status ?? "No Attempt",
                        AttemptScore = quizAttempt?.Score,
                        AttemptPassed = quizAttempt?.Passed
                    });
                }

                ViewBag.Candidate = viewModel;
                ViewBag.Applications = appDebug;

                return View("DebugCandidate");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Debug error: " + ex.Message;
                return View("DebugCandidate");
            }
        }

        // POST: JobPost/DebugCreateQuiz
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> DebugCreateQuiz(int jobPostId)
        {
            try
            {
                var jobPost = db.JobPosts.Find(jobPostId);
                if (jobPost == null)
                {
                    return Json(new { success = false, message = "Job post not found." });
                }

                // Check if quiz already exists
                var existingQuiz = db.Quizzes.FirstOrDefault(q => q.JobPostId == jobPostId);
                if (existingQuiz != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Quiz already exists. Quiz ID: {existingQuiz.Id}, Title: {existingQuiz.Title}, Questions: {existingQuiz.Questions.Count}"
                    });
                }

                var quiz = await CreateAutoQuiz(jobPost, 5, 20, 60);

                if (quiz != null)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Quiz created successfully! Quiz ID: {quiz.Id}, Questions: {quiz.Questions.Count}",
                        quizId = quiz.Id,
                        questionCount = quiz.Questions.Count
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to create quiz. Check server logs." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // GET: JobPost/DebugCheckQuizStatus
        [Authorize(Roles = "Admin")]
        public JsonResult DebugCheckQuizStatus(int jobPostId)
        {
            try
            {
                var jobPost = db.JobPosts.Find(jobPostId);
                if (jobPost == null)
                {
                    return Json(new { success = false, message = "Job post not found." });
                }

                var quiz = db.Quizzes.FirstOrDefault(q => q.JobPostId == jobPostId && q.IsActive);
                var applications = db.JobApplications.Where(a => a.JobPostId == jobPostId).ToList();
                var attempts = db.QuizAttempts
                    .Where(qa => applications.Select(a => a.Id).Contains(qa.ApplicationId))
                    .ToList();

                return Json(new
                {
                    success = true,
                    jobPost = new
                    {
                        jobPost.Id,
                        jobPost.JobTitle,
                        jobPost.IsPublished,
                        jobPost.IsClosed
                    },
                    quiz = quiz != null ? new
                    {
                        quiz.Id,
                        quiz.Title,
                        quiz.IsActive,
                        quiz.NumberOfQuestions,
                        quiz.PassingScore,
                        quiz.TimeLimitMinutes,
                        QuestionCount = quiz.Questions.Count
                    } : null,
                    applications = applications.Select(a => new
                    {
                        a.Id,
                        a.Status,
                        a.QuizScore,
                        a.QuizPassed,
                        a.QuizCompletedDate
                    }),
                    attempts = attempts.Select(a => new
                    {
                        a.Id,
                        a.Status,
                        a.Score,
                        a.Passed,
                        a.StartTime,
                        a.EndTime
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
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