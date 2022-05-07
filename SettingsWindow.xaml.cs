using MaterialDesignThemes.Wpf;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using BespokeFusion;
using Google.Cloud.TextToSpeech.V1;
using Microsoft.EntityFrameworkCore;
using TTS_Chan.Database;
using TTS_Chan.Properties;
using TTS_Chan.TTS;
using TTS_Chan.TTS.TTS_Providers;
using TTS_Chan.Twitch;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace TTS_Chan
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        private readonly TwitchConnector _twitchConnector;
        private string _lastValidatedToken;
        public SettingsWindow(TwitchConnector twitchConnector)
        {
            InitializeComponent();
            _twitchConnector = twitchConnector;
            try
            {
                var twitchCredential = CredentialManager.ReadCredential(CredentialManager.TwitchAuthName);
                if (twitchCredential != null)
                {
                    TwitchAuthInputBox.Password = twitchCredential.Password;
                    _lastValidatedToken = twitchCredential.Password;
                    TwitchAuthInputBox.SetCurrentValue(HintAssist.ForegroundProperty, Brushes.LightGreen);
                    TwitchAuthInputBox.SetCurrentValue(TextFieldAssist.UnderlineBrushProperty, Brushes.LightGreen);
                }
            }
            catch (Exception)
            {
                // ignored
            }
            ValidateGoogleCreds();
            var providers = TtsManager.GetProviders();
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

        private void ValidateGoogleCreds(string credentials = null)
        {
            string usingCredentials = null;
            try
            {
                if (credentials == null)
                {
                    if (!File.Exists(GoogleTtsProvider.CredentialsPath))
                    {
                        GoogleAccountStatusBlock.Foreground = Brushes.IndianRed;
                        GoogleAccountStatusBlock.Text = "Invalid";
                        return;
                    }
                    else
                    {
                        usingCredentials = File.ReadAllText(GoogleTtsProvider.CredentialsPath);
                    }
                }
                else
                {
                    usingCredentials = credentials;
                }

                var builder = new TextToSpeechClientBuilder
                {
                    JsonCredentials = usingCredentials
                };

                var client = builder.Build();
                client.ListVoices(new ListVoicesRequest());
                GoogleAccountStatusBlock.Foreground = Brushes.ForestGreen;
                GoogleAccountStatusBlock.Text = "Valid";
                if (credentials != null)
                {
                    MaterialMessageBox.Show("You have provided valid Google cloud credentials.\nPlease note that TTS-Chan lists all available google voices including Wavenet voices.\n Those voices have a separate billing strategy please consult google API pricing page", "Google Authentication");
                }
            }
            catch (Exception)
            {
                GoogleAccountStatusBlock.Foreground = Brushes.IndianRed;
                GoogleAccountStatusBlock.Text = "Invalid";
                return;
            }

            if (File.Exists(GoogleTtsProvider.CredentialsPath)) return;
            File.WriteAllText(GoogleTtsProvider.CredentialsPath, usingCredentials);
            TtsManager.GetProvider(GoogleTtsProvider.Name).Initialize();
            MainWindow.Instance.AddLog("Google TTS reloaded!");
        }

        private async Task CheckAuth()
        {
            if(TwitchAuthInputBox.Password == _lastValidatedToken) { return; }
            Credential credentials = new(CredentialType.Generic, CredentialManager.TwitchAuthName, "", TwitchAuthInputBox.Password);
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

        private void OpenGAuthFile_Click(object sender, RoutedEventArgs e)
        {
            using OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files (*.json)|*.json";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            var fileStream = openFileDialog.OpenFile();
            using var reader = new StreamReader(fileStream);
            var fileContent = reader.ReadToEnd();
            ValidateGoogleCreds(fileContent);
        }

        private void GenerateToken_Click(object sender, RoutedEventArgs e)
        {
            MaterialMessageBox.Show("This feature is not fully implemented yet. If you have a twitch token you can input it in the token input field. Its not currently being used for anything", "Not implemented");
        }

        private void DefaultVoiceButton_Click(object sender, RoutedEventArgs e)
        {
            var defaultVoice = DatabaseManager.Context.UserVoices
                .FirstOrDefault(userVoice => userVoice.UserId == "_default" && userVoice.Username == "_default");
            var voiceWindow = new UserVoiceWindow(defaultVoice);
            var results = voiceWindow.ShowDialog();
            if (results != true) return;
            DatabaseManager.Context.SaveChanges();
        }
    }
}
