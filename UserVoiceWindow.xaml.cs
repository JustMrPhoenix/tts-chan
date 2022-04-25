using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BespokeFusion;
using NAudio.Wave;
using TTS_Chan.Database;
using TTS_Chan.TTS;
using TTS_Chan.Twitch;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Orientation = System.Windows.Controls.Orientation;
using TextBox = System.Windows.Controls.TextBox;

namespace TTS_Chan
{
    /// <summary>
    /// Interaction logic for UserVoiceWindow.xaml
    /// </summary>
    public partial class UserVoiceWindow
    {
        public UserVoice UserVoice;
        private WaveOut waveOut;
        private string _previewPrompt = "Hello streamer. This is a preview for user %username%. Using %provider% %voice%";
        public UserVoiceWindow(UserVoice userVoice)
        {
            waveOut = new WaveOut()
            {
                DesiredLatency = 500,
                NumberOfBuffers = 32
            };
            UserVoice = userVoice;
            DataContext = UserVoice;
            InitializeComponent();
            var providers = TtsManager.GetProviders();
            ProviderComboBox.ItemsSource = providers;
            ProviderComboBox.SelectedItem = providers[0];
            UsernameComboBox.ItemsSource = TwitchConnector.KnownUsernames;
            if (UserVoice.UserId != null)
            {
                UsernameComboBox.IsEnabled = false;
            }
        }

        private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProviderComboBox.SelectedValue == null)
            {
                return;
            }
            var voices = TtsManager.GetProvider((string) ProviderComboBox.SelectedValue).GetVoices();
            VoiceNameComboBox.ItemsSource = voices;
            if (!voices.Contains(UserVoice.VoiceName))
            {
                VoiceNameComboBox.SelectedItem = voices[0];
            }
        }

        private void PreviewPromptButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                var msg = new CustomMaterialMessageBox
                {
                    TxtTitle = { Text = "Preview input" },
                };
                msg.BtnCopyMessage.Visibility = Visibility.Hidden;
                msg.BtnOk.Visibility = Visibility.Hidden;
                var textBox = new TextBox()
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(20, 20, 20, 20),
                    Text = _previewPrompt
                };
                textBox.TextChanged += (_, _) =>
                {
                    _previewPrompt = textBox.Text;
                };
                var button = new Button()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Content = "Play"
                };
                button.Click += (_, _) =>
                {
                    _ = DoPreview(textBox.Text);
                };
                var msgContent = new StackPanel()
                {
                    Children = { textBox, button },
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                
                msg.MessageControl.Content = msgContent;
                msg.Show();
                msg.Dispose();
            });

        }

        private async Task DoPreview(string textToPreview)
        {
            if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Stop();
            }
            textToPreview = textToPreview.Replace("%username%", UserVoice.Username);
            textToPreview = textToPreview.Replace("%provider%", UserVoice.VoiceProvider);
            textToPreview = textToPreview.Replace("%voice%", UserVoice.VoiceName);
            var provider = TtsManager.GetProvider(UserVoice.VoiceProvider);
            var entry = await provider.MakeEntry(new TwitchMessage(new IrcMessageParser(
                $":{UserVoice.Username}!{UserVoice.Username}@{UserVoice.Username}.tmi.twitch.tv PRIVMSG #{UserVoice.Username} :{textToPreview}"
                )), UserVoice);
            waveOut.Volume = (float) (Properties.Settings.Default.GlobalVolume / 100);
            waveOut.Init(entry.Provider);
            waveOut.Play();
            while (waveOut.PlaybackState == PlaybackState.Playing)
            {
                await Task.Delay(100);
            }
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Stop();
            }
            else
            {
                _ = DoPreview(_previewPrompt);
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            waveOut?.Dispose();
        }

    }
}
