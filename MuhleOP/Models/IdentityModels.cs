using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MuhleOP.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Additional properties
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public DateTime? CreatedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string ProfilePicturePath { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        // Core Tables
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<JobPost> JobPosts { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<ApplicationStatusHistory> ApplicationStatusHistories { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // UC11.5 Duty Roster
        public DbSet<DutyRoster> DutyRosters { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<ShiftAssignment> ShiftAssignments { get; set; }
        public DbSet<LeaveRecord> LeaveRecords { get; set; }
        // Attendance
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }

        // Client & Contract Tables
        public DbSet<Client> Clients { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ContractSignature> ContractSignatures { get; set; }

        // Project Tables
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectScope> ProjectScopes { get; set; }
        public DbSet<ProjectDeliverable> ProjectDeliverables { get; set; }
        public DbSet<ProjectBudget> ProjectBudgets { get; set; }
        public DbSet<ProjectResourceAllocation> ProjectResourceAllocations { get; set; }
        public DbSet<EquipmentAssignment> EquipmentAssignments { get; set; }

        // Daily Operations
        public DbSet<DailyTask> DailyTasks { get; set; }
        public DbSet<Timesheet> Timesheets { get; set; }
        public DbSet<TimesheetEntry> TimesheetEntries { get; set; }

        // Equipment Maintenance
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<EquipmentMaintenance> EquipmentMaintenances { get; set; }
        public DbSet<EquipmentRepair> EquipmentRepairs { get; set; }
        public DbSet<EquipmentCertificationRequirement> EquipmentCertificationRequirements { get; set; }

        // UC17 Inventory & Audit DbSets
        public DbSet<RepairProgressLog> RepairProgressLogs { get; set; }
        public DbSet<EquipmentPart> EquipmentParts { get; set; }
        public DbSet<RepairPartUsage> RepairPartUsages { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }

        // Enhanced Lifecycle DbSets
        public DbSet<EquipmentTechnician> EquipmentTechnicians { get; set; }
        public DbSet<EquipmentCheckoutChecklist> EquipmentCheckoutChecklists { get; set; }

        // Quality & Progress
        public DbSet<SiteInspection> SiteInspections { get; set; }
        public DbSet<InspectionTemplateItem> InspectionTemplateItems { get; set; }
        public DbSet<InspectionItemResult> InspectionItemResults { get; set; }
        public DbSet<NonConformance> NonConformances { get; set; }
        public DbSet<ProjectVariation> ProjectVariations { get; set; }
        public DbSet<ProjectBaselineSnapshot> ProjectBaselineSnapshots { get; set; }
        public DbSet<VariationApproval> VariationApprovals { get; set; }

        // Finance
        public DbSet<Payroll> Payrolls { get; set; }
        public DbSet<PayrollPeriodLock> PayrollPeriodLocks { get; set; }
        public DbSet<ClientInvoice> ClientInvoices { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<SupplierContract> SupplierContracts { get; set; }
        public DbSet<PayRateAudit> PayRateAudits { get; set; }

        // Project Handover & Snag Items
        public DbSet<ProjectHandover> ProjectHandovers { get; set; }
        public DbSet<HandoverDocument> HandoverDocuments { get; set; }
        public DbSet<SnagItem> SnagItems { get; set; }

        // Quiz Tables
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal precision for financial fields
            modelBuilder.Entity<ProjectBudget>()
                .Property(p => p.EstimatedCost)
                .HasPrecision(18, 2);
            modelBuilder.Entity<ProjectBudget>()
                .Property(p => p.ActualCost)
                .HasPrecision(18, 2);
            modelBuilder.Entity<ProjectBudget>()
                .Property(p => p.AllocatedAmount)
                .HasPrecision(18, 2);
            modelBuilder.Entity<ProjectBudget>()
                .Property(p => p.SpentAmount)
                .HasPrecision(18, 2);
            modelBuilder.Entity<ProjectBudget>()
                .Property(p => p.RemainingAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ClientInvoice>()
                .Property(i => i.Amount)
                .HasPrecision(18, 2);
            modelBuilder.Entity<ClientInvoice>()
                .Property(i => i.TaxAmount)
                .HasPrecision(18, 2);
            modelBuilder.Entity<ClientInvoice>()
                .Property(i => i.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payroll>()
                .Property(p => p.TotalAmount)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Payroll>()
                .Property(p => p.RegularPay)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Payroll>()
                .Property(p => p.OvertimePay)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Payroll>()
                .Property(p => p.GrossPay)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Payroll>()
                .Property(p => p.NetPay)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Payroll>()
                .Property(p => p.RegularHours)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Payroll>()
                .Property(p => p.OvertimeHours)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Payroll>()
                .Property(p => p.HourlyRate)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Payroll>()
                .Property(p => p.OvertimeRate)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Payroll>()
                .Property(p => p.Deductions)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Payroll>()
                .Property(p => p.Tax)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Payroll>()
                .Property(p => p.Bonus)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Payroll>()
                .Property(p => p.MedicalAidContribution)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Payroll>()
                .Property(p => p.PensionContribution)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Payroll>()
                .Property(p => p.OtherDeductions)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Payroll>()
                .Property(p => p.OtherAdditions)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Payroll>()
                .Property(p => p.AdvanceRecovery)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SupplierContract>()
                .Property(s => s.ContractValue)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payroll>()
                .HasRequired(p => p.Staff)
                .WithMany()
                .HasForeignKey(p => p.StaffId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<PayRateAudit>()
                .HasRequired(a => a.Staff)
                .WithMany()
                .HasForeignKey(a => a.StaffId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PayRateAudit>()
                .Property(a => a.OldRate)
                .HasPrecision(18, 2);
            modelBuilder.Entity<PayRateAudit>()
                .Property(a => a.NewRate)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Contract>()
                .Property(c => c.ContractValue)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Equipment>()
                .Property(e => e.PurchaseCost)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Equipment>()
                .Property(e => e.CurrentValue)
                .HasPrecision(18, 2);

            modelBuilder.Entity<EquipmentMaintenance>()
                .Property(e => e.Cost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<EquipmentRepair>()
                .Property(e => e.PartsCost)
                .HasPrecision(18, 2);
            modelBuilder.Entity<EquipmentRepair>()
                .Property(e => e.LabourCost)
                .HasPrecision(18, 2);
            modelBuilder.Entity<EquipmentRepair>()
                .Property(e => e.TotalCost)
                .HasPrecision(18, 2);
            modelBuilder.Entity<EquipmentRepair>()
                .Property(e => e.ActualHours)
                .HasPrecision(18, 2);
            modelBuilder.Entity<EquipmentRepair>()
                .Property(e => e.EstimatedPartsCost)
                .HasPrecision(18, 2);
            modelBuilder.Entity<EquipmentRepair>()
                .Property(e => e.EstimatedLabourCost)
                .HasPrecision(18, 2);
            modelBuilder.Entity<EquipmentRepair>()
                .Property(e => e.EstimatedTotalCost)
                .HasPrecision(18, 2);
            modelBuilder.Entity<EquipmentRepair>()
                .Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            modelBuilder.Entity<EquipmentPart>()
                .Property(p => p.UnitCost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<RepairPartUsage>()
                .Property(p => p.UnitCostAtUse)
                .HasPrecision(18, 2);

            modelBuilder.Entity<EquipmentCheckoutChecklist>()
                .Property(c => c.LoadTestResultKg)
                .HasPrecision(18, 2);

            // UC17 Entity Relationships & Cascades
            modelBuilder.Entity<RepairProgressLog>()
                .HasRequired(l => l.EquipmentRepair)
                .WithMany(r => r.ProgressLogs)
                .HasForeignKey(l => l.EquipmentRepairId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<RepairPartUsage>()
                .HasRequired(u => u.EquipmentRepair)
                .WithMany(r => r.PartUsages)
                .HasForeignKey(u => u.EquipmentRepairId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<RepairPartUsage>()
                .HasRequired(u => u.EquipmentPart)
                .WithMany(p => p.PartUsages)
                .HasForeignKey(u => u.EquipmentPartId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<StockMovement>()
                .HasRequired(m => m.EquipmentPart)
                .WithMany(p => p.StockMovements)
                .HasForeignKey(m => m.EquipmentPartId)
                .WillCascadeOnDelete(true);

            // Enhanced Lifecycle Entity Configurations
            modelBuilder.Entity<EquipmentTechnician>()
                .HasOptional(t => t.LinkedStaff)
                .WithMany()
                .HasForeignKey(t => t.LinkedStaffId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<EquipmentCheckoutChecklist>()
                .HasRequired(c => c.EquipmentRepair)
                .WithOptional(r => r.CheckoutChecklist)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<EquipmentRepair>()
                .HasOptional(r => r.Technician)
                .WithMany()
                .HasForeignKey(r => r.TechnicianId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Staff>()
                .Property(s => s.PayRatePerHour)
                .HasPrecision(18, 2);

            modelBuilder.Entity<AttendanceRecord>()
                .HasRequired(ar => ar.Staff)
                .WithMany()
                .HasForeignKey(ar => ar.StaffId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<AttendanceRecord>()
                .HasRequired(ar => ar.Project)
                .WithMany()
                .HasForeignKey(ar => ar.ProjectId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<AttendanceRecord>()
                .Property(ar => ar.TotalHours)
                .HasPrecision(10, 2);

            modelBuilder.Entity<ProjectBudget>()
                .HasKey(pb => pb.ProjectId);
            modelBuilder.Entity<ProjectBudget>()
                .HasRequired(pb => pb.Project)
                .WithOptional(p => p.Budget)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<ProjectScope>()
                .HasKey(ps => ps.ProjectId);
            modelBuilder.Entity<ProjectScope>()
                .HasRequired(ps => ps.Project)
                .WithOptional(p => p.ProjectScope)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<ProjectDeliverable>()
                .HasRequired(pd => pd.Project)
                .WithMany(p => p.Deliverables)
                .HasForeignKey(pd => pd.ProjectId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<ProjectResourceAllocation>()
                .HasRequired(pra => pra.Project)
                .WithMany(p => p.ResourceAllocations)
                .HasForeignKey(pra => pra.ProjectId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<ProjectVariation>()
                .HasRequired(pv => pv.Project)
                .WithMany(p => p.Variations)
                .HasForeignKey(pv => pv.ProjectId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<ProjectBaselineSnapshot>()
                .HasRequired(pbs => pbs.Project)
                .WithMany()
                .HasForeignKey(pbs => pbs.ProjectId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ProjectBaselineSnapshot>()
                .HasRequired(pbs => pbs.Variation)
                .WithMany(v => v.BaselineSnapshots)
                .HasForeignKey(pbs => pbs.VariationId)
                .WillCascadeOnDelete(false);

            // UC19 Entity Configurations
            modelBuilder.Entity<SiteInspection>()
                .HasRequired(si => si.Project)
                .WithMany(p => p.Inspections)
                .HasForeignKey(si => si.ProjectId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<InspectionItemResult>()
                .HasRequired(r => r.SiteInspection)
                .WithMany(i => i.ItemResults)
                .HasForeignKey(r => r.SiteInspectionId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<InspectionItemResult>()
                .HasRequired(r => r.TemplateItem)
                .WithMany()
                .HasForeignKey(r => r.TemplateItemId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<NonConformance>()
                .HasRequired(nc => nc.SiteInspection)
                .WithMany(i => i.NonConformances)
                .HasForeignKey(nc => nc.SiteInspectionId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<ProjectVariation>()
                .HasOptional(v => v.NonConformance)
                .WithMany()
                .HasForeignKey(v => v.NonConformanceId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ProjectHandover>()
                .HasRequired(ph => ph.Project)
                .WithMany(p => p.Handovers)
                .HasForeignKey(ph => ph.ProjectId)
                .WillCascadeOnDelete(true);

            // SnagItem relationship configuration
            modelBuilder.Entity<SnagItem>()
                .HasRequired(s => s.ProjectHandover)
                .WithMany(h => h.SnagItems)
                .HasForeignKey(s => s.ProjectHandoverId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Project>()
                .HasRequired(p => p.ProjectManager)
                .WithMany(s => s.ManagedProjects)
                .HasForeignKey(p => p.ProjectManagerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Project>()
                .HasOptional(p => p.SiteManager)
                .WithMany(s => s.SupervisedProjects)
                .HasForeignKey(p => p.SiteManagerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DailyTask>()
                .HasRequired(dt => dt.AssignedTo)
                .WithMany(s => s.AssignedTasks)
                .HasForeignKey(dt => dt.AssignedToStaffId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DailyTask>()
                .HasRequired(dt => dt.AssignedBy)
                .WithMany(s => s.CreatedTasks)
                .HasForeignKey(dt => dt.AssignedByStaffId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Timesheet>()
                .HasRequired(t => t.Staff)
                .WithMany(s => s.Timesheets)
                .HasForeignKey(t => t.StaffId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<TimesheetEntry>()
                .HasRequired(te => te.Timesheet)
                .WithMany(t => t.Entries)
                .HasForeignKey(te => te.TimesheetId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<EquipmentAssignment>()
                .HasRequired(ea => ea.Project)
                .WithMany()
                .HasForeignKey(ea => ea.ProjectId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<EquipmentAssignment>()
                .HasRequired(ea => ea.Equipment)
                .WithMany(e => e.Assignments)
                .HasForeignKey(ea => ea.EquipmentId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<EquipmentCertificationRequirement>()
                .HasRequired(ec => ec.Equipment)
                .WithMany(e => e.CertificationRequirements)
                .HasForeignKey(ec => ec.EquipmentId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<EquipmentMaintenance>()
                .HasRequired(em => em.Equipment)
                .WithMany(e => e.Maintenances)
                .HasForeignKey(em => em.EquipmentId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<EquipmentRepair>()
                .HasRequired(er => er.Equipment)
                .WithMany(e => e.Repairs)
                .HasForeignKey(er => er.EquipmentId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<SiteInspection>()
                .HasRequired(si => si.PerformedBy)
                .WithMany(s => s.Inspections)
                .HasForeignKey(si => si.PerformedByStaffId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Contract>()
                .HasRequired(c => c.Client)
                .WithMany(cl => cl.Contracts)
                .HasForeignKey(c => c.ClientId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<ContractSignature>()
                .HasRequired(cs => cs.Contract)
                .WithMany(c => c.Signatures)
                .HasForeignKey(cs => cs.ContractId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Project>()
                .HasOptional(p => p.Client)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.ClientId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Project>()
                .HasOptional(p => p.Contract)
                .WithMany(c => c.Projects)
                .HasForeignKey(c => c.ContractId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ClientInvoice>()
                .HasRequired(ci => ci.Client)
                .WithMany(c => c.Invoices)
                .HasForeignKey(ci => ci.ClientId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<ClientInvoice>()
                .HasOptional(ci => ci.Project)
                .WithMany()
                .HasForeignKey(ci => ci.ProjectId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SupplierContract>()
                .HasRequired(sc => sc.Supplier)
                .WithMany(s => s.Contracts)
                .HasForeignKey(sc => sc.SupplierId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<JobApplication>()
                .HasRequired(ja => ja.Candidate)
                .WithMany(c => c.JobApplications)
                .HasForeignKey(ja => ja.CandidateId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<JobApplication>()
                .HasRequired(ja => ja.JobPost)
                .WithMany(jp => jp.JobApplications)
                .HasForeignKey(ja => ja.JobPostId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<ApplicationStatusHistory>()
                .HasRequired(ash => ash.Application)
                .WithMany(ja => ja.StatusHistory)
                .HasForeignKey(ash => ash.ApplicationId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<HandoverDocument>()
                .HasRequired(hd => hd.Handover)
                .WithMany(h => h.Documents)
                .HasForeignKey(hd => hd.HandoverId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<VariationApproval>()
                .HasRequired(va => va.Variation)
                .WithMany(v => v.Approvals)
                .HasForeignKey(va => va.VariationId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Equipment>()
                .HasOptional(e => e.AssignedProject)
                .WithMany()
                .HasForeignKey(e => e.AssignedProjectId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Notification>()
                .HasRequired(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Candidate>()
                .HasRequired(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Staff>()
                .HasRequired(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Quiz>()
                .HasOptional(q => q.JobPost)
                .WithMany()
                .HasForeignKey(q => q.JobPostId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<QuizQuestion>()
                .HasRequired(qq => qq.Quiz)
                .WithMany(q => q.Questions)
                .HasForeignKey(qq => qq.QuizId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<QuizAttempt>()
                .HasRequired(qa => qa.Quiz)
                .WithMany()
                .HasForeignKey(qa => qa.QuizId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<QuizAttempt>()
                .HasRequired(qa => qa.Application)
                .WithMany()
                .HasForeignKey(qa => qa.ApplicationId)
                .WillCascadeOnDelete(true);

            // ===================== UC11.5 Duty Roster =====================
            modelBuilder.Entity<DutyRoster>()
                .HasRequired(dr => dr.Project)
                .WithMany(p => p.DutyRosters)
                .HasForeignKey(dr => dr.ProjectId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DutyRoster>()
                .HasOptional(dr => dr.ParentRoster)
                .WithMany(dr => dr.ChildVersions)
                .HasForeignKey(dr => dr.ParentRosterId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Shift>()
                .HasRequired(s => s.DutyRoster)
                .WithMany(dr => dr.Shifts)
                .HasForeignKey(s => s.DutyRosterId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Shift>()
                .HasRequired(s => s.Project)
                .WithMany()
                .HasForeignKey(s => s.ProjectId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ShiftAssignment>()
                .HasRequired(sa => sa.Shift)
                .WithMany(s => s.Assignments)
                .HasForeignKey(sa => sa.ShiftId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<ShiftAssignment>()
                .HasRequired(sa => sa.Staff)
                .WithMany(st => st.ShiftAssignments)
                .HasForeignKey(sa => sa.StaffId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<LeaveRecord>()
                .HasRequired(lr => lr.Staff)
                .WithMany(st => st.LeaveRecords)
                .HasForeignKey(lr => lr.StaffId)
                .WillCascadeOnDelete(false);

            // Unique index on roster identity
            modelBuilder.Entity<DutyRoster>()
                .HasIndex(dr => new { dr.ProjectId, dr.PeriodStart, dr.PeriodEnd, dr.Version })
                .IsUnique();

            // Performance indexes for validation queries
            modelBuilder.Entity<Shift>()
                .HasIndex(s => new { s.ProjectId, s.ShiftDate });

            modelBuilder.Entity<ShiftAssignment>()
                .HasIndex(sa => new { sa.StaffId, sa.ShiftId });
        }
    }
}