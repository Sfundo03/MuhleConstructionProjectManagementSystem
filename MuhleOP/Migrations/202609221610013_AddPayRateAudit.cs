namespace MuhleOP.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class AddPayRateAudit : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PayRateAudits",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    StaffId = c.Int(nullable: false),
                    OldRate = c.Decimal(nullable: false, precision: 18, scale: 2),
                    NewRate = c.Decimal(nullable: false, precision: 18, scale: 2),
                    ChangedBy = c.String(nullable: false, maxLength: 256),
                    ChangedByUserId = c.String(nullable: false, maxLength: 128),
                    ChangedAt = c.DateTime(nullable: false),
                    Notes = c.String(maxLength: 500),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Staffs", t => t.StaffId)   // ← "Staffs" (plural)
                .Index(t => t.StaffId);
        }

        public override void Down()
        {
            DropForeignKey("dbo.PayRateAudits", "StaffId", "dbo.Staffs");  // ← "Staffs" (plural)
            DropIndex("dbo.PayRateAudits", new[] { "StaffId" });
            DropTable("dbo.PayRateAudits");
        }
    }
}