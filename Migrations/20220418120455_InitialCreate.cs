using Microsoft.EntityFrameworkCore.Migrations;

namespace TTS_Chan.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.Sql(@"
            CREATE TABLE UserVoices (
                UserVoiceId INTEGER PRIMARY KEY AUTOINCREMENT, 
	            UserId TEXT,
   	            Username TEXT,
                IsMuted BOOLEAN DEFAULT FALSE,
	            VoiceProvider TEXT NOT NULL,
	            VoiceName TEXT NOT NULL,
                Rate INTEGER,
                Pitch INTEGER
            );");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.Sql(@"DROP TABLE UserVoices");
        }
    }
}
