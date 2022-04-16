using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TTS_Chan.Components;

namespace TTS_Chan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static int MaxMessages = 50;
        private static SolidColorBrush[] MessageBGColors = {
            (SolidColorBrush)new BrushConverter().ConvertFrom("#242427"),
            (SolidColorBrush)new BrushConverter().ConvertFrom("#18181b")
        };
        private int messageCount = 0;
        public Twitch.TwitchConnector TwitchConnector;
        public ObservableCollection<string> EventLog = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            EmulateMessages();
            TwitchConnector = new Twitch.TwitchConnector(this);
            TwitchConnector.IRCStatusChanged += new Twitch.ConnectionStatusChanged(delegate (Twitch.TwitchConnectionStatus status, string? message)
            {
                Dispatcher.Invoke(new Action(() => { OnIRCStatusChanged(status, message); }));
            });
            if(Properties.Settings.Default.AutoConnect)
            {
                ConnectTwitch();
            }
        }

        private async Task ConnectTwitch(bool isReconenct = false)
        {
            await TwitchConnector.Connect(isReconenct);
            await TwitchConnector.Authorize();

            if(Properties.Settings.Default.AutoJoin)
            {
                await TwitchConnector.JoinChannel(Properties.Settings.Default.ChannelName);
            }
        }

        private void OnIRCStatusChanged(Twitch.TwitchConnectionStatus status, string? message = null)
        {
            Button connectButton = (Button)FindName("ConnetionButton");
            TextBlock statusTextBlock = (TextBlock)FindName("StatusTextBlock");
            TextBlock buttonLabel = (TextBlock)connectButton.Content;
            switch (status)
            {
                case Twitch.TwitchConnectionStatus.NOT_CONNECTED:
                case Twitch.TwitchConnectionStatus.DISCONNECTED:
                    buttonLabel.Text = "Connect";
                    connectButton.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("green");
                    statusTextBlock.Text = "Not connected";
                    break;
                case Twitch.TwitchConnectionStatus.CONNECTING:
                    buttonLabel.Text = "Connecting...";
                    connectButton.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("gray");
                    statusTextBlock.Text = "Connecting...";
                    break;
                case Twitch.TwitchConnectionStatus.CONNECTED:
                    if (!Properties.Settings.Default.AutoJoin)
                    {
                        buttonLabel.Text = "Join " + Properties.Settings.Default.ChannelName;
                        connectButton.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("DarkGreen");
                        statusTextBlock.Text = "Connected";
                    }
                    else
                    {
                        buttonLabel.Text = "Disconnect";
                        connectButton.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("red");
                        statusTextBlock.Text = "Connected";
                    }
                    break;

                case Twitch.TwitchConnectionStatus.JOINED:
                    buttonLabel.Text = "Disconnect";
                    connectButton.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("red");
                    statusTextBlock.Text = "Connected";
                    break;
                case Twitch.TwitchConnectionStatus.RECONNECTING:
                    buttonLabel.Text = "Reconnecting...";
                    connectButton.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("DarkOrange");
                    statusTextBlock.Text = "Reconnecting...";
                    break;
                case Twitch.TwitchConnectionStatus.ERROR:
                    buttonLabel.Text = "Reconnecting...";
                    connectButton.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("DarkOrange");
                    statusTextBlock.Text = "Reconnecting...";
                    break;
                default:
                    break;
            }
            if(message != null)
            {
                statusTextBlock.Text += "\n" + message;
                EventLog.Add(message);
            } else
            {
                EventLog.Add(Enum.GetName(typeof(Twitch.TwitchConnectionStatus), status));
            }
            ListBox logBox = (ListBox)FindName("LogListBox");
            logBox.ItemsSource = EventLog;
            if (VisualTreeHelper.GetChildrenCount(logBox) > 0)
            {
                Border border = (Border)VisualTreeHelper.GetChild(logBox, 0);
                ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                scrollViewer.ScrollToBottom();
            }
        }


        public void AddMessage(Twitch.TwitchMessage message)
        {
            ScrollViewer msgScrollView = (ScrollViewer)FindName("MessagesScrollView");
            double currentScroll = msgScrollView.VerticalOffset;
            StackPanel msgList = (StackPanel)FindName("MessagesStackPanel");
            ChatMessage newItm = new ChatMessage(message);
            msgList.Children.Add(newItm);
            messageCount += 1;
            if (msgList.Children.Count > MaxMessages)
                msgList.Children.RemoveAt(0);
            newItm.Background = MessageBGColors[messageCount % MessageBGColors.Length];
            if (currentScroll == 0 || currentScroll == msgScrollView.ScrollableHeight)
                msgScrollView.ScrollToBottom();
        }

        private void EmulateMessages()
        {
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        private void ConnetionButton_Click(object sender, RoutedEventArgs e)
        {
            if(TwitchConnector.IRCStatus == Twitch.TwitchConnectionStatus.NOT_CONNECTED || TwitchConnector.IRCStatus == Twitch.TwitchConnectionStatus.DISCONNECTED)
            {
                ConnectTwitch(TwitchConnector.IRCStatus == Twitch.TwitchConnectionStatus.DISCONNECTED);
            }
            else if(TwitchConnector.IRCStatus == Twitch.TwitchConnectionStatus.CONNECTED && !Properties.Settings.Default.AutoJoin)
            {
                TwitchConnector.JoinChannel(Properties.Settings.Default.ChannelName);
            }
            else
            {
                TwitchConnector.Disconnect();
            }
            
        }
    }
}
