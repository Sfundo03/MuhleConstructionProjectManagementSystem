// ============ Models/StaffModels.cs ============
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuhleOP.Models
{
    public class Staff
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

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        [StringLength(256)]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Role")]
        [StringLength(50)]
        public string Role { get; set; } // Crew Member, Project Manager, Site Manager, Finance Officer, Procurement Officer

        [Display(Name = "Phone Number")]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [Display(Name = "Employee ID")]
        [StringLength(50)]
        public string EmployeeId { get; set; }

        [Display(Name = "Department")]
        [StringLength(100)]
        public string Department { get; set; }

        [Display(Name = "Certifications")]
        [StringLength(500)]
        public string Certifications { get; set; }

        [Display(Name = "Hire Date")]
        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Profile Picture Path")]
        [StringLength(500)]
        public string ProfilePicturePath { get; set; }

        /// <summary>
        /// Links to AspNetUsers.Id
        /// </summary>
        [Required]
        [StringLength(128)]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // ------------------------------------------------------------------
        // Payroll / Finance
        // ------------------------------------------------------------------

        /// <summary>
        /// Hourly pay rate used by Payroll generation and calculation.
        /// </summary>
        [Display(Name = "Pay Rate Per Hour")]
        [Range(0, 999999)]
        public decimal PayRatePerHour { get; set; }

        /// <summary>
        /// Bank account number / details used when processing payment.
        /// </summary>
        [Display(Name = "Bank Account")]
        [StringLength(100)]
        public string BankAccount { get; set; }

        // ------------------------------------------------------------------
        // QR Attendance Token
        // ------------------------------------------------------------------

        [StringLength(64)]
        public string QrToken { get; set; }

        public DateTime? QrTokenCreatedDate { get; set; }

        public bool QrTokenRevoked { get; set; }

        // ------------------------------------------------------------------
        // Navigation properties
        // ------------------------------------------------------------------

        public virtual ICollection<Project> ManagedProjects { get; set; }
        public virtual ICollection<Project> SupervisedProjects { get; set; }
        public virtual ICollection<DailyTask> AssignedTasks { get; set; }
        public virtual ICollection<DailyTask> CreatedTasks { get; set; }
        public virtual ICollection<Timesheet> Timesheets { get; set; }
        public virtual ICollection<SiteInspection> Inspections { get; set; }
        public virtual ICollection<ProjectResourceAllocation> ResourceAllocations { get; set; }

        public virtual ICollection<ShiftAssignment> ShiftAssignments { get; set; }
        public virtual ICollection<LeaveRecord> LeaveRecords { get; set; }
        public Staff()
        {
            HireDate = DateTime.Now;
            IsActive = true;
            QrTokenRevoked = false;
            PayRatePerHour = 0m;
            ManagedProjects = new List<Project>();
            SupervisedProjects = new List<Project>();
            AssignedTasks = new List<DailyTask>();
            CreatedTasks = new List<DailyTask>();
            Timesheets = new List<Timesheet>();
            Inspections = new List<SiteInspection>();
            ResourceAllocations = new List<ProjectResourceAllocation>();
        }
    }

    public class PayRateAudit
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StaffId { get; set; }

        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }

        [Required]
        [Display(Name = "Old Rate")]
        public decimal OldRate { get; set; }

        [Required]
        [Display(Name = "New Rate")]
        public decimal NewRate { get; set; }

        [Required]
        [StringLength(256)]
        public string ChangedBy { get; set; }

        [Required]
        [StringLength(128)]
        public string ChangedByUserId { get; set; }

        [Required]
        public DateTime ChangedAt { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        public PayRateAudit()
        {
            ChangedAt = DateTime.Now;
        }
    }
}