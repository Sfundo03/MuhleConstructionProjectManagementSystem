using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuhleOP.Models
{
    public class DailyTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Task Title")]
        [StringLength(200)]
        public string TaskTitle { get; set; }

        [Display(Name = "Task Description")]
        [DataType(DataType.MultilineText)]
        public string TaskDescription { get; set; }

        [Required]
        public int AssignedToStaffId { get; set; }

        [ForeignKey("AssignedToStaffId")]
        public virtual Staff AssignedTo { get; set; }

        [Required]
        public int AssignedByStaffId { get; set; }

        [ForeignKey("AssignedByStaffId")]
        public virtual Staff AssignedBy { get; set; }

        public int? ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [Required]
        [Display(Name = "Task Date")]
        [DataType(DataType.Date)]
        public DateTime TaskDate { get; set; }

        [Display(Name = "Start Time")]
        [DataType(DataType.Time)]
        public TimeSpan? StartTime { get; set; }

        [Display(Name = "End Time")]
        [DataType(DataType.Time)]
        public TimeSpan? EndTime { get; set; }

        [Display(Name = "Priority")]
        public string Priority { get; set; } // Low, Medium, High, Urgent

        [Display(Name = "Status")]
        public string Status { get; set; } // Pending, In Progress, Completed, Cancelled

        [Display(Name = "Completion Notes")]
        [DataType(DataType.MultilineText)]
        public string CompletionNotes { get; set; }

        [Display(Name = "Is Completed")]
        public bool IsCompleted { get; set; }

        [Display(Name = "Completion Date")]
        public DateTime? CompletionDate { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Is Recurring")]
        public bool IsRecurring { get; set; }

        [Display(Name = "Recurrence Pattern")]
        public string RecurrencePattern { get; set; } // Daily, Weekly, Monthly

        public DailyTask()
        {
            Status = "Pending";
            IsCompleted = false;
            CreatedDate = DateTime.Now;
            TaskDate = DateTime.Now.Date;
            Priority = "Medium";
        }
    }

    public class Timesheet
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StaffId { get; set; }

        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }

        [Required]
        [Display(Name = "Week Starting")]
        [DataType(DataType.Date)]
        public DateTime WeekStarting { get; set; }

        [Display(Name = "Total Hours")]
        public decimal TotalHours { get; set; }

        [Display(Name = "Overtime Hours")]
        public decimal OvertimeHours { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } // Draft, Submitted, Approved, Rejected

        [Display(Name = "Submitted Date")]
        public DateTime? SubmittedDate { get; set; }

        [Display(Name = "Submitted By")]
        public string SubmittedBy { get; set; }

        [Display(Name = "Approved By")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Approval Date")]
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Rejection Reason")]
        [DataType(DataType.MultilineText)]
        public string RejectionReason { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        // Navigation Properties
        public virtual ICollection<TimesheetEntry> Entries { get; set; }

        public Timesheet()
        {
            Status = "Draft";
            WeekStarting = GetWeekStartDate(DateTime.Now);
            Entries = new List<TimesheetEntry>();
        }

        private DateTime GetWeekStartDate(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }
    }

    public class TimesheetEntry
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TimesheetId { get; set; }

        [ForeignKey("TimesheetId")]
        public virtual Timesheet Timesheet { get; set; }

        [Required]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Display(Name = "Start Time")]
        [DataType(DataType.Time)]
        public TimeSpan? StartTime { get; set; }

        [Display(Name = "End Time")]
        [DataType(DataType.Time)]
        public TimeSpan? EndTime { get; set; }

        [Display(Name = "Break Duration")]
        public int? BreakDurationMinutes { get; set; }

        [Display(Name = "Hours Worked")]
        public decimal HoursWorked { get; set; }

        [Display(Name = "Overtime Hours")]
        public decimal OvertimeHours { get; set; }

        [Display(Name = "Task Description")]
        [StringLength(500)]
        public string TaskDescription { get; set; }

        [Display(Name = "Project ID")]
        public int? ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [Display(Name = "Is Overtime")]
        public bool IsOvertime { get; set; }

        [Display(Name = "Is Holiday")]
        public bool IsHoliday { get; set; }

        [Display(Name = "Is Sick Leave")]
        public bool IsSickLeave { get; set; }

        [Display(Name = "Is Vacation")]
        public bool IsVacation { get; set; }

        public TimesheetEntry()
        {
            Date = DateTime.Now.Date;
        }
    }
}