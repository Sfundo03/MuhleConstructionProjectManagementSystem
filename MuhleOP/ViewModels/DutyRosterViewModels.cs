// ============ ViewModels/DutyRosterViewModels.cs ============
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace MuhleOP.ViewModels
{
    public class DutyRosterIndexViewModel
    {
        public List<DutyRosterListItemViewModel> Rosters { get; set; }
        public string StatusFilter { get; set; }
        public int? ProjectIdFilter { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<SelectListItem> Projects { get; set; }
        public List<SelectListItem> Statuses { get; set; }

        public int DraftCount { get; set; }
        public int PublishedCount { get; set; }
        public int SupersededCount { get; set; }
        public decimal TotalHoursThisWeek { get; set; }

        /// <summary>True only when the current user is a Project Manager who manages at least one active project.</summary>
        public bool CanCreate { get; set; }

        public DutyRosterIndexViewModel()
        {
            Rosters = new List<DutyRosterListItemViewModel>();
            Projects = new List<SelectListItem>();
            Statuses = new List<SelectListItem>();
        }
    }

    public class DutyRosterListItemViewModel
    {
        public int Id { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public string PeriodLabel { get; set; }
        public string Status { get; set; }
        public string StatusBadgeColor { get; set; }
        public int Version { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? PublishedDate { get; set; }
        public int ShiftCount { get; set; }
        public int AssignedStaffCount { get; set; }

        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanRevise { get; set; }
    }

    public class CreateDutyRosterViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Please select a project.")]
        [Display(Name = "Project")]
        public int ProjectId { get; set; }

        [Required]
        [Display(Name = "Period Start")]
        [DataType(DataType.Date)]
        public DateTime PeriodStart { get; set; }

        [Required]
        [Display(Name = "Period End")]
        [DataType(DataType.Date)]
        public DateTime PeriodEnd { get; set; }

        [StringLength(1000)]
        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        /// <summary>"SaveDraft" or "Publish"</summary>
        public string SubmitAction { get; set; }

        public List<SelectListItem> Projects { get; set; }
        public List<ShiftInputViewModel> Shifts { get; set; }

        public CreateDutyRosterViewModel()
        {
            PeriodStart = DateTime.Today;
            PeriodEnd = DateTime.Today.AddDays(6);
            Projects = new List<SelectListItem>();
            Shifts = new List<ShiftInputViewModel>();
            SubmitAction = "SaveDraft";
        }
    }

    public class ShiftInputViewModel
    {
        public int? Id { get; set; }

        [Required]
        [Display(Name = "Shift Date")]
        [DataType(DataType.Date)]
        public DateTime ShiftDate { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        public string StartTime { get; set; } // "HH:mm" for form binding

        [Required]
        [Display(Name = "End Time")]
        public string EndTime { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Shift Role")]
        public string ShiftRole { get; set; }

        [StringLength(150)]
        [Display(Name = "Site Location")]
        public string SiteLocation { get; set; }

        [Range(1, 50)]
        [Display(Name = "Required Staff")]
        public int RequiredStaffCount { get; set; }

        public List<int> StaffIds { get; set; }

        // Client-side validation feedback (populated by AJAX)
        public List<string> Blockers { get; set; }
        public List<string> Warnings { get; set; }

        public ShiftInputViewModel()
        {
            RequiredStaffCount = 1;
            StaffIds = new List<int>();
            Blockers = new List<string>();
            Warnings = new List<string>();
            ShiftDate = DateTime.Today;
            StartTime = "07:00";
            EndTime = "16:00";
        }
    }

    public class DutyRosterDetailsViewModel
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string Status { get; set; }
        public string StatusBadgeColor { get; set; }
        public int Version { get; set; }
        public string Notes { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string PublishedBy { get; set; }
        public DateTime? PublishedDate { get; set; }

        public List<ShiftDayGroupViewModel> ShiftsByDate { get; set; }
        public List<RosterVersionItemViewModel> VersionHistory { get; set; }

        /// <summary>True only when the current user is the Project Manager who owns this project.</summary>
        public bool CanManage { get; set; }
        public bool CanEdit { get; set; }
        public bool CanPublish { get; set; }
        public bool CanRevise { get; set; }
        public bool CanDelete { get; set; }

        public DutyRosterDetailsViewModel()
        {
            ShiftsByDate = new List<ShiftDayGroupViewModel>();
            VersionHistory = new List<RosterVersionItemViewModel>();
        }
    }

    public class ShiftDayGroupViewModel
    {
        public DateTime Date { get; set; }
        public string DateLabel { get; set; }
        public List<ShiftDetailItemViewModel> Shifts { get; set; }

        public ShiftDayGroupViewModel()
        {
            Shifts = new List<ShiftDetailItemViewModel>();
        }
    }

    public class ShiftDetailItemViewModel
    {
        public int Id { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string ShiftRole { get; set; }
        public string SiteLocation { get; set; }
        public int RequiredStaffCount { get; set; }
        public bool IsOvertime { get; set; }
        public bool IsSundayShift { get; set; }
        public bool IsHolidayShift { get; set; }
        public List<string> AssignedStaffNames { get; set; }
        public string Notes { get; set; }

        public ShiftDetailItemViewModel()
        {
            AssignedStaffNames = new List<string>();
        }
    }

    public class RosterVersionItemViewModel
    {
        public int Id { get; set; }
        public int Version { get; set; }
        public string Status { get; set; }
        public string StatusBadgeColor { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? PublishedDate { get; set; }
    }

    public class RosterValidationResultViewModel
    {
        public bool IsValid { get; set; }
        public bool HasBlockers { get; set; }
        public bool HasWarnings { get; set; }
        public List<string> Blockers { get; set; }
        public List<string> Warnings { get; set; }
        public List<ReplacementSuggestionViewModel> SuggestedReplacements { get; set; }

        public RosterValidationResultViewModel()
        {
            Blockers = new List<string>();
            Warnings = new List<string>();
            SuggestedReplacements = new List<ReplacementSuggestionViewModel>();
        }
    }

    public class ReplacementSuggestionViewModel
    {
        public int StaffId { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string Reason { get; set; }
    }

    public class PublishRosterViewModel
    {
        public int DutyRosterId { get; set; }
        public string ProjectName { get; set; }
        public string PeriodLabel { get; set; }
        public int ShiftCount { get; set; }
        public int AssignedStaffCount { get; set; }
        public List<string> Blockers { get; set; }
        public List<string> Warnings { get; set; }

        [Display(Name = "Overtime Override Reason")]
        [StringLength(500)]
        public string OvertimeOverrideReason { get; set; }

        [Display(Name = "Certification Override Reason")]
        [StringLength(500)]
        public string CertificationOverrideReason { get; set; }

        [Display(Name = "I confirm this roster is correct and ready to publish")]
        public bool ConfirmPublish { get; set; }

        public bool HasOvertimeWarnings { get; set; }
        public bool HasCertificationWarnings { get; set; }

        public PublishRosterViewModel()
        {
            Blockers = new List<string>();
            Warnings = new List<string>();
        }
    }

    public class MyShiftsViewModel
    {
        public string StaffName { get; set; }
        public List<MyShiftItemViewModel> UpcomingShifts { get; set; }
        public List<MyShiftItemViewModel> PastShifts { get; set; }

        public MyShiftsViewModel()
        {
            UpcomingShifts = new List<MyShiftItemViewModel>();
            PastShifts = new List<MyShiftItemViewModel>();
        }
    }

    public class MyShiftItemViewModel
    {
        public int ShiftId { get; set; }
        public DateTime ShiftDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string ProjectName { get; set; }
        public string ShiftRole { get; set; }
        public string SiteLocation { get; set; }
        public bool IsToday { get; set; }
        public bool CanClockIn { get; set; }
        public decimal? HoursWorked { get; set; }
        public string StatusLabel { get; set; }
    }

    public class ReviseRosterViewModel : CreateDutyRosterViewModel
    {
        public int ParentRosterId { get; set; }
        public int Version { get; set; }
    }

    public class AvailableStaffItemViewModel
    {
        public int StaffId { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string Certifications { get; set; }
        public bool IsAvailable { get; set; }
        public string UnavailableReason { get; set; }
    }
}