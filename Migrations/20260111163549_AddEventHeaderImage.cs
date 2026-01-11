using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jointly.Migrations
{
    /// <inheritdoc />
    public partial class AddEventHeaderImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HeaderImagePath",
                table: "Events",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeaderImagePath",
                table: "Events");
        }
    }
}
