using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace MuhleOP.ViewModels
{
    public class JobPostViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Job title is required")]
        [Display(Name = "Job Title")]
        [StringLength(100)]
        public string JobTitle { get; set; }

        [Required(ErrorMessage = "Job description is required")]
        [Display(Name = "Job Description")]
        [DataType(DataType.MultilineText)]
        public string JobDescription { get; set; }

        [Required(ErrorMessage = "Requirements are required")]
        [Display(Name = "Requirements")]
        [DataType(DataType.MultilineText)]
        public string Requirements { get; set; }

        [Required(ErrorMessage = "Closing date is required")]
        [Display(Name = "Closing Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime ClosingDate { get; set; }

        [Display(Name = "Job Type")]
        public string JobType { get; set; }

        [Display(Name = "Experience Level")]
        public string ExperienceLevel { get; set; }

        [Display(Name = "Location")]
        [StringLength(100)]
        public string Location { get; set; }

        [Display(Name = "Salary Range")]
        [StringLength(50)]
        public string SalaryRange { get; set; }

        [Display(Name = "Number of Openings")]
        [Range(1, 100)]
        public int NumberOfOpenings { get; set; } = 1;

        [Display(Name = "Department")]
        [StringLength(50)]
        public string Department { get; set; }

        [Display(Name = "Is Published")]
        public bool IsPublished { get; set; }

        [Display(Name = "Is Closed")]
        public bool IsClosed { get; set; }

        [Display(Name = "Is Auto Closed")]
        public bool IsAutoClosed { get; set; }

        [Display(Name = "Date Posted")]
        public DateTime DatePosted { get; set; }

        [Display(Name = "Posted By")]
        public string PostedBy { get; set; }

        public int ApplicationCount { get; set; }
        public int AcceptedCount { get; set; }
        public string StatusBadgeColor => IsClosed ? "secondary" : IsPublished ? "success" : "warning";
        public string StatusText => IsClosed ? "Closed" : IsPublished ? "Published" : "Draft";
    }

    public class JobApplicationViewModel
    {
        public int Id { get; set; }
        public int JobPostId { get; set; }
        public string JobTitle { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Cover Letter")]
        [DataType(DataType.MultilineText)]
        public string CoverLetter { get; set; }

        [Display(Name = "CV/Resume")]
        [Required(ErrorMessage = "CV is required")]
        public HttpPostedFileBase CvFile { get; set; }

        public string CvFilePath { get; set; }

        [Display(Name = "Application Status")]
        public string Status { get; set; }

        [Display(Name = "Date Applied")]
        public DateTime DateApplied { get; set; }

        [Display(Name = "Notes")]
        public string Notes { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }

    public class ApplicationReviewViewModel
    {
        public int ApplicationId { get; set; }
        public int JobPostId { get; set; }
        public string JobTitle { get; set; }
        public int CandidateId { get; set; }
        public string CandidateFullName { get; set; }
        public string CandidateEmail { get; set; }
        public string CandidatePhone { get; set; }
        public string CandidateNationality { get; set; }
        public DateTime? CandidateDateOfBirth { get; set; }
        public string CandidateGender { get; set; }
        public string CandidateCurrentAddress { get; set; }
        public string IdDocumentPath { get; set; }
        public string QualificationsPath { get; set; }
        public string CvPath { get; set; }
        public string LicensePath { get; set; }
        public string ProfilePicturePath { get; set; }
        public string CoverLetter { get; set; }
        public DateTime ApplicationDate { get; set; }
        public string Status { get; set; }

        // Ranking Properties
        public double RankingScore { get; set; }
        public int SkillsMatchPercentage { get; set; }
        public bool HasMatric { get; set; }
        public bool HasLicense { get; set; }
        public string QualificationLevel { get; set; }
        public bool IsShortlisted { get; set; }
        public bool InterviewScheduled { get; set; }
        public DateTime? InterviewDateTime { get; set; }
        public string InterviewNotes { get; set; }

        // Interview Result Properties
        public string InterviewResult { get; set; }
        public string InterviewFeedback { get; set; }
        public bool IsHired { get; set; }
        public bool IsInterviewCompleted { get; set; }

        // Quiz Properties
        public int? QuizScore { get; set; }
        public bool? QuizPassed { get; set; }
        public string QuizResult { get; set; }

        // Extracted CV Data
        public List<string> ExtractedSkills { get; set; }
        public List<string> ExtractedQualifications { get; set; }
        public int YearsOfExperience { get; set; }
        public string HighestQualification { get; set; }
        public bool HasGrade12 { get; set; }
        public bool HasDriversLicense { get; set; }

        public ApplicationReviewViewModel()
        {
            ExtractedSkills = new List<string>();
            ExtractedQualifications = new List<string>();
            InterviewResult = "Pending";
            IsHired = false;
            IsInterviewCompleted = false;
            QuizResult = "Not Taken";
        }
    }

    public class AssessInterviewViewModel
    {
        public int ApplicationId { get; set; }
        public int CandidateId { get; set; }
        public string CandidateFullName { get; set; }
        public string CandidateEmail { get; set; }
        public string JobTitle { get; set; }
        public DateTime? InterviewDateTime { get; set; }
        public string InterviewNotes { get; set; }

        [Required(ErrorMessage = "Please select the interview result")]
        [Display(Name = "Interview Result")]
        public string InterviewResult { get; set; }

        [Display(Name = "Interview Feedback")]
        [DataType(DataType.MultilineText)]
        public string InterviewFeedback { get; set; }

        public AssessInterviewViewModel()
        {
            InterviewResult = "Pending";
        }
    }

    public class BulkShortlistViewModel
    {
        public List<int> ApplicationIds { get; set; }
        public int JobPostId { get; set; }

        public BulkShortlistViewModel()
        {
            ApplicationIds = new List<int>();
        }
    }

    public class ScheduleInterviewViewModel
    {
        public int ApplicationId { get; set; }
        public int CandidateId { get; set; }
        public string CandidateFullName { get; set; }
        public string JobTitle { get; set; }

        [Required(ErrorMessage = "Interview date and time is required")]
        [Display(Name = "Interview Date & Time")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime InterviewDateTime { get; set; }

        [Display(Name = "Interview Notes")]
        [DataType(DataType.MultilineText)]
        public string InterviewNotes { get; set; }

        [Display(Name = "Interview Location")]
        [StringLength(200)]
        public string InterviewLocation { get; set; }

        [Display(Name = "Interview Type")]
        public string InterviewType { get; set; }

        public ScheduleInterviewViewModel()
        {
            InterviewDateTime = DateTime.Now.AddDays(3).Date.AddHours(10);
            InterviewLocation = "Muhle Building Office";
            InterviewType = "In-person";
        }
    }

    public class CandidateDetailsViewModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Nationality { get; set; }
        public string CurrentAddress { get; set; }
        public string IdDocumentPath { get; set; }
        public string QualificationsPath { get; set; }
        public string CvPath { get; set; }
        public string LicensePath { get; set; }
        public string ProfilePicturePath { get; set; }
        public DateTime RegistrationDate { get; set; }
        public bool IsActive { get; set; }

        // Application History
        public List<ApplicationHistoryViewModel> ApplicationHistory { get; set; }

        public CandidateDetailsViewModel()
        {
            ApplicationHistory = new List<ApplicationHistoryViewModel>();
        }
    }

    public class ApplicationHistoryViewModel
    {
        public int ApplicationId { get; set; }
        public string JobTitle { get; set; }
        public DateTime ApplicationDate { get; set; }
        public string Status { get; set; }
        public double RankingScore { get; set; }
        public int SkillsMatchPercentage { get; set; }
        public bool IsShortlisted { get; set; }
        public bool InterviewScheduled { get; set; }
        public DateTime? InterviewDateTime { get; set; }
        public string InterviewNotes { get; set; }
        public string InterviewResult { get; set; }
        public string InterviewFeedback { get; set; }
        public bool IsHired { get; set; }
        public bool IsInterviewCompleted { get; set; }
    }

    public class CandidateApplicationSummaryViewModel
    {
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int ShortlistedApplications { get; set; }
        public int InterviewScheduledCount { get; set; }
        public int InterviewPassedCount { get; set; }
        public int HiredCount { get; set; }
        public int RejectedCount { get; set; }
        public double AverageRankingScore { get; set; }
    }

    public class TakeQuizViewModel
    {
        public int ApplicationId { get; set; }
        public string CandidateName { get; set; }
        public string JobTitle { get; set; }
        public int QuizId { get; set; }
        public List<QuizQuestionViewModel> Questions { get; set; }
        public TimeSpan TimeLimit { get; set; }
        public DateTime StartTime { get; set; }

        public TakeQuizViewModel()
        {
            Questions = new List<QuizQuestionViewModel>();
            TimeLimit = TimeSpan.FromMinutes(30);
            StartTime = DateTime.Now;
        }
    }

    public class QuizQuestionViewModel
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public string QuestionType { get; set; } // MultipleChoice, TrueFalse, ShortAnswer
        public List<string> Options { get; set; }
        public string CorrectAnswer { get; set; }
        public int Points { get; set; }

        public QuizQuestionViewModel()
        {
            Options = new List<string>();
            Points = 1;
        }
    }

    public class QuizResultViewModel
    {
        public int ApplicationId { get; set; }
        public string CandidateName { get; set; }
        public string JobTitle { get; set; }
        public int Score { get; set; }
        public int TotalPoints { get; set; }
        public int Percentage { get; set; }
        public bool Passed { get; set; }
        public DateTime CompletedDate { get; set; }
        public Dictionary<int, string> Answers { get; set; }
        public Dictionary<int, bool> Results { get; set; }

        public QuizResultViewModel()
        {
            Answers = new Dictionary<int, string>();
            Results = new Dictionary<int, bool>();
            CompletedDate = DateTime.Now;
        }
    }
}