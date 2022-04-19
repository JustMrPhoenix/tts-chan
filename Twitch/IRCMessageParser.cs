using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace TTS_Chan.Twitch
{
    public class IrcMessageParser
    {
        public readonly Dictionary<string, string> Tags = new();
        public IrcMessageSource Source { get; } = new();
        public string Command { get; private set; }
        public string[] Parameters { get; private set; }

        private readonly string _ircLine;
        public IrcMessageParser(string ircLine)
        {
            _ircLine = ircLine;
            Parse();
        }

        private void Parse()
        {
            var messageBuffer = _ircLine;
            if (messageBuffer.StartsWith("@"))
            {
                var tags = ReadUntil(ref messageBuffer, " ");
                tags = tags[1..];
                var tagSplit = tags.Split(';');
                foreach (var tag in tagSplit)
                {
                    var tagKeyVal = tag.Split('=');
                    Tags.Add(tagKeyVal[0], tagKeyVal[1]);
                }
            }
            if (messageBuffer.StartsWith(":"))
            {
                var sourceStr = ReadUntil(ref messageBuffer, " ");
                sourceStr = sourceStr[1..];
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
            var paramsStr = ReadUntil(ref messageBuffer, ":").Trim();
            if (paramsStr.Contains(' '))
            {
                var parameters = paramsStr.Split(' ');
                parameters = parameters.Concat(new[] { messageBuffer }).ToArray();
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

        private static string ReadUntil(ref string input, string util)
        {
            var charLocation = input.IndexOf(util, StringComparison.Ordinal);

            var result = input;

            if (charLocation > 0)
            {
                result = input[..charLocation];
                input = input[(charLocation + 1)..];
                return result;
            }

            input = "";
            return result;
        }
    }

    public class IrcMessageSource
    {
        public string Nick;
        public string Username;
        public string Host;
    }

#pragma warning disable IDE0079 // Remove unnecessary suppression
    [SuppressMessage("ReSharper", "InconsistentNaming")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
    public static class IrcCommands
    {
        public const string PRIVSMG = "PRIVMSG";
        public const string PING = "PING";
        public const string PONG = "PONG";
        public const string JOIN = "JOIN";
        public const string RPL_WELCOME = "001";
        public const string RPL_ENDOFMOTD = "376";
    }
}
