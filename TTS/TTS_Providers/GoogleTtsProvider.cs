using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Google.Cloud.TextToSpeech.V1;
using NAudio.Wave;
using TTS_Chan.Database;
using TTS_Chan.Twitch;

namespace TTS_Chan.TTS.TTS_Providers
{
    class GoogleTtsProvider: ITtsProvider
    {
        public const string Name = "Google";
        public const string CredentialsPath = "./google-credentials.json";
        private TextToSpeechClient _client;
        private readonly List<string> _voices = new();
        private readonly Dictionary<string, Voice> _voicesNamed = new();
        public async Task Initialize()
        {
            if (_client != null)
            {
                _voices.Clear();
                _voicesNamed.Clear();
            }
            if (!File.Exists(CredentialsPath))
            {
                MainWindow.Instance.AddLog("Google credentials not found!");
                return;
            }

            var builder = new TextToSpeechClientBuilder
            {
                JsonCredentials = await File.ReadAllTextAsync(CredentialsPath)
            };

            _client = await builder.BuildAsync();
            var googleVoices = await _client.ListVoicesAsync(new ListVoicesRequest());
            foreach (var voice in googleVoices.Voices)
            {
                var name = $"{voice.LanguageCodes[0].ToUpper()} : {Regex.Match(voice.Name, @"\w+-\w+-(.*)").Groups[1].Value} # {voice.SsmlGender}";
                _voices.Add(name);
                _voicesNamed[name] = voice;
            }
            _voices.Sort();
        }

        public string GetProviderName()
        {
            return Name;
        }

        public List<string> GetVoices()
        {
            return _voices;
        }

        public async Task<TtsEntry> MakeEntry(TwitchMessage message, UserVoice voice)
        {
            var voiceInfo = _voicesNamed[voice.VoiceName];
            var rate = Math.Pow(0.0001125 * voice.Rate, 2) + 0.01875 * voice.Rate + 1.000;
            var speechResults = await _client.SynthesizeSpeechAsync(new SynthesizeSpeechRequest
            {
                Voice = new VoiceSelectionParams
                {
                    Name = voiceInfo.Name,
                    LanguageCode = voiceInfo.LanguageCodes[0],
                    SsmlGender = voiceInfo.SsmlGender
                },
                AudioConfig = new AudioConfig
                {
                    SpeakingRate = rate,
                    AudioEncoding = AudioEncoding.Linear16,
                    SampleRateHertz = 48000,
                    Pitch = voice.Pitch / 5
                },
                Input = new SynthesisInput
                {
                    Text = message.SpeakableText
                }
            });
            
            var source = new RawSourceWaveStream(speechResults.AudioContent.ToByteArray(), 0, speechResults.AudioContent.Length, new WaveFormat(48000, 16, 1));

            var entry = new TtsEntry(source);

            return entry;
        }
    }
}
