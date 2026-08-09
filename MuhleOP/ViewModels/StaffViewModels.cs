using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MuhleOP.ViewModels
{
    public class CreateStaffViewModel
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

        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "Role")]
        public string Role { get; set; }

        [Display(Name = "Phone Number")]
        [Phone]
        public string PhoneNumber { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }
    }

    public class StaffViewModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string PhoneNumber { get; set; }
        public string EmployeeId { get; set; }
        public string Department { get; set; }
        public DateTime HireDate { get; set; }
        public bool IsActive { get; set; }
        public string ProfilePicturePath { get; set; }
        public string RoleBadgeColor => GetRoleColor();

        private string GetRoleColor()
        {
            switch (Role)
            {
                case "Crew Member": return "primary";
                case "Project Manager": return "success";
                case "Finance Officer": return "warning";
                case "Site Manager": return "info";
                case "Procurement Officer": return "secondary";
                default: return "secondary";
            }
        }
    }

    public class StaffListViewModel
    {
        public List<StaffViewModel> StaffMembers { get; set; }
        public int TotalStaff { get; set; }
        public int ActiveStaff { get; set; }
        public int InactiveStaff { get; set; }

        public StaffListViewModel()
        {
            StaffMembers = new List<StaffViewModel>();
        }
    }

    // ===== HIRE MANAGEMENT (auto account creation for hired candidates) =====

    public class HireManagementListItemViewModel
    {
        public int ApplicationId { get; set; }
        public int CandidateId { get; set; }
        public string CandidateFullName { get; set; }
        public string CandidateEmail { get; set; }
        public string CandidatePhone { get; set; }
        public string ProfilePicturePath { get; set; }

        public int JobPostId { get; set; }
        public string JobTitle { get; set; }
        public string Department { get; set; }
        public string SystemRoleName { get; set; }

        public DateTime? InterviewAssessedDate { get; set; }
        public string HiredBy { get; set; }
    }

    public class HireManagementListViewModel
    {
        public List<HireManagementListItemViewModel> ReadyToHire { get; set; }

        public HireManagementListViewModel()
        {
            ReadyToHire = new List<HireManagementListItemViewModel>();
        }
    }

    public class HireManagementDetailsViewModel
    {
        public int ApplicationId { get; set; }
        public int CandidateId { get; set; }
        public string CandidateFullName { get; set; }
        public string CandidateEmail { get; set; }
        public string CandidatePhone { get; set; }
        public string ProfilePicturePath { get; set; }

        public int JobPostId { get; set; }
        public string JobTitle { get; set; }
        public string Department { get; set; }

        // The role the auto-created account will be assigned - derived from the
        // job post's linked system role, falling back to the job title.
        public string SystemRoleName { get; set; }
    }

    public class StaffDetailsViewModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string PhoneNumber { get; set; }
        public string EmployeeId { get; set; }
        public string Department { get; set; }
        public DateTime HireDate { get; set; }
        public bool IsActive { get; set; }
        public string ProfilePicturePath { get; set; }
        public string UserId { get; set; }

        // Assigned Projects
        public List<AssignedProjectViewModel> AssignedProjects { get; set; }
        public List<AssignedTaskViewModel> CurrentTasks { get; set; }
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }

        public StaffDetailsViewModel()
        {
            AssignedProjects = new List<AssignedProjectViewModel>();
            CurrentTasks = new List<AssignedTaskViewModel>();
        }
    }

    public class AssignedProjectViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
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

    public class AssignedTaskViewModel
    {
        public int TaskId { get; set; }
        public string TaskTitle { get; set; }
        public string TaskDescription { get; set; }
        public DateTime TaskDate { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public string ProjectName { get; set; }
        public bool IsCompleted { get; set; }
        public string PriorityBadgeColor => GetPriorityColor();

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
}