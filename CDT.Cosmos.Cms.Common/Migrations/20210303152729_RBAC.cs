using Microsoft.EntityFrameworkCore.Migrations;

namespace CDT.Cosmos.Cms.Common.Migrations
{
    /// <summary>
    /// Migration RBAC
    /// </summary>
    public partial class RBAC : Migration
    {
        /// <summary>
        /// Migration up
        /// </summary>
        /// <param name="migrationBuilder"></param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RoleList",
                table: "Articles",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }
        /// <summary>
        /// Migration roll back
        /// </summary>
        /// <param name="migrationBuilder"></param>

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoleList",
                table: "Articles");
        }
    }
}
