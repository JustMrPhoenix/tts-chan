using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace TTS_Chan.TTS
{
    public class TtsEntry
    {
        public IWaveProvider Provider { get; private set; }
        public int Pitch { get; private set; }

        public TtsEntry(IWaveProvider provider, int pitch)
        {
            Pitch = pitch;
            Provider = provider;
            /*var pitchShift = new SmbPitchShiftingSampleProvider(provider.ToSampleProvider())
            {
                PitchFactor = pitch
            };
            Provider = pitchShift.ToWaveProvider();*/
        }
    }
}
