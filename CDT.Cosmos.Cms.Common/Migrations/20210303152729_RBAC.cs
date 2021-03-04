using Microsoft.EntityFrameworkCore.Migrations;

namespace CDT.Cosmos.Cms.Common.Migrations
{
    public partial class RBAC : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RoleList",
                table: "Articles",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoleList",
                table: "Articles");
        }
    }
}
