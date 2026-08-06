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
    }

    public class StaffListViewModel
    {
        public List<StaffViewModel> StaffMembers { get; set; }
        public int TotalStaff { get; set; }
        public int ActiveStaff { get; set; }
        public int InactiveStaff { get; set; }
    }
}