namespace bootstrap_git_auto_notes.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddExtraUserInformation : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ExtraUserInformation",
                c => new
                    {
                        id = c.String(nullable: false, maxLength: 128),
                        login = c.String(),
                        avatar_url = c.String(),
                        gravatar_id = c.String(),
                        url = c.String(),
                        html_url = c.String(),
                        followers_url = c.String(),
                        following_url = c.String(),
                        gists_url = c.String(),
                        starred_url = c.String(),
                        subscriptions_url = c.String(),
                        organizations_url = c.String(),
                        repos_url = c.String(),
                        events_url = c.String(),
                        received_events_url = c.String(),
                        type = c.String(),
                        site_admin = c.String(),
                        access_token = c.String(),
                    })
                .PrimaryKey(t => t.id);
        }
        
        public override void Down()
        {
            DropTable("dbo.ExtraUserInformation");
        }
    }
}
