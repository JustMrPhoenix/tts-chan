using Microsoft.EntityFrameworkCore.Migrations;

namespace TTS_Chan.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MessageSubstitutions",
                columns: table => new
                {
                    MessageSubstitutionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Pattern = table.Column<string>(type: "TEXT", nullable: true),
                    Replacement = table.Column<string>(type: "TEXT", nullable: true),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    IsRegex = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageSubstitutions", x => x.MessageSubstitutionId);
                });

            migrationBuilder.CreateTable(
                name: "UserVoices",
                columns: table => new
                {
                    UserVoiceId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    IsMuted = table.Column<bool>(type: "INTEGER", nullable: false),
                    VoiceProvider = table.Column<string>(type: "TEXT", nullable: true),
                    VoiceName = table.Column<string>(type: "TEXT", nullable: true),
                    Rate = table.Column<int>(type: "INTEGER", nullable: false),
                    Pitch = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVoices", x => x.UserVoiceId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessageSubstitutions");

            migrationBuilder.DropTable(
                name: "UserVoices");
        }
    }
}
