﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TTS_Chan.TTS;

namespace TTS_Chan.Twitch
{
    public enum TwitchConnectionStatus
    {
        NotConnected, Connecting, Connected, Joined, Reconnecting, Error, Disconnected
    }
    public delegate void ConnectionStatusChanged(TwitchConnectionStatus status, string message = null);
    public sealed class TwitchConnector
    {
        public event ConnectionStatusChanged IrcStatusChanged;

        private const string IrcUri = "wss://irc-ws.chat.twitch.tv:443";
        private string _loginUsername = "justinfan1337";
        private CancellationToken _cancellationToken;
        private CancellationTokenSource _cancellationSource;
        private string _joinedChannel;
        private Credential _authCredential;

        public ClientWebSocket IrcSocket { get; private set; }
        public ClientWebSocket PubSubWebsocket { get; private set; }
        public TwitchConnectionStatus IrcStatus { get; private set; } = TwitchConnectionStatus.NotConnected;
        public TwitchConnectionStatus PubSubStatus { get; private set; } = TwitchConnectionStatus.NotConnected;
        public static readonly List<string> KnownUsernames;

        private readonly MainWindow _mainWindow;

        public TwitchConnector(MainWindow mainWindow)
        {
            IrcSocket = new ClientWebSocket();
            PubSubWebsocket = new ClientWebSocket();
            _mainWindow = mainWindow;
            _cancellationSource = new CancellationTokenSource();
            _cancellationToken = _cancellationSource.Token;
            if (!Properties.Settings.Default.HasAuth) return;
            _authCredential = CredentialManager.ReadCredential(CredentialManager.TwitchAuthName);
            if (_authCredential != null)
            {
                _loginUsername = _authCredential.UserName;
            }
        }

        static TwitchConnector()
        {
            KnownUsernames = new List<string>();
        }

        public async Task<bool> CheckAuth(Credential credential = null)
        {
            if(credential == null)
            {
                try
                {
                    credential = CredentialManager.ReadCredential(CredentialManager.TwitchAuthName);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            var request = (HttpWebRequest) WebRequest.Create("https://api.twitch.tv/helix/users");
            request.Method = WebRequestMethods.Http.Get;
            request.ContentType = "application/json; charset=utf-8";
            request.Headers["Authorization"] = "Bearer " + credential.Password;
            request.Headers["Client-Id"] = "nigte614qq67asu42axhhw0q7hje1q";
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)await request.GetResponseAsync();
            }
            catch (Exception)
            {
                return false;
            }
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return false;
            }
            using (var stream = response.GetResponseStream())
            using (StreamReader reader = new(stream))
            {
                var responseBody = await reader.ReadToEndAsync();
                var o = JObject.Parse(responseBody);
                var data = (JArray)o["data"];
                var userRepresentation = (JObject)data?[0];
                var login = userRepresentation?.Value<string>("login");
                CredentialManager.WriteCredential(CredentialManager.TwitchAuthName, login, credential.Password);
                _authCredential = credential;
                _loginUsername = login;
            }
            return true;
        }

        public async Task Connect(bool isReconnect = false)
        {
            if(isReconnect)
            {
                IrcSocket = new ClientWebSocket();
                PubSubWebsocket = new ClientWebSocket();
            }
            _cancellationSource = new CancellationTokenSource();
            _cancellationToken = _cancellationSource.Token;
            ChangeConnectionStatus(isReconnect ? TwitchConnectionStatus.Reconnecting : TwitchConnectionStatus.Connecting);
            await IrcSocket.ConnectAsync(new Uri(IrcUri), _cancellationToken);
            ChangeConnectionStatus(TwitchConnectionStatus.Connected);
            _ = IrcPingTask();
            _ = IrcReadTask();
        }

        public async Task Authorize()
        {
            if(Properties.Settings.Default.HasAuth && _authCredential != null)
            {
                await SendMessageToWebsocket(IrcSocket, "PASS oauth:" + _authCredential.Password);
            }
            await SendMessageToWebsocket(IrcSocket, "NICK " + _loginUsername);
            await SendMessageToWebsocket(IrcSocket, "CAP REQ :twitch.tv/tags");
        }

