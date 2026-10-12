using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuhleOP.Models
{
    /// <summary>
    /// One row = one work session for a crew member on a project.
    /// CheckOutTime is null while the session is open.
    /// </summary>
    public class AttendanceRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StaffId { get; set; }

        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        /// <summary>Optional link to the published shift the crew member was expected to work.</summary>
        public int? ShiftId { get; set; }

        [ForeignKey("ShiftId")]
        public virtual Shift Shift { get; set; }

        [Required]
        [Display(Name = "Check-In Time")]
        public DateTime CheckInTime { get; set; }

        [Display(Name = "Check-Out Time")]
        public DateTime? CheckOutTime { get; set; }

        /// <summary>Computed hours (decimal). Null while still checked in.</summary>
        [Display(Name = "Total Hours")]
        public decimal? TotalHours { get; set; }

        [StringLength(128)]
        public string CheckInByUserId { get; set; }

        [StringLength(128)]
        public string CheckOutByUserId { get; set; }

        [StringLength(50)]
        public string CheckInMethod { get; set; } // "QR", "Manual"

        [StringLength(50)]
        public string CheckOutMethod { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        public bool IsManualOverride { get; set; }

        public DateTime CreatedDate { get; set; }

        // ---- Roster reconciliation flags (populated at scan time) ----
        public bool IsLate { get; set; }
        public int LateMinutes { get; set; }
        public bool IsSignificantlyLate { get; set; }
        public bool IsEarlyDeparture { get; set; }
        public int EarlyMinutes { get; set; }
        public bool IsSundayShift { get; set; }
        public bool IsHolidayShift { get; set; }
        public bool IsOvertime { get; set; }
        public decimal OvertimeHours { get; set; }

        public AttendanceRecord()
        {
            CheckInTime = DateTime.Now;
            CreatedDate = DateTime.Now;
            CheckInMethod = "QR";
            IsManualOverride = false;
            IsLate = false;
            LateMinutes = 0;
            IsSignificantlyLate = false;
            IsEarlyDeparture = false;
            EarlyMinutes = 0;
            IsSundayShift = false;
            IsHolidayShift = false;
            IsOvertime = false;
            OvertimeHours = 0m;
        }
    }
}