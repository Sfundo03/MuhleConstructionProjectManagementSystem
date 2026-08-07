using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace MuhleOP.ViewModels
{
    public class SiteInspectionViewModel
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int PerformedByStaffId { get; set; }
        public string PerformedByName { get; set; }
        public DateTime InspectionDate { get; set; }
        public string InspectionType { get; set; }
        public string InspectionArea { get; set; }
        public string Findings { get; set; }
        public string Recommendations { get; set; }
        public int? Score { get; set; }
        public string Status { get; set; }
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

        public string StatusBadgeColor => GetStatusColor();

        private string GetStatusColor()
        {
            switch (Status)
            {
                case "Draft": return "secondary";
                case "Completed": return "info";
                case "Reviewed": return "primary";
                case "Closed": return "success";
                default: return "secondary";
            }
        }
    }

    public class CreateInspectionViewModel
    {
        [Required(ErrorMessage = "Project is required")]
        [Display(Name = "Project")]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Inspection date is required")]
        [Display(Name = "Inspection Date")]
        [DataType(DataType.DateTime)]
        public DateTime InspectionDate { get; set; }

        [Required(ErrorMessage = "Inspection type is required")]
        [Display(Name = "Inspection Type")]
        public string InspectionType { get; set; }

        [Display(Name = "Inspection Area")]
        public string InspectionArea { get; set; }

        [Required(ErrorMessage = "Findings are required")]
        [Display(Name = "Findings")]
        [DataType(DataType.MultilineText)]
        public string Findings { get; set; }

        [Display(Name = "Recommendations")]
        [DataType(DataType.MultilineText)]
        public string Recommendations { get; set; }

        [Display(Name = "Score")]
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

        [Display(Name = "Photos")]
        public HttpPostedFileBase Photos { get; set; }

        [Display(Name = "Inspection Report")]
        public HttpPostedFileBase Report { get; set; }

        public List<SelectListItem> Projects { get; set; }
        public List<SelectListItem> InspectionTypes { get; set; }

        public CreateInspectionViewModel()
        {
            Projects = new List<SelectListItem>();
            InspectionTypes = new List<SelectListItem>();
        }
    }

    public class ProjectVariationViewModel
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string VariationNumber { get; set; }
        public string VariationType { get; set; }
        public string Description { get; set; }
        public string Reason { get; set; }
        public string ImpactOnScope { get; set; }
        public decimal? ImpactOnCost { get; set; }
        public int? ImpactOnTimelineDays { get; set; }
        public string RequestedBy { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string RejectionReason { get; set; }
        public DateTime? ImplementationDate { get; set; }
        public string ImplementationNotes { get; set; }
        public string SupportingDocumentsPath { get; set; }

        public string StatusBadgeColor => GetStatusColor();

        private string GetStatusColor()
        {
            switch (Status)
            {
                case "Requested": return "warning";
                case "Under Review": return "info";
                case "Approved": return "success";
                case "Rejected": return "danger";
                case "Implemented": return "primary";
                default: return "secondary";
            }
        }
    }

    public class RequestVariationViewModel
    {
        [Required(ErrorMessage = "Project is required")]
        [Display(Name = "Project")]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Variation type is required")]
        [Display(Name = "Variation Type")]
        public string VariationType { get; set; }

        [Required(ErrorMessage = "Description is required")]
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

        [Display(Name = "Impact on Timeline (Days)")]
        public int? ImpactOnTimelineDays { get; set; }

        [Display(Name = "Supporting Documents")]
        public HttpPostedFileBase SupportingDocuments { get; set; }

        public List<SelectListItem> Projects { get; set; }
        public List<SelectListItem> VariationTypes { get; set; }

        public RequestVariationViewModel()
        {
            Projects = new List<SelectListItem>();
            VariationTypes = new List<SelectListItem>();
        }
    }

    public class ApproveVariationViewModel
    {
        public int VariationId { get; set; }
        public string VariationNumber { get; set; }
        public string Description { get; set; }
        public string ProjectName { get; set; }
        public string RequestedBy { get; set; }
        public DateTime RequestDate { get; set; }
        public decimal? ImpactOnCost { get; set; }
        public int? ImpactOnTimelineDays { get; set; }

        [Required(ErrorMessage = "Approval decision is required")]
        [Display(Name = "Decision")]
        public string Decision { get; set; }

        [Display(Name = "Comments")]
        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }
    }
}