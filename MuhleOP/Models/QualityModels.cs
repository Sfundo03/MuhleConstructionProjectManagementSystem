using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuhleOP.Models
{
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

        public SiteInspection()
        {
            InspectionDate = DateTime.Now;
            Status = "Draft";
            IsPassed = false;
            ActionRequired = false;
            CreatedDate = DateTime.Now;
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

        [Display(Name = "Impact on Cost")]
        public decimal? ImpactOnCost { get; set; }

        [Display(Name = "Impact on Timeline")]
        public int? ImpactOnTimelineDays { get; set; }

        [Display(Name = "Requested By")]
        public string RequestedBy { get; set; }

        [Display(Name = "Request Date")]
        public DateTime RequestDate { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } // Requested, Under Review, Approved, Rejected, Implemented

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

        public ProjectVariation()
        {
            RequestDate = DateTime.Now;
            Status = "Requested";
            VariationNumber = GenerateVariationNumber();
            Approvals = new List<VariationApproval>();
        }

        private string GenerateVariationNumber()
        {
            return "VAR" + DateTime.Now.ToString("yyyyMMdd") + new Random().Next(1000, 9999).ToString();
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