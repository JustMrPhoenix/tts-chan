using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TTS_Chan.Twitch
{
    public enum TwitchConnectionStatus
    {
        NOT_CONNECTED, CONNECTING, CONNECTED, JOINED, RECONNECTING, ERROR, DISCONNECTED
    }
    public delegate void ConnectionStatusChanged(TwitchConnectionStatus status, string? message = null);
    public class TwitchConnector
    {
        public event ConnectionStatusChanged IRCStatusChanged;
        public event ConnectionStatusChanged PubSubStatusChanged;

        private static string IRC_URI = "wss://irc-ws.chat.twitch.tv:443";
        private static string NICK = "justinfan1337";
        private CancellationToken cancellationToken;
        private CancellationTokenSource cancelationSource;
        private string joinedChannel = null;

        public ClientWebSocket IRCSocket { get; private set; }
        public ClientWebSocket PubSubWebsocket { get; private set; }
        public TwitchConnectionStatus IRCStatus { get; private set; } = TwitchConnectionStatus.NOT_CONNECTED;
        public TwitchConnectionStatus PubSubStatus { get; private set; } = TwitchConnectionStatus.NOT_CONNECTED;

        public MainWindow MainWindow;

        public TwitchConnector(MainWindow mainWindow)
        {
            IRCSocket = new ClientWebSocket();
            PubSubWebsocket = new ClientWebSocket();
            MainWindow = mainWindow;
            cancelationSource = new CancellationTokenSource();
            cancellationToken = cancelationSource.Token;
        }

        public async Task Connect(bool isReconnect = false)
        {
            if(isReconnect)
            {
                IRCSocket = new ClientWebSocket();
                PubSubWebsocket = new ClientWebSocket();
            }
            cancelationSource = new CancellationTokenSource();
            cancellationToken = cancelationSource.Token;
            ChangeConnectionStatus(isReconnect ? TwitchConnectionStatus.RECONNECTING : TwitchConnectionStatus.CONNECTING);
            await IRCSocket.ConnectAsync(new Uri(IRC_URI), cancellationToken);
            ChangeConnectionStatus(TwitchConnectionStatus.CONNECTED);
            IRCPingTask();
            IRCReadTask();
        }

        public async Task Authorize()
        {
            await SendMessageToWebsocket(IRCSocket, "NICK " + NICK);
            await SendMessageToWebsocket(IRCSocket, "CAP REQ :twitch.tv/tags");
        }

        public async Task JoinChannel(string channelName)
        {
            await SendMessageToWebsocket(IRCSocket, "JOIN #" + channelName.ToLower());
        }

        private async Task SendMessageToWebsocket(ClientWebSocket webSocket, string message)
        {
            await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, cancellationToken);
        }

        private async Task<string> ReadFromWebsocket(ClientWebSocket webSocket, int bufferSize = 2048)
        {
            byte[] buffer = new byte[bufferSize];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new Exception("Connection closed");
            }
            return Encoding.UTF8.GetString(buffer, 0, result.Count);
        }

        private async Task IRCPingTask()
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                await SendMessageToWebsocket(IRCSocket, "PING");
                await Task.Delay(5000, cancellationToken);
            }
        }

        private async Task IRCReadTask()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                string data = "";
                try
                {
                    data = await ReadFromWebsocket(IRCSocket);
                } catch (Exception ex)
                {
                    if(cancellationToken.IsCancellationRequested)
                    {
                        ChangeConnectionStatus(TwitchConnectionStatus.DISCONNECTED);
                    }
                    else
                    {
                        cancelationSource.Cancel();
                        ChangeConnectionStatus(TwitchConnectionStatus.ERROR);
                        ChangeConnectionStatus(TwitchConnectionStatus.ERROR, "Reocnnecting in 5 secodns....");
                        await Task.Delay(5000);
                        await Connect(true);
                        await Authorize();
                        if (joinedChannel != null)
                        {
                            await JoinChannel(joinedChannel);
                        }
                    }
                    return;
                }
                
                string[] messages = data.Split("\r\n");
                foreach (string message in messages)
                {
                    if (message == "") { continue; }
                    IRCMessageParser parser = new IRCMessageParser(message);
                    await HandleIRCMessage(parser);
                }
            }
        }

        private async Task HandleIRCMessage(IRCMessageParser message)
        {
            switch (message.Command)
            {
                case IRCCommands.PING:
                    await SendMessageToWebsocket(IRCSocket, "PONG");
                    break;

                case IRCCommands.PRIVSMG:
                    MainWindow.Dispatcher.Invoke(new Action(() => { MainWindow.AddMessage(new TwitchMessage(message)); }));
                    break;

                case IRCCommands.RPL_ENDOFMOTD:
                    ChangeConnectionStatus(TwitchConnectionStatus.CONNECTED, "Auth succeseful as " + message.Parameters[0]);
                    break;

                case IRCCommands.JOIN:
                    if(message.Source.Username == NICK)
                    {
                        joinedChannel = message.Parameters[0].Substring(1);
                        ChangeConnectionStatus(TwitchConnectionStatus.JOINED, "Joined " + message.Parameters[0]);
                    }
                    break;
                default:
                    break;
            }
        }

        public void ChangeConnectionStatus(TwitchConnectionStatus status, string? message = null)
        {
            IRCStatus = status;
            IRCStatusChanged?.Invoke(status, message);
        }

        public async void Disconnect()
        {
            IRCSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            cancelationSource.Cancel();
        }
    }
}
