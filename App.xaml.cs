using TTS_Chan.TTS;
using TTS_Chan.TTS.TTS_Providers;

namespace TTS_Chan
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public App()
        {
            TtsManager.Init();
            TtsManager.AddProvider(new WindowsTtsProvider());
        }
    }
}
