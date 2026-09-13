// ViewModels/CrewViewModels.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace MuhleOP.ViewModels
{
    public class ClockInOutViewModel
    {
        public int StaffId { get; set; }

        [Display(Name = "Staff Name")]
        public string StaffName { get; set; }

        [Display(Name = "Action")]
        public string Action { get; set; } // ClockIn | ClockOut

        public string Token { get; set; }

        public int? TimesheetEntryId { get; set; }

        [Display(Name = "Clock In Time")]
        public TimeSpan? ClockInTime { get; set; }

        [Display(Name = "Project")]
        public string ProjectName { get; set; }

        [Display(Name = "Weekly Hours")]
        public string WeeklyHoursSummary { get; set; }
    }

    public class ScanQrRequestViewModel
    {
        [Required]
        public string Payload { get; set; }
    }

    public class QrPayloadViewModel
    {
        public string Action { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double Accuracy { get; set; }
        public string Timestamp { get; set; }
        public string Token { get; set; }
        public int? TimesheetEntryId { get; set; }
    }

    public class PendingScanToken
    {
        public int StaffId { get; set; }
        public string Action { get; set; }
        public DateTime ExpiresUtc { get; set; }
        public bool Used { get; set; }
        public DateTime? ScannedAtUtc { get; set; }
        public string ScannedByName { get; set; }
        public string ResultAction { get; set; }
        public int? ResultTimesheetEntryId { get; set; }
    }
}