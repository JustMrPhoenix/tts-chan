using System.Windows.Media;
using TTS_Chan.Twitch;

namespace TTS_Chan.Components
{
    /// <summary>
    /// Interaction logic for ChatMessage.xaml
    /// </summary>
    public partial class ChatMessage
    {
        public string MessageText { get; }
        public string UserDisplayName { get; }
        public Brush UserColor { get; }
        public TwitchMessage TwitchMessage { get; }
        public ChatMessage(TwitchMessage message)
        {
            MessageText = message.Text;
            UserDisplayName = message.DisplayName;
            UserColor = (SolidColorBrush)new BrushConverter().ConvertFrom(message.Color);
            TwitchMessage = message;
            InitializeComponent();
        }
    }
}
