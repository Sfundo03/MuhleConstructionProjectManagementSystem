using System.ComponentModel.DataAnnotations;

namespace MuhleOP.ViewModels
{
    public class EquipmentPartFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Part name is required")]
        [Display(Name = "Part Name")]
        [StringLength(100)]
        public string PartName { get; set; }

        [Display(Name = "Part Code")]
        [StringLength(50)]
        public string PartCode { get; set; }

        [Display(Name = "Unit")]
        [StringLength(20)]
        public string Unit { get; set; } = "each";

        [Required(ErrorMessage = "Quantity on hand is required")]
        [Display(Name = "Quantity On Hand")]
        public int QuantityOnHand { get; set; }

        [Display(Name = "Minimum Stock Level")]
        public int MinimumStockLevel { get; set; }

        [Display(Name = "Unit Cost (R)")]
        public decimal? UnitCost { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class EquipmentTechnicianFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Technician full name is required")]
        [Display(Name = "Full Name")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email address is required for notifications")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        [Display(Name = "Email Address")]
        [StringLength(150)]
        public string Email { get; set; }

        [Display(Name = "Phone Number")]
        [StringLength(30)]
        public string Phone { get; set; }

        [Display(Name = "Specialization")]
        [StringLength(100)]
        public string Specialization { get; set; }

        public bool IsActive { get; set; } = true;
    }
}