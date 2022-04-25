using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace TTS_Chan.TTS
{
    public class TtsEntry
    {
        public IWaveProvider Provider { get; private set; }

        public TtsEntry(IWaveProvider provider)
        {
            Provider = provider;
            Provider = provider;
        }
    }
}
