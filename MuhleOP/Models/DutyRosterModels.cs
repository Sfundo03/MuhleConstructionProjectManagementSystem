// ============ Models/DutyRosterModels.cs ============
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuhleOP.Models
{
    /// <summary>
    /// Header for a duty roster covering a date range on a single project.
    /// Versions are linked via ParentRosterId; only one version is IsActive at a time.
    /// </summary>
    public class DutyRoster
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [Required]
        [Display(Name = "Period Start")]
        [DataType(DataType.Date)]
        public DateTime PeriodStart { get; set; }

        [Required]
        [Display(Name = "Period End")]
        [DataType(DataType.Date)]
        public DateTime PeriodEnd { get; set; }

        /// <summary>Draft | Published | Superseded</summary>
        [Required]
        [StringLength(30)]
        public string Status { get; set; }

        [Required]
        public int Version { get; set; }

        /// <summary>Self-reference to the previous version when this roster is a revision.</summary>
        public int? ParentRosterId { get; set; }

        [ForeignKey("ParentRosterId")]
        public virtual DutyRoster ParentRoster { get; set; }

        public virtual ICollection<DutyRoster> ChildVersions { get; set; }

        /// <summary>True for the currently active version of a roster lineage.</summary>
        public bool IsActive { get; set; }

        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        [Required]
        [StringLength(128)]
        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        [StringLength(128)]
        public string PublishedBy { get; set; }

        public DateTime? PublishedDate { get; set; }

        [StringLength(128)]
        public string DeletedBy { get; set; }

        public DateTime? DeletedDate { get; set; }

        public bool IsDeleted { get; set; }

        public virtual ICollection<Shift> Shifts { get; set; }

        public DutyRoster()
        {
            Status = "Draft";
            Version = 1;
            IsActive = true;
            IsDeleted = false;
            CreatedDate = DateTime.Now;
            Shifts = new List<Shift>();
            ChildVersions = new List<DutyRoster>();
        }
    }

    /// <summary>
    /// A single work shift within a duty roster.
    /// </summary>
    public class Shift
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DutyRosterId { get; set; }

        [ForeignKey("DutyRosterId")]
        public virtual DutyRoster DutyRoster { get; set; }

        /// <summary>Denormalised for fast lookup / validation queries.</summary>
        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [Required]
        [Display(Name = "Shift Date")]
        [DataType(DataType.Date)]
        public DateTime ShiftDate { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Shift Role")]
        public string ShiftRole { get; set; }

        [StringLength(150)]
        [Display(Name = "Site Location")]
        public string SiteLocation { get; set; }

        [Display(Name = "Required Staff Count")]
        [Range(1, 50)]
        public int RequiredStaffCount { get; set; }

        [StringLength(500)]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        public bool IsOvertime { get; set; }

        public bool IsSundayShift { get; set; }

        public bool IsHolidayShift { get; set; }

        [StringLength(128)]
        public string OvertimeApprovedBy { get; set; }

        public DateTime? OvertimeApprovedDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public virtual ICollection<ShiftAssignment> Assignments { get; set; }

        public Shift()
        {
            RequiredStaffCount = 1;
            IsOvertime = false;
            IsSundayShift = false;
            IsHolidayShift = false;
            CreatedDate = DateTime.Now;
            Assignments = new List<ShiftAssignment>();
        }
    }

    /// <summary>
    /// Assignment of a Crew Member to a specific shift.
    /// </summary>
    public class ShiftAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ShiftId { get; set; }

        [ForeignKey("ShiftId")]
        public virtual Shift Shift { get; set; }

        [Required]
        public int StaffId { get; set; }

        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }

        public DateTime AssignedDate { get; set; }

        [StringLength(128)]
        public string AssignedBy { get; set; }

        public bool IsActive { get; set; }

        /// <summary>Populated when the PM overrode a certification or overtime warning.</summary>
        [StringLength(500)]
        public string OverrideReason { get; set; }

        [StringLength(128)]
        public string OverrideApprovedBy { get; set; }

        public ShiftAssignment()
        {
            AssignedDate = DateTime.Now;
            IsActive = true;
        }
    }

    /// <summary>
    /// Approved (or pending) leave for a staff member. Used by the roster validation engine.
    /// </summary>
    public class LeaveRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StaffId { get; set; }

        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        /// <summary>Annual | Sick | Family Responsibility | Maternity | Unpaid | Other</summary>
        [Required]
        [StringLength(50)]
        public string LeaveType { get; set; }

        /// <summary>Pending | Approved | Rejected | Cancelled</summary>
        [Required]
        [StringLength(30)]
        public string Status { get; set; }

        [StringLength(500)]
        [DataType(DataType.MultilineText)]
        public string Reason { get; set; }

        [StringLength(128)]
        public string ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public DateTime CreatedDate { get; set; }

        [StringLength(128)]
        public string CreatedBy { get; set; }

        public LeaveRecord()
        {
            Status = "Pending";
            CreatedDate = DateTime.Now;
        }
    }
}