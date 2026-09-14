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

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; } // Crew Member, Project Manager, Finance Officer, Site Manager, Procurement Officer

        [Display(Name = "Phone Number")]
        [Phone]
        public string PhoneNumber { get; set; }

        [Display(Name = "Employee ID")]
        public string EmployeeId { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }

        [Display(Name = "Hire Date")]
        public DateTime HireDate { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        [Display(Name = "User ID")]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Display(Name = "Profile Picture")]
        public string ProfilePicturePath { get; set; }

        // ---------------------------------------------------------------
        // Payroll fields (Use Case 23)
        // ---------------------------------------------------------------

        [Display(Name = "Pay Rate Per Hour")]
        public decimal PayRatePerHour { get; set; }

        [Display(Name = "Bank Account")]
        [StringLength(100)]
        public string BankAccount { get; set; }

        // Navigation Properties
        public virtual ICollection<ProjectResourceAllocation> ProjectAllocations { get; set; }
        public virtual ICollection<DailyTask> AssignedTasks { get; set; }
        public virtual ICollection<Timesheet> Timesheets { get; set; }
        public virtual ICollection<SiteInspection> Inspections { get; set; }

        // Projects where this Staff member is the Project Manager
        public virtual ICollection<Project> ManagedProjects { get; set; }

        // Projects where this Staff member is the Site Manager
        public virtual ICollection<Project> SupervisedProjects { get; set; }

        // DailyTasks created/assigned by this Staff member
        public virtual ICollection<DailyTask> CreatedTasks { get; set; }

        public Staff()
        {
            HireDate = DateTime.Now;
            IsActive = true;
            EmployeeId = GenerateEmployeeId();
            ProjectAllocations = new List<ProjectResourceAllocation>();
            AssignedTasks = new List<DailyTask>();
            Timesheets = new List<Timesheet>();
            Inspections = new List<SiteInspection>();
            ManagedProjects = new List<Project>();
            SupervisedProjects = new List<Project>();
            CreatedTasks = new List<DailyTask>();
        }

        private string GenerateEmployeeId()
        {
            return "EMP" + DateTime.Now.ToString("yyyyMMdd") + new Random().Next(1000, 9999).ToString();
        }

        public string FullName => $"{FirstName} {LastName}";
    }
}