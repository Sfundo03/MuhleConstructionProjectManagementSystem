namespace MuhleOP.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class updatedatabase : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectHandovers", "ClientInvitedToSignOff", c => c.Boolean(nullable: false));
            AddColumn("dbo.ProjectHandovers", "ClientInvitedDate", c => c.DateTime());
            AddColumn("dbo.ProjectHandovers", "SignedHandoverPdfPath", c => c.String(maxLength: 500));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ProjectHandovers", "SignedHandoverPdfPath");
            DropColumn("dbo.ProjectHandovers", "ClientInvitedDate");
            DropColumn("dbo.ProjectHandovers", "ClientInvitedToSignOff");
        }
    }
}
