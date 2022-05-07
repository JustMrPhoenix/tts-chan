using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using TTS_Chan.TTS;

namespace TTS_Chan.Database
{
    public static class DatabaseManager
    {
        public static readonly DatabaseContext Context;
        static DatabaseManager()
        {
            Context = new DatabaseContext();
            Context.Migrate();
            EnsureDefaultVoice();
        }

        public static void EnsureDefaultVoice()
        {
            var defaultVoice = Context.UserVoices.FirstOrDefault(userVoice => userVoice.UserId == "_default" && userVoice.Username == "_default");
            if (defaultVoice != null) return;
            var defaultProvider = TtsManager.GetProvider(TtsManager.GetProviders().First());
            var userVoice = new UserVoice()
            {
                Username = "_default",
                UserId = "_default",
                Pitch = 0,
                Rate = 1,
                VoiceProvider = defaultProvider.GetProviderName(),
                VoiceName = defaultProvider.GetVoices().First()
            };
            Context.UserVoices.Add(userVoice);
            Context.SaveChanges();
        }
    }
}
