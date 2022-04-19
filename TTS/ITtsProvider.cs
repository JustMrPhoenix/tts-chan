using System.Collections.Generic;
using System.Threading.Tasks;

namespace TTS_Chan.TTS
{
    public interface ITtsProvider
    {
        public Task Initialize();
        public string GetProviderName();
        public List<string> GetVoices();
        public Task<TtsEntry> MakeEntry(string message, string voice, int rate, int pitch);
    }
}