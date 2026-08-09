using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace MuhleOP.ViewModels
{
    // ===== CANDIDATE REGISTRATION =====

    public class CandidateRegisterViewModel
    {
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

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Gender")]
        public string Gender { get; set; }

        [Display(Name = "Nationality")]
        [StringLength(50)]
        public string Nationality { get; set; }

        [Display(Name = "Current Address")]
        [DataType(DataType.MultilineText)]
        public string CurrentAddress { get; set; }

        [Display(Name = "ID Document")]
        [Required(ErrorMessage = "ID document is required")]
        public HttpPostedFileBase IdDocument { get; set; }

        [Display(Name = "Qualifications")]
        [Required(ErrorMessage = "Qualifications document is required")]
        public HttpPostedFileBase Qualifications { get; set; }

        [Display(Name = "CV/Resume")]
        [Required(ErrorMessage = "CV is required")]
        public HttpPostedFileBase Cv { get; set; }

        [Display(Name = "Driver's License (Optional)")]
        public HttpPostedFileBase License { get; set; }

        [Display(Name = "Profile Picture (Optional)")]
        public HttpPostedFileBase ProfilePicture { get; set; }
    }

    // ===== CANDIDATE DASHBOARD =====

    public class CandidateDashboardViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfilePicturePath { get; set; }
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int ShortlistedApplications { get; set; }
        public int InterviewScheduledCount { get; set; }
        public int HiredCount { get; set; }
        public List<JobApplicationSummaryViewModel> RecentApplications { get; set; }
        public List<JobPostSummaryViewModel> AvailableJobs { get; set; }

        public CandidateDashboardViewModel()
        {
            RecentApplications = new List<JobApplicationSummaryViewModel>();
            AvailableJobs = new List<JobPostSummaryViewModel>();
        }
    }

    public class JobApplicationSummaryViewModel
    {
        public int Id { get; set; }
        public string JobTitle { get; set; }
        public string CompanyName { get; set; }
        public DateTime ApplicationDate { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
        public string InterviewResult { get; set; }
        public bool IsHired { get; set; }
        public bool IsInterviewCompleted { get; set; }
        public bool IsShortlisted { get; set; }
        public bool InterviewScheduled { get; set; }
        public DateTime? InterviewDateTime { get; set; }
    }

    public class JobPostSummaryViewModel
    {
        public int Id { get; set; }
        public string JobTitle { get; set; }
        public string Department { get; set; }
        public string Location { get; set; }
        public string JobType { get; set; }
        public DateTime ClosingDate { get; set; }
        public bool IsPublished { get; set; }
        public bool IsClosed { get; set; }
        public int DaysLeft { get; set; }
        public int ApplicationCount { get; set; }
        public bool HasApplied { get; set; }
    }

    // ===== APPLY FOR JOB =====

    public class ApplyJobViewModel
    {
        public int JobPostId { get; set; }

        [Display(Name = "Job Title")]
        public string JobTitle { get; set; }

        [Display(Name = "Salary Range")]
        public string SalaryRange { get; set; }

        [Display(Name = "Job Description")]
        public string JobDescription { get; set; }

        [Display(Name = "Requirements")]
        public string Requirements { get; set; }

        [Display(Name = "Location")]
        public string Location { get; set; }

        [Display(Name = "Job Type")]
        public string JobType { get; set; }

        [Display(Name = "Closing Date")]
        [DataType(DataType.Date)]
        public DateTime? ClosingDate { get; set; }

        [Display(Name = "Cover Letter")]
        [DataType(DataType.MultilineText)]
        public string CoverLetter { get; set; }

        [Display(Name = "Update CV")]
        public HttpPostedFileBase UpdatedCv { get; set; }

        [Display(Name = "Update Qualifications")]
        public HttpPostedFileBase UpdatedQualifications { get; set; }

        [Display(Name = "Update ID Document")]
        public HttpPostedFileBase UpdatedIdDocument { get; set; }

        [Display(Name = "Update License (Optional)")]
        public HttpPostedFileBase UpdatedLicense { get; set; }

        public bool HasExistingDocuments { get; set; }
        public bool HasApplied { get; set; }
    }

    public class CandidateProfileViewModel
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
    }

    // ===== APPLICATION STATUS =====

    public class ApplicationStatusViewModel
    {
        public int ApplicationId { get; set; }
        public string JobTitle { get; set; }
        public string Status { get; set; }
        public DateTime ApplicationDate { get; set; }
        public string CoverLetter { get; set; }
        public bool IsShortlisted { get; set; }
        public bool InterviewScheduled { get; set; }
        public DateTime? InterviewDateTime { get; set; }
        public string InterviewNotes { get; set; }
        public string InterviewResult { get; set; }
        public string InterviewFeedback { get; set; }
        public bool IsHired { get; set; }
        public bool IsInterviewCompleted { get; set; }
        public double RankingScore { get; set; }
        public int SkillsMatchPercentage { get; set; }

        // Quiz Properties
        public bool QuizInvited { get; set; }
        public bool QuizCompleted { get; set; }
        public int? QuizScore { get; set; }
        public bool? QuizPassed { get; set; }
        public DateTime? QuizCompletedDate { get; set; }
        public string QuizResult { get; set; }
        public QuizAttemptViewModel LatestQuizAttempt { get; set; }

        public List<ApplicationStatusHistoryViewModel> StatusHistory { get; set; }

        public ApplicationStatusViewModel()
        {
            StatusHistory = new List<ApplicationStatusHistoryViewModel>();
            InterviewResult = "Pending";
            IsHired = false;
            IsInterviewCompleted = false;
            QuizInvited = false;
            QuizCompleted = false;
            QuizResult = "Not Taken";
        }
    }

    public class ApplicationStatusHistoryViewModel
    {
        public string Status { get; set; }
        public string Notes { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangeDate { get; set; }
    }

    // ===== NOTIFICATION =====

    public class NotificationViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public bool IsRead { get; set; }
        public string Link { get; set; }
        public DateTime CreatedDate { get; set; }
        public string TimeAgo { get; set; }
        public string Priority { get; set; }

        public string IconClass => GetIconClass();
        public string TypeIcon => GetTypeIcon();

        private string GetIconClass()
        {
            switch (Type)
            {
                case "Application": return "application";
                case "Interview": return "interview";
                case "Status": return "status";
                case "System": return "system";
                case "Project": return "project";
                case "Finance": return "finance";
                case "Maintenance": return "maintenance";
                case "Quiz": return "quiz";
                default: return "system";
            }
        }

        private string GetTypeIcon()
        {
            switch (Type)
            {
                case "Application": return "file-alt";
                case "Interview": return "calendar-check";
                case "Status": return "clock";
                case "Project": return "project-diagram";
                case "Finance": return "coins";
                case "Maintenance": return "tools";
                case "Quiz": return "brain";
                case "System":
                default: return "bell";
            }
        }
    }

    public class NotificationListViewModel
    {
        public List<NotificationViewModel> Notifications { get; set; }
        public int UnreadCount { get; set; }
        public int TotalCount { get; set; }

        public NotificationListViewModel()
        {
            Notifications = new List<NotificationViewModel>();
        }
    }

    // ===== QUIZ ATTEMPT =====

    public class QuizAttemptViewModel
    {
        public int AttemptId { get; set; }
        public int QuizId { get; set; }
        public string QuizTitle { get; set; }
        public int ApplicationId { get; set; }
        public string JobTitle { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? Score { get; set; }
        public bool? Passed { get; set; }
        public string Status { get; set; }

        public string StatusBadgeColor => GetStatusColor();

        private string GetStatusColor()
        {
            switch (Status)
            {
                case "Available": return "primary";
                case "InProgress": return "warning";
                case "Completed": return Passed == true ? "success" : "danger";
                case "TimedOut": return "danger";
                default: return "secondary";
            }
        }
    }

    // ===== CLIENT VIEW MODELS =====

    public class ClientRegistrationViewModel
    {
        [Required(ErrorMessage = "Company name is required")]
        [Display(Name = "Company Name")]
        [StringLength(200)]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "Registration number is required")]
        [Display(Name = "Registration Number")]
        [StringLength(50)]
        public string RegistrationNumber { get; set; }

        [Required(ErrorMessage = "Tax identification number is required")]
        [Display(Name = "Tax Identification Number")]
        [StringLength(50)]
        public string TaxIdNumber { get; set; }

        [Required(ErrorMessage = "Contact person name is required")]
        [Display(Name = "Primary Contact Person")]
        [StringLength(100)]
        public string PrimaryContactName { get; set; }

        [Required(ErrorMessage = "Contact email is required")]
        [EmailAddress]
        [Display(Name = "Contact Email")]
        public string ContactEmail { get; set; }

        [Required(ErrorMessage = "Contact phone is required")]
        [Phone]
        [Display(Name = "Contact Phone")]
        public string ContactPhone { get; set; }

        [Display(Name = "Alternative Phone")]
        [Phone]
        public string AlternativePhone { get; set; }

        [Required(ErrorMessage = "Physical address is required")]
        [Display(Name = "Physical Address")]
        [DataType(DataType.MultilineText)]
        public string PhysicalAddress { get; set; }

        [Display(Name = "Postal Address")]
        [DataType(DataType.MultilineText)]
        public string PostalAddress { get; set; }

        [Display(Name = "Website")]
        [Url]
        public string Website { get; set; }

        [Display(Name = "Industry")]
        [StringLength(100)]
        public string Industry { get; set; }

        [Display(Name = "Company Size")]
        public string CompanySize { get; set; }

        [Required(ErrorMessage = "Billing information is required")]
        [Display(Name = "Billing Information")]
        [DataType(DataType.MultilineText)]
        public string BillingInformation { get; set; }

        [Required(ErrorMessage = "Payment terms are required")]
        [Display(Name = "Payment Terms")]
        public string PaymentTerms { get; set; }

        [Display(Name = "Credit Limit")]
        [Range(0, double.MaxValue, ErrorMessage = "Credit limit cannot be negative")]
        public decimal? CreditLimit { get; set; }
    }

    public class ClientViewModel
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string RegistrationNumber { get; set; }
        public string TaxIdNumber { get; set; }
        public string PrimaryContactName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string AlternativePhone { get; set; }
        public string PhysicalAddress { get; set; }
        public string PostalAddress { get; set; }
        public string Website { get; set; }
        public string Industry { get; set; }
        public string CompanySize { get; set; }
        public string BillingInformation { get; set; }
        public string PaymentTerms { get; set; }
        public decimal? CreditLimit { get; set; }
        public bool IsActive { get; set; }
        public DateTime RegistrationDate { get; set; }
        public int ContractCount { get; set; }
        public int ProjectCount { get; set; }
        public string DisplayName { get; set; }
    }

    public class ContractViewModel
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public string ProjectName { get; set; }
        public string ContractType { get; set; }
        public decimal ContractValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ProjectScope { get; set; }
        public string Deliverables { get; set; }
        public string PaymentTerms { get; set; }
        public string PaymentSchedule { get; set; }
        public string Milestones { get; set; }
        public string Timeline { get; set; }
        public string PenaltyClauses { get; set; }
        public string TerminationConditions { get; set; }
        public string SpecialConditions { get; set; }
        public string Status { get; set; }
        public bool IsSigned { get; set; }
        public DateTime? SignatureDate { get; set; }
        public string SignedDocumentPath { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string ContractFilePath { get; set; }
        public bool IsActive => Status != "Terminated" && Status != "Completed";
        public string StatusBadgeColor => GetStatusColor();

        private string GetStatusColor()
        {
            switch (Status)
            {
                case "Draft": return "secondary";
                case "Sent": return "info";
                case "Signed": return "success";
                case "Active": return "primary";
                case "Completed": return "success";
                case "Terminated": return "danger";
                default: return "secondary";
            }
        }
    }

    public class CreateContractViewModel
    {
        [Required]
        public int ClientId { get; set; }

        [Required(ErrorMessage = "Project name is required")]
        [Display(Name = "Project Name")]
        [StringLength(200)]
        public string ProjectName { get; set; }

        [Display(Name = "Contract Type")]
        public string ContractType { get; set; }

        [Required(ErrorMessage = "Contract value is required")]
        [Display(Name = "Contract Value")]
        public decimal ContractValue { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Project scope is required")]
        [Display(Name = "Project Scope")]
        [DataType(DataType.MultilineText)]
        public string ProjectScope { get; set; }

        [Required(ErrorMessage = "Deliverables are required")]
        [Display(Name = "Deliverables")]
        [DataType(DataType.MultilineText)]
        public string Deliverables { get; set; }

        [Required(ErrorMessage = "Payment terms are required")]
        [Display(Name = "Payment Terms")]
        [DataType(DataType.MultilineText)]
        public string PaymentTerms { get; set; }

        [Required(ErrorMessage = "Payment schedule is required")]
        [Display(Name = "Payment Schedule")]
        [DataType(DataType.MultilineText)]
        public string PaymentSchedule { get; set; }

        [Required(ErrorMessage = "Milestones are required")]
        [Display(Name = "Milestones")]
        [DataType(DataType.MultilineText)]
        public string Milestones { get; set; }

        [Required(ErrorMessage = "Timeline is required")]
        [Display(Name = "Timeline")]
        [DataType(DataType.MultilineText)]
        public string Timeline { get; set; }

        [Display(Name = "Penalty Clauses")]
        [DataType(DataType.MultilineText)]
        public string PenaltyClauses { get; set; }

        [Display(Name = "Termination Conditions")]
        [DataType(DataType.MultilineText)]
        public string TerminationConditions { get; set; }

        [Display(Name = "Special Conditions")]
        [DataType(DataType.MultilineText)]
        public string SpecialConditions { get; set; }

        public string Status { get; set; } = "Draft";

        [Display(Name = "Contract File")]
        public HttpPostedFileBase ContractFile { get; set; }

        public List<SelectListItem> Clients { get; set; }

        public CreateContractViewModel()
        {
            Clients = new List<SelectListItem>();
        }
    }

    public class SignContractViewModel
    {
        public int ContractId { get; set; }
        public string ContractNumber { get; set; }
        public string ProjectName { get; set; }
        public string ClientName { get; set; }
        public decimal ContractValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ProjectScope { get; set; }
        public string PaymentTerms { get; set; }
        public string Status { get; set; }

        [Required(ErrorMessage = "You must accept the terms to sign")]
        [Display(Name = "I accept the terms and conditions")]
        public bool AcceptTerms { get; set; }

        [Required(ErrorMessage = "Signature is required")]
        [Display(Name = "Electronic Signature")]
        public string SignatureData { get; set; }

        [Display(Name = "Full Name")]
        public string SignerName { get; set; }

        public string SignerEmail { get; set; }
    }

    // ===== PROJECT VIEW MODELS =====

    public class ProjectViewModel
    {
        public int Id { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public int? ClientId { get; set; }
        public string ClientName { get; set; }
        public int? ContractId { get; set; }
        public string ContractNumber { get; set; }
        public int ProjectManagerId { get; set; }
        public string ProjectManagerName { get; set; }
        public int? SiteManagerId { get; set; }
        public string SiteManagerName { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EstimatedEndDate { get; set; }
        public DateTime? ActualEndDate { get; set; }
        public string ProjectLocation { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public decimal? EstimatedCost { get; set; }
        public decimal? ActualCost { get; set; }
        public int? ProgressPercentage { get; set; }
        public int DeliverableCount { get; set; }
        public int CompletedDeliverableCount { get; set; }
        public int ResourceCount { get; set; }
        public int VariationCount { get; set; }
        public string StatusBadgeColor => GetStatusColor();

        private string GetStatusColor()
        {
            switch (Status)
            {
                case "Initiated": return "info";
                case "In Progress": return "primary";
                case "On Hold": return "warning";
                case "Completed": return "success";
                case "Closed": return "secondary";
                default: return "secondary";
            }
        }
    }

    public class InitiateProjectViewModel
    {
        [Required(ErrorMessage = "Project name is required")]
        [Display(Name = "Project Name")]
        [StringLength(200)]
        public string ProjectName { get; set; }

        [Required(ErrorMessage = "Client is required")]
        [Display(Name = "Client")]
        public int ClientId { get; set; }

        [Display(Name = "Contract")]
        public int? ContractId { get; set; }

        [Required(ErrorMessage = "Project Manager is required")]
        [Display(Name = "Project Manager")]
        public int ProjectManagerId { get; set; }

        [Display(Name = "Site Manager")]
        public int? SiteManagerId { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Estimated end date is required")]
        [Display(Name = "Estimated End Date")]
        [DataType(DataType.Date)]
        public DateTime EstimatedEndDate { get; set; }

        [Required(ErrorMessage = "Project location is required")]
        [Display(Name = "Project Location")]
        public string ProjectLocation { get; set; }

        [Required(ErrorMessage = "Project objectives are required")]
        [Display(Name = "Project Objectives")]
        [DataType(DataType.MultilineText)]
        public string Objectives { get; set; }

        [Required(ErrorMessage = "Scope definition is required")]
        [Display(Name = "Scope Definition")]
        [DataType(DataType.MultilineText)]
        public string ScopeDefinition { get; set; }

        [Display(Name = "Out of Scope")]
        [DataType(DataType.MultilineText)]
        public string OutOfScope { get; set; }

        [Display(Name = "Assumptions")]
        [DataType(DataType.MultilineText)]
        public string Assumptions { get; set; }

        [Display(Name = "Constraints")]
        [DataType(DataType.MultilineText)]
        public string Constraints { get; set; }

        [Required(ErrorMessage = "Estimated cost is required")]
        [Display(Name = "Estimated Cost")]
        public decimal EstimatedCost { get; set; }

        [Display(Name = "Budget Category")]
        public string BudgetCategory { get; set; }

        [Display(Name = "Deliverables")]
        [DataType(DataType.MultilineText)]
        public string Deliverables { get; set; }

        public List<SelectListItem> Clients { get; set; }
        public List<SelectListItem> Contracts { get; set; }
        public List<SelectListItem> ProjectManagers { get; set; }
        public List<SelectListItem> SiteManagers { get; set; }

        public InitiateProjectViewModel()
        {
            Clients = new List<SelectListItem>();
            Contracts = new List<SelectListItem>();
            ProjectManagers = new List<SelectListItem>();
            SiteManagers = new List<SelectListItem>();
        }
    }

    public class AssignStaffViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }

        [Required(ErrorMessage = "Staff member is required")]
        [Display(Name = "Staff Member")]
        public int StaffId { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "Role")]
        public string Role { get; set; }

        [Display(Name = "Allocation Percentage")]
        [Range(0, 100)]
        public int AllocationPercentage { get; set; } = 100;

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        public List<SelectListItem> AvailableStaff { get; set; }
        public List<SelectListItem> AvailableRoles { get; set; }
        public List<AssignedStaffViewModel> CurrentAssignments { get; set; }

        public AssignStaffViewModel()
        {
            AvailableStaff = new List<SelectListItem>();
            AvailableRoles = new List<SelectListItem>();
            CurrentAssignments = new List<AssignedStaffViewModel>();
        }
    }

    public class AssignedStaffViewModel
    {
        public int Id { get; set; }
        public string StaffName { get; set; }
        public string Role { get; set; }
        public int AllocationPercentage { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class AssignEquipmentViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }

        [Required(ErrorMessage = "Equipment is required")]
        [Display(Name = "Equipment")]
        public int EquipmentId { get; set; }

        [Display(Name = "Expected Return Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedReturnDate { get; set; }

        [Display(Name = "Condition on Assignment")]
        public string ConditionOnAssignment { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        public List<SelectListItem> AvailableEquipment { get; set; }
        public List<AssignedEquipmentViewModel> CurrentAssignments { get; set; }

        public AssignEquipmentViewModel()
        {
            AvailableEquipment = new List<SelectListItem>();
            CurrentAssignments = new List<AssignedEquipmentViewModel>();
        }
    }

    public class AssignedEquipmentViewModel
    {
        public int Id { get; set; }
        public string EquipmentName { get; set; }
        public string EquipmentCode { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? ExpectedReturnDate { get; set; }
        public bool IsActive { get; set; }
        public string ConditionOnAssignment { get; set; }
    }

    // ===== DAILY TASK VIEW MODELS =====

    public class DailyTaskViewModel
    {
        public int Id { get; set; }
        public string TaskTitle { get; set; }
        public string TaskDescription { get; set; }
        public int AssignedToStaffId { get; set; }
        public string AssignedToName { get; set; }
        public int AssignedByStaffId { get; set; }
        public string AssignedByName { get; set; }
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; }
        public DateTime TaskDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public string CompletionNotes { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletionDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsRecurring { get; set; }
        public string RecurrencePattern { get; set; }
        public string StatusBadgeColor => GetStatusColor();
        public string PriorityBadgeColor => GetPriorityColor();

        private string GetStatusColor()
        {
            switch (Status)
            {
                case "Pending": return "secondary";
                case "In Progress": return "info";
                case "Completed": return "success";
                case "Cancelled": return "danger";
                default: return "secondary";
            }
        }

        private string GetPriorityColor()
        {
            switch (Priority)
            {
                case "Low": return "secondary";
                case "Medium": return "info";
                case "High": return "warning";
                case "Urgent": return "danger";
                default: return "secondary";
            }
        }
    }

    public class CreateDailyTaskViewModel
    {
        [Required(ErrorMessage = "Task title is required")]
        [Display(Name = "Task Title")]
        [StringLength(200)]
        public string TaskTitle { get; set; }

        [Display(Name = "Task Description")]
        [DataType(DataType.MultilineText)]
        public string TaskDescription { get; set; }

        [Required(ErrorMessage = "Staff member is required")]
        [Display(Name = "Assign To")]
        public int AssignedToStaffId { get; set; }

        [Display(Name = "Project")]
        public int? ProjectId { get; set; }

        [Required(ErrorMessage = "Task date is required")]
        [Display(Name = "Task Date")]
        [DataType(DataType.Date)]
        public DateTime TaskDate { get; set; }

        [Display(Name = "Start Time")]
        [DataType(DataType.Time)]
        public TimeSpan? StartTime { get; set; }

        [Display(Name = "End Time")]
        [DataType(DataType.Time)]
        public TimeSpan? EndTime { get; set; }

        [Display(Name = "Priority")]
        public string Priority { get; set; }

        [Display(Name = "Is Recurring")]
        public bool IsRecurring { get; set; }

        [Display(Name = "Recurrence Pattern")]
        public string RecurrencePattern { get; set; }

        public List<SelectListItem> StaffMembers { get; set; }
        public List<SelectListItem> Projects { get; set; }
        public List<SelectListItem> Priorities { get; set; }

        public CreateDailyTaskViewModel()
        {
            StaffMembers = new List<SelectListItem>();
            Projects = new List<SelectListItem>();
            Priorities = new List<SelectListItem>();
        }
    }

    // ===== TIMESHEET VIEW MODELS =====

    public class TimesheetViewModel
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; }
        public DateTime WeekStarting { get; set; }
        public decimal TotalHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public string Status { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public string SubmittedBy { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string RejectionReason { get; set; }
        public string Notes { get; set; }
        public List<TimesheetEntryViewModel> Entries { get; set; }
        public string StatusBadgeColor => GetStatusColor();

        public TimesheetViewModel()
        {
            Entries = new List<TimesheetEntryViewModel>();
        }

        private string GetStatusColor()
        {
            switch (Status)
            {
                case "Draft": return "secondary";
                case "Submitted": return "info";
                case "Approved": return "success";
                case "Rejected": return "danger";
                default: return "secondary";
            }
        }
    }

    public class TimesheetEntryViewModel
    {
        public int Id { get; set; }
        public int TimesheetId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public int? BreakDurationMinutes { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal OvertimeHours { get; set; }
        public string TaskDescription { get; set; }
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; }
        public bool IsOvertime { get; set; }
        public bool IsHoliday { get; set; }
        public bool IsSickLeave { get; set; }
        public bool IsVacation { get; set; }
    }

    public class CreateTimesheetEntryViewModel
    {
        [Required(ErrorMessage = "Date is required")]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Display(Name = "Start Time")]
        [DataType(DataType.Time)]
        public TimeSpan? StartTime { get; set; }

        [Display(Name = "End Time")]
        [DataType(DataType.Time)]
        public TimeSpan? EndTime { get; set; }

        [Display(Name = "Break Duration (Minutes)")]
        public int? BreakDurationMinutes { get; set; }

        [Required(ErrorMessage = "Hours worked is required")]
        [Display(Name = "Hours Worked")]
        public decimal HoursWorked { get; set; }

        [Display(Name = "Overtime Hours")]
        public decimal OvertimeHours { get; set; }

        [Required(ErrorMessage = "Task description is required")]
        [Display(Name = "Task Description")]
        [StringLength(500)]
        public string TaskDescription { get; set; }

        [Display(Name = "Project")]
        public int? ProjectId { get; set; }

        [Display(Name = "Is Overtime")]
        public bool IsOvertime { get; set; }

        [Display(Name = "Is Holiday")]
        public bool IsHoliday { get; set; }

        [Display(Name = "Is Sick Leave")]
        public bool IsSickLeave { get; set; }

        [Display(Name = "Is Vacation")]
        public bool IsVacation { get; set; }

        public List<SelectListItem> Projects { get; set; }

        public CreateTimesheetEntryViewModel()
        {
            Projects = new List<SelectListItem>();
        }
    }

    public class SubmitTimesheetViewModel
    {
        public int TimesheetId { get; set; }
        public string StaffName { get; set; }
        public DateTime WeekStarting { get; set; }
        public decimal TotalHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public List<TimesheetEntryViewModel> Entries { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        public SubmitTimesheetViewModel()
        {
            Entries = new List<TimesheetEntryViewModel>();
        }
    }

    public class ApproveTimesheetViewModel
    {
        public int TimesheetId { get; set; }
        public string StaffName { get; set; }
        public DateTime WeekStarting { get; set; }
        public decimal TotalHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public List<TimesheetEntryViewModel> Entries { get; set; }

        [Display(Name = "Action")]
        public string Action { get; set; }

        [Display(Name = "Reason (if rejecting)")]
        [DataType(DataType.MultilineText)]
        public string RejectionReason { get; set; }

        public ApproveTimesheetViewModel()
        {
            Entries = new List<TimesheetEntryViewModel>();
        }
    }
}