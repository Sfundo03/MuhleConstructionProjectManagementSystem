using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace MuhleOP.ViewModels
{
    public class PayrollIndexViewModel
    {
        public List<PayrollListItemViewModel> Items { get; set; } = new List<PayrollListItemViewModel>();
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int CalculatedCount { get; set; }
        public int ApprovedCount { get; set; }
        public int ProcessedCount { get; set; }
        public int PaidCount { get; set; }
        public string ActiveFilter { get; set; }
        public int? FilterYear { get; set; }
        public int? FilterMonth { get; set; }
        public List<SelectListItem> Years { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Months { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Statuses { get; set; } = new List<SelectListItem>();
        public decimal TotalGross { get; set; }
        public decimal TotalNet { get; set; }
    }

    public class PayrollListItemViewModel
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; }
        public string EmployeeId { get; set; }
        public string Role { get; set; }
        public DateTime PayPeriodStart { get; set; }
        public DateTime PayPeriodEnd { get; set; }
        public string PeriodLabel { get; set; }
        public decimal RegularHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal GrossPay { get; set; }
        public decimal NetPay { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string StatusBadgeColor { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
    }

    public class GeneratePayrollViewModel
    {
        [Required(ErrorMessage = "Year is required")]
        [Display(Name = "Year")]
        public int Year { get; set; }

        [Required(ErrorMessage = "Month is required")]
        [Range(1, 12, ErrorMessage = "Month must be between 1 and 12")]
        [Display(Name = "Month")]
        public int Month { get; set; }

        public List<SelectListItem> Years { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Months { get; set; } = new List<SelectListItem>();

        public int GeneratedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> Messages { get; set; } = new List<string>();
    }

    public class PayrollDetailsViewModel
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; }
        public string EmployeeId { get; set; }
        public string StaffRole { get; set; }
        public string StaffEmail { get; set; }
        public string BankAccount { get; set; }

        public DateTime PayPeriodStart { get; set; }
        public DateTime PayPeriodEnd { get; set; }
        public string PeriodLabel { get; set; }

        public decimal RegularHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal OvertimeRate { get; set; }

        public decimal RegularPay { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal Bonus { get; set; }
        public decimal GrossPay { get; set; }
        public decimal Deductions { get; set; }
        public decimal Tax { get; set; }
        public decimal NetPay { get; set; }
        public decimal TotalAmount { get; set; }

        public string Status { get; set; }
        public string StatusBadgeColor { get; set; }

        public string CalculatedBy { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string ProcessedBy { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public string Notes { get; set; }

        public bool CanCalculate { get; set; }
        public bool CanApprove { get; set; }
        public bool CanProcess { get; set; }
        public bool CanMarkPaid { get; set; }
    }

    public class ProcessPayrollViewModel
    {
        public int Id { get; set; }
        public string StaffName { get; set; }
        public string EmployeeId { get; set; }
        public string PeriodLabel { get; set; }
        public decimal NetPay { get; set; }
        public string CurrentBankAccount { get; set; }

        [Required(ErrorMessage = "Payment method is required")]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; }

        [Required(ErrorMessage = "Payment date is required")]
        [Display(Name = "Payment Date")]
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; }

        [Display(Name = "Bank Account")]
        [StringLength(100)]
        public string BankAccount { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        public List<SelectListItem> PaymentMethods { get; set; } = new List<SelectListItem>();
    }
}