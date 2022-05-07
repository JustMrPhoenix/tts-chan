using Microsoft.EntityFrameworkCore.Migrations;

namespace TTS_Chan.Migrations
{
    public partial class TogglableSubstitutionsWithComments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "MessageSubstitutions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "MessageSubstitutions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comment",
                table: "MessageSubstitutions");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "MessageSubstitutions");
        }
    }
}
