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
        public string Status { get; set; } // Available, In Use, Under Maintenance, Retired, Damaged, In Repair

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

        public EquipmentRepair()
        {
            RepairDate = DateTime.Now;
            Status = "Reported";
            CreatedDate = DateTime.Now;
        }
    }
}