// JobPostViewModels.cs - Updated ViewModels
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

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
        public DateTime? AutoCloseDate { get; set; }

        [Required(ErrorMessage = "Please select a system role")]
        [Display(Name = "System Role")]
        public string SystemRoleId { get; set; }

        public string SystemRoleName { get; set; }

        public string StatusBadgeColor => IsClosed ? "secondary" : IsPublished ? "success" : "warning";
        public string StatusText => IsClosed ? "Closed" : IsPublished ? "Published" : "Draft";

        public List<SelectListItem> AvailableRoles { get; set; }

        public JobPostViewModel()
        {
            AvailableRoles = new List<SelectListItem>();
        }
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

        [DataType(DataType.MultilineText)]
        public string CoverLetter { get; set; }
        public DateTime ApplicationDate { get; set; }
        public string Status { get; set; }

        public double RankingScore { get; set; }
        public int SkillsMatchPercentage { get; set; }
        public bool HasMatric { get; set; }
        public bool HasLicense { get; set; }
        public string QualificationLevel { get; set; }

        public bool IsShortlisted { get; set; }
        public bool InterviewScheduled { get; set; }
        public DateTime? InterviewDateTime { get; set; }
        public string InterviewNotes { get; set; }
        public string InterviewResult { get; set; }
        public string InterviewFeedback { get; set; }
        public bool IsInterviewCompleted { get; set; }

        public bool IsHired { get; set; }

        // Quiz Properties
        public bool QuizInvited { get; set; }
        public bool QuizCompleted { get; set; }
        public int? QuizScore { get; set; }
        public bool? QuizPassed { get; set; }
        public DateTime? QuizCompletedDate { get; set; }

        // Ranking helper data (used by RankCandidate)
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
        }
    }

    public class BulkShortlistViewModel
    {
        [Required]
        public int JobPostId { get; set; }

        [Required(ErrorMessage = "Please select at least one candidate")]
        public List<int> ApplicationIds { get; set; }

        public BulkShortlistViewModel()
        {
            ApplicationIds = new List<int>();
        }
    }

    public class ScheduleInterviewViewModel
    {
        [Required]
        public int ApplicationId { get; set; }

        public int CandidateId { get; set; }

        [Display(Name = "Candidate")]
        public string CandidateFullName { get; set; }

        [Display(Name = "Job Title")]
        public string JobTitle { get; set; }

        [Required(ErrorMessage = "Interview date and time is required")]
        [Display(Name = "Interview Date & Time")]
        public DateTime InterviewDateTime { get; set; }

        [Display(Name = "Interview Notes")]
        [DataType(DataType.MultilineText)]
        public string InterviewNotes { get; set; }

        [Required(ErrorMessage = "Interview location is required")]
        [Display(Name = "Interview Location")]
        [StringLength(200)]
        public string InterviewLocation { get; set; }

        [Required(ErrorMessage = "Interview type is required")]
        [Display(Name = "Interview Type")]
        public string InterviewType { get; set; }

        // Quiz invitation
        [Display(Name = "Send Quiz Invitation")]
        public bool SendQuizInvitation { get; set; } = true;

        [Display(Name = "Quiz Time Limit (Minutes)")]
        public int QuizTimeLimit { get; set; } = 30;

        [Display(Name = "Quiz Passing Score")]
        public int QuizPassingScore { get; set; } = 60;

        [Display(Name = "Number of Questions")]
        public int QuizQuestionCount { get; set; } = 10;
    }

    public class AssessInterviewViewModel
    {
        [Required]
        public int ApplicationId { get; set; }

        public int CandidateId { get; set; }

        [Display(Name = "Candidate")]
        public string CandidateFullName { get; set; }

        [Display(Name = "Email")]
        public string CandidateEmail { get; set; }

        [Display(Name = "Job Title")]
        public string JobTitle { get; set; }

        [Display(Name = "Interview Date & Time")]
        public DateTime? InterviewDateTime { get; set; }

        [Display(Name = "Interview Notes")]
        [DataType(DataType.MultilineText)]
        public string InterviewNotes { get; set; }

        [Required(ErrorMessage = "Please select an interview result")]
        [Display(Name = "Interview Result")]
        public string InterviewResult { get; set; }

        [Display(Name = "Interview Feedback")]
        [DataType(DataType.MultilineText)]
        public string InterviewFeedback { get; set; }

        // Quiz Properties - lets the admin see the candidate's autograded quiz result
        // while assessing the interview
        [Display(Name = "Quiz Completed")]
        public bool QuizCompleted { get; set; }

        [Display(Name = "Quiz Score")]
        public int? QuizScore { get; set; }

        [Display(Name = "Quiz Passed")]
        public bool? QuizPassed { get; set; }

        [Display(Name = "Quiz Completed Date")]
        public DateTime? QuizCompletedDate { get; set; }

        [Display(Name = "Quiz Attempt Id")]
        public int? QuizAttemptId { get; set; }
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
        public bool IsInterviewCompleted { get; set; }

        public bool IsHired { get; set; }

        // Quiz Properties
        public bool QuizCompleted { get; set; }
        public int? QuizScore { get; set; }
        public bool? QuizPassed { get; set; }
        public DateTime? QuizCompletedDate { get; set; }
    }

    public class CandidateDetailsViewModel
    {
        public int Id { get; set; }

        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Gender")]
        public string Gender { get; set; }

        [Display(Name = "Nationality")]
        public string Nationality { get; set; }

        [Display(Name = "Current Address")]
        public string CurrentAddress { get; set; }

        public string IdDocumentPath { get; set; }
        public string QualificationsPath { get; set; }
        public string CvPath { get; set; }
        public string LicensePath { get; set; }
        public string ProfilePicturePath { get; set; }

        [Display(Name = "Registration Date")]
        public DateTime RegistrationDate { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        public List<ApplicationHistoryViewModel> ApplicationHistory { get; set; }

        public CandidateDetailsViewModel()
        {
            ApplicationHistory = new List<ApplicationHistoryViewModel>();
        }
    }

    // ===== QUIZ VIEWMODELS =====

    public class QuizViewModel
    {
        public int QuizId { get; set; }

        [Required]
        [Display(Name = "Quiz Title")]
        public string Title { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Time Limit (Minutes)")]
        [Range(1, 120)]
        public int TimeLimitMinutes { get; set; } = 30;

        [Required]
        [Display(Name = "Passing Score")]
        [Range(1, 100)]
        public int PassingScore { get; set; } = 60;

        [Required]
        [Display(Name = "Number of Questions")]
        [Range(1, 20)]
        public int NumberOfQuestions { get; set; } = 10;

        public int ApplicationId { get; set; }
        public string JobTitle { get; set; }
        public string CandidateName { get; set; }
        public List<QuizQuestionViewModel> Questions { get; set; }

        public QuizViewModel()
        {
            Questions = new List<QuizQuestionViewModel>();
        }
    }

    public class QuizQuestionViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Question { get; set; }

        public List<string> Options { get; set; }
        public string SelectedAnswer { get; set; }
        public string CorrectAnswer { get; set; }
        public int Points { get; set; } = 1;
        public int Order { get; set; }
        public bool IsCorrect { get; set; }

        // For form submission
        public string OptionsText { get; set; }

        public QuizQuestionViewModel()
        {
            Options = new List<string>();
        }
    }

    public class QuizResultViewModel
    {
        public int QuizId { get; set; }
        public string QuizTitle { get; set; }
        public int ApplicationId { get; set; }
        public string JobTitle { get; set; }
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public bool Passed { get; set; }
        public int PassingScore { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TimeTakenSeconds { get; set; }
        public List<QuizQuestionViewModel> Questions { get; set; }

        public QuizResultViewModel()
        {
            Questions = new List<QuizQuestionViewModel>();
        }
    }

    public class QuizSubmissionViewModel
    {
        public int AttemptId { get; set; }
        public Dictionary<int, string> Answers { get; set; }

        public QuizSubmissionViewModel()
        {
            Answers = new Dictionary<int, string>();
        }
    }

    public class GenerateQuestionsViewModel
    {
        public int JobPostId { get; set; }
        public int Count { get; set; } = 10;
        public List<QuizQuestionViewModel> Questions { get; set; }

        public GenerateQuestionsViewModel()
        {
            Questions = new List<QuizQuestionViewModel>();
        }
    }
}