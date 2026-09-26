using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuhleOP.Models
{
    public class Equipment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Equipment Name")]
        [StringLength(100)]
        public string EquipmentName { get; set; }

        [Required]
        [Display(Name = "Equipment Code")]
        [StringLength(50)]
        public string EquipmentCode { get; set; }

        [Display(Name = "Serial Number")]
        [StringLength(50)]
        public string SerialNumber { get; set; }

        [Display(Name = "Category")]
        public string Category { get; set; } // Heavy Equipment, Light Equipment, Tools, Vehicles

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
        public string Status { get; set; } // Available, In Use, Under Maintenance, Retired, Damaged, In Repair, Quarantined

        [Display(Name = "Condition")]
        public string Condition { get; set; } // Excellent, Good, Fair, Poor

        [Display(Name = "Location")]
        public string Location { get; set; }

        [Display(Name = "Current Location")]
        [StringLength(200)]
        public string CurrentLocation { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Last Maintenance Date")]
        [DataType(DataType.Date)]
        public DateTime? LastMaintenanceDate { get; set; }

        [Display(Name = "Next Maintenance Date")]
        [DataType(DataType.Date)]
        public DateTime? NextMaintenanceDate { get; set; }

        [Display(Name = "Maintenance Frequency")]
        public int? MaintenanceFrequencyDays { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        [Display(Name = "Assigned Project ID")]
        public int? AssignedProjectId { get; set; }

        [ForeignKey("AssignedProjectId")]
        public virtual Project AssignedProject { get; set; }

        // Navigation Properties
        public virtual ICollection<EquipmentAssignment> Assignments { get; set; }
        public virtual ICollection<EquipmentMaintenance> Maintenances { get; set; }
        public virtual ICollection<EquipmentRepair> Repairs { get; set; }
        public virtual ICollection<EquipmentCertificationRequirement> CertificationRequirements { get; set; }

        public Equipment()
        {
            Status = "Available";
            Condition = "Good";
            IsActive = true;
            Assignments = new List<EquipmentAssignment>();
            Maintenances = new List<EquipmentMaintenance>();
            Repairs = new List<EquipmentRepair>();
            CertificationRequirements = new List<EquipmentCertificationRequirement>();
            EquipmentCode = GenerateEquipmentCode();
        }

        private string GenerateEquipmentCode()
        {
            return "EQP" + DateTime.Now.ToString("yyyyMMdd") + new Random().Next(1000, 9999).ToString();
        }
    }

    public class EquipmentCertificationRequirement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EquipmentId { get; set; }

        [ForeignKey("EquipmentId")]
        public virtual Equipment Equipment { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Required Certification")]
        public string RequiredCertification { get; set; }
    }

    public class EquipmentMaintenance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EquipmentId { get; set; }

        [ForeignKey("EquipmentId")]
        public virtual Equipment Equipment { get; set; }

        [Required]
        [Display(Name = "Maintenance Date")]
        [DataType(DataType.Date)]
        public DateTime MaintenanceDate { get; set; }

        [Display(Name = "Scheduled Date")]
        [DataType(DataType.Date)]
        public DateTime? ScheduledDate { get; set; }

        [Display(Name = "Maintenance Type")]
        public string MaintenanceType { get; set; }

        [Required]
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

        [Display(Name = "Status")]
        public string Status { get; set; }

        [Display(Name = "Completion Notes")]
        [DataType(DataType.MultilineText)]
        public string CompletionNotes { get; set; }

        [Display(Name = "Performed By")]
        public string PerformedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        public EquipmentMaintenance()
        {
            MaintenanceDate = DateTime.Now;
            Status = "Scheduled";
            CreatedDate = DateTime.Now;
        }
    }

    public class EquipmentRepair
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EquipmentId { get; set; }

        [ForeignKey("EquipmentId")]
        public virtual Equipment Equipment { get; set; }

        [Required]
        [Display(Name = "Repair Date")]
        [DataType(DataType.Date)]
        public DateTime RepairDate { get; set; }

        [Required]
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

        [Display(Name = "Total Cost")]
        public decimal TotalCost { get; set; }

        [Display(Name = "Technician Name")]
        [StringLength(100)]
        public string TechnicianName { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }

        [Display(Name = "Repair Notes")]
        [DataType(DataType.MultilineText)]
        public string RepairNotes { get; set; }

        [Display(Name = "Verified By")]
        public string VerifiedBy { get; set; }

        [Display(Name = "Verification Date")]
        public DateTime? VerificationDate { get; set; }

        [Display(Name = "Reported By")]
        public string ReportedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        // UC17 Additive Fields & Navigations
        public decimal? ActualHours { get; set; }
        public DateTime? RevisedCompletionDate { get; set; }

        [StringLength(500)]
        public string PhotoEvidencePath { get; set; }

        [StringLength(50)]
        public string ProcurementRequestReference { get; set; }

        [StringLength(500)]
        public string CancellationReason { get; set; }

        public bool IsEscalated { get; set; }
        public int? EstimatedDurationDays { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        // Enhanced Lifecycle Additions (UC14, UC16, UC18)
        [StringLength(20)]
        public string Severity { get; set; } // Minor, Major, Critical, Unsafe

        [StringLength(2000)]
        public string PhotoEvidencePaths { get; set; }

        [StringLength(100)]
        public string WitnessName { get; set; }

        [StringLength(200)]
        public string ReportedLocation { get; set; }

        [StringLength(2000)]
        public string DiagnosisNotes { get; set; }

        [StringLength(50)]
        public string RepairCategory { get; set; } // Mechanical, Electrical, Structural, Cosmetic, Other

        public int? TechnicianId { get; set; }
        [ForeignKey("TechnicianId")]
        public virtual EquipmentTechnician Technician { get; set; }

        public decimal? EstimatedPartsCost { get; set; }
        public decimal? EstimatedLabourCost { get; set; }
        public decimal? EstimatedTotalCost { get; set; }
        public bool RequiresApproval { get; set; }

        [StringLength(20)]
        public string Priority { get; set; } // Low, Medium, High, Emergency

        public virtual EquipmentCheckoutChecklist CheckoutChecklist { get; set; }

        public virtual ICollection<RepairProgressLog> ProgressLogs { get; set; }
        public virtual ICollection<RepairPartUsage> PartUsages { get; set; }

        public EquipmentRepair()
        {
            RepairDate = DateTime.Now;
            Status = "Reported";
            CreatedDate = DateTime.Now;
            ProgressLogs = new List<RepairProgressLog>();
            PartUsages = new List<RepairPartUsage>();
        }
    }

    public class EquipmentTechnician
    {
        [Key] public int Id { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; }

        [StringLength(100)] public string Specialisation { get; set; }
        [StringLength(20)] public string PhoneNumber { get; set; }
        [StringLength(100)] public string Email { get; set; }

        public bool IsActive { get; set; } = true;

        public int? LinkedStaffId { get; set; }
        [ForeignKey("LinkedStaffId")]
        public virtual Staff LinkedStaff { get; set; }
    }

    public class EquipmentCheckoutChecklist
    {
        [Key] public int Id { get; set; }

        [Required] public int EquipmentRepairId { get; set; }
        [ForeignKey("EquipmentRepairId")]
        public virtual EquipmentRepair EquipmentRepair { get; set; }

        public bool LoadTestPassed { get; set; }
        public bool SafetyCheckPassed { get; set; }
        public bool CalibrationVerified { get; set; }
        public bool VisualInspectionPassed { get; set; }

        [StringLength(2000)] public string Notes { get; set; }

        [StringLength(100)] public string TechnicianSignOffName { get; set; }
        public DateTime? TechnicianSignOffDate { get; set; }

        [StringLength(100)] public string AdminSignOffName { get; set; }
        public DateTime? AdminSignOffDate { get; set; }

        public decimal? LoadTestResultKg { get; set; }
    }

    public class RepairProgressLog
    {
        [Key] public int Id { get; set; }

        [Required] public int EquipmentRepairId { get; set; }
        [ForeignKey("EquipmentRepairId")]
        public virtual EquipmentRepair EquipmentRepair { get; set; }

        [Required, StringLength(50)]
        public string Status { get; set; }

        [StringLength(2000)]
        public string Notes { get; set; }

        [StringLength(500)]
        public string PartsUsedSummary { get; set; }

        public decimal? ActualHours { get; set; }

        public DateTime? RevisedCompletionDate { get; set; }

        [StringLength(500)]
        public string PhotoPath { get; set; }

        [Required, StringLength(100)]
        public string ChangedBy { get; set; }

        [Required, StringLength(128)]
        public string ChangedByUserId { get; set; }

        public DateTime ChangedAt { get; set; }

        [StringLength(20)]
        public string ChangeType { get; set; } // Progress, StatusChange, Parts, Photo, Escalation, Reassignment, Cancelled, Delay

        public bool IsDelayFlagged { get; set; }

        public RepairProgressLog()
        {
            ChangedAt = DateTime.Now;
        }
    }

    public class EquipmentPart
    {
        [Key] public int Id { get; set; }

        [Required, StringLength(100)]
        public string PartName { get; set; }

        [StringLength(50)]
        public string PartCode { get; set; }

        [StringLength(50)]
        public string Unit { get; set; } // e.g. "each", "metre", "litre"

        public int QuantityOnHand { get; set; }

        public int MinimumStockLevel { get; set; }

        public decimal? UnitCost { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<RepairPartUsage> PartUsages { get; set; }
        public virtual ICollection<StockMovement> StockMovements { get; set; }

        public EquipmentPart()
        {
            IsActive = true;
            PartUsages = new List<RepairPartUsage>();
            StockMovements = new List<StockMovement>();
        }
    }

    public class RepairPartUsage
    {
        [Key] public int Id { get; set; }

        [Required] public int EquipmentRepairId { get; set; }
        [ForeignKey("EquipmentRepairId")]
        public virtual EquipmentRepair EquipmentRepair { get; set; }

        [Required] public int EquipmentPartId { get; set; }
        [ForeignKey("EquipmentPartId")]
        public virtual EquipmentPart EquipmentPart { get; set; }

        public int QuantityUsed { get; set; }

        public decimal UnitCostAtUse { get; set; }

        public DateTime UsedDate { get; set; }

        [StringLength(100)]
        public string RecordedBy { get; set; }

        public RepairPartUsage()
        {
            UsedDate = DateTime.Now;
        }
    }

    public class StockMovement
    {
        [Key] public int Id { get; set; }

        [Required] public int EquipmentPartId { get; set; }
        [ForeignKey("EquipmentPartId")]
        public virtual EquipmentPart EquipmentPart { get; set; }

        [Required, StringLength(20)]
        public string MovementType { get; set; } // IN, OUT, ADJUST

        public int Quantity { get; set; } // signed: negative for OUT

        public int BalanceAfter { get; set; }

        [StringLength(500)]
        public string Reason { get; set; }

        [StringLength(100)]
        public string RecordedBy { get; set; }

        public DateTime RecordedAt { get; set; }

        public StockMovement()
        {
            RecordedAt = DateTime.Now;
        }
    }
}