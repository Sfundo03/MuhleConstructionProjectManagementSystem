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

        // Project Handover
        public DbSet<ProjectHandover> ProjectHandovers { get; set; }
        public DbSet<HandoverDocument> HandoverDocuments { get; set; }

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
            modelBuilder.Entity<SupplierContract>()
                .Property(s => s.ContractValue)
                .HasPrecision(18, 2);
        }
    }
}