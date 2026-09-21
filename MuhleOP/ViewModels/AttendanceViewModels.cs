using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace MuhleOP.ViewModels
{
    public class AttendanceScanResultViewModel
    {
        public bool Success { get; set; }
        public string Action { get; set; }          // "CheckIn" | "CheckOut"
        public string StaffName { get; set; }
        public string StaffPhotoUrl { get; set; }
        public string ProjectName { get; set; }
        public string Timestamp { get; set; }
        public decimal? HoursWorked { get; set; }
        public string Message { get; set; }
    }

    public class AttendanceTodayItemViewModel
    {
        public int Id { get; set; }
        public string StaffName { get; set; }
        public string StaffPhotoUrl { get; set; }
        public string ProjectName { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public decimal? TotalHours { get; set; }
        public string Status { get; set; }           // "IN" | "OUT"
        public string StatusBadgeColor { get; set; }
        public string CheckInBy { get; set; }
    }

    public class AttendanceTodayViewModel
    {
        public List<AttendanceTodayItemViewModel> Records { get; set; } = new List<AttendanceTodayItemViewModel>();
        public int CurrentlyInCount { get; set; }
        public int CheckedOutCount { get; set; }
        public int TotalSessions { get; set; }
        public int? FilterProjectId { get; set; }
        public string FilterStatus { get; set; }
        public List<SelectListItem> Projects { get; set; } = new List<SelectListItem>();
    }

    public class AttendanceHistoryItemViewModel
    {
        public int Id { get; set; }
        public string StaffName { get; set; }
        public string ProjectName { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public decimal? TotalHours { get; set; }
        public string Status { get; set; }
        public string StatusBadgeColor { get; set; }
    }

    public class AttendanceHistoryViewModel
    {
        public List<AttendanceHistoryItemViewModel> Records { get; set; } = new List<AttendanceHistoryItemViewModel>();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? ProjectId { get; set; }
        public List<SelectListItem> Projects { get; set; } = new List<SelectListItem>();
        public decimal TotalHoursSum { get; set; }
        public int SessionCount { get; set; }
    }

    public class MyQrViewModel
    {
        public string StaffName { get; set; }
        public string EmployeeId { get; set; }
        public string QrToken { get; set; }
        public string QrImageBase64 { get; set; }   // data:image/png;base64,...
        public DateTime? TokenCreatedDate { get; set; }
        public bool HasToken { get; set; }
        /// <summary>Seconds left in the current 5-minute rolling window (server-side initial value).</summary>
        public int SecondsRemaining { get; set; }
        public int WindowSeconds { get; set; }
    }

    public class MyHoursViewModel
    {
        public string StaffName { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public List<AttendanceHistoryItemViewModel> Records { get; set; } = new List<AttendanceHistoryItemViewModel>();
        public decimal MonthHoursTotal { get; set; }
        public int SessionCount { get; set; }
        public int OpenSessionCount { get; set; }
        public List<SelectListItem> Years { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Months { get; set; } = new List<SelectListItem>();
    }
}