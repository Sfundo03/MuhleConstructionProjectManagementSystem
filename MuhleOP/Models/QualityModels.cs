using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuhleOP.Models
{
    public class InspectionTemplateItem
    {
        [Key] public int Id { get; set; }

        [Required, StringLength(50)]
        public string InspectionType { get; set; } // Safety, Quality, Progress, Compliance

        [Required, StringLength(200)]
        public string ItemLabel { get; set; }

        [StringLength(50)]
        public string Category { get; set; } // Foundation, Framing, Electrical, Plumbing, etc.

        public int Order { get; set; }

        public bool IsActive { get; set; }
    }

    public class InspectionItemResult
    {
        [Key] public int Id { get; set; }

        [Required] public int SiteInspectionId { get; set; }
        [ForeignKey("SiteInspectionId")]
        public virtual SiteInspection SiteInspection { get; set; }

        [Required] public int TemplateItemId { get; set; }
        [ForeignKey("TemplateItemId")]
        public virtual InspectionTemplateItem TemplateItem { get; set; }

        [Required, StringLength(20)]
        public string Result { get; set; } // Pass, Fail, Partial, N/A

        [StringLength(1000)]
        public string Notes { get; set; }
    }

    public class NonConformance
    {
        [Key] public int Id { get; set; }

        [Required, StringLength(20)]
        public string ReferenceNumber { get; set; } // e.g. NC-0001

        [Required] public int SiteInspectionId { get; set; }
        [ForeignKey("SiteInspectionId")]
        public virtual SiteInspection SiteInspection { get; set; }

        [Required, StringLength(200)]
        public string Description { get; set; }

        [Required, StringLength(20)]
        public string Severity { get; set; } // Minor, Major, Critical

        [Required, StringLength(100)]
        public string ResponsibleParty { get; set; }

        [StringLength(50)]
        public string Status { get; set; } // Open, In Progress, Closed

        public DateTime? DueDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public NonConformance()
        {
            CreatedDate = DateTime.Now;
            Status = "Open";
        }
    }

    public class SiteInspection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [Required]
        public int PerformedByStaffId { get; set; }

        [ForeignKey("PerformedByStaffId")]
        public virtual Staff PerformedBy { get; set; }

        [Required]
        [Display(Name = "Inspection Date")]
        [DataType(DataType.DateTime)]
        public DateTime InspectionDate { get; set; }

        [Required]
        [Display(Name = "Inspection Type")]
        public string InspectionType { get; set; } // Safety, Quality, Progress, Compliance

        [Display(Name = "Inspection Area")]
        public string InspectionArea { get; set; }

        [Required]
        [Display(Name = "Findings")]
        [DataType(DataType.MultilineText)]
        public string Findings { get; set; }

        [Display(Name = "Recommendations")]
        [DataType(DataType.MultilineText)]
        public string Recommendations { get; set; }

        [Display(Name = "Score")]
        [Range(0, 100)]
        public int? Score { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } // Draft, Completed, Reviewed, Closed

        [Display(Name = "Is Passed")]
        public bool IsPassed { get; set; }

        [Display(Name = "Action Required")]
        public bool ActionRequired { get; set; }

        [Display(Name = "Action Items")]
        [DataType(DataType.MultilineText)]
        public string ActionItems { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Completion Date")]
        [DataType(DataType.Date)]
        public DateTime? CompletionDate { get; set; }

        [Display(Name = "Reviewed By")]
        public string ReviewedBy { get; set; }

        [Display(Name = "Review Date")]
        public DateTime? ReviewDate { get; set; }

        [Display(Name = "Photos")]
        public string PhotosPath { get; set; }

        [Display(Name = "Inspection Report Path")]
        public string ReportPath { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Measurement Notes")]
        [StringLength(2000)]
        public string MeasurementNotes { get; set; }

        [Display(Name = "Signed Off By")]
        [StringLength(100)]
        public string SignedOffBy { get; set; }

        [Display(Name = "Signed Off Date")]
        public DateTime? SignedOffDate { get; set; }

        [Display(Name = "Signature Data Path")]
        [StringLength(500)]
        public string SignatureDataPath { get; set; }

        // Navigation properties
        public virtual ICollection<InspectionItemResult> ItemResults { get; set; }
        public virtual ICollection<NonConformance> NonConformances { get; set; }

        public SiteInspection()
        {
            InspectionDate = DateTime.Now;
            Status = "Draft";
            IsPassed = false;
            ActionRequired = false;
            CreatedDate = DateTime.Now;
            ItemResults = new List<InspectionItemResult>();
            NonConformances = new List<NonConformance>();
        }
    }

    public class ProjectVariation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        // Link back to the inspection that triggered this variation (optional)
        public int? SiteInspectionId { get; set; }

        [ForeignKey("SiteInspectionId")]
        public virtual SiteInspection SiteInspection { get; set; }

        // Link back to specific non-conformance (optional)
        public int? NonConformanceId { get; set; }

        [ForeignKey("NonConformanceId")]
        public virtual NonConformance NonConformance { get; set; }

        [Required]
        [Display(Name = "Variation Number")]
        [StringLength(50)]
        public string VariationNumber { get; set; }

        [Required]
        [Display(Name = "Variation Type")]
        public string VariationType { get; set; } // Change Order, Scope Change, Time Extension, Cost Adjustment

        [Required]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "Reason")]
        [DataType(DataType.MultilineText)]
        public string Reason { get; set; }

        [Display(Name = "Impact on Scope")]
        [DataType(DataType.MultilineText)]
        public string ImpactOnScope { get; set; }

        [StringLength(2000)]
        [Display(Name = "Impact on Quality")]
        public string ImpactOnQuality { get; set; }

        [StringLength(2000)]
        [Display(Name = "Impact on Resources")]
        public string ImpactOnResources { get; set; }

        [Display(Name = "Impact on Cost")]
        public decimal? ImpactOnCost { get; set; }

        [Display(Name = "Impact on Timeline")]
        public int? ImpactOnTimelineDays { get; set; }

        [Display(Name = "Approved Cost Cap")]
        public decimal? ApprovedCostCap { get; set; }

        [Display(Name = "Finance Officer Approved")]
        public bool? FinanceOfficerApproved { get; set; }

        [StringLength(100)]
        [Display(Name = "Finance Officer Approved By")]
        public string FinanceOfficerApprovedBy { get; set; }

        [Display(Name = "Finance Officer Approval Date")]
        public DateTime? FinanceOfficerApprovalDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Variation Order PDF Path")]
        public string VariationOrderPdfPath { get; set; }

        [Display(Name = "Requested By")]
        public string RequestedBy { get; set; }

        [Display(Name = "Request Date")]
        public DateTime RequestDate { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } // Draft, PendingValidation, Requested, Under Review, PendingFinanceApproval, ConditionalApproval, Approved, Rejected, Implemented

        [Display(Name = "Approved By")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Approval Date")]
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Rejection Reason")]
        [DataType(DataType.MultilineText)]
        public string RejectionReason { get; set; }

        [Display(Name = "Implementation Date")]
        public DateTime? ImplementationDate { get; set; }

        [Display(Name = "Implementation Notes")]
        [DataType(DataType.MultilineText)]
        public string ImplementationNotes { get; set; }

        [Display(Name = "Supporting Documents")]
        public string SupportingDocumentsPath { get; set; }

        // Navigation Properties
        public virtual ICollection<VariationApproval> Approvals { get; set; }
        public virtual ICollection<ProjectBaselineSnapshot> BaselineSnapshots { get; set; }

        public ProjectVariation()
        {
            RequestDate = DateTime.Now;
            Status = "Requested";
            VariationNumber = "VAR-PENDING";
            Approvals = new List<VariationApproval>();
            BaselineSnapshots = new List<ProjectBaselineSnapshot>();
        }
    }

    public class ProjectBaselineSnapshot
    {
        [Key] public int Id { get; set; }

        [Required] public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [Required] public int VariationId { get; set; }
        [ForeignKey("VariationId")]
        public virtual ProjectVariation Variation { get; set; }

        public decimal PreviousBudgetEstimated { get; set; }
        public decimal PreviousBudgetAllocated { get; set; }
        public decimal PreviousBudgetRemaining { get; set; }
        public DateTime? PreviousEstimatedEndDate { get; set; }

        public decimal NewBudgetEstimated { get; set; }
        public decimal NewBudgetAllocated { get; set; }
        public decimal NewBudgetRemaining { get; set; }
        public DateTime? NewEstimatedEndDate { get; set; }

        [StringLength(100)] public string SnapshottedBy { get; set; }
        public DateTime SnapshottedDate { get; set; }

        public ProjectBaselineSnapshot()
        {
            SnapshottedDate = DateTime.Now;
        }
    }

    public class VariationApproval
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VariationId { get; set; }

        [ForeignKey("VariationId")]
        public virtual ProjectVariation Variation { get; set; }

        [Required]
        [Display(Name = "Approver Name")]
        [StringLength(100)]
        public string ApproverName { get; set; }

        [Required]
        [Display(Name = "Approver Email")]
        [EmailAddress]
        public string ApproverEmail { get; set; }

        [Display(Name = "Approval Status")]
        public string ApprovalStatus { get; set; } // Pending, Approved, Rejected

        [Display(Name = "Comments")]
        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }

        [Display(Name = "Approval Date")]
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Approval Level")]
        public int ApprovalLevel { get; set; }

        public VariationApproval()
        {
            ApprovalStatus = "Pending";
        }
    }
}