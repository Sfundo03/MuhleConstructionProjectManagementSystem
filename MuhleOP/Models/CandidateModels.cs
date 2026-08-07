using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuhleOP.Models
{
    public class Candidate
    {
        [Key]
        public int Id { get; set; }

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

        [Display(Name = "ID Document Path")]
        public string IdDocumentPath { get; set; }

        [Display(Name = "Qualifications Document Path")]
        public string QualificationsPath { get; set; }

        [Display(Name = "CV Document Path")]
        public string CvPath { get; set; }

        [Display(Name = "License Document Path")]
        public string LicensePath { get; set; }

        [Display(Name = "Profile Picture")]
        public string ProfilePicturePath { get; set; }

        [Display(Name = "Registration Date")]
        public DateTime RegistrationDate { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        [Display(Name = "User ID")]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // Navigation Properties
        public virtual ICollection<JobApplication> JobApplications { get; set; }

        public Candidate()
        {
            RegistrationDate = DateTime.Now;
            IsActive = true;
            JobApplications = new List<JobApplication>();
        }

        public string FullName => $"{FirstName} {LastName}";
    }
}