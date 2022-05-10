using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Navigation;
using BespokeFusion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using TTS_Chan.Database;
using TTS_Chan.TTS;
using TTS_Chan.TTS.TTS_Providers;

namespace TTS_Chan
{
    /// <summary>
    /// Interaction logic for SpeechChatImportWindow.xaml
    /// </summary>
    public partial class SpeechChatImportWindow
    {
        private static readonly Dictionary<string, string> GoogleVoicesMap = new()
        {
            {"Google Deutsch","de-DE-Standard-F"},
            {"Google US English","en-US-Standard-H"},
            {"Google UK English Female","en-GB-Standard-A"},
            {"Google UK English Male","en-GB-Standard-D"},
            {"Google español","es-ES-Standard-A"},
            {"Google español de Estados Unidos","es-US-Standard-A"},
            {"Google français","fr-FR-Standard-E"},
            {"Google हिन्दी","hi-IN-Standard-A"},
            {"Google Bahasa Indonesia","id-ID-Standard-D"},
            {"Google italiano","it-IT-Standard-A"},
            {"Google 日本語","ja-JP-Standard-A"},
            {"Google 한국의","ko-KR-Standard-A"},
            {"Google Nederlands","nl-NL-Standard-A"},
            {"Google polski","pl-PL-Standard-E"},
            {"Google português do Brasil","pt-BR-Standard-A"},
            {"Google русский","ru-RU-Standard-E"},
            {"Google 普通话（中国大陆）", "cmn-CN-Standard-D"},
            {"Google 粤語（香港）", "yue-HK-Standard-A"},
            {"Google 國語（臺灣）", "cmn-TW-Standard-A"}
        };

        public SpeechChatImportWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Hyperlink hl = (Hyperlink)sender;
            string navigateUri = hl.NavigateUri.ToString();
            var psi = new ProcessStartInfo
            {
                FileName = navigateUri,
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private void ImportFileButton_Click(object sender, RoutedEventArgs e)
        {
            using var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files (*.json)|*.json";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            var fileStream = openFileDialog.OpenFile();
            using var reader = new StreamReader(fileStream);
            var fileContent = reader.ReadToEnd();
            var missingVoices = PerformImport(fileContent);
            if (missingVoices.Count == 0)
            {
                MaterialMessageBox.Show("All users have been imported successfully!", "Speechchat import");
            }
            else
            {
                var message = missingVoices.Aggregate("Some users failed to import due to missing voices:", (current, missingVoice) => current + $"\n{missingVoice.Key} : {missingVoice.Value}");
                MaterialMessageBox.Show(message, "Speechchat import");
            }
            Close();
        }


        private Dictionary<string,string> PerformImport(string usersJson)
        {
            var missingVoices = new Dictionary<string, string>();
            var dict = JsonConvert.DeserializeObject<Dictionary<string, SpeechchatUserInfo>>(usersJson);
            foreach (var speechchatUserInfo in dict)
            {
                var entry = DatabaseManager.Context.UserVoices
                    .FirstOrDefault(uv => uv.Username == speechchatUserInfo.Key);
                var isNew = false;
                if (entry == null)
                {
                    isNew = true;
                    entry = new UserVoice
                    {
                        Username = speechchatUserInfo.Key
                    };
                }
                var info = speechchatUserInfo.Value;
                if (string.IsNullOrEmpty(info.UserVoice)) continue;

                var userVoiceProvider = "";
                foreach (var provider in TtsManager.GetProviders().Where(provider => info.UserVoice.StartsWith(provider)))
                {
                    userVoiceProvider = provider;
                }

                if (!TtsManager.GetProviders().Contains(userVoiceProvider))
                {
                    missingVoices.Add(speechchatUserInfo.Key, info.UserVoice);
                    continue;
                }

                entry.VoiceProvider = userVoiceProvider;

                var ttsProvider = TtsManager.GetProvider(userVoiceProvider);
                var usedVoice = info.UserVoice;

                switch (ttsProvider.GetProviderName())
                {
                    case GoogleTtsProvider.Name:
                        if (!GoogleVoicesMap.ContainsKey(usedVoice))
                        {
                            missingVoices.Add(speechchatUserInfo.Key, info.UserVoice);
                            continue;
                        }

                        var googleVoiceCode = GoogleVoicesMap[usedVoice];
                        usedVoice = ((GoogleTtsProvider)ttsProvider).VoicesNamed.First(vn => vn.Value.Name == googleVoiceCode).Key;
                        break;
                    case WindowsTtsProvider.Name:
                        var voiceName = Regex.Match(usedVoice, @"(Microsoft [\w ]+)[ - ]?").Groups[0].Value;
                        usedVoice = voiceName.Trim();
                        break;
                }

                if (!ttsProvider.GetVoices().Contains(usedVoice))
                {
                    missingVoices.Add(speechchatUserInfo.Key, info.UserVoice);
                    continue;
                }
                
                entry.VoiceName = usedVoice;

                if (!string.IsNullOrEmpty(info.Volume))
                {
                    entry.Volume = int.Parse(info.Volume);
                }
                if (!string.IsNullOrEmpty(info.Pitch))
                {
                    var pitchFactor = (1 + int.Parse(info.Pitch)) * 0.02;
                    var pitch = pitchFactor - 1;

                    entry.Pitch = Math.Clamp((int) (pitch * 100), -100, 100);
                }
                if (!string.IsNullOrEmpty(info.Rate))
                {
                    var rateFactor = (1 + int.Parse(info.Rate)) * 0.02;
                    var rate = rateFactor - 1;
                    entry.Rate = Math.Clamp((int)(rate * 100), -100, 100);
                }

                if (isNew)
                {
                    DatabaseManager.Context.UserVoices.Add(entry);
                }
            }

            DatabaseManager.Context.SaveChanges();
            return missingVoices;
        }
    }

    public class SpeechchatUserInfo
    {
        [JsonProperty("enable")]
        public bool Enable;

        [JsonProperty("username_voice")]
        public string UsernameVoice;

        [JsonProperty("user_voice")]
        public string UserVoice;

        [JsonProperty("volume")]
        public string Volume;

        [JsonProperty("pitch")]
        public string Pitch;

        [JsonProperty("rate")]
        public string Rate;
    }
}
