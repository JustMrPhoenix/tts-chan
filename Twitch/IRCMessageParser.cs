using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTS_Chan.Twitch
{
    public class IRCMessageParser
    {
        public Dictionary<string, string> Tags = new Dictionary<string, string>();
        public IRCMessageSource Source { get; private set; } = new IRCMessageSource();
        public string Command { get; private set; }
        public string[] Parameters { get; private set; }

        private string ircLine;
        public IRCMessageParser(string ircLine)
        {
            this.ircLine = ircLine;
            Parse();
        }

        private void Parse()
        {
            string messageBuffer = ircLine;
            if (messageBuffer.StartsWith("@"))
            {
                string tags = ReadUntil(ref messageBuffer, " ");
                tags = tags.Substring(1);
                string[] tagSplit = tags.Split(';');
                foreach (string tag in tagSplit)
                {
                    string[] tagKeyVal = tag.Split('=');
                    Tags.Add(tagKeyVal[0], tagKeyVal[1]);
                }
            }
            if (messageBuffer.StartsWith(":"))
            {
                string sourceStr = ReadUntil(ref messageBuffer, " ");
                sourceStr.Substring(1);
                if (sourceStr.Contains("!"))
                {
                    Source.Nick = ReadUntil(ref sourceStr, "!");
                }
                if (sourceStr.Contains("@"))
                {
                    Source.Username = ReadUntil(ref sourceStr, "@");
                }
                Source.Host = sourceStr;

            }
            Command = ReadUntil(ref messageBuffer, " ");
            string paramsStr = ReadUntil(ref messageBuffer, ":").Trim();
            if (paramsStr.Contains(' '))
            {
                string[] parameters = paramsStr.Split(' ');
                parameters = parameters.Concat(new string[] { messageBuffer }).ToArray();
                Parameters = parameters;
            }
            else if (paramsStr == "")
            {
                string[] parameters = { messageBuffer };
                Parameters = parameters;
            }
            else if (messageBuffer != "")
            {
                string[] parameters = { paramsStr, messageBuffer };
                Parameters = parameters;
            }
            else
            {
                string[] parameters = { paramsStr };
                Parameters = parameters;
            }
        }

        private string ReadUntil(ref string input, string util)
        {
            int charLocation = input.IndexOf(util);

            string result = input;

            if (charLocation > 0)
            {
                result = input.Substring(0, charLocation);
                input = input.Substring(charLocation + 1);
                return result;
            }

            input = "";
            return result;
        }
    }

    public class IRCMessageSource
    {
        public string Nick;
        public string Username;
        public string Host;
    }

    public static class IRCCommands
    {
        public const string PRIVSMG = "PRIVMSG";
        public const string PING = "PING";
        public const string PONG = "PONG";
        public const string JOIN = "JOIN";
        public const string RPL_WELCOME = "001";
        public const string RPL_ENDOFMOTD = "376";
    }
}
