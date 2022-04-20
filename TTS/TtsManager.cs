using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NAudio.Wave;
using TTS_Chan.Database;
using TTS_Chan.Twitch;

namespace TTS_Chan.TTS
{
    public static class TtsManager
    {
        private static readonly Dictionary<string, ITtsProvider> Providers = new();
        private static readonly Queue<TtsEntry> Queue = new();
        private static WaveOutEvent _outputDevice;

        public static void Init()
        {
            _outputDevice = new WaveOutEvent();
            _outputDevice.PlaybackStopped += OnPlaybackStopped;
        }

        private static void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (Queue.Count == 0) return;
            var entry = Queue.Dequeue();
            _outputDevice.Init(entry.Provider);
            _outputDevice.Volume = (float)Properties.Settings.Default.GlobalVolume / 100;
            _outputDevice.Play();
        }


        public static void AddProvider(ITtsProvider provider)
        {
            Providers[provider.GetProviderName()] = provider;
        }

        public static ITtsProvider GetProvider(string providerName)
        {
            return Providers[providerName];
        }

        public static List<string> GetProviders()
        {
            return Providers.Keys.ToList();
        }

        public static WaveFormat GetOutputFormat()
        {
            return _outputDevice.OutputWaveFormat;
        }

        public static async Task ProcessMessage(TwitchMessage message)
        {
            FilterMessageText(message);

            var userVoice = await DatabaseManager.Context.UserVoices.Where(userVoice => userVoice.UserId == message.Userid).FirstOrDefaultAsync(CancellationToken.None);
            if (userVoice == null)
            {
                var foundByUsername = await DatabaseManager.Context.UserVoices.Where(voice1 => voice1.UserId == null && voice1.Username == message.Username).FirstOrDefaultAsync(CancellationToken.None);
                if (foundByUsername != null)
                {
                    userVoice = foundByUsername;
                    foundByUsername.UserId = message.Userid;
                    await DatabaseManager.Context.SaveChangesAsync();
                }
            }

            if (userVoice == null)
            {
                // TODO: Default voice from settings
                var microsoftProvider = GetProvider("Microsoft");
                var entry = await microsoftProvider.MakeEntry(message, null);
                AddToQueue(entry);
            }
            else
            {

                if (userVoice.Username != message.Username)
                {
                    userVoice.Username = message.Username;
                }
                if (userVoice.IsMuted)
                {
                    return;
                }
                var microsoftProvider = GetProvider(userVoice.VoiceProvider);
                var entry = await microsoftProvider.MakeEntry(message, userVoice);
                AddToQueue(entry);
            }
        }

        public static void FilterMessageText(TwitchMessage message)
        {
            message.SpeakableText = message.Text;
        }

        public static void AddToQueue(TtsEntry entry)
        {
            if (_outputDevice.PlaybackState == PlaybackState.Stopped)
            {
                _outputDevice.Init(entry.Provider);
                _outputDevice.Volume = (float)Properties.Settings.Default.GlobalVolume / 100;
                _outputDevice.Play();
            }
            else
            {
                Queue.Enqueue(entry);
            }
        }
    }
}
