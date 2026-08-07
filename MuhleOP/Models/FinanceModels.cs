using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuhleOP.Models
{
    public class Payroll
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StaffId { get; set; }

        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }

        [Required]
        [Display(Name = "Pay Period Start")]
        [DataType(DataType.Date)]
        public DateTime PayPeriodStart { get; set; }

        [Required]
        [Display(Name = "Pay Period End")]
        [DataType(DataType.Date)]
        public DateTime PayPeriodEnd { get; set; }

        [Display(Name = "Regular Hours")]
        public decimal RegularHours { get; set; }

        [Display(Name = "Overtime Hours")]
        public decimal OvertimeHours { get; set; }

        [Display(Name = "Hourly Rate")]
        public decimal HourlyRate { get; set; }

        [Display(Name = "Overtime Rate")]
        public decimal OvertimeRate { get; set; }

        [Display(Name = "Regular Pay")]
        public decimal RegularPay { get; set; }

        [Display(Name = "Overtime Pay")]
        public decimal OvertimePay { get; set; }

        [Display(Name = "Gross Pay")]
        public decimal GrossPay { get; set; }

        [Display(Name = "Deductions")]
        public decimal Deductions { get; set; }

        [Display(Name = "Tax")]
        public decimal Tax { get; set; }

        [Display(Name = "Net Pay")]
        public decimal NetPay { get; set; }

        [Display(Name = "Bonus")]
        public decimal Bonus { get; set; }

        // ADDED: TotalAmount property for consistency
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } // Draft, Calculated, Approved, Processed, Paid

        [Display(Name = "Payment Date")]
        [DataType(DataType.Date)]
        public DateTime? PaymentDate { get; set; }

        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } // Bank Transfer, Cash, Cheque

        [Display(Name = "Bank Account")]
        public string BankAccount { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        [Display(Name = "Calculated By")]
        public string CalculatedBy { get; set; }

        [Display(Name = "Approved By")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Approval Date")]
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Processed By")]
        public string ProcessedBy { get; set; }

        [Display(Name = "Processed Date")]
        public DateTime? ProcessedDate { get; set; }

        public Payroll()
        {
            Status = "Draft";
            TotalAmount = 0;
        }

        // Helper method to calculate totals
        public void CalculateTotals()
        {
            RegularPay = RegularHours * HourlyRate;
            OvertimePay = OvertimeHours * OvertimeRate;
            GrossPay = RegularPay + OvertimePay + Bonus;
            NetPay = GrossPay - Deductions - Tax;
            TotalAmount = NetPay;
        }
    }

    // Rest of the finance models remain the same...
    public class ClientInvoice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Invoice Number")]
        [StringLength(50)]
        public string InvoiceNumber { get; set; }

        [Required]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        public virtual Client Client { get; set; }

        public int? ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [Required]
        [Display(Name = "Invoice Date")]
        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; }

        [Required]
        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Display(Name = "Milestone")]
        [StringLength(200)]
        public string Milestone { get; set; }

        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Display(Name = "Tax Rate")]
        public decimal TaxRate { get; set; } = 15;

        [Display(Name = "Tax Amount")]
        public decimal TaxAmount { get; set; }

        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Currency")]
        public string Currency { get; set; } = "ZAR";

        [Display(Name = "Status")]
        public string Status { get; set; } // Draft, Sent, Paid, Overdue, Cancelled

        [Display(Name = "Payment Date")]
        [DataType(DataType.Date)]
        public DateTime? PaymentDate { get; set; }

        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; }

        [Display(Name = "Payment Reference")]
        [StringLength(50)]
        public string PaymentReference { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        [Display(Name = "Terms")]
        [DataType(DataType.MultilineText)]
        public string Terms { get; set; }

        [Display(Name = "Invoice File Path")]
        public string InvoiceFilePath { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Last Modified")]
        public DateTime? LastModifiedDate { get; set; }

        public ClientInvoice()
        {
            InvoiceDate = DateTime.Now;
            DueDate = DateTime.Now.AddDays(30);
            Status = "Draft";
            CreatedDate = DateTime.Now;
            InvoiceNumber = GenerateInvoiceNumber();
            CalculateTotals();
        }

        private string GenerateInvoiceNumber()
        {
            return "INV" + DateTime.Now.ToString("yyyyMMdd") + new Random().Next(1000, 9999).ToString();
        }

        public void CalculateTotals()
        {
            TaxAmount = Amount * (TaxRate / 100);
            TotalAmount = Amount + TaxAmount;
        }
    }

    public class Supplier
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Supplier Name")]
        [StringLength(200)]
        public string SupplierName { get; set; }

        [Required]
        [Display(Name = "Registration Number")]
        [StringLength(50)]
        public string RegistrationNumber { get; set; }

        [Display(Name = "Tax Identification Number")]
        [StringLength(50)]
        public string TaxIdNumber { get; set; }

        [Required]
        [Display(Name = "Contact Person")]
        [StringLength(100)]
        public string ContactPerson { get; set; }

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

        [Display(Name = "Physical Address")]
        [DataType(DataType.MultilineText)]
        public string PhysicalAddress { get; set; }

        [Display(Name = "Postal Address")]
        [DataType(DataType.MultilineText)]
        public string PostalAddress { get; set; }

        [Display(Name = "Website")]
        [Url]
        public string Website { get; set; }

        [Display(Name = "Category")]
        public string Category { get; set; }

        [Display(Name = "Bank Details")]
        [DataType(DataType.MultilineText)]
        public string BankDetails { get; set; }

        [Display(Name = "Payment Terms")]
        public string PaymentTerms { get; set; }

        [Display(Name = "Credit Limit")]
        public decimal? CreditLimit { get; set; }

        [Display(Name = "Rating")]
        public int? Rating { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Registration Date")]
        public DateTime RegistrationDate { get; set; }

        [Display(Name = "Registered By")]
        public string RegisteredBy { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        public virtual ICollection<SupplierContract> Contracts { get; set; }

        public Supplier()
        {
            IsActive = true;
            RegistrationDate = DateTime.Now;
            Contracts = new List<SupplierContract>();
        }
    }

    public class SupplierContract
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public virtual Supplier Supplier { get; set; }

        [Required]
        [Display(Name = "Contract Number")]
        [StringLength(50)]
        public string ContractNumber { get; set; }

        [Required]
        [Display(Name = "Contract Type")]
        public string ContractType { get; set; }

        [Required]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Contract Value")]
        public decimal? ContractValue { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }

        [Display(Name = "Pricing Agreement")]
        [DataType(DataType.MultilineText)]
        public string PricingAgreement { get; set; }

        [Display(Name = "Delivery Terms")]
        [DataType(DataType.MultilineText)]
        public string DeliveryTerms { get; set; }

        [Display(Name = "Quality Requirements")]
        [DataType(DataType.MultilineText)]
        public string QualityRequirements { get; set; }

        [Display(Name = "Contract File Path")]
        public string ContractFilePath { get; set; }

        [Display(Name = "Is Signed")]
        public bool IsSigned { get; set; }

        [Display(Name = "Signature Date")]
        public DateTime? SignatureDate { get; set; }

        [Display(Name = "Signed By")]
        public string SignedBy { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Last Modified")]
        public DateTime? LastModifiedDate { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        public SupplierContract()
        {
            Status = "Draft";
            IsSigned = false;
            CreatedDate = DateTime.Now;
            ContractNumber = GenerateContractNumber();
        }

        private string GenerateContractNumber()
        {
            return "SPC" + DateTime.Now.ToString("yyyyMMdd") + new Random().Next(1000, 9999).ToString();
        }
    }
}