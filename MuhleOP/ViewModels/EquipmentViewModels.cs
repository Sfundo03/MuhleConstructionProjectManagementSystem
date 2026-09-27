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
        public bool IsEscalated { get; set; }
        public bool IsDelayFlagged { get; set; }
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

        public List<string> EquipmentNameSuggestions { get; set; } = new List<string>();
        public List<string> CategorySuggestions { get; set; } = new List<string>();

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
        public bool IsEscalated { get; set; }
        public bool IsDelayFlagged { get; set; }

        // Enhanced Fields
        public string Severity { get; set; }
        public string SeverityBadgeColor { get; set; }
        public string Priority { get; set; }
        public string PriorityBadgeColor { get; set; }
        public bool RequiresApproval { get; set; }
        public bool IsQuarantined { get; set; }
        public bool RequiresProcurement { get; set; }
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

        // UC17 Postcondition Surfacing
        public decimal? ActualHours { get; set; }
        public DateTime? RevisedCompletionDate { get; set; }
        public string PhotoEvidencePath { get; set; }
        public bool IsDelayFlagged { get; set; }
        public bool IsEscalated { get; set; }
        public string ProcurementRequestReference { get; set; }
        public string CancellationReason { get; set; }

        // Enhanced Lifecycle Fields
        public string Severity { get; set; }
        public string SeverityBadgeColor { get; set; }
        public string Priority { get; set; }
        public string PriorityBadgeColor { get; set; }
        public string RepairCategory { get; set; }
        public string DiagnosisNotes { get; set; }
        public string WitnessName { get; set; }
        public string ReportedLocation { get; set; }
        public List<string> PhotoEvidencePaths { get; set; } = new List<string>();
        public decimal? EstimatedPartsCost { get; set; }
        public decimal? EstimatedLabourCost { get; set; }
        public decimal? EstimatedTotalCost { get; set; }
        public bool RequiresApproval { get; set; }
        public int? TechnicianId { get; set; }
        public CheckoutChecklistViewModel CheckoutChecklist { get; set; }
        public bool IsQuarantined { get; set; }
        public bool RequiresProcurement { get; set; }
        public string PartsNotes { get; set; }

        public List<RepairProgressLogItemViewModel> ProgressLogs { get; set; } = new List<RepairProgressLogItemViewModel>();
        public List<RepairPartUsageItemViewModel> PartUsages { get; set; } = new List<RepairPartUsageItemViewModel>();
    }

    public class RepairProgressLogItemViewModel
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public string PartsUsedSummary { get; set; }
        public decimal? ActualHours { get; set; }
        public DateTime? RevisedCompletionDate { get; set; }
        public string PhotoPath { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangeType { get; set; }
        public bool IsDelayFlagged { get; set; }
    }

    public class RepairPartUsageItemViewModel
    {
        public int Id { get; set; }
        public string PartName { get; set; }
        public string PartCode { get; set; }
        public int QuantityUsed { get; set; }
        public decimal UnitCostAtUse { get; set; }
        public decimal TotalCost => QuantityUsed * UnitCostAtUse;
        public DateTime UsedDate { get; set; }
        public string RecordedBy { get; set; }
    }

    public class UpdateRepairPartInputModel
    {
        public int EquipmentPartId { get; set; }
        public int QuantityUsed { get; set; }
    }

    public class RepairPartInputModel
    {
        public int? EquipmentPartId { get; set; }
        public int QuantityUsed { get; set; }
        public bool IsPendingOrder { get; set; }
        public string NewPartName { get; set; }
        public string NewPartCode { get; set; }
        public string NewPartUnit { get; set; }
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

        [Required(ErrorMessage = "Severity is required")]
        [Display(Name = "Severity")]
        public string Severity { get; set; }

        [Display(Name = "Witness Name")]
        [StringLength(100)]
        public string WitnessName { get; set; }

        [Display(Name = "Photos (optional)")]
        public IEnumerable<HttpPostedFileBase> Photos { get; set; }

        public string ProjectFilterLabel { get; set; }

        public List<SelectListItem> AvailableEquipment { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> SeverityOptions { get; set; } = new List<SelectListItem>();
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

        [Display(Name = "Technician")]
        public int? TechnicianId { get; set; }

        [Required(ErrorMessage = "Repair category is required")]
        [Display(Name = "Repair Category")]
        public string RepairCategory { get; set; }

        [Display(Name = "Diagnosis Notes")]
        [DataType(DataType.MultilineText)]
        public string DiagnosisNotes { get; set; }

        [Required(ErrorMessage = "Priority is required")]
        [Display(Name = "Priority")]
        public string Priority { get; set; }

        [Display(Name = "Estimated Parts Cost")]
        public decimal? EstimatedPartsCost { get; set; }

        [Display(Name = "Estimated Labour Cost")]
        public decimal? EstimatedLabourCost { get; set; }

        [Required(ErrorMessage = "Workshop start date is required")]
        [Display(Name = "Workshop Slot Start")]
        [DataType(DataType.Date)]
        public DateTime WorkshopSlotStart { get; set; }

        [Required(ErrorMessage = "Workshop end date is required")]
        [Display(Name = "Workshop Slot End")]
        [DataType(DataType.Date)]
        public DateTime WorkshopSlotEnd { get; set; }

        [Display(Name = "Required Parts (Free Text)")]
        [DataType(DataType.MultilineText)]
        public string RequiredParts { get; set; }

        [Display(Name = "Parts Notes")]
        [DataType(DataType.MultilineText)]
        public string PartsNotes { get; set; }

        [Display(Name = "Check-In Notes")]
        [DataType(DataType.MultilineText)]
        public string CheckInNotes { get; set; }

        public List<RepairPartInputModel> PartsToUse { get; set; } = new List<RepairPartInputModel>();
        public List<SelectListItem> AvailableParts { get; set; } = new List<SelectListItem>();
        public bool AnyPendingParts { get; set; }
        public string NewPartName { get; set; }
        public string NewPartCode { get; set; }
        public string NewPartUnit { get; set; }
        public int? NewPartQuantityNeeded { get; set; }

        public List<SelectListItem> AvailableTechnicians { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> PriorityOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();
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

        [Display(Name = "Actual Hours")]
        public decimal? ActualHours { get; set; }

        [Display(Name = "Revised Completion Date")]
        [DataType(DataType.Date)]
        public DateTime? RevisedCompletionDate { get; set; }

        [Display(Name = "Photo Evidence")]
        public HttpPostedFileBase PhotoEvidence { get; set; }

        [Display(Name = "Cancellation Reason")]
        [DataType(DataType.MultilineText)]
        public string CancellationReason { get; set; }

        public string OriginalStatus { get; set; }

        [Required]
        public byte[] RowVersion { get; set; }

        public List<UpdateRepairPartInputModel> PartsUsed { get; set; } = new List<UpdateRepairPartInputModel>();

        public List<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableParts { get; set; } = new List<SelectListItem>();
    }

    public class CheckoutChecklistViewModel
    {
        [Display(Name = "Load Test Passed")]
        public bool LoadTestPassed { get; set; }

        [Display(Name = "Safety Check Passed")]
        public bool SafetyCheckPassed { get; set; }

        [Display(Name = "Calibration Verified")]
        public bool CalibrationVerified { get; set; }

        [Display(Name = "Visual Inspection Passed")]
        public bool VisualInspectionPassed { get; set; }

        [Display(Name = "Checklist Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        [Display(Name = "Technician Sign-Off Name")]
        [StringLength(100)]
        public string TechnicianSignOffName { get; set; }
        public DateTime? TechnicianSignOffDate { get; set; }

        [Display(Name = "Admin Sign-Off Name")]
        [StringLength(100)]
        public string AdminSignOffName { get; set; }
        public DateTime? AdminSignOffDate { get; set; }

        [Display(Name = "Load Test Result (kg)")]
        public decimal? LoadTestResultKg { get; set; }
    }

    public class ReallocationSuggestionViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public DateTime AssignedDate { get; set; }
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

        public CheckoutChecklistViewModel CheckoutChecklist { get; set; } = new CheckoutChecklistViewModel();
        public List<ReallocationSuggestionViewModel> ReallocationSuggestions { get; set; } = new List<ReallocationSuggestionViewModel>();

        public List<SelectListItem> ConditionOptions { get; set; } = new List<SelectListItem>();
    }
}