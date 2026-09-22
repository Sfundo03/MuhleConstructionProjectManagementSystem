// ============ Models/IdentityModels.cs ============
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Data.Entity;
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

        // Quality & Progress
        public DbSet<SiteInspection> SiteInspections { get; set; }
        public DbSet<ProjectVariation> ProjectVariations { get; set; }
        public DbSet<VariationApproval> VariationApprovals { get; set; }


        // Finance
        public DbSet<Payroll> Payrolls { get; set; }
        public DbSet<ClientInvoice> ClientInvoices { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<SupplierContract> SupplierContracts { get; set; }
        public DbSet<PayRateAudit> PayRateAudits { get; set; }
        // Project Handover
        public DbSet<ProjectHandover> ProjectHandovers { get; set; }
        public DbSet<HandoverDocument> HandoverDocuments { get; set; }

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

            modelBuilder.Entity<SupplierContract>()
                .Property(s => s.ContractValue)
                .HasPrecision(18, 2);

            // Configure Payroll - Staff relationship
            modelBuilder.Entity<Payroll>()
                .HasRequired(p => p.Staff)
                .WithMany()
                .HasForeignKey(p => p.StaffId)
                .WillCascadeOnDelete(true);

            // Configure PayRateAudit
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

            // Staff – pay rate precision
            modelBuilder.Entity<Staff>()
                .Property(s => s.PayRatePerHour)
                .HasPrecision(18, 2);


            // AttendanceRecord
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

            // ============================================
            // One-to-One relationships with shared primary key
            // ============================================

            // Configure Project - ProjectBudget (One-to-One using ProjectId as PK)
            modelBuilder.Entity<ProjectBudget>()
                .HasKey(pb => pb.ProjectId);
            modelBuilder.Entity<ProjectBudget>()
                .HasRequired(pb => pb.Project)
                .WithOptional(p => p.Budget)
                .WillCascadeOnDelete(true);

            // Configure Project - ProjectScope (One-to-One using ProjectId as PK)
            modelBuilder.Entity<ProjectScope>()
                .HasKey(ps => ps.ProjectId);
            modelBuilder.Entity<ProjectScope>()
                .HasRequired(ps => ps.Project)
                .WithOptional(p => p.ProjectScope)
                .WillCascadeOnDelete(true);

            // ============================================
            // One-to-Many relationships
            // ============================================

            // Configure Project - ProjectDeliverable relationship
            modelBuilder.Entity<ProjectDeliverable>()
                .HasRequired(pd => pd.Project)
                .WithMany(p => p.Deliverables)
                .HasForeignKey(pd => pd.ProjectId)
                .WillCascadeOnDelete(true);

            // Configure Project - ProjectResourceAllocation relationship
            modelBuilder.Entity<ProjectResourceAllocation>()
                .HasRequired(pra => pra.Project)
                .WithMany(p => p.ResourceAllocations)
                .HasForeignKey(pra => pra.ProjectId)
                .WillCascadeOnDelete(true);

            // Configure Project - ProjectVariation relationship
            modelBuilder.Entity<ProjectVariation>()
                .HasRequired(pv => pv.Project)
                .WithMany(p => p.Variations)
                .HasForeignKey(pv => pv.ProjectId)
                .WillCascadeOnDelete(true);

            // Configure Project - SiteInspection relationship
            modelBuilder.Entity<SiteInspection>()
                .HasRequired(si => si.Project)
                .WithMany(p => p.Inspections)
                .HasForeignKey(si => si.ProjectId)
                .WillCascadeOnDelete(true);

            // Configure Project - ProjectHandover relationship
            modelBuilder.Entity<ProjectHandover>()
                .HasRequired(ph => ph.Project)
                .WithMany(p => p.Handovers)
                .HasForeignKey(ph => ph.ProjectId)
                .WillCascadeOnDelete(true);

            // Configure Staff - Project relationships
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

            // Configure Staff - DailyTask relationships
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

            // Configure Staff - Timesheet relationship
            modelBuilder.Entity<Timesheet>()
                .HasRequired(t => t.Staff)
                .WithMany(s => s.Timesheets)
                .HasForeignKey(t => t.StaffId)
                .WillCascadeOnDelete(true);

            // Configure Timesheet - TimesheetEntry relationship
            modelBuilder.Entity<TimesheetEntry>()
                .HasRequired(te => te.Timesheet)
                .WithMany(t => t.Entries)
                .HasForeignKey(te => te.TimesheetId)
                .WillCascadeOnDelete(true);

            // Configure Equipment - EquipmentAssignment relationship
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

            // Configure Equipment - EquipmentMaintenance relationship
            modelBuilder.Entity<EquipmentMaintenance>()
                .HasRequired(em => em.Equipment)
                .WithMany(e => e.Maintenances)
                .HasForeignKey(em => em.EquipmentId)
                .WillCascadeOnDelete(true);

            // Configure Equipment - EquipmentRepair relationship
            modelBuilder.Entity<EquipmentRepair>()
                .HasRequired(er => er.Equipment)
                .WithMany(e => e.Repairs)
                .HasForeignKey(er => er.EquipmentId)
                .WillCascadeOnDelete(true);

            // Configure Staff - SiteInspection relationship
            modelBuilder.Entity<SiteInspection>()
                .HasRequired(si => si.PerformedBy)
                .WithMany(s => s.Inspections)
                .HasForeignKey(si => si.PerformedByStaffId)
                .WillCascadeOnDelete(false);

            // Configure Contract - Client relationship
            modelBuilder.Entity<Contract>()
                .HasRequired(c => c.Client)
                .WithMany(cl => cl.Contracts)
                .HasForeignKey(c => c.ClientId)
                .WillCascadeOnDelete(true);

            // Configure ContractSignature - Contract relationship
            modelBuilder.Entity<ContractSignature>()
                .HasRequired(cs => cs.Contract)
                .WithMany(c => c.Signatures)
                .HasForeignKey(cs => cs.ContractId)
                .WillCascadeOnDelete(true);

            // Configure Project - Client relationship
            modelBuilder.Entity<Project>()
                .HasOptional(p => p.Client)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.ClientId)
                .WillCascadeOnDelete(false);

            // Configure Project - Contract relationship
            modelBuilder.Entity<Project>()
                .HasOptional(p => p.Contract)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.ContractId)
                .WillCascadeOnDelete(false);

            // Configure Client - ClientInvoice relationship
            modelBuilder.Entity<ClientInvoice>()
                .HasRequired(ci => ci.Client)
                .WithMany(c => c.Invoices)
                .HasForeignKey(ci => ci.ClientId)
                .WillCascadeOnDelete(true);

            // Configure Project - ClientInvoice relationship
            modelBuilder.Entity<ClientInvoice>()
                .HasOptional(ci => ci.Project)
                .WithMany()
                .HasForeignKey(ci => ci.ProjectId)
                .WillCascadeOnDelete(false);

            // Configure Supplier - SupplierContract relationship
            modelBuilder.Entity<SupplierContract>()
                .HasRequired(sc => sc.Supplier)
                .WithMany(s => s.Contracts)
                .HasForeignKey(sc => sc.SupplierId)
                .WillCascadeOnDelete(true);

            // Configure Payroll - Staff relationship
            modelBuilder.Entity<Payroll>()
                .HasRequired(p => p.Staff)
                .WithMany()
                .HasForeignKey(p => p.StaffId)
                .WillCascadeOnDelete(true);

            // Configure JobApplication - Candidate relationship
            modelBuilder.Entity<JobApplication>()
                .HasRequired(ja => ja.Candidate)
                .WithMany(c => c.JobApplications)
                .HasForeignKey(ja => ja.CandidateId)
                .WillCascadeOnDelete(true);

            // Configure JobApplication - JobPost relationship
            modelBuilder.Entity<JobApplication>()
                .HasRequired(ja => ja.JobPost)
                .WithMany(jp => jp.JobApplications)
                .HasForeignKey(ja => ja.JobPostId)
                .WillCascadeOnDelete(true);

            // Configure ApplicationStatusHistory - JobApplication relationship
            modelBuilder.Entity<ApplicationStatusHistory>()
                .HasRequired(ash => ash.Application)
                .WithMany(ja => ja.StatusHistory)
                .HasForeignKey(ash => ash.ApplicationId)
                .WillCascadeOnDelete(true);

            // Configure HandoverDocument - ProjectHandover relationship
            modelBuilder.Entity<HandoverDocument>()
                .HasRequired(hd => hd.Handover)
                .WithMany(h => h.Documents)
                .HasForeignKey(hd => hd.HandoverId)
                .WillCascadeOnDelete(true);

            // Configure VariationApproval - ProjectVariation relationship
            modelBuilder.Entity<VariationApproval>()
                .HasRequired(va => va.Variation)
                .WithMany(v => v.Approvals)
                .HasForeignKey(va => va.VariationId)
                .WillCascadeOnDelete(true);

            // Configure Equipment - Project relationship (for assigned project)
            modelBuilder.Entity<Equipment>()
                .HasOptional(e => e.AssignedProject)
                .WithMany()
                .HasForeignKey(e => e.AssignedProjectId)
                .WillCascadeOnDelete(false);

            // Configure Notification - User relationship
            modelBuilder.Entity<Notification>()
                .HasRequired(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .WillCascadeOnDelete(false);

            // Configure Candidate - User relationship
            modelBuilder.Entity<Candidate>()
                .HasRequired(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .WillCascadeOnDelete(false);

            // Configure Staff - User relationship
            modelBuilder.Entity<Staff>()
                .HasRequired(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .WillCascadeOnDelete(false);

            // Configure Quiz relationships
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
        }
    }
}