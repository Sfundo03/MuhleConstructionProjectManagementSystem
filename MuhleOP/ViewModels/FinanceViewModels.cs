using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace MuhleOP.ViewModels
{
    public class PayrollViewModels
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; }
        public DateTime PayPeriodStart { get; set; }
        public DateTime PayPeriodEnd { get; set; }
        public decimal RegularHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal OvertimeRate { get; set; }
        public decimal RegularPay { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal GrossPay { get; set; }
        public decimal Deductions { get; set; }
        public decimal Tax { get; set; }
        public decimal NetPay { get; set; }
        public decimal Bonus { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public string BankAccount { get; set; }
        public string Notes { get; set; }
        public string CalculatedBy { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string ProcessedBy { get; set; }
        public DateTime? ProcessedDate { get; set; }

        public string StatusBadgeColor => GetStatusColor();

        private string GetStatusColor()
        {
            switch (Status)
            {
                case "Draft": return "secondary";
                case "Calculated": return "info";
                case "Approved": return "success";
                case "Processed": return "primary";
                case "Paid": return "success";
                default: return "secondary";
            }
        }
    }

   

    public class ApprovePayrollViewModel
    {
        public int PayrollId { get; set; }
        public string StaffName { get; set; }
        public decimal NetPay { get; set; }
        public DateTime PayPeriodStart { get; set; }
        public DateTime PayPeriodEnd { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }
    }

    public class ClientInvoiceViewModel
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Milestone { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentReference { get; set; }
        public string Notes { get; set; }
        public string Terms { get; set; }
        public string InvoiceFilePath { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }

        public string StatusBadgeColor => GetStatusColor();
        public bool IsOverdue => Status != "Paid" && Status != "Cancelled" && DueDate < DateTime.Now;

        private string GetStatusColor()
        {
            switch (Status)
            {
                case "Draft": return "secondary";
                case "Sent": return "info";
                case "Paid": return "success";
                case "Overdue": return "danger";
                case "Cancelled": return "secondary";
                default: return "secondary";
            }
        }
    }

    public class GenerateInvoiceViewModel
    {
        [Required(ErrorMessage = "Client is required")]
        [Display(Name = "Client")]
        public int ClientId { get; set; }

        [Display(Name = "Project")]
        public int? ProjectId { get; set; }

        [Display(Name = "Milestone")]
        [StringLength(200)]
        public string Milestone { get; set; }

        [Required(ErrorMessage = "Invoice date is required")]
        [Display(Name = "Invoice Date")]
        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; }

        [Required(ErrorMessage = "Due date is required")]
        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Display(Name = "Tax Rate (%)")]
        public decimal TaxRate { get; set; } = 15;

        [Display(Name = "Terms")]
        [DataType(DataType.MultilineText)]
        public string Terms { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        public List<SelectListItem> Clients { get; set; }
        public List<SelectListItem> Projects { get; set; }

        public GenerateInvoiceViewModel()
        {
            Clients = new List<SelectListItem>();
            Projects = new List<SelectListItem>();
        }
    }

    public class SupplierViewModel
    {
        public int Id { get; set; }
        public string SupplierName { get; set; }
        public string RegistrationNumber { get; set; }
        public string TaxIdNumber { get; set; }
        public string ContactPerson { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string AlternativePhone { get; set; }
        public string PhysicalAddress { get; set; }
        public string PostalAddress { get; set; }
        public string Website { get; set; }
        public string Category { get; set; }
        public string BankDetails { get; set; }
        public string PaymentTerms { get; set; }
        public decimal? CreditLimit { get; set; }
        public int? Rating { get; set; }
        public bool IsActive { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string RegisteredBy { get; set; }
        public string Notes { get; set; }
        public int ContractCount { get; set; }

        public string CategoryBadgeColor => GetCategoryColor();

        private string GetCategoryColor()
        {
            switch (Category)
            {
                case "Materials": return "primary";
                case "Equipment": return "warning";
                case "Services": return "info";
                case "Subcontractors": return "success";
                default: return "secondary";
            }
        }
    }

    public class CreateSupplierViewModel
    {
        [Required(ErrorMessage = "Supplier name is required")]
        [Display(Name = "Supplier Name")]
        [StringLength(200)]
        public string SupplierName { get; set; }

        [Required(ErrorMessage = "Registration number is required")]
        [Display(Name = "Registration Number")]
        [StringLength(50)]
        public string RegistrationNumber { get; set; }

        [Display(Name = "Tax Identification Number")]
        [StringLength(50)]
        public string TaxIdNumber { get; set; }

        [Required(ErrorMessage = "Contact person is required")]
        [Display(Name = "Contact Person")]
        [StringLength(100)]
        public string ContactPerson { get; set; }

        [Required(ErrorMessage = "Contact email is required")]
        [EmailAddress]
        [Display(Name = "Contact Email")]
        public string ContactEmail { get; set; }

        [Required(ErrorMessage = "Contact phone is required")]
        [Phone]
        [Display(Name = "Contact Phone")]
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

        [Required(ErrorMessage = "Category is required")]
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
        [Range(1, 5)]
        public int? Rating { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        public List<SelectListItem> Categories { get; set; }

        public CreateSupplierViewModel()
        {
            Categories = new List<SelectListItem>();
        }
    }

    public class SupplierContractViewModel
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string ContractNumber { get; set; }
        public string ContractType { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal? ContractValue { get; set; }
        public string Status { get; set; }
        public string PricingAgreement { get; set; }
        public string DeliveryTerms { get; set; }
        public string QualityRequirements { get; set; }
        public string ContractFilePath { get; set; }
        public bool IsSigned { get; set; }
        public DateTime? SignatureDate { get; set; }
        public string SignedBy { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string Notes { get; set; }

        public string StatusBadgeColor => GetStatusColor();

        private string GetStatusColor()
        {
            switch (Status)
            {
                case "Draft": return "secondary";
                case "Active": return "success";
                case "Expired": return "warning";
                case "Terminated": return "danger";
                default: return "secondary";
            }
        }
    }

    public class CreateSupplierContractViewModel
    {
        [Required(ErrorMessage = "Supplier is required")]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Contract type is required")]
        [Display(Name = "Contract Type")]
        public string ContractType { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Contract Value")]
        public decimal? ContractValue { get; set; }

        [Display(Name = "Pricing Agreement")]
        [DataType(DataType.MultilineText)]
        public string PricingAgreement { get; set; }

        [Display(Name = "Delivery Terms")]
        [DataType(DataType.MultilineText)]
        public string DeliveryTerms { get; set; }

        [Display(Name = "Quality Requirements")]
        [DataType(DataType.MultilineText)]
        public string QualityRequirements { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        [Display(Name = "Contract File")]
        public HttpPostedFileBase ContractFile { get; set; }

        public List<SelectListItem> Suppliers { get; set; }
        public List<SelectListItem> ContractTypes { get; set; }

        public CreateSupplierContractViewModel()
        {
            Suppliers = new List<SelectListItem>();
            ContractTypes = new List<SelectListItem>();
        }
    }
}