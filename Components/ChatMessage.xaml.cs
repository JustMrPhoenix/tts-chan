using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TTS_Chan.Components
{
    /// <summary>
    /// Interaction logic for ChatMessage.xaml
    /// </summary>
    public partial class ChatMessage : UserControl
    {
        public string MessageText { get; private set; }
        public string UserDisplayname { get; private set; }
        public Brush UserColor { get; private set; }
        public ChatMessage(Twitch.TwitchMessage message)
        {
            MessageText = message.Text;
            UserDisplayname = message.Displayname;
            UserColor = (SolidColorBrush)new BrushConverter().ConvertFrom(message.Color);
            InitializeComponent();
            /*TextBlock usernameTextBlock = (TextBlock)FindName("usernameTextBlock");
            usernameTextBlock.Foreground = userControl;*/
        }
    }
}
