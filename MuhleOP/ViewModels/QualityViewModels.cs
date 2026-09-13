using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;
using MuhleOP.Models;

namespace MuhleOP.ViewModels
{
    public class QualityIndexViewModel
    {
        public int TotalInspections { get; set; }
        public int VariationsPending { get; set; }
        public int VariationsApprovedThisMonth { get; set; }
        public int ProjectsWithActiveNonConformances { get; set; }
    }

    public class CreateInspectionViewModel
    {
        [Required(ErrorMessage = "Please select a project")]
        [Display(Name = "Project")]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Inspection type is required")]
        [Display(Name = "Inspection Type")]
        public string InspectionType { get; set; }

        [Display(Name = "Inspection Area")]
        [StringLength(200)]
        public string InspectionArea { get; set; }

        [Required(ErrorMessage = "Findings are required")]
        [Display(Name = "Findings")]
        [DataType(DataType.MultilineText)]
        public string Findings { get; set; }

        [Display(Name = "Recommendations")]
        [DataType(DataType.MultilineText)]
        public string Recommendations { get; set; }

        [Display(Name = "Score (0-100)")]
        [Range(0, 100)]
        public int? Score { get; set; }

        [Display(Name = "Action Required")]
        public bool ActionRequired { get; set; }

        [Display(Name = "Action Items")]
        [DataType(DataType.MultilineText)]
        public string ActionItems { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Inspection Date")]
        [DataType(DataType.Date)]
        public DateTime? InspectionDate { get; set; }

        [Display(Name = "Photos (optional)")]
        public HttpPostedFileBase Photos { get; set; }

        [Display(Name = "Report File (optional)")]
        public HttpPostedFileBase Report { get; set; }

        public List<SelectListItem> Projects { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> InspectionTypes { get; set; } = new List<SelectListItem>();
    }

    public class InspectionDetailsViewModel
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public string PerformedByName { get; set; }
        public DateTime InspectionDate { get; set; }
        public string InspectionType { get; set; }
        public string InspectionArea { get; set; }
        public string Findings { get; set; }
        public string Recommendations { get; set; }
        public int? Score { get; set; }
        public string Status { get; set; }
        public string StatusBadgeColor { get; set; }
        public bool IsPassed { get; set; }
        public bool ActionRequired { get; set; }
        public string ActionItems { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string ReviewedBy { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string PhotosPath { get; set; }
        public string ReportPath { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<VariationListItemViewModel> RelatedVariations { get; set; } = new List<VariationListItemViewModel>();
        public bool CanReview { get; set; }
        public bool CanRequestVariation { get; set; }
    }

    public class InspectionListItemViewModel
    {
        public int Id { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public string InspectionType { get; set; }
        public DateTime InspectionDate { get; set; }
        public string PerformedByName { get; set; }
        public string Status { get; set; }
        public string StatusBadgeColor { get; set; }
        public int? Score { get; set; }
        public bool ActionRequired { get; set; }
        public bool IsPassed { get; set; }
    }

    public class InspectionQueueViewModel
    {
        public List<InspectionListItemViewModel> Items { get; set; } = new List<InspectionListItemViewModel>();
        public string StatusFilter { get; set; }
        public int? ProjectIdFilter { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<SelectListItem> Projects { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Statuses { get; set; } = new List<SelectListItem>();
    }

    public class RequestVariationViewModel
    {
        [Required(ErrorMessage = "Please select a project")]
        [Display(Name = "Project")]
        public int ProjectId { get; set; }

        [Display(Name = "Related Inspection")]
        public int? SiteInspectionId { get; set; }

        public string RelatedInspectionSummary { get; set; }

        [Required(ErrorMessage = "Variation type is required")]
        [Display(Name = "Variation Type")]
        public string VariationType { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [Display(Name = "Reason")]
        [DataType(DataType.MultilineText)]
        public string Reason { get; set; }

        [Display(Name = "Impact on Scope")]
        [DataType(DataType.MultilineText)]
        public string ImpactOnScope { get; set; }

        [Display(Name = "Impact on Cost (R)")]
        public decimal? ImpactOnCost { get; set; }

        [Display(Name = "Impact on Timeline (Days)")]
        public int? ImpactOnTimelineDays { get; set; }

        [Display(Name = "Supporting Documents (optional)")]
        public HttpPostedFileBase SupportingDocuments { get; set; }

        public List<SelectListItem> Projects { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> VariationTypes { get; set; } = new List<SelectListItem>();
    }

    public class VariationListItemViewModel
    {
        public int Id { get; set; }
        public string VariationNumber { get; set; }
        public string ProjectName { get; set; }
        public string VariationType { get; set; }
        public string Status { get; set; }
        public string StatusBadgeColor { get; set; }
        public DateTime RequestDate { get; set; }
        public string RequestedBy { get; set; }
        public decimal? ImpactOnCost { get; set; }
        public int? ImpactOnTimelineDays { get; set; }
    }

    public class VariationQueueViewModel
    {
        public List<VariationListItemViewModel> Items { get; set; } = new List<VariationListItemViewModel>();
        public string StatusFilter { get; set; }
        public int? ProjectIdFilter { get; set; }
        public string TypeFilter { get; set; }
        public List<SelectListItem> Projects { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Statuses { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> VariationTypes { get; set; } = new List<SelectListItem>();
    }

    public class VariationDetailsViewModel
    {
        public int Id { get; set; }
        public string VariationNumber { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public string VariationType { get; set; }
        public string Description { get; set; }
        public string Reason { get; set; }
        public string ImpactOnScope { get; set; }
        public decimal? ImpactOnCost { get; set; }
        public int? ImpactOnTimelineDays { get; set; }
        public string RequestedBy { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
        public string StatusBadgeColor { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string RejectionReason { get; set; }
        public DateTime? ImplementationDate { get; set; }
        public string ImplementationNotes { get; set; }
        public string SupportingDocumentsPath { get; set; }
        public int? LinkedInspectionId { get; set; }
        public string LinkedInspectionSummary { get; set; }
        public List<VariationApprovalItemViewModel> Approvals { get; set; } = new List<VariationApprovalItemViewModel>();
        public bool CanApprove { get; set; }
    }

    public class VariationApprovalItemViewModel
    {
        public string ApproverName { get; set; }
        public string ApprovalStatus { get; set; }
        public string Comments { get; set; }
        public DateTime ApprovalDate { get; set; }
        public int ApprovalLevel { get; set; }
    }

    public class ApproveVariationViewModel
    {
        public int VariationId { get; set; }
        public string VariationNumber { get; set; }
        public string ProjectName { get; set; }
        public string VariationType { get; set; }
        public string Description { get; set; }
        public string Reason { get; set; }
        public decimal? ImpactOnCost { get; set; }
        public int? ImpactOnTimelineDays { get; set; }

        [Required(ErrorMessage = "Please choose Approve or Reject")]
        [Display(Name = "Decision")]
        public string Decision { get; set; }

        [Display(Name = "Implementation Date")]
        [DataType(DataType.Date)]
        public DateTime? ImplementationDate { get; set; }

        [Display(Name = "Comments / Notes")]
        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }
    }
}