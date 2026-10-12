using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;

namespace MuhleOP.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Project Name")]
        [StringLength(200)]
        public string ProjectName { get; set; }

        [Required]
        [Display(Name = "Project Code")]
        [StringLength(50)]
        public string ProjectCode { get; set; }

        public int? ClientId { get; set; }

        [ForeignKey("ClientId")]
        public virtual Client Client { get; set; }

        public int? ContractId { get; set; }

        [ForeignKey("ContractId")]
        public virtual Contract Contract { get; set; }

        [Required]
        [Display(Name = "Project Manager ID")]
        public int ProjectManagerId { get; set; }

        [ForeignKey("ProjectManagerId")]
        public virtual Staff ProjectManager { get; set; }

        [Display(Name = "Site Manager ID")]
        public int? SiteManagerId { get; set; }

        [ForeignKey("SiteManagerId")]
        public virtual Staff SiteManager { get; set; }

        [Display(Name = "Project Status")]
        public string Status { get; set; } // Initiated, In Progress, On Hold, Completed, Closed

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "Estimated End Date")]
        [DataType(DataType.Date)]
        public DateTime? EstimatedEndDate { get; set; }

        [Display(Name = "Actual End Date")]
        [DataType(DataType.Date)]
        public DateTime? ActualEndDate { get; set; }

        [Display(Name = "Project Location")]
        public string ProjectLocation { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [Display(Name = "Last Modified Date")]
        public DateTime? LastModifiedDate { get; set; }

        [Display(Name = "Last Modified By")]
        public string LastModifiedBy { get; set; }

        // Navigation Properties
        public virtual ProjectScope ProjectScope { get; set; }
        public virtual ProjectBudget Budget { get; set; }
        public virtual ICollection<ProjectDeliverable> Deliverables { get; set; }
        public virtual ICollection<ProjectResourceAllocation> ResourceAllocations { get; set; }
        public virtual ICollection<ProjectVariation> Variations { get; set; }
        public virtual ICollection<SiteInspection> Inspections { get; set; }
        public virtual ICollection<ProjectHandover> Handovers { get; set; }

        public virtual ICollection<DutyRoster> DutyRosters { get; set; }

        public Project()
        {
            Status = "Initiated";
            IsActive = true;
            CreatedDate = DateTime.Now;
            Deliverables = new List<ProjectDeliverable>();
            ResourceAllocations = new List<ProjectResourceAllocation>();
            Variations = new List<ProjectVariation>();
            Inspections = new List<SiteInspection>();
            Handovers = new List<ProjectHandover>();
            ProjectCode = GenerateProjectCode();
        }

        private string GenerateProjectCode()
        {
            return "PRJ" + DateTime.Now.ToString("yyyyMMdd") + new Random().Next(1000, 9999).ToString();
        }
    }

    public class ProjectScope
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [Required]
        [Display(Name = "Project Objectives")]
        [DataType(DataType.MultilineText)]
        public string Objectives { get; set; }

        [Required]
        [Display(Name = "Scope Definition")]
        [DataType(DataType.MultilineText)]
        public string ScopeDefinition { get; set; }

        [Display(Name = "Out of Scope")]
        [DataType(DataType.MultilineText)]
        public string OutOfScope { get; set; }

        [Display(Name = "Assumptions")]
        [DataType(DataType.MultilineText)]
        public string Assumptions { get; set; }

        [Display(Name = "Constraints")]
        [DataType(DataType.MultilineText)]
        public string Constraints { get; set; }

        [Display(Name = "Risks")]
        [DataType(DataType.MultilineText)]
        public string Risks { get; set; }

        [Display(Name = "Quality Standards")]
        [DataType(DataType.MultilineText)]
        public string QualityStandards { get; set; }

        [Display(Name = "Is Approved")]
        public bool IsApproved { get; set; }

        [Display(Name = "Approved By")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Approval Date")]
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; }

        public ProjectScope()
        {
            IsApproved = false;
            LastUpdated = DateTime.Now;
        }
    }

    public class ProjectDeliverable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [Required]
        [Display(Name = "Deliverable Name")]
        [StringLength(200)]
        public string DeliverableName { get; set; }

        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "Deliverable Type")]
        public string DeliverableType { get; set; } // Document, Physical, Service, Software

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Display(Name = "Completion Date")]
        [DataType(DataType.Date)]
        public DateTime? CompletionDate { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } // Pending, In Progress, Completed, Approved, Rejected

        [Display(Name = "Priority")]
        public string Priority { get; set; } // Low, Medium, High, Critical

        [Display(Name = "Assigned To")]
        public int? AssignedToStaffId { get; set; }

        [ForeignKey("AssignedToStaffId")]
        public virtual Staff AssignedTo { get; set; }

        [Display(Name = "Deliverable File Path")]
        public string DeliverableFilePath { get; set; }

        [Display(Name = "Comments")]
        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }

        [Display(Name = "Is Approved")]
        public bool IsApproved { get; set; }

        [Display(Name = "Approved By")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Approval Date")]
        public DateTime? ApprovalDate { get; set; }

        public ProjectDeliverable()
        {
            Status = "Pending";
            Priority = "Medium";
            IsApproved = false;
        }
    }

    public class ProjectBudget
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [Required]
        [Display(Name = "Estimated Cost")]
        public decimal EstimatedCost { get; set; }

        [Display(Name = "Actual Cost")]
        public decimal ActualCost { get; set; }

        [Display(Name = "Budget Category")]
        public string BudgetCategory { get; set; } // Labour, Materials, Equipment, Subcontractors, Overhead

        [Display(Name = "Allocated Amount")]
        public decimal AllocatedAmount { get; set; }

        [Display(Name = "Spent Amount")]
        public decimal SpentAmount { get; set; }

        [Display(Name = "Remaining Amount")]
        public decimal RemainingAmount { get; set; }

        [Display(Name = "Variance")]
        public decimal Variance => AllocatedAmount - SpentAmount;

        [Display(Name = "Currency")]
        public string Currency { get; set; } // ZAR, USD, etc.

        [Display(Name = "Fiscal Year")]
        public int FiscalYear { get; set; }

        [Display(Name = "Is Approved")]
        public bool IsApproved { get; set; }

        [Display(Name = "Approved By")]
        public string ApprovedBy { get; set; }

        [Display(Name = "Approval Date")]
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; }

        public ProjectBudget()
        {
            IsApproved = false;
            LastUpdated = DateTime.Now;
            Currency = "ZAR";
            FiscalYear = DateTime.Now.Year;
        }
    }

    public class ProjectResourceAllocation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [Required]
        public int StaffId { get; set; }

        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; }

        [Display(Name = "Allocation Percentage")]
        [Range(0, 100)]
        public int AllocationPercentage { get; set; } = 100;

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        [Display(Name = "Assigned By")]
        public string AssignedBy { get; set; }

        [Display(Name = "Assigned Date")]
        public DateTime AssignedDate { get; set; }

        public ProjectResourceAllocation()
        {
            IsActive = true;
            AssignedDate = DateTime.Now;
            StartDate = DateTime.Now;
        }
    }

    public class EquipmentAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        [Required]
        public int EquipmentId { get; set; }

        [ForeignKey("EquipmentId")]
        public virtual Equipment Equipment { get; set; }

        [Display(Name = "Assigned Date")]
        public DateTime AssignedDate { get; set; }

        [Display(Name = "Expected Return Date")]
        public DateTime? ExpectedReturnDate { get; set; }

        [Display(Name = "Actual Return Date")]
        public DateTime? ActualReturnDate { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Condition on Assignment")]
        public string ConditionOnAssignment { get; set; }

        [Display(Name = "Condition on Return")]
        public string ConditionOnReturn { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        [Display(Name = "Assigned By")]
        public string AssignedBy { get; set; }

        public EquipmentAssignment()
        {
            AssignedDate = DateTime.Now;
            IsActive = true;
        }
    }
}