using MaterialDesignThemes.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TTS_Chan.Components;
using TTS_Chan.TTS;

namespace TTS_Chan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static readonly int MaxMessages = 50;
        private static readonly SolidColorBrush[] MessageBgColors = {
            (SolidColorBrush)new BrushConverter().ConvertFrom("#242427"),
            (SolidColorBrush)new BrushConverter().ConvertFrom("#18181b")
        };
        private int _messageCount;
        private readonly Twitch.TwitchConnector _twitchConnector;
        private readonly ObservableCollection<string> _eventLog = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            _twitchConnector = new Twitch.TwitchConnector(this);
            _twitchConnector.IrcStatusChanged += delegate (Twitch.TwitchConnectionStatus status, string message)
            {
                Dispatcher.Invoke(() => { OnIRCStatusChanged(status, message); });
            };
            _ = StartupTask();
        }

        private async Task StartupTask()
        {
            foreach (var providerName in TtsManager.GetProviders())
            {
                var provider = TtsManager.GetProvider(providerName);
                try
                {
                    await provider.Initialize();
                    AddLog($"{providerName} loaded: {provider.GetVoices().Count} voices");
                }
                catch (Exception ex)
                {
                    AddLog($"{providerName} failed: {ex.Message}");
                }
                
            }
            if (Properties.Settings.Default.AutoConnect)
            {
                await ConnectTwitch();
            }
#if DEBUG
            EmulateMessages();
#endif
        }

        private async Task ConnectTwitch(bool isReconnect = false)
        {
            await _twitchConnector.Connect(isReconnect);
            await _twitchConnector.Authorize();

            if(Properties.Settings.Default.AutoJoin)
            {
                await _twitchConnector.JoinChannel(Properties.Settings.Default.ChannelName);
            }
        }

        private void OnIRCStatusChanged(Twitch.TwitchConnectionStatus status, string message = null)
        {
            switch (status)
            {
                case Twitch.TwitchConnectionStatus.NotConnected:
                case Twitch.TwitchConnectionStatus.Disconnected:
                    ConnetionButtonContent.Text = "Connect";
                    ConnetionButton.Background = Brushes.Green;
                    StatusTextBlock.Text = "Not connected";
                    ConnetionButton.SetValue(ButtonProgressAssist.IsIndeterminateProperty, false);
                    ConnetionButton.IsEnabled = true;
                    break;
                case Twitch.TwitchConnectionStatus.Connecting:
                    ConnetionButtonContent.Text = "Connecting...";
                    ConnetionButton.Background = Brushes.Gray;
                    StatusTextBlock.Text = "Connecting...";
                    ConnetionButton.SetValue(ButtonProgressAssist.IsIndeterminateProperty, true);
                    ConnetionButton.IsEnabled = false;
                    break;
                case Twitch.TwitchConnectionStatus.Connected:
                    ConnetionButtonContent.Text = "Join " + Properties.Settings.Default.ChannelName;
                    ConnetionButton.Background = Brushes.DarkGreen;
                    StatusTextBlock.Text = "Connected";
                    ConnetionButton.SetValue(ButtonProgressAssist.IsIndeterminateProperty, false);
                    ConnetionButton.IsEnabled = true;
                    break;

                case Twitch.TwitchConnectionStatus.Joined:
                    ConnetionButtonContent.Text = "Disconnect";
                    ConnetionButton.Background = Brushes.Red;
                    StatusTextBlock.Text = "Connected";
                    ConnetionButton.SetValue(ButtonProgressAssist.IsIndeterminateProperty, false);
                    ConnetionButton.IsEnabled = true;
                    break;
                case Twitch.TwitchConnectionStatus.Reconnecting:
                    ConnetionButtonContent.Text = "Reconnecting...";
                    ConnetionButton.Background = Brushes.DarkOrange;
                    StatusTextBlock.Text = "Reconnecting...";
                    ConnetionButton.SetValue(ButtonProgressAssist.IsIndeterminateProperty, true);
                    ConnetionButton.IsEnabled = false;
                    break;
                case Twitch.TwitchConnectionStatus.Error:
                    ConnetionButtonContent.Text = "Reconnecting...";
                    ConnetionButton.Background = Brushes.DarkOrange;
                    StatusTextBlock.Text = "Reconnecting...";
                    ConnetionButton.SetValue(ButtonProgressAssist.IsIndeterminateProperty, true);
                    ConnetionButton.IsEnabled = false;
                    break;
            }
            if(message != null)
            {
                StatusTextBlock.Text += "\n" + message;
                AddLog(message);
            } else
            {
                AddLog(Enum.GetName(typeof(Twitch.TwitchConnectionStatus), status));
            }
        }

        private void AddLog(string message)
        {
            _eventLog.Add(message);
            LogListBox.ItemsSource = _eventLog;
            if (VisualTreeHelper.GetChildrenCount(LogListBox) <= 0) return;
            var border = (Border)VisualTreeHelper.GetChild(LogListBox, 0);
            var scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
            scrollViewer.ScrollToBottom();
        }

        public void AddMessage(Twitch.TwitchMessage message)
        {
            var currentScroll = MessagesScrollView.VerticalOffset;
            ChatMessage newItm = new(message);
            MessagesStackPanel.Children.Add(newItm);
            _messageCount += 1;
            if (MessagesStackPanel.Children.Count > MaxMessages)
                MessagesStackPanel.Children.RemoveAt(0);
            newItm.Background = MessageBgColors[_messageCount % MessageBgColors.Length];
            if (currentScroll == 0 || Math.Abs(currentScroll - MessagesScrollView.ScrollableHeight) < 10)
                MessagesScrollView.ScrollToBottom();
        }

        private void EmulateMessages()
        {
            Title += " (DEBUG)";
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new(_twitchConnector);
            settingsWindow.ShowDialog();
        }

        private void ConnetionButton_Click(object sender, RoutedEventArgs e)
        {
            if(_twitchConnector.IrcStatus == Twitch.TwitchConnectionStatus.NotConnected || _twitchConnector.IrcStatus == Twitch.TwitchConnectionStatus.Disconnected)
            {
                _ = ConnectTwitch(_twitchConnector.IrcStatus == Twitch.TwitchConnectionStatus.Disconnected);
            }
            else if(_twitchConnector.IrcStatus == Twitch.TwitchConnectionStatus.Connected && !Properties.Settings.Default.AutoJoin)
            {
                _ = _twitchConnector.JoinChannel(Properties.Settings.Default.ChannelName);
            }
            else
            {
                _ = _twitchConnector.Disconnect();
            }
            
        }
    }
}