        public async Task JoinChannel(string channelName)
        {
            await SendMessageToWebsocket(IrcSocket, "JOIN #" + channelName.ToLower());
        }

        private async Task SendMessageToWebsocket(ClientWebSocket webSocket, string message)
        {
            await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, _cancellationToken);
        }

        private async Task<string> ReadFromWebsocket(ClientWebSocket webSocket, int bufferSize = 2048)
        {
            var buffer = new byte[bufferSize];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new Exception("Connection closed");
            }
            return Encoding.UTF8.GetString(buffer, 0, result.Count);
        }

        private async Task IrcPingTask()
        {
            while(!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await SendMessageToWebsocket(IrcSocket, "PING");
                }
                catch (Exception)
                {
                    // ignored
                }

                await Task.Delay(5000, _cancellationToken);
            }
        }

        private async Task IrcReadTask()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                string data;
                try
                {
                    data = await ReadFromWebsocket(IrcSocket);
                } catch (Exception)
                {
                    if(_cancellationToken.IsCancellationRequested)
                    {
                        ChangeConnectionStatus(TwitchConnectionStatus.Disconnected);
                    }
                    else
                    {
                        _cancellationSource.Cancel();
                        ChangeConnectionStatus(TwitchConnectionStatus.Error);
                        ChangeConnectionStatus(TwitchConnectionStatus.Error, "Reconnecting in 5 seconds....");
                        await Task.Delay(5000, CancellationToken.None);
                        _cancellationSource = new CancellationTokenSource();
                        _cancellationToken = _cancellationSource.Token;
                        await Connect(true);
                        await Authorize();
                        if (_joinedChannel != null)
                        {
                            await JoinChannel(_joinedChannel);
                        }
                    }
                    return;
                }
                
                var messages = data.Split("\r\n");
                foreach (var message in messages)
                {
                    if (message == "") { continue; }
                    IrcMessageParser parser = new(message);
                    await HandleIrcMessage(parser);
                }
            }
        }

        private async Task HandleIrcMessage(IrcMessageParser message)
        {
            switch (message.Command)
            {
                case IrcCommands.PING:
                    await SendMessageToWebsocket(IrcSocket, "PONG");
                    break;

                case IrcCommands.PRIVSMG:
                    await OnPRIVSMG(message);
                    break;

                case IrcCommands.RPL_ENDOFMOTD:
                    ChangeConnectionStatus(TwitchConnectionStatus.Connected, "Auth successful as " + message.Parameters[0]);

                    #region DebugMessages

#if DEBUG
                    //await HandleIrcMessage(new IrcMessageParser(
                    //    "@badge-info=;badges=;client-nonce=67ec4d63b4996a9d33da8badacf17601;color=#FF69B4;display-name=素人若い女の子;emotes=;first-msg=0;flags=;id=6fe09cf6-1ec5-47b1-929d-f135e21aa2da;mod=0;room-id=728884633;subscriber=0;tmi-sent-ts=1650223131088;turbo=0;user-id=51857679;user-type= :justmrphoenix!justmrphoenix@justmrphoenix.tmi.twitch.tv PRIVMSG #pex_is_cute :Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse semper semper ante. Nunc id elit fermentum, tincidunt quam vitae, congue massa. Aliquam eget felis non turpis bibendum iaculis. Mauris vitae iaculis ex, fringilla efficitur felis. Aenean non diam in libero varius commodo. Proin tincidunt porttitor magna in lobortis. Nam lobortis massa quis tellus consectetur, tempor maximus dui commodo. Donec ut nisl egestas, ultricies magna quis, egestas ante. Maecenas tempor ut eros ut semper. Fusce tempus, quam non vestibulum sagittis, nunc arcu tincidunt dui, eu aliquam tellus nunc vitae dolor. Sed gravida, lacus et malesuada bibendum, justo eros commodo elit, a dignissim neque felis nec augue. In viverra ultrices libero, et tincidunt quam pretium eu. Aliquam quam neque, tristique vitae tristique sit amet, pellentesque sed lorem. Maecenas sagittis eu ex sed porttitor. Suspendisse blandit scelerisque mauris ut accumsan."));
                    //await HandleIrcMessage(new IrcMessageParser(
                    //    "@badge-info=;badges=;client-nonce=67ec4d63b4996a9d33da8badacf17601;color=#FF69B4;display-name=素人若い女の子;emotes=;first-msg=0;flags=;id=6fe09cf6-1ec5-47b1-929d-f135e21aa2da;mod=0;room-id=728884633;subscriber=0;tmi-sent-ts=1650223131088;turbo=0;user-id=51857679;user-type= :justmrphoenix!justmrphoenix@justmrphoenix.tmi.twitch.tv PRIVMSG #pex_is_cute :good thing my internet is stable enough for ossa strim vrossaComfy"));
                    //await HandleIrcMessage(new IrcMessageParser(
                    //    "@badge-info=;badges=;client-nonce=4eec2ae146fd7bc6305270740ce6351f;color=#FF69B4;display-name=素人若い女の子;emotes=emotesv2_3361c7d006e949efbea398cfc73fea8b:3-12/emotesv2_07e98ddf13e74ab1b667b84de344b99a:14-20/emotesv2_e7dd977d9f494d5999570c9ff5ce7e9e:22-31;first-msg=0;flags=;id=257a8049-7bf2-48e5-b50f-ecceb3418a5e;mod=0;room-id=728884633;subscriber=0;tmi-sent-ts=1650912362469;turbo=0;user-id=51857679;user-type= :justmrphoenix!justmrphoenix@justmrphoenix.tmi.twitch.tv PRIVMSG #pex_is_cute :hi vrossaChad vrossaL vrossaRoll hello"));
                    //await HandleIrcMessage(new IrcMessageParser(
                    //    "@badge-info=;badges=;client-nonce=02b8f27dbc443e8f02d13afed30f0742;color=#FF69B4;display-name=素人若い女の子;emotes=301003454:8-14/emotesv2_b0c31cd7b0c54b60a53bd2a2e0178b86:19-28/emotesv2_9ad639b55bec4079a6e0228c2171f554:35-46/302490862:53-61;first-msg=0;flags=;id=3f2e6cb3-f293-48df-b1ce-b0de46c7e944;mod=0;room-id=728884633;subscriber=0;tmi-sent-ts=1650913101407;turbo=0;user-id=51857679;user-type= :justmrphoenix!justmrphoenix@justmrphoenix.tmi.twitch.tv PRIVMSG #pex_is_cute :imagine vrossaW if vrossaWalk this vrossaWiggle will vrossaSip works Stare"));
#endif

                    #endregion
                    break;

                case IrcCommands.JOIN:
                    if(message.Source.Username == _loginUsername)
                    {
                        _joinedChannel = message.Parameters[0][1..];
                        ChangeConnectionStatus(TwitchConnectionStatus.Joined, "Joined " + message.Parameters[0]);
                    }
                    break;
            }
        }

        private async Task OnPRIVSMG(IrcMessageParser message)
        {
            var msg = new TwitchMessage(message);
            msg.ApplyFilters();
            if (!KnownUsernames.Contains(msg.Username))
            {
                KnownUsernames.Add(msg.Username);
            }

            if (msg.SpeakableText == "") return;
            await TtsManager.ProcessMessage(msg);
            if (msg.SpeakableText == "") return;
            _mainWindow.Dispatcher.Invoke(() => { _mainWindow.AddMessage(msg); });
        }

        private void ChangeConnectionStatus(TwitchConnectionStatus status, string message = null)
        {
            IrcStatus = status;
            IrcStatusChanged?.Invoke(status, message);
        }

        public async Task Disconnect()
        {
            _joinedChannel = null;
            await IrcSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            _cancellationSource.Cancel();
        }
    }
}
