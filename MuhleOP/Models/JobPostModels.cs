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
        public int CandidateId { get; set; }

        [ForeignKey("CandidateId")]
        public virtual Candidate Candidate { get; set; }

        [Display(Name = "Application Date")]
        public DateTime ApplicationDate { get; set; }

        [Display(Name = "Application Status")]
        public string Status { get; set; }

        [Display(Name = "Cover Letter")]
        [DataType(DataType.MultilineText)]
        public string CoverLetter { get; set; }

        [Display(Name = "Additional Notes")]
        public string Notes { get; set; }

        [Display(Name = "Is Withdrawn")]
        public bool IsWithdrawn { get; set; }

        public JobApplication()
        {
            ApplicationDate = DateTime.Now;
            Status = "Pending";
            IsWithdrawn = false;
        }
    }
}