using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

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
