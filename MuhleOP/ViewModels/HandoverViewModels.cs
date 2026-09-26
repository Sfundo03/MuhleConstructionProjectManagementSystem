using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;
using MuhleOP.Models;

namespace MuhleOP.ViewModels
{
    public class HandoverIndexViewModel
    {
        public List<HandoverListItemViewModel> Items { get; set; } = new List<HandoverListItemViewModel>();
        public int PendingCount { get; set; }
        public int AwaitingClientSignOffCount { get; set; }
        public int CompletedThisMonthCount { get; set; }
        public int TotalDocumentsCount { get; set; }
    }

    public class HandoverListItemViewModel
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public string ClientName { get; set; }
        public string HandoverType { get; set; }
        public string Status { get; set; }
        public string StatusBadgeColor { get; set; }
        public string ClientSignOffStatus { get; set; }
        public string ClientSignOffBadgeColor { get; set; }
        public DateTime HandoverDate { get; set; }
        public int DocumentCount { get; set; }
        public string CreatedBy { get; set; }
    }

    public class InitiateHandoverViewModel
    {
        [Required(ErrorMessage = "Please select a project")]
        [Display(Name = "Project")]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Handover type is required")]
        [Display(Name = "Handover Type")]
        public string HandoverType { get; set; }

        [Display(Name = "Final Inspection Date")]
        [DataType(DataType.Date)]
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

        [Display(Name = "Certificate (optional)")]
        public HttpPostedFileBase CertificateFile { get; set; }

        public List<SelectListItem> Projects { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> HandoverTypes { get; set; } = new List<SelectListItem>();
    }

    public class HandoverDetailsViewModel
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public string ProjectStatus { get; set; }
        public string ClientName { get; set; }
        public string ProjectManagerName { get; set; }
        public DateTime HandoverDate { get; set; }
        public string HandoverType { get; set; }
        public DateTime? FinalInspectionDate { get; set; }
        public string FinalInspectionFindings { get; set; }
        public string ClientSignOffStatus { get; set; }
        public DateTime? ClientSignOffDate { get; set; }
        public string ClientSignOffNotes { get; set; }
        public bool ClientInvitedToSignOff { get; set; }
        public DateTime? ClientInvitedDate { get; set; }
        public bool ProjectManagerSignOff { get; set; }
        public DateTime? ProjectManagerSignOffDate { get; set; }
        public string Status { get; set; }
        public string StatusBadgeColor { get; set; }
        public string ClientSignOffBadgeColor { get; set; }
        public string CompletionNotes { get; set; }
        public string LessonsLearned { get; set; }
        public string Recommendations { get; set; }
        public string ClientFeedback { get; set; }
        public string CertificatePath { get; set; }
        public string SignedHandoverPdfPath { get; set; }
        public int? WarrantyPeriodDays { get; set; }
        public DateTime? WarrantyStartDate { get; set; }
        public DateTime? WarrantyEndDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }

        // UC23 Additions
        public string ClientSignatureIpAddress { get; set; }
        public string ClientSignatureUserAgent { get; set; }
        public string ClientAcceptanceLetterPath { get; set; }
        public string ClosureReportPath { get; set; }
        public bool IsPracticalCompletionPending { get; set; }

        public List<SnagItemViewModel> Snags { get; set; } = new List<SnagItemViewModel>();
        public int OpenCriticalSnagCount { get; set; }
        public int OpenMajorSnagCount { get; set; }
        public int OpenMinorSnagCount { get; set; }

        public List<string> MissingRequiredDocumentTypes { get; set; } = new List<string>();
        public List<ProjectVariation> OpenVariations { get; set; } = new List<ProjectVariation>();

        public List<HandoverDocumentListItemViewModel> Documents { get; set; } = new List<HandoverDocumentListItemViewModel>();
    }

    public class SnagItemViewModel
    {
        public int Id { get; set; }
        public int ProjectHandoverId { get; set; }
        public string ReferenceNumber { get; set; }
        [Required, StringLength(300)] public string Description { get; set; }
        [StringLength(200)] public string Location { get; set; }
        [Required] public string Severity { get; set; }
        [Required, StringLength(150)] public string ResponsibleParty { get; set; }
        [DataType(DataType.Date)] public DateTime? DueDate { get; set; }
        public string Status { get; set; }
        public string ReportedBy { get; set; }
        public DateTime ReportedDate { get; set; }
        public string ResolutionNotes { get; set; }
    }

    public class RefuseSignOffViewModel
    {
        public int HandoverId { get; set; }
        [Required(ErrorMessage = "Refusal reason is required")]
        [StringLength(1000)]
        [Display(Name = "Refusal Reason")]
        [DataType(DataType.MultilineText)]
        public string RefusalReason { get; set; }
    }

    public class HandoverDocumentListItemViewModel
    {
        public int Id { get; set; }
        public string DocumentName { get; set; }
        public string DocumentType { get; set; }
        public string Description { get; set; }
        public string FilePath { get; set; }
        public long? FileSizeBytes { get; set; }
        public DateTime UploadDate { get; set; }
        public string UploadedBy { get; set; }
        public bool IsApproved { get; set; }
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

        [Required(ErrorMessage = "Please select a file")]
        [Display(Name = "File")]
        public HttpPostedFileBase File { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        public List<SelectListItem> DocumentTypes { get; set; } = new List<SelectListItem>();
    }

    public class ClientSignOffViewModel
    {
        public int HandoverId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public string HandoverType { get; set; }
        public string CompletionNotes { get; set; }
        public string FinalInspectionFindings { get; set; }
        public string LessonsLearned { get; set; }
        public string Recommendations { get; set; }
        public int? WarrantyPeriodDays { get; set; }
        public DateTime? WarrantyStartDate { get; set; }
        public DateTime? WarrantyEndDate { get; set; }
        public DateTime? ClientInvitedDate { get; set; }

        public List<HandoverDocumentListItemViewModel> Documents { get; set; } = new List<HandoverDocumentListItemViewModel>();
        public List<SnagItemViewModel> Snags { get; set; } = new List<SnagItemViewModel>();
        public SnagItemViewModel NewSnag { get; set; } = new SnagItemViewModel();

        public bool HasOpenCriticalSnags { get; set; }
        public bool CanSignOff { get; set; }

        [Required]
        [Display(Name = "I accept the project as completed")]
        public bool AcceptTerms { get; set; }

        [Display(Name = "Client Feedback")]
        [DataType(DataType.MultilineText)]
        public string ClientFeedback { get; set; }

        [Display(Name = "Sign-off Notes")]
        [DataType(DataType.MultilineText)]
        public string ClientSignOffNotes { get; set; }
    }
}