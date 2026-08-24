using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameVerifiedToPubliclySourced : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "verified",
                table: "projects",
                newName: "publicly_sourced");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "publicly_sourced",
                table: "projects",
                newName: "verified");
        }
    }
}
