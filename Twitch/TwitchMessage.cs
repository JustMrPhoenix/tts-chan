using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TTS_Chan.Database;

namespace TTS_Chan.Twitch
{
    public class TwitchMessage
    {
        static readonly string[] NameColors =
        {
            "Blue",
            "Coral",
            "DodgerBlue",
            "SpringGreen",
            "YellowGreen",
            "Green",
            "OrangeRed",
            "Red",
            "GoldenRod",
            "HotPink",
            "CadetBlue",
            "SeaGreen",
            "Chocolate",
            "BlueViolet",
            "Firebrick",
        };
        static readonly Dictionary<string, string> UserColors = new();
        public string Username { get; }
        public string DisplayName { get; }
        public string Userid { get; }
        private Dictionary<string, string> Tags { get; }
        public string Text { get; }
        public string Color { get; }
        public string SpeakableText;

        public TwitchMessage(IrcMessageParser parser)
        {
            if (parser.Command != IrcCommands.PRIVSMG)
            {
                throw new Exception("Invalid message type");
            }
            Text = parser.Parameters[^1];
            SpeakableText = Text;
            Tags = parser.Tags;
            Username = parser.Source.Username;
            DisplayName = Tags.ContainsKey("display-name") ? Tags["display-name"] : Username;
            Userid = Tags.ContainsKey("user-id") ? Tags["user-id"] : "UNKNOWN";
            if(Tags.ContainsKey("color") && Tags["color"] != "")
            {
                Color = Tags["color"];
            }
            else
            {
                if (UserColors.ContainsKey(Userid))
                {
                    Color = UserColors[Userid];
                }
                else
                {
                    Random random = new();
                    var randColor = random.Next(0, NameColors.Length);
                    Color = NameColors[randColor];
                    UserColors[Userid] = Color;
                }
            }
        }

        public void ApplyFilters()
        {
            var text = Text;
            if (Properties.Settings.Default.DisableTwitchEmotes && Tags.ContainsKey("emotes") && Tags["emotes"] != "")
            {
                var matches = Regex.Matches(Tags["emotes"], @"(?<emote>\w+):(?<start>\d+)-(?<end>\d+)");
                var emoteOffset = 0;
                foreach (Match match in matches)
                {
                    var start = int.Parse(match.Groups["start"].Value);
                    var end = int.Parse(match.Groups["end"].Value);
                    text = text.Remove(start - emoteOffset , end-start+1);
                    emoteOffset += end - start + 1;
                }
            }
            var substituted = MessageSubstitution.PerformAll(text);
            var words = substituted.Split(' ');
            var result = "";
            foreach (var word in words)
            {
                if (result.Length + word.Length > Properties.Settings.Default.MessageSymbolLimit)
                {
                    break;
                }

                result += ' ' + word;
            }

            SpeakableText = result.Trim();
        }
    }
}
