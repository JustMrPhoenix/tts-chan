namespace TTS_Chan.Database
{
    public class UserVoice
    {
        public int UserVoiceId { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public bool IsMuted { get; set; }
        public string VoiceProvider { get; set; }
        public string VoiceName { get; set; }
        public int Rate { get; set; }
        public int Pitch { get; set; }
    }
}
