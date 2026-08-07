using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace MuhleOP.ViewModels
{
    public class EquipmentViewModel
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
        public string Notes { get; set; }
        public int? AssignedProjectId { get; set; }
        public string AssignedProjectName { get; set; }

        public string StatusBadgeColor => GetStatusColor();
        public string ConditionBadgeColor => GetConditionColor();

        private string GetStatusColor()
        {
            switch (Status)
            {
                case "Available": return "success";
                case "In Use": return "primary";
                case "Under Maintenance": return "warning";
                case "Retired": return "danger";
                default: return "secondary";
            }
        }

        private string GetConditionColor()
        {
            switch (Condition)
            {
                case "Excellent": return "success";
                case "Good": return "info";
                case "Fair": return "warning";
                case "Poor": return "danger";
                default: return "secondary";
            }
        }
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
        public string Location { get; set; }

        [Display(Name = "Maintenance Frequency (Days)")]
        public int? MaintenanceFrequencyDays { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }
    }

    public class EditEquipmentViewModel
    {
        public int Id { get; set; }

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
        public string Location { get; set; }

        [Display(Name = "Maintenance Frequency (Days)")]
        public int? MaintenanceFrequencyDays { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }
    }

    public class EquipmentMaintenanceViewModel
    {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public string EquipmentName { get; set; }
        public DateTime MaintenanceDate { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public string MaintenanceType { get; set; }
        public string Description { get; set; }
        public decimal Cost { get; set; }
        public int? DurationHours { get; set; }
        public string TechnicianName { get; set; }
        public string ServiceProvider { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public string Status { get; set; }
        public string CompletionNotes { get; set; }
        public string PerformedBy { get; set; }
        public DateTime CreatedDate { get; set; }

        public string StatusBadgeColor => GetStatusColor();

        private string GetStatusColor()
        {
            switch (Status)
            {
                case "Scheduled": return "info";
                case "In Progress": return "warning";
                case "Completed": return "success";
                case "Cancelled": return "danger";
                default: return "secondary";
            }
        }
    }

    public class ScheduleMaintenanceViewModel
    {
        [Required(ErrorMessage = "Equipment is required")]
        [Display(Name = "Equipment")]
        public int EquipmentId { get; set; }

        [Required(ErrorMessage = "Maintenance date is required")]
        [Display(Name = "Maintenance Date")]
        [DataType(DataType.Date)]
        public DateTime MaintenanceDate { get; set; }

        [Display(Name = "Scheduled Date")]
        [DataType(DataType.Date)]
        public DateTime? ScheduledDate { get; set; }

        [Required(ErrorMessage = "Maintenance type is required")]
        [Display(Name = "Maintenance Type")]
        public string MaintenanceType { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "Cost")]
        public decimal Cost { get; set; }

        [Display(Name = "Duration Hours")]
        public int? DurationHours { get; set; }

        [Display(Name = "Technician Name")]
        [StringLength(100)]
        public string TechnicianName { get; set; }

        [Display(Name = "Service Provider")]
        [StringLength(100)]
        public string ServiceProvider { get; set; }

        [Display(Name = "Next Maintenance Date")]
        [DataType(DataType.Date)]
        public DateTime? NextMaintenanceDate { get; set; }

        public List<SelectListItem> EquipmentList { get; set; }
        public List<SelectListItem> MaintenanceTypes { get; set; }

        public ScheduleMaintenanceViewModel()
        {
            EquipmentList = new List<SelectListItem>();
            MaintenanceTypes = new List<SelectListItem>();
        }
    }

    public class EquipmentRepairViewModel
    {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public string EquipmentName { get; set; }
        public DateTime RepairDate { get; set; }
        public string IssueDescription { get; set; }
        public string PartsReplaced { get; set; }
        public decimal PartsCost { get; set; }
        public decimal LabourCost { get; set; }
        public decimal TotalCost { get; set; }
        public string TechnicianName { get; set; }
        public string Status { get; set; }
        public string RepairNotes { get; set; }
        public string VerifiedBy { get; set; }
        public DateTime? VerificationDate { get; set; }
        public string ReportedBy { get; set; }
        public DateTime CreatedDate { get; set; }

        public string StatusBadgeColor => GetStatusColor();

        private string GetStatusColor()
        {
            switch (Status)
            {
                case "Reported": return "danger";
                case "In Progress": return "warning";
                case "Completed": return "info";
                case "Verified": return "success";
                default: return "secondary";
            }
        }
    }

    public class RecordRepairViewModel
    {
        [Required(ErrorMessage = "Equipment is required")]
        [Display(Name = "Equipment")]
        public int EquipmentId { get; set; }

        [Required(ErrorMessage = "Repair date is required")]
        [Display(Name = "Repair Date")]
        [DataType(DataType.Date)]
        public DateTime RepairDate { get; set; }

        [Required(ErrorMessage = "Issue description is required")]
        [Display(Name = "Issue Description")]
        [DataType(DataType.MultilineText)]
        public string IssueDescription { get; set; }

        [Display(Name = "Parts Replaced")]
        [DataType(DataType.MultilineText)]
        public string PartsReplaced { get; set; }

        [Display(Name = "Parts Cost")]
        public decimal PartsCost { get; set; }

        [Display(Name = "Labour Cost")]
        public decimal LabourCost { get; set; }

        [Display(Name = "Technician Name")]
        [StringLength(100)]
        public string TechnicianName { get; set; }

        [Display(Name = "Repair Notes")]
        [DataType(DataType.MultilineText)]
        public string RepairNotes { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }

        [Display(Name = "Reported By")]
        public string ReportedBy { get; set; }

        public List<SelectListItem> EquipmentList { get; set; }
        public List<SelectListItem> RepairStatuses { get; set; }

        public RecordRepairViewModel()
        {
            EquipmentList = new List<SelectListItem>();
            RepairStatuses = new List<SelectListItem>();
        }
    }
}