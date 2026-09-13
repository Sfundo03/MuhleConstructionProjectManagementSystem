using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace MuhleOP.ViewModels
{
    // ------------------------------------------------------------------
    // Inventory
    // ------------------------------------------------------------------

    public class EquipmentIndexViewModel
    {
        public List<EquipmentListItemViewModel> Items { get; set; } = new List<EquipmentListItemViewModel>();
        public int TotalCount { get; set; }
        public int AvailableCount { get; set; }
        public int InUseCount { get; set; }
        public int UnderMaintenanceCount { get; set; }
        public int DueCount { get; set; }
        public string ActiveFilter { get; set; }
        public List<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Statuses { get; set; } = new List<SelectListItem>();
        public string SearchTerm { get; set; }
        public bool DueOnly { get; set; }
    }

    public class EquipmentListItemViewModel
    {
        public int Id { get; set; }
        public string EquipmentName { get; set; }
        public string EquipmentCode { get; set; }
        public string SerialNumber { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public string StatusBadgeColor { get; set; }
        public string Condition { get; set; }
        public string ConditionBadgeColor { get; set; }
        public string Location { get; set; }
        public string AssignedProjectName { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public string MaintenanceBadgeColor { get; set; }
        public string MaintenanceBadgeText { get; set; }
    }

    public class CreateEquipmentViewModel
    {
        [Required(ErrorMessage = "Equipment name is required")]
        [Display(Name = "Equipment Name")]
        [StringLength(100)]
        public string EquipmentName { get; set; }

        [Required(ErrorMessage = "Serial number is required")]
        [Display(Name = "Serial Number")]
        [StringLength(50)]
        public string SerialNumber { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public string Category { get; set; }

        [Display(Name = "Sub-Category")]
        [StringLength(50)]
        public string SubCategory { get; set; }

        [Display(Name = "Make")]
        [StringLength(50)]
        public string Make { get; set; }

        [Display(Name = "Model")]
        [StringLength(50)]
        public string Model { get; set; }

        [Display(Name = "Year")]
        public int? Year { get; set; }

        [Display(Name = "Purchase Date")]
        [DataType(DataType.Date)]
        public DateTime? PurchaseDate { get; set; }

        [Display(Name = "Purchase Cost")]
        public decimal? PurchaseCost { get; set; }

        [Display(Name = "Current Value")]
        public decimal? CurrentValue { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }

        [Display(Name = "Condition")]
        public string Condition { get; set; }

        [Display(Name = "Location")]
        [StringLength(100)]
        public string Location { get; set; }

        [Display(Name = "Maintenance Frequency (Days)")]
        public int? MaintenanceFrequencyDays { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        public List<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Conditions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Statuses { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> MaintenanceFrequencies { get; set; } = new List<SelectListItem>();
    }

    public class EditEquipmentViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Equipment name is required")]
        [Display(Name = "Equipment Name")]
        [StringLength(100)]
        public string EquipmentName { get; set; }

        [Display(Name = "Equipment Code")]
        public string EquipmentCode { get; set; }

        [Required(ErrorMessage = "Serial number is required")]
        [Display(Name = "Serial Number")]
        [StringLength(50)]
        public string SerialNumber { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public string Category { get; set; }

        [Display(Name = "Sub-Category")]
        [StringLength(50)]
        public string SubCategory { get; set; }

        [Display(Name = "Make")]
        [StringLength(50)]
        public string Make { get; set; }

        [Display(Name = "Model")]
        [StringLength(50)]
        public string Model { get; set; }

        [Display(Name = "Year")]
        public int? Year { get; set; }

        [Display(Name = "Purchase Date")]
        [DataType(DataType.Date)]
        public DateTime? PurchaseDate { get; set; }

        [Display(Name = "Purchase Cost")]
        public decimal? PurchaseCost { get; set; }

        [Display(Name = "Current Value")]
        public decimal? CurrentValue { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }

        [Display(Name = "Condition")]
        public string Condition { get; set; }

        [Display(Name = "Location")]
        [StringLength(100)]
        public string Location { get; set; }

        [Display(Name = "Maintenance Frequency (Days)")]
        public int? MaintenanceFrequencyDays { get; set; }

        [Display(Name = "Next Maintenance Date")]
        [DataType(DataType.Date)]
        public DateTime? NextMaintenanceDate { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        public List<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Conditions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Statuses { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> MaintenanceFrequencies { get; set; } = new List<SelectListItem>();
    }

    public class EquipmentDetailsViewModel
    {
        public int Id { get; set; }
        public string EquipmentName { get; set; }
        public string EquipmentCode { get; set; }
        public string SerialNumber { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int? Year { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public decimal? PurchaseCost { get; set; }
        public decimal? CurrentValue { get; set; }
        public string Status { get; set; }
        public string Condition { get; set; }
        public string Location { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public int? MaintenanceFrequencyDays { get; set; }
        public string Notes { get; set; }
        public int? AssignedProjectId { get; set; }
        public string AssignedProjectName { get; set; }
        public string AssignedProjectManager { get; set; }
        public DateTime? AssignedDate { get; set; }

        public List<EquipmentMaintenanceListItemViewModel> MaintenanceHistory { get; set; } = new List<EquipmentMaintenanceListItemViewModel>();
        public List<EquipmentRepairListItemViewModel> RepairHistory { get; set; } = new List<EquipmentRepairListItemViewModel>();
    }

    public class EquipmentMaintenanceListItemViewModel
    {
        public int Id { get; set; }
        public DateTime MaintenanceDate { get; set; }
        public string MaintenanceType { get; set; }
        public string Description { get; set; }
        public string TechnicianName { get; set; }
        public string Status { get; set; }
        public string StatusBadgeColor { get; set; }
    }

    // ------------------------------------------------------------------
    // Repair lifecycle
    // ------------------------------------------------------------------

    public class EquipmentRepairQueueViewModel
    {
        public List<EquipmentRepairListItemViewModel> Items { get; set; } = new List<EquipmentRepairListItemViewModel>();
        public int ReportedCount { get; set; }
        public int InRepairCount { get; set; }
        public int ReadyForVerificationCount { get; set; }
        public int VerifiedCount { get; set; }
        public string ActiveFilter { get; set; }
    }

    public class EquipmentRepairListItemViewModel
    {
        public int RepairId { get; set; }
        public string TicketLabel { get; set; }
        public string EquipmentName { get; set; }
        public string EquipmentCode { get; set; }
        public string ProjectName { get; set; }
        public string ReportedBy { get; set; }
        public DateTime ReportedDate { get; set; }
        public string Status { get; set; }
        public string StatusBadgeColor { get; set; }
        public string TechnicianName { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class EquipmentRepairDetailsViewModel
    {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public string EquipmentName { get; set; }
        public string EquipmentCode { get; set; }
        public string EquipmentCategory { get; set; }
        public string EquipmentStatus { get; set; }
        public string EquipmentCondition { get; set; }
        public string AssignedProjectName { get; set; }

        public DateTime RepairDate { get; set; }
        public string IssueDescription { get; set; }
        public string PartsReplaced { get; set; }
        public decimal PartsCost { get; set; }
        public decimal LabourCost { get; set; }
        public decimal TotalCost { get; set; }
        public string TechnicianName { get; set; }
        public string Status { get; set; }
        public string StatusBadgeColor { get; set; }
        public string RepairNotes { get; set; }
        public string VerifiedBy { get; set; }
        public DateTime? VerificationDate { get; set; }
        public string ReportedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ReportDamageViewModel
    {
        [Required(ErrorMessage = "Please select the equipment")]
        [Display(Name = "Equipment")]
        public int EquipmentId { get; set; }

        [Required(ErrorMessage = "Please describe the damage / issue")]
        [Display(Name = "Issue Description")]
        [DataType(DataType.MultilineText)]
        public string IssueDescription { get; set; }

        [Display(Name = "Photo (optional)")]
        public HttpPostedFileBase Photo { get; set; }

        public List<SelectListItem> AvailableEquipment { get; set; } = new List<SelectListItem>();
    }

    public class ScheduleRepairViewModel
    {
        public int RepairId { get; set; }
        public string EquipmentName { get; set; }
        public string CurrentStatus { get; set; }

        [Required(ErrorMessage = "Technician name is required")]
        [Display(Name = "Technician Name")]
        [StringLength(100)]
        public string TechnicianName { get; set; }

        [Required(ErrorMessage = "Workshop start date is required")]
        [Display(Name = "Workshop Slot Start")]
        [DataType(DataType.Date)]
        public DateTime WorkshopSlotStart { get; set; }

        [Required(ErrorMessage = "Workshop end date is required")]
        [Display(Name = "Workshop Slot End")]
        [DataType(DataType.Date)]
        public DateTime WorkshopSlotEnd { get; set; }

        [Display(Name = "Required Parts")]
        [DataType(DataType.MultilineText)]
        public string RequiredParts { get; set; }

        [Display(Name = "Check-In Notes")]
        [DataType(DataType.MultilineText)]
        public string CheckInNotes { get; set; }
    }

    public class UpdateRepairProgressViewModel
    {
        public int RepairId { get; set; }
        public string EquipmentName { get; set; }

        [Display(Name = "Technician Name")]
        [StringLength(100)]
        public string TechnicianName { get; set; }

        [Display(Name = "Parts Replaced")]
        [DataType(DataType.MultilineText)]
        public string PartsReplaced { get; set; }

        [Display(Name = "Parts Cost")]
        public decimal PartsCost { get; set; }

        [Display(Name = "Labour Cost")]
        public decimal LabourCost { get; set; }

        [Display(Name = "Total Cost")]
        public decimal TotalCost { get; set; }

        [Display(Name = "Repair Notes")]
        [DataType(DataType.MultilineText)]
        public string RepairNotes { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Status")]
        public string Status { get; set; }

        public List<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
    }

    public class CheckOutRepairViewModel
    {
        public int RepairId { get; set; }
        public string EquipmentName { get; set; }
        public string EquipmentCode { get; set; }
        public string TechnicianName { get; set; }
        public string RepairSummary { get; set; }

        [Required(ErrorMessage = "Final condition is required")]
        [Display(Name = "Final Condition")]
        public string FinalCondition { get; set; }

        [Display(Name = "Final Verification Notes")]
        [DataType(DataType.MultilineText)]
        public string FinalVerificationNotes { get; set; }

        [Required]
        [Display(Name = "I confirm the equipment passed the final safety/load test")]
        public bool FinalVerified { get; set; }

        public List<SelectListItem> ConditionOptions { get; set; } = new List<SelectListItem>();
    }
}