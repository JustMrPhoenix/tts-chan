using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTS_Chan.Twitch
{
    /*
    @badge-info=subscriber/2
    badges=subscriber/2
    color=#00FF7F
    display-name=SlayinBS_
    emotes = emotesv2_4d85616954914203b35537e80dcca84e:7-14
    first-msg=0
    flags=
    id=5788c705-a73d-49fe-951f-c0431f1d53e4
    mod = 0
    room-id=36332302
    subscriber=1
    tmi-sent-ts=1649857673817
    turbo=0
    user-id=752265894
    user-type= :slayinbs_!slayinbs_ @slayinbs_.tmi.twitch.tv PRIVMSG #mizuz :5 gold mizuzYay
    */
    public class TwitchMessage
    {
        public string Username { get; private set; } = "justinfan";
        public string Displayname { get; private set; } = "justinfan";
        public string Userid { get; private set; } = "UNKNOWN";
        public Dictionary<string, string> Tags { get; private set; }
        public string Text { get; private set; } = "Kappa";
        public string Color { get; private set; } = "#FFFFFF";

        public TwitchMessage(IRCMessageParser parser)
        {
            if (parser.Command != IRCCommands.PRIVSMG)
            {
                throw new Exception("Invalid message type");
            }
            Text = parser.Parameters[^1];
            Tags = parser.Tags;
            Displayname = Tags["display-name"];
            Userid = Tags["user-id"];
            Color = Tags["color"];
        }
    }
}
