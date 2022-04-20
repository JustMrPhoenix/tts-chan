using System.Collections.Generic;
using System.Threading.Tasks;
using TTS_Chan.Database;
using TTS_Chan.Twitch;

namespace TTS_Chan.TTS
{
    public interface ITtsProvider
    {
        public Task Initialize();
        public string GetProviderName();
        public List<string> GetVoices();
        public Task<TtsEntry> MakeEntry(TwitchMessage message, UserVoice voice);
    }
}