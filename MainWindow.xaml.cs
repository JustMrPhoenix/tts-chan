using MaterialDesignThemes.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TTS_Chan.Components;
using TTS_Chan.Database;
using TTS_Chan.TTS;

namespace TTS_Chan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static MainWindow Instance;
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
            DataContext = this;
            InitializeComponent();
            Instance = this;
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
                    ConnectionButtonContent.Text = "Connect";
                    ConnectionButton.Background = Brushes.Green;
                    StatusTextBlock.Text = "Not connected";
                    ConnectionButton.SetValue(ButtonProgressAssist.IsIndeterminateProperty, false);
                    ConnectionButton.IsEnabled = true;
                    break;
                case Twitch.TwitchConnectionStatus.Connecting:
                    ConnectionButtonContent.Text = "Connecting...";
                    ConnectionButton.Background = Brushes.Gray;
                    StatusTextBlock.Text = "Connecting...";
                    ConnectionButton.SetValue(ButtonProgressAssist.IsIndeterminateProperty, true);
                    ConnectionButton.IsEnabled = false;
                    break;
                case Twitch.TwitchConnectionStatus.Connected:
                    ConnectionButtonContent.Text = "Join " + Properties.Settings.Default.ChannelName;
                    ConnectionButton.Background = Brushes.DarkGreen;
                    StatusTextBlock.Text = "Connected";
                    if (!Properties.Settings.Default.AutoJoin)
                    {
                        ConnectionButton.SetValue(ButtonProgressAssist.IsIndeterminateProperty, false);
                        ConnectionButton.IsEnabled = true;
                    }
                    else
                    {
                        ConnectionButton.SetValue(ButtonProgressAssist.IsIndeterminateProperty, true);
                        ConnectionButton.IsEnabled = false;
                    }
                    break;

                case Twitch.TwitchConnectionStatus.Joined:
                    ConnectionButtonContent.Text = "Disconnect";
                    ConnectionButton.Background = Brushes.Red;
                    StatusTextBlock.Text = "Connected";
                    ConnectionButton.SetValue(ButtonProgressAssist.IsIndeterminateProperty, false);
                    ConnectionButton.IsEnabled = true;
                    break;
                case Twitch.TwitchConnectionStatus.Reconnecting:
                    ConnectionButtonContent.Text = "Reconnecting...";
                    ConnectionButton.Background = Brushes.DarkOrange;
                    StatusTextBlock.Text = "Reconnecting...";
                    ConnectionButton.SetValue(ButtonProgressAssist.IsIndeterminateProperty, true);
                    ConnectionButton.IsEnabled = false;
                    break;
                case Twitch.TwitchConnectionStatus.Error:
                    ConnectionButtonContent.Text = "Reconnecting...";
                    ConnectionButton.Background = Brushes.DarkOrange;
                    StatusTextBlock.Text = "Reconnecting...";
                    ConnectionButton.SetValue(ButtonProgressAssist.IsIndeterminateProperty, true);
                    ConnectionButton.IsEnabled = false;
                    break;
            }
            if(message != null)
            {
                StatusTextBlock.Text += "\n" + message;
                AddLog(message, false);
            } else
            {
                AddLog(Enum.GetName(typeof(Twitch.TwitchConnectionStatus), status), false);
            }
        }

        public void AddLog(string message, bool shouldInvoke = true)
        {
            if (shouldInvoke)
            {
                Dispatcher.Invoke(() => { AddLog(message, false); });
                return;
            }
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

        private void ConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_twitchConnector.IrcStatus)
            {
                case Twitch.TwitchConnectionStatus.NotConnected:
                case Twitch.TwitchConnectionStatus.Disconnected:
                    _ = ConnectTwitch(_twitchConnector.IrcStatus == Twitch.TwitchConnectionStatus.Disconnected);
                    break;
                case Twitch.TwitchConnectionStatus.Connected when !Properties.Settings.Default.AutoJoin:
                    _ = _twitchConnector.JoinChannel(Properties.Settings.Default.ChannelName);
                    break;
                default:
                    _ = _twitchConnector.Disconnect();
                    break;
            }
        }

        private void DatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            DatabaseWindow dbWindow = new();
            dbWindow.ShowDialog();
        }
    }
}
