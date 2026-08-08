using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNet.Identity.EntityFramework;

namespace MuhleOP.Models
{
    public class JobPost
    {
        [Key]
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

        [Display(Name = "Accepted Applicants Count")]
        public int AcceptedApplicantsCount { get; set; }

        [Display(Name = "Auto Close Date")]
        public DateTime? AutoCloseDate { get; set; }

        // System Role ID for the position - This links to the ASP.NET Identity Role
        [Display(Name = "System Role")]
        public string SystemRoleId { get; set; }

        // Navigation Property to the Identity Role
        [ForeignKey("SystemRoleId")]
        public virtual IdentityRole SystemRole { get; set; }

        // Navigation Properties
        public virtual ICollection<JobApplication> JobApplications { get; set; }
        public virtual ICollection<Quiz> Quizzes { get; set; }

        public JobPost()
        {
            DatePosted = DateTime.Now;
            JobApplications = new List<JobApplication>();
            Quizzes = new List<Quiz>();
        }
    }

    public class JobApplication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int JobPostId { get; set; }

        [ForeignKey("JobPostId")]
        public virtual JobPost JobPost { get; set; }

        [Required]
        public int CandidateId { get; set; }

        [ForeignKey("CandidateId")]
        public virtual Candidate Candidate { get; set; }

        [Display(Name = "Cover Letter")]
        [DataType(DataType.MultilineText)]
        public string CoverLetter { get; set; }

        [Display(Name = "Application Date")]
        public DateTime ApplicationDate { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }

        // Ranking Properties
        [Display(Name = "Ranking Score")]
        public double RankingScore { get; set; }

        [Display(Name = "Skills Match Percentage")]
        public int SkillsMatchPercentage { get; set; }

        [Display(Name = "Has Matric")]
        public bool HasMatric { get; set; }

        [Display(Name = "Has License")]
        public bool HasLicense { get; set; }

        [Display(Name = "Qualification Level")]
        public string QualificationLevel { get; set; }

        [Display(Name = "Is Shortlisted")]
        public bool IsShortlisted { get; set; }

        [Display(Name = "Shortlist Date")]
        public DateTime? ShortlistDate { get; set; }

        // Interview Properties
        [Display(Name = "Interview Scheduled")]
        public bool InterviewScheduled { get; set; }

        [Display(Name = "Interview Date & Time")]
        public DateTime? InterviewDateTime { get; set; }

        [Display(Name = "Interview Notes")]
        [DataType(DataType.MultilineText)]
        public string InterviewNotes { get; set; }

        [Display(Name = "Interview Result")]
        public string InterviewResult { get; set; }

        [Display(Name = "Interview Feedback")]
        [DataType(DataType.MultilineText)]
        public string InterviewFeedback { get; set; }

        [Display(Name = "Interview Assessed By")]
        public string InterviewAssessedBy { get; set; }

        [Display(Name = "Interview Assessed Date")]
        public DateTime? InterviewAssessedDate { get; set; }

        // Hiring Properties
        [Display(Name = "Is Hired")]
        public bool IsHired { get; set; }

        [Display(Name = "Hire Date")]
        public DateTime? HireDate { get; set; }

        [Display(Name = "Hired By")]
        public string HiredBy { get; set; }

        // Quiz Properties
        [Display(Name = "Quiz Score")]
        public int? QuizScore { get; set; }

        [Display(Name = "Quiz Passed")]
        public bool? QuizPassed { get; set; }

        [Display(Name = "Quiz Completed Date")]
        public DateTime? QuizCompletedDate { get; set; }

        // Navigation Properties
        public virtual ICollection<ApplicationStatusHistory> StatusHistory { get; set; }
        public virtual ICollection<QuizAttempt> QuizAttempts { get; set; }

        public JobApplication()
        {
            ApplicationDate = DateTime.Now;
            Status = "Pending";
            InterviewResult = "Pending";
            IsShortlisted = false;
            InterviewScheduled = false;
            IsHired = false;
            RankingScore = 0;
            SkillsMatchPercentage = 0;
            StatusHistory = new List<ApplicationStatusHistory>();
            QuizAttempts = new List<QuizAttempt>();
        }
    }

    public class ApplicationStatusHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ApplicationId { get; set; }

        [ForeignKey("ApplicationId")]
        public virtual JobApplication Application { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        [Display(Name = "Changed By")]
        public string ChangedBy { get; set; }

        [Display(Name = "Change Date")]
        public DateTime ChangeDate { get; set; }

        public ApplicationStatusHistory()
        {
            ChangeDate = DateTime.Now;
        }
    }

    // ===== QUIZ MODELS =====

    public class Quiz
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Quiz Title")]
        [StringLength(200)]
        public string Title { get; set; }

        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "Job Post")]
        public int? JobPostId { get; set; }

        [ForeignKey("JobPostId")]
        public virtual JobPost JobPost { get; set; }

        [Display(Name = "System Role")]
        public string SystemRoleId { get; set; }

        [Display(Name = "Time Limit (Minutes)")]
        public int TimeLimitMinutes { get; set; } = 30;

        [Display(Name = "Passing Score")]
        public int PassingScore { get; set; } = 60;

        [Display(Name = "Number of Questions")]
        public int NumberOfQuestions { get; set; } = 10;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        // Navigation Properties
        public virtual ICollection<QuizQuestion> Questions { get; set; }
        public virtual ICollection<QuizAttempt> Attempts { get; set; }

        public Quiz()
        {
            IsActive = true;
            CreatedDate = DateTime.Now;
            Questions = new List<QuizQuestion>();
            Attempts = new List<QuizAttempt>();
        }
    }

    public class QuizQuestion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuizId { get; set; }

        [ForeignKey("QuizId")]
        public virtual Quiz Quiz { get; set; }

        [Required]
        [Display(Name = "Question")]
        [DataType(DataType.MultilineText)]
        public string Question { get; set; }

        [Required]
        [Display(Name = "Question Type")]
        public string QuestionType { get; set; } = "MultipleChoice";

        [Display(Name = "Options (JSON)")]
        public string OptionsJson { get; set; }

        [Required]
        [Display(Name = "Correct Answer")]
        public string CorrectAnswer { get; set; }

        [Display(Name = "Points")]
        public int Points { get; set; } = 1;

        [Display(Name = "Order")]
        public int Order { get; set; }
    }

    public class QuizAttempt
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuizId { get; set; }

        [ForeignKey("QuizId")]
        public virtual Quiz Quiz { get; set; }

        [Required]
        public int ApplicationId { get; set; }

        [ForeignKey("ApplicationId")]
        public virtual JobApplication Application { get; set; }

        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; }

        [Display(Name = "End Time")]
        public DateTime? EndTime { get; set; }

        [Display(Name = "Score")]
        public int? Score { get; set; }

        [Display(Name = "Passed")]
        public bool? Passed { get; set; }

        [Display(Name = "Answers (JSON)")]
        public string AnswersJson { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } // InProgress, Completed, TimedOut

        public QuizAttempt()
        {
            StartTime = DateTime.Now;
            Status = "InProgress";
        }
    }
}