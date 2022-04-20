using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace TTS_Chan.TTS
{
    public class TtsEntry
    {
        private static double _semitone = Math.Pow(2, 1.0/12);
        public IWaveProvider Provider { get; private set; }
        public int Pitch { get; private set; }

        public TtsEntry(IWaveProvider provider, int pitch)
        {
            Pitch = pitch;
            Provider = provider;
            var pitchFactor = Math.Clamp(1 + pitch / 100.0f, 0.001, 2);
            var pitchShift = new SmbPitchShiftingSampleProvider(provider.ToSampleProvider())
            {
                PitchFactor = (float) pitchFactor
            };
            Provider = pitchShift.ToWaveProvider();
        }
    }
}
