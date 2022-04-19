using System;
using Microsoft.EntityFrameworkCore;

namespace TTS_Chan.Database
{
    public class DatabaseContext : DbContext
    {
        public DbSet<UserVoice> UserVoices { get; set; }
        private string DbPath { get; }

        public DatabaseContext()
        {
            const Environment.SpecialFolder folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "tts-chan.db");
        }

        public void Migrate()
        {
            Database.Migrate();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite($"Data Source={DbPath}");
        }
    }
}
