using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;

namespace MuhleOP.ViewModels
{
    public class PayrollAnomaly
    {
        public string Code { get; set; }
        public string Severity { get; set; }    // Info, Warning, Blocker
        public string Message { get; set; }
        public string RelatedRecord { get; set; }
    }

    public class PayrollValidationResult
    {
        public List<PayrollAnomaly> Anomalies { get; set; } = new List<PayrollAnomaly>();
        public bool HasBlocker => Anomalies.Any(a => a.Severity == "Blocker");
        public bool HasWarning => Anomalies.Any(a => a.Severity == "Warning");
    }

    public class PayrollAnomalyViewModel
    {
        public string Code { get; set; }
        public string Severity { get; set; }
        public string Message { get; set; }
        public string RelatedRecord { get; set; }
    }

    public class PayrollPeriodLockViewModel
    {
        public int Id { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public bool IsLocked { get; set; }
        public string LockedBy { get; set; }
        public DateTime? LockedDate { get; set; }
        public string LockReason { get; set; }
    }

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
        public bool IsPeriodLocked { get; set; }
        public string PeriodLockReason { get; set; }
        public string PeriodLockedBy { get; set; }
        public DateTime? PeriodLockedDate { get; set; }
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
        public string Status { get; set; }
        public string StatusBadgeColor { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public bool IsPeriodLocked { get; set; }
        public bool HasPayslip { get; set; }
        public bool HasWarning { get; set; }
        public string PayslipPdfPath { get; set; }
        public string ValidationWarningsJson { get; set; }
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

        public int PreviewActiveStaffCount { get; set; }
        public int PreviewZeroRateStaffCount { get; set; }
        public int PreviewMissingClockOutCount { get; set; }
        public bool IsPeriodLocked { get; set; }
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
        public decimal RosteredHours { get; set; }
        public decimal UnworkedRosteredHours { get; set; }
        public decimal SundayHours { get; set; }
        public decimal HolidayHours { get; set; }

        public decimal HourlyRate { get; set; }
        public decimal OvertimeRate { get; set; }

        public decimal RegularPay { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal SundayPay { get; set; }
        public decimal HolidayPay { get; set; }
        public decimal Bonus { get; set; }
        public decimal GrossPay { get; set; }
        public decimal Deductions { get; set; }
        public decimal Tax { get; set; }
        public decimal NetPay { get; set; }

        public decimal UifEmployee { get; set; }
        public decimal UifEmployer { get; set; }
        public decimal Sdl { get; set; }
        public decimal MedicalTaxCredit { get; set; }
        public decimal RetirementDeduction { get; set; }
        public int Dependants { get; set; }
        public string TaxYear { get; set; }

        public decimal YtdGross { get; set; }
        public decimal YtdPaye { get; set; }
        public decimal YtdUif { get; set; }
        public decimal YtdNet { get; set; }

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

        public List<PayrollAnomalyViewModel> Anomalies { get; set; } = new List<PayrollAnomalyViewModel>();
        public bool HasBlocker { get; set; }
        public bool HasWarning { get; set; }

        public decimal MedicalAidContribution { get; set; }
        public decimal PensionContribution { get; set; }
        public decimal OtherDeductions { get; set; }
        public decimal OtherAdditions { get; set; }
        public decimal AdvanceRecovery { get; set; }

        public string DeductionNotes { get; set; }
        public string AdditionNotes { get; set; }
        public string PayslipPdfPath { get; set; }
        public bool IsPeriodLocked { get; set; }
    }

    public class StaffRateRowViewModel
    {
        public int StaffId { get; set; }
        public string FullName { get; set; }
        public string EmployeeId { get; set; }
        public string Role { get; set; }
        public decimal PayRatePerHour { get; set; }
        public string BankAccountMasked { get; set; }
        public DateTime? LastModified { get; set; }
        public bool IsCurrentUser { get; set; }
    }

    public class PayRateAuditItemViewModel
    {
        public int Id { get; set; }
        public string StaffName { get; set; }
        public decimal OldRate { get; set; }
        public decimal NewRate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    public class ManageRatesViewModel
    {
        public List<StaffRateRowViewModel> StaffRows { get; set; } = new List<StaffRateRowViewModel>();
        public List<PayRateAuditItemViewModel> RecentAudits { get; set; } = new List<PayRateAuditItemViewModel>();
        public string Search { get; set; }
        public string RoleFilter { get; set; }
        public string SortBy { get; set; } = "name";
        public string SortDir { get; set; } = "asc";
        public List<SelectListItem> Roles { get; set; } = new List<SelectListItem>();
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