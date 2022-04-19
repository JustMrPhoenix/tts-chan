using MaterialDesignThemes.Wpf;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace TTS_Chan
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        private readonly Twitch.TwitchConnector _twitchConnector;
        private string _lastValidatedToken;
        public SettingsWindow(Twitch.TwitchConnector twitchConnector)
        {
            InitializeComponent();
            _twitchConnector = twitchConnector;
            try
            {
                var credential = CredentialManager.ReadCredential(CredentialManager.AppName);
                if (credential == null)
                    return;
                TwitchAuthInputBox.Password = credential.Password;
                _lastValidatedToken = credential.Password;
                TwitchAuthInputBox.SetCurrentValue(HintAssist.ForegroundProperty, Brushes.LightGreen);
                TwitchAuthInputBox.SetCurrentValue(TextFieldAssist.UnderlineBrushProperty, Brushes.LightGreen);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void TwitchAuthInputBox_LostFocus(object sender, RoutedEventArgs e) => _ = CheckAuth();

        private void TwitchAuthInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            _ = CheckAuth();
        }

        private async Task CheckAuth()
        {
            if(TwitchAuthInputBox.Password == _lastValidatedToken) { return; }
            Credential credentials = new(CredentialType.Generic, CredentialManager.AppName, "", TwitchAuthInputBox.Password);
            _lastValidatedToken = TwitchAuthInputBox.Password;
            var checkResults = await _twitchConnector.CheckAuth(credentials);
            if(checkResults)
            {
                TwitchAuthInputBox.SetCurrentValue(HintAssist.ForegroundProperty, Brushes.LightGreen);
                TwitchAuthInputBox.SetCurrentValue(TextFieldAssist.UnderlineBrushProperty, Brushes.LightGreen);
                DialogueTextBox.Text = "Twitch authorization successful!";
                DialogueButtonText.Text = "Pog";
                _ = DialogHost.ShowDialog(DialogHost.DialogContent!);
            }
            else
            {
                TwitchAuthInputBox.SetCurrentValue(HintAssist.ForegroundProperty, Brushes.IndianRed);
                TwitchAuthInputBox.SetCurrentValue(TextFieldAssist.UnderlineBrushProperty, Brushes.IndianRed);
                DialogueTextBox.Text = "Twitch authorization failed!";
                DialogueButtonText.Text = "Unpog";
                _ = DialogHost.ShowDialog(DialogHost.DialogContent!);
            }
        }
    }
}
