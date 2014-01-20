namespace bootstrap_git_auto_notes.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddExtraUserInformationForeignKey : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ExtraUserInformation", "UserId", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ExtraUserInformation", "UserId");
        }
    }
}
