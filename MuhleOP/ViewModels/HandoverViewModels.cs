using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace MuhleOP.ViewModels
{
    public class ProjectHandoverViewModel
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public DateTime HandoverDate { get; set; }
        public string HandoverType { get; set; }
        public DateTime? FinalInspectionDate { get; set; }
        public string FinalInspectionFindings { get; set; }
        public string ClientSignOffStatus { get; set; }
        public DateTime? ClientSignOffDate { get; set; }
        public string ClientSignOffNotes { get; set; }
        public bool ProjectManagerSignOff { get; set; }
        public DateTime? ProjectManagerSignOffDate { get; set; }
        public string Status { get; set; }
        public string CompletionNotes { get; set; }
        public string LessonsLearned { get; set; }
        public string Recommendations { get; set; }
        public string ClientFeedback { get; set; }
        public string CertificatePath { get; set; }
        public int? WarrantyPeriodDays { get; set; }
        public DateTime? WarrantyStartDate { get; set; }
        public DateTime? WarrantyEndDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public List<HandoverDocumentViewModel> Documents { get; set; }

        public string StatusBadgeColor => GetStatusColor();
        public string ClientSignOffBadgeColor => GetClientSignOffColor();

        public ProjectHandoverViewModel()
        {
            Documents = new List<HandoverDocumentViewModel>();
        }

        private string GetStatusColor()
        {
            switch (Status)
            {
                case "Initiated": return "info";
                case "In Progress": return "primary";
                case "Completed": return "success";
                case "Closed": return "secondary";
                default: return "secondary";
            }
        }

        private string GetClientSignOffColor()
        {
            switch (ClientSignOffStatus)
            {
                case "Pending": return "warning";
                case "Signed": return "success";
                case "Rejected": return "danger";
                default: return "secondary";
            }
        }
    }

    public class InitiateHandoverViewModel
    {
        [Required(ErrorMessage = "Project is required")]
        [Display(Name = "Project")]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Handover date is required")]
        [Display(Name = "Handover Date")]
        [DataType(DataType.DateTime)]
        public DateTime HandoverDate { get; set; }

        [Required(ErrorMessage = "Handover type is required")]
        [Display(Name = "Handover Type")]
        public string HandoverType { get; set; }

        [Display(Name = "Final Inspection Date")]
        [DataType(DataType.DateTime)]
        public DateTime? FinalInspectionDate { get; set; }

        [Display(Name = "Final Inspection Findings")]
        [DataType(DataType.MultilineText)]
        public string FinalInspectionFindings { get; set; }

        [Display(Name = "Completion Notes")]
        [DataType(DataType.MultilineText)]
        public string CompletionNotes { get; set; }

        [Display(Name = "Lessons Learned")]
        [DataType(DataType.MultilineText)]
        public string LessonsLearned { get; set; }

        [Display(Name = "Recommendations")]
        [DataType(DataType.MultilineText)]
        public string Recommendations { get; set; }

        [Display(Name = "Warranty Period (Days)")]
        public int? WarrantyPeriodDays { get; set; }

        [Display(Name = "Warranty Start Date")]
        [DataType(DataType.Date)]
        public DateTime? WarrantyStartDate { get; set; }

        [Display(Name = "Certificate of Completion")]
        public HttpPostedFileBase Certificate { get; set; }

        public List<SelectListItem> Projects { get; set; }
        public List<SelectListItem> HandoverTypes { get; set; }

        public InitiateHandoverViewModel()
        {
            Projects = new List<SelectListItem>();
            HandoverTypes = new List<SelectListItem>();
        }
    }

    public class ClientSignOffViewModel
    {
        public int HandoverId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public DateTime HandoverDate { get; set; }
        public string CompletionNotes { get; set; }

        [Display(Name = "Client Sign-off")]
        public bool IsSignedOff { get; set; }

        [Display(Name = "Client Feedback")]
        [DataType(DataType.MultilineText)]
        public string ClientFeedback { get; set; }

        [Display(Name = "Client Sign-off Notes")]
        [DataType(DataType.MultilineText)]
        public string ClientSignOffNotes { get; set; }

        [Display(Name = "I accept the project as completed")]
        public bool AcceptTerms { get; set; }
    }

    public class HandoverDocumentViewModel
    {
        public int Id { get; set; }
        public int HandoverId { get; set; }
        public string DocumentName { get; set; }
        public string DocumentType { get; set; }
        public string Description { get; set; }
        public string FilePath { get; set; }
        public long? FileSizeBytes { get; set; }
        public DateTime UploadDate { get; set; }
        public string UploadedBy { get; set; }
        public bool IsApproved { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string Notes { get; set; }

        public string FileSizeFormatted => FileSizeBytes.HasValue ? FormatFileSize(FileSizeBytes.Value) : "N/A";
        public string DocumentTypeBadgeColor => GetDocumentTypeColor();

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private string GetDocumentTypeColor()
        {
            switch (DocumentType)
            {
                case "As-Built Drawings": return "primary";
                case "Operation Manuals": return "info";
                case "Maintenance Manuals": return "warning";
                case "Warranty Certificates": return "success";
                case "Compliance Certificates": return "danger";
                default: return "secondary";
            }
        }
    }

    public class UploadHandoverDocumentViewModel
    {
        public int HandoverId { get; set; }
        public string ProjectName { get; set; }

        [Required(ErrorMessage = "Document name is required")]
        [Display(Name = "Document Name")]
        [StringLength(200)]
        public string DocumentName { get; set; }

        [Required(ErrorMessage = "Document type is required")]
        [Display(Name = "Document Type")]
        public string DocumentType { get; set; }

        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Required(ErrorMessage = "File is required")]
        [Display(Name = "File")]
        public HttpPostedFileBase File { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        public List<SelectListItem> DocumentTypes { get; set; }

        public UploadHandoverDocumentViewModel()
        {
            DocumentTypes = new List<SelectListItem>();
        }
    }
}