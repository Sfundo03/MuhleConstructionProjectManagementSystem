using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuhleOP.Models
{
    public class Client
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Company Name")]
        [StringLength(200)]
        public string CompanyName { get; set; }

        [Required]
        [Display(Name = "Registration Number")]
        [StringLength(50)]
        public string RegistrationNumber { get; set; }

        [Display(Name = "Tax Identification Number")]
        [StringLength(50)]
        public string TaxIdNumber { get; set; }

        [Required]
        [Display(Name = "Primary Contact Person")]
        [StringLength(100)]
        public string PrimaryContactName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Contact Email")]
        public string ContactEmail { get; set; }

        [Required]
        [Display(Name = "Contact Phone")]
        [Phone]
        public string ContactPhone { get; set; }

        [Display(Name = "Alternative Phone")]
        [Phone]
        public string AlternativePhone { get; set; }

        [Required]
        [Display(Name = "Physical Address")]
        [DataType(DataType.MultilineText)]
        public string PhysicalAddress { get; set; }

        [Display(Name = "Postal Address")]
        [DataType(DataType.MultilineText)]
        public string PostalAddress { get; set; }

        [Display(Name = "Website")]
        [Url]
        public string Website { get; set; }

        [Display(Name = "Industry")]
        [StringLength(100)]
        public string Industry { get; set; }

        [Display(Name = "Company Size")]
        public string CompanySize { get; set; } // Small, Medium, Large, Enterprise

        [Display(Name = "Billing Information")]
        [DataType(DataType.MultilineText)]
        public string BillingInformation { get; set; }

        [Display(Name = "Payment Terms")]
        public string PaymentTerms { get; set; } // Net 30, Net 60, etc.

        [Display(Name = "Credit Limit")]
        public decimal? CreditLimit { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Registration Date")]
        public DateTime RegistrationDate { get; set; }

        [Display(Name = "Registered By")]
        public string RegisteredBy { get; set; }

        [Display(Name = "User ID")]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // Navigation Properties
        public virtual ICollection<Contract> Contracts { get; set; }
        public virtual ICollection<Project> Projects { get; set; }
        public virtual ICollection<ClientInvoice> Invoices { get; set; }

        public Client()
        {
            IsActive = true;
            RegistrationDate = DateTime.Now;
            Contracts = new List<Contract>();
            Projects = new List<Project>();
            Invoices = new List<ClientInvoice>();
        }

        public string DisplayName => $"{CompanyName} ({PrimaryContactName})";
    }

    // Centralised status string constants so the controller and views never
    // rely on hand-typed "magic strings" for the contract workflow.
    public static class ContractStatus
    {
        public const string Draft = "Draft";
        public const string PendingReview = "PendingReview";
        public const string Sent = "Sent";
        public const string Signed = "Signed";
        public const string Active = "Active";
        public const string Completed = "Completed";
        public const string Terminated = "Terminated";
    }

    public class Contract
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Contract Number")]
        [StringLength(50)]
        public string ContractNumber { get; set; }

        [Required]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        public virtual Client Client { get; set; }

        [Required]
        [Display(Name = "Project Name")]
        [StringLength(200)]
        public string ProjectName { get; set; }

        [Display(Name = "Contract Type")]
        public string ContractType { get; set; } // Fixed Price, Time and Materials, Cost Plus

        [Required]
        [Display(Name = "Contract Value")]
        public decimal ContractValue { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Project Scope")]
        [DataType(DataType.MultilineText)]
        public string ProjectScope { get; set; }

        [Display(Name = "Deliverables")]
        [DataType(DataType.MultilineText)]
        public string Deliverables { get; set; }

        [Display(Name = "Payment Terms")]
        [DataType(DataType.MultilineText)]
        public string PaymentTerms { get; set; }

        [Display(Name = "Payment Schedule")]
        [DataType(DataType.MultilineText)]
        public string PaymentSchedule { get; set; }

        [Display(Name = "Milestones")]
        [DataType(DataType.MultilineText)]
        public string Milestones { get; set; }

        [Display(Name = "Timeline")]
        [DataType(DataType.MultilineText)]
        public string Timeline { get; set; }

        [Display(Name = "Penalty Clauses")]
        [DataType(DataType.MultilineText)]
        public string PenaltyClauses { get; set; }

        [Display(Name = "Termination Conditions")]
        [DataType(DataType.MultilineText)]
        public string TerminationConditions { get; set; }

        [Display(Name = "Special Conditions")]
        [DataType(DataType.MultilineText)]
        public string SpecialConditions { get; set; }

        [Display(Name = "Contract Status")]
        // Draft -> PendingReview -> Sent -> Signed -> Active -> Completed
        // (or Terminated at any point before Signed)
        public string Status { get; set; }

        [Display(Name = "Is Signed")]
        public bool IsSigned { get; set; }

        [Display(Name = "Signature Date")]
        public DateTime? SignatureDate { get; set; }

        [Display(Name = "Signed Document Path")]
        public string SignedDocumentPath { get; set; } // path to the raw signature image

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [Display(Name = "Contract File Path")]
        public string ContractFilePath { get; set; } // optional manually-uploaded contract file

        // ===== Project Manager review (added for admin -> PM -> client workflow) =====

        [Display(Name = "Reviewed By")]
        public string ReviewedBy { get; set; }

        [Display(Name = "Review Date")]
        public DateTime? ReviewedDate { get; set; }

        [Display(Name = "Review Notes")]
        [DataType(DataType.MultilineText)]
        public string ReviewNotes { get; set; }

        // ===== Generated PDF tracking (iTextSharp) =====

        [Display(Name = "Generated Contract PDF Path")]
        public string GeneratedPdfPath { get; set; } // unsigned draft/preview PDF, regenerated whenever contract text changes

        [Display(Name = "Signed Contract PDF Path")]
        public string SignedPdfPath { get; set; } // final PDF with the signature stamped in, produced after signing

        [Display(Name = "Sent For Review Date")]
        public DateTime? SentForReviewDate { get; set; }

        [Display(Name = "Sent To Client Date")]
        public DateTime? SentToClientDate { get; set; }

        // Navigation Properties
        public virtual ICollection<ContractSignature> Signatures { get; set; }
        public virtual ICollection<Project> Projects { get; set; }

        public Contract()
        {
            Status = ContractStatus.Draft;
            IsSigned = false;
            CreatedDate = DateTime.Now;
            Signatures = new List<ContractSignature>();
            Projects = new List<Project>();
            ContractNumber = GenerateContractNumber();
        }

        private string GenerateContractNumber()
        {
            return "CTR" + DateTime.Now.ToString("yyyyMMdd") + new Random().Next(1000, 9999).ToString();
        }
    }

    public class ContractSignature
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ContractId { get; set; }

        [ForeignKey("ContractId")]
        public virtual Contract Contract { get; set; }

        [Required]
        [Display(Name = "Signer Name")]
        [StringLength(100)]
        public string SignerName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Signer Email")]
        public string SignerEmail { get; set; }

        [Display(Name = "IP Address")]
        public string IpAddress { get; set; }

        [Display(Name = "User Agent")]
        public string UserAgent { get; set; }

        [Display(Name = "Signature Data")]
        public string SignatureData { get; set; } // Base64 encoded signature image

        [Display(Name = "Signature Type")]
        public string SignatureType { get; set; } // Electronic, Digital, Wet

        [Display(Name = "Signed Date")]
        public DateTime SignedDate { get; set; }

        [Display(Name = "Is Valid")]
        public bool IsValid { get; set; }

        [Display(Name = "Verification Token")]
        public string VerificationToken { get; set; }

        public ContractSignature()
        {
            SignedDate = DateTime.Now;
            IsValid = true;
            VerificationToken = GenerateVerificationToken();
        }

        private string GenerateVerificationToken()
        {
            return Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16).ToUpper();
        }
    }
}