using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace TTS_Chan.TTS
{
    public class TtsEntry
    {
        private readonly IWaveProvider _originalProvider;
        private IWaveProvider _volumeAdjusted;

        public TtsEntry(IWaveProvider provider)
        {
            _originalProvider = provider;
        }

        public IWaveProvider UpdateVolume(float volume)
        {
            var volumeProvider = new VolumeSampleProvider(_originalProvider.ToSampleProvider())
            {
                Volume = volume
            };
            _volumeAdjusted = volumeProvider.ToWaveProvider();
            return _volumeAdjusted;
        }

        public IWaveProvider GetProvider()
        {
            return _volumeAdjusted ?? _originalProvider;
        }
    }
}
