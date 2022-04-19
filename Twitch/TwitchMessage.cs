using System;
using System.Collections.Generic;

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
        public string Username { get; } = "justinfan";
        public string DisplayName { get; }
        public string Userid { get; }
        private Dictionary<string, string> Tags { get; }
        public string Text { get; }
        public string Color { get; }

        public TwitchMessage(IrcMessageParser parser)
        {
            if (parser.Command != IrcCommands.PRIVSMG)
            {
                throw new Exception("Invalid message type");
            }
            Text = parser.Parameters[^1];
            Tags = parser.Tags;
            DisplayName = Tags["display-name"];
            Userid = Tags["user-id"];
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
    }
}
