using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace MuhleOP.ViewModels
{
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
        [Compare("Password", ErrorMessage = "Passwords do not match")]
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
        public List<JobApplicationSummaryViewModel> RecentApplications { get; set; }
        public List<JobPostSummaryViewModel> AvailableJobs { get; set; }
    }

    public class JobApplicationSummaryViewModel
    {
        public int Id { get; set; }
        public string JobTitle { get; set; }
        public string CompanyName { get; set; }
        public DateTime ApplicationDate { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
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
        public int DaysLeft { get; set; }
    }

    public class ApplyJobViewModel
    {
        public int JobPostId { get; set; }
        public string JobTitle { get; set; }
        public string JobDescription { get; set; }
        public string Requirements { get; set; }
        public string Location { get; set; }
        public string JobType { get; set; }
        public DateTime ClosingDate { get; set; }

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
}