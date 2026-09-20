namespace MuhleOP.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddStaffPayRateAndBankAccount : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AttendanceRecords",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        StaffId = c.Int(nullable: false),
                        ProjectId = c.Int(nullable: false),
                        CheckInTime = c.DateTime(nullable: false),
                        CheckOutTime = c.DateTime(),
                        TotalHours = c.Decimal(precision: 10, scale: 2),
                        CheckInByUserId = c.String(maxLength: 128),
                        CheckOutByUserId = c.String(maxLength: 128),
                        CheckInMethod = c.String(maxLength: 50),
                        CheckOutMethod = c.String(maxLength: 50),
                        Notes = c.String(maxLength: 500),
                        IsManualOverride = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Projects", t => t.ProjectId)
                .ForeignKey("dbo.Staffs", t => t.StaffId)
                .Index(t => t.StaffId)
                .Index(t => t.ProjectId);
            
            AddColumn("dbo.Staffs", "QrToken", c => c.String(maxLength: 64));
            AddColumn("dbo.Staffs", "QrTokenCreatedDate", c => c.DateTime());
            AddColumn("dbo.Staffs", "QrTokenRevoked", c => c.Boolean(nullable: false));
            AlterColumn("dbo.Staffs", "Email", c => c.String(nullable: false, maxLength: 256));
            AlterColumn("dbo.Staffs", "Role", c => c.String(nullable: false, maxLength: 50));
            AlterColumn("dbo.Staffs", "PhoneNumber", c => c.String(maxLength: 20));
            AlterColumn("dbo.Staffs", "EmployeeId", c => c.String(maxLength: 50));
            AlterColumn("dbo.Staffs", "Department", c => c.String(maxLength: 100));
            AlterColumn("dbo.Staffs", "ProfilePicturePath", c => c.String(maxLength: 500));
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AttendanceRecords", "StaffId", "dbo.Staffs");
            DropForeignKey("dbo.AttendanceRecords", "ProjectId", "dbo.Projects");
            DropIndex("dbo.AttendanceRecords", new[] { "ProjectId" });
            DropIndex("dbo.AttendanceRecords", new[] { "StaffId" });
            AlterColumn("dbo.Staffs", "ProfilePicturePath", c => c.String());
            AlterColumn("dbo.Staffs", "Department", c => c.String());
            AlterColumn("dbo.Staffs", "EmployeeId", c => c.String());
            AlterColumn("dbo.Staffs", "PhoneNumber", c => c.String());
            AlterColumn("dbo.Staffs", "Role", c => c.String(nullable: false));
            AlterColumn("dbo.Staffs", "Email", c => c.String(nullable: false));
            DropColumn("dbo.Staffs", "QrTokenRevoked");
            DropColumn("dbo.Staffs", "QrTokenCreatedDate");
            DropColumn("dbo.Staffs", "QrToken");
            DropTable("dbo.AttendanceRecords");
        }
    }
}
