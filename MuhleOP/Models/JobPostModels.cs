using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuhleOP.Models
{
    public class JobPost
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Job title is required")]
        [Display(Name = "Job Title")]
        [StringLength(100, ErrorMessage = "Job title cannot exceed 100 characters")]
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
        public string JobType { get; set; } // Full-time, Part-time, Contract, Temporary

        [Display(Name = "Experience Level")]
        public string ExperienceLevel { get; set; } // Entry, Junior, Mid, Senior, Lead

        [Display(Name = "Location")]
        [StringLength(100)]
        public string Location { get; set; }

        [Display(Name = "Salary Range")]
        [StringLength(50)]
        public string SalaryRange { get; set; }

        [Display(Name = "Number of Openings")]
        [Range(1, 100, ErrorMessage = "Number of openings must be between 1 and 100")]
        public int NumberOfOpenings { get; set; } = 1;

        [Display(Name = "Department")]
        [StringLength(50)]
        public string Department { get; set; }

        [Display(Name = "Is Published")]
        public bool IsPublished { get; set; }

        [Display(Name = "Date Posted")]
        public DateTime DatePosted { get; set; }

        [Display(Name = "Posted By")]
        public string PostedBy { get; set; }

        public JobPost()
        {
            DatePosted = DateTime.Now;
            IsPublished = true;
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
        [Display(Name = "First Name")]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Phone Number")]
        [Phone]
        public string PhoneNumber { get; set; }

        [Display(Name = "Cover Letter")]
        [DataType(DataType.MultilineText)]
        public string CoverLetter { get; set; }

        [Required]
        [Display(Name = "CV/Resume Path")]
        public string CvFilePath { get; set; }

        [Display(Name = "Application Status")]
        public string Status { get; set; } // Applied, Shortlisted, Interviewed, Hired, Rejected

        [Display(Name = "Date Applied")]
        public DateTime DateApplied { get; set; }

        [Display(Name = "Notes")]
        public string Notes { get; set; }

        public JobApplication()
        {
            DateApplied = DateTime.Now;
            Status = "Applied";
        }

        public string FullName => $"{FirstName} {LastName}";
    }
}