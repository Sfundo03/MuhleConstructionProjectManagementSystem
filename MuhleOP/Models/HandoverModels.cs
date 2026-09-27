using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuhleOP.Models
{
    public class ProjectHandover
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [Required]
        [Display(Name = "Handover Date")]
        [DataType(DataType.DateTime)]
        public DateTime HandoverDate { get; set; }

        [Required]
        [Display(Name = "Handover Type")]
        public string HandoverType { get; set; } // Final, Partial, Provisional

        [Display(Name = "Final Inspection Date")]
        [DataType(DataType.DateTime)]
        public DateTime? FinalInspectionDate { get; set; }

        [Display(Name = "Final Inspection Findings")]
        [DataType(DataType.MultilineText)]
        public string FinalInspectionFindings { get; set; }

        [Display(Name = "Client Sign-off Status")]
        public string ClientSignOffStatus { get; set; } // Pending, Signed, Rejected

        [Display(Name = "Client Sign-off Date")]
        public DateTime? ClientSignOffDate { get; set; }

        [Display(Name = "Client Sign-off Notes")]
        [DataType(DataType.MultilineText)]
        public string ClientSignOffNotes { get; set; }

        [Display(Name = "Project Manager Sign-off")]
        public bool ProjectManagerSignOff { get; set; }

        [Display(Name = "Project Manager Sign-off Date")]
        public DateTime? ProjectManagerSignOffDate { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } // Initiated, In Progress, Practical Completion Pending, Completed, Closed

        [Display(Name = "Completion Notes")]
        [DataType(DataType.MultilineText)]
        public string CompletionNotes { get; set; }

        [Display(Name = "Lessons Learned")]
        [DataType(DataType.MultilineText)]
        public string LessonsLearned { get; set; }

        [Display(Name = "Recommendations")]
        [DataType(DataType.MultilineText)]
        public string Recommendations { get; set; }

        [Display(Name = "Client Feedback")]
        [DataType(DataType.MultilineText)]
        public string ClientFeedback { get; set; }

        [Display(Name = "Certificate of Completion Path")]
        public string CertificatePath { get; set; }

        [Display(Name = "Warranty Period Days")]
        public int? WarrantyPeriodDays { get; set; }

        [Display(Name = "Warranty Start Date")]
        [DataType(DataType.Date)]
        public DateTime? WarrantyStartDate { get; set; }

        [Display(Name = "Warranty End Date")]
        [DataType(DataType.Date)]
        public DateTime? WarrantyEndDate { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Last Modified")]
        public DateTime? LastModifiedDate { get; set; }

        [Display(Name = "Client Invited To Sign Off")]
        public bool ClientInvitedToSignOff { get; set; }

        [Display(Name = "Client Invited Date")]
        public DateTime? ClientInvitedDate { get; set; }

        [Display(Name = "Signed Handover PDF Path")]
        [StringLength(500)]
        public string SignedHandoverPdfPath { get; set; }

        // UC23 Additions
        [StringLength(50)]
        public string ClientSignatureIpAddress { get; set; }

        [StringLength(500)]
        public string ClientSignatureUserAgent { get; set; }

        [StringLength(500)]
        public string ClientAcceptanceLetterPath { get; set; }

        [StringLength(500)]
        public string ClosureReportPath { get; set; }

        // Navigation Properties
        public virtual ICollection<HandoverDocument> Documents { get; set; }
        public virtual ICollection<SnagItem> SnagItems { get; set; }

        public ProjectHandover()
        {
            HandoverDate = DateTime.Now;
            Status = "Initiated";
            ClientSignOffStatus = "Pending";
            ProjectManagerSignOff = false;
            ClientInvitedToSignOff = false;
            CreatedDate = DateTime.Now;
            Documents = new List<HandoverDocument>();
            SnagItems = new List<SnagItem>();
        }
    }

    public class SnagItem
    {
        [Key] public int Id { get; set; }

        [Required] public int ProjectHandoverId { get; set; }
        [ForeignKey("ProjectHandoverId")]
        public virtual ProjectHandover ProjectHandover { get; set; }

        [Required, StringLength(20)]
        public string ReferenceNumber { get; set; } // SNAG-0001

        [Required, StringLength(300)]
        public string Description { get; set; }

        [StringLength(200)]
        public string Location { get; set; } // "Unit 4, Kitchen, North wall"

        [Required, StringLength(20)]
        public string Severity { get; set; } // Minor, Major, Critical

        [Required, StringLength(150)]
        public string ResponsibleParty { get; set; }

        public DateTime? DueDate { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } // Open, In Progress, Fixed, Verified, Rejected

        [StringLength(500)] public string PhotoPath { get; set; }
        [StringLength(500)] public string VerificationPhotoPath { get; set; }

        [StringLength(100)] public string ReportedBy { get; set; }
        public DateTime ReportedDate { get; set; }

        [StringLength(100)] public string VerifiedBy { get; set; }
        public DateTime? VerifiedDate { get; set; }

        [StringLength(2000)] public string ResolutionNotes { get; set; }

        public SnagItem()
        {
            ReportedDate = DateTime.Now;
            Status = "Open";
            Severity = "Minor";
        }
    }

    public class HandoverDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int HandoverId { get; set; }

        [ForeignKey("HandoverId")]
        public virtual ProjectHandover Handover { get; set; }

        [Required]
        [Display(Name = "Document Name")]
        [StringLength(200)]
        public string DocumentName { get; set; }

        [Display(Name = "Document Type")]
        public string DocumentType { get; set; } // As-Built Drawings, Operation Manuals, Maintenance Manuals, Warranty Certificates, Compliance Certificates

        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Required]
        [Display(Name = "File Path")]
        public string FilePath { get; set; }

        [Display(Name = "File Size")]
        public long? FileSizeBytes { get; set; }

        [Display(Name = "Upload Date")]
        public DateTime UploadDate { get; set; }

        [Display(Name = "Uploaded By")]
        public string UploadedBy { get; set; }

        [Display(Name = "Is Approved")]
        public bool IsApproved { get; set; }

        [Display(Name = "Approved By")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Approval Date")]
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        public HandoverDocument()
        {
            UploadDate = DateTime.Now;
            IsApproved = false;
        }
    }
}