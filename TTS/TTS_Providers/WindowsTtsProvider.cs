using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BespokeFusion;
using Microsoft.Win32;
using NAudio.Wave;
using SpeechLib;
using TTS_Chan.Database;
using TTS_Chan.Twitch;
using static System.String;

namespace TTS_Chan.TTS.TTS_Providers
{
    public class WindowsTtsProvider: ITtsProvider
    {
        public const string Name = "Microsoft";
        public async Task Initialize()
        {
            await Task.Factory.StartNew(PerformTtsImport);
        }
        
        private static void PerformTtsImport()
        {
            var src = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens");
            var dst = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Speech\Voices\Tokens");
            var dst32 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\SPEECH\Voices\Tokens");
            if (src == null || dst == null || dst32 == null)
            {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    MaterialMessageBox.ShowError(
                        "One or more TTS component are configured incorrectly. \nPlease configure windows TTS in order to proceed");
                });
                System.Windows.Forms.Application.Exit();
                return;
            }
            if (!src.GetSubKeyNames().Except(dst.GetSubKeyNames()).Any() && !src.GetSubKeyNames().Except(dst32.GetSubKeyNames()).Any()) return;
            var results = MessageBoxResult.None;
            Application.Current.Dispatcher.Invoke(delegate {
                var msg = new CustomMaterialMessageBox
                {
                    TxtMessage = { Text = "TTS-Chan has detected new Microsoft TTS voices in your system. Would you like to perform automatic import?" },
                    TxtTitle = { Text = "Voices import" },
                    BtnOk = { Content = "Yes" },
                    BtnCancel = { Content = "No" }
                };
                msg.Show();
                results = msg.Result;
            });
            if (results != MessageBoxResult.OK) {return;}
            PerformRegistryCopy(src, new[] {dst, dst32});
        }

        private static void PerformRegistryCopy(RegistryKey src, RegistryKey[] destinations)
        {
            var tempFilename = Path.GetTempPath() + Guid.NewGuid() + ".reg";
            using (var exportProc = new Process())
            {
                exportProc.StartInfo.FileName = "reg.exe";
                exportProc.StartInfo.UseShellExecute = false;
                exportProc.StartInfo.RedirectStandardOutput = true;
                exportProc.StartInfo.RedirectStandardError = true;
                exportProc.StartInfo.CreateNoWindow = true;
                exportProc.StartInfo.Arguments = "export \"" + src.Name + "\" \"" + tempFilename + "\" /y";
                exportProc.Start();
                exportProc.StandardOutput.ReadToEnd();
                exportProc.StandardError.ReadToEnd();
                exportProc.WaitForExit();
            }
            var originalExportLines = File.ReadAllLines(tempFilename);
            var newExportText = originalExportLines[0] + "\n";
            var originalExportText = Join('\n', originalExportLines.Skip(1));
            newExportText = destinations.Aggregate(newExportText, (current, destination) => current + originalExportText.Replace(src.Name, destination.Name) + "\n");
            var generatedFilename = Path.GetTempPath() + Guid.NewGuid() + ".reg";
            File.WriteAllText(generatedFilename, newExportText);
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "regedit.exe";
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.Verb = "runas";
                proc.StartInfo.Arguments = "/s \"" + generatedFilename + "\" /y";
                proc.Start();
                proc.WaitForExit();
            }
            File.Delete(generatedFilename);
            File.Delete(tempFilename);
        }
        public string GetProviderName()
        {
            return Name;
        }

        public List<string> GetVoices()
        {
            var synth = new SpVoice();
            var voices = synth.GetVoices();
            var voicesList = voices.Cast<SpObjectToken>().Select(voiceToken => voiceToken.GetAttribute("Name")).ToList();
            Marshal.ReleaseComObject(voices);
            Marshal.ReleaseComObject(synth);
            return voicesList;
        }
        
        public async Task<TtsEntry> MakeEntry(TwitchMessage message, UserVoice voice)
        {
            return await Task.Factory.StartNew(() =>
            {
                // Yoinked from https://stackoverflow.com/a/47956034
                // ReSharper disable BitwiseOperatorOnEnumWithoutFlags
                const SpeechVoiceSpeakFlags speechFlags = SpeechVoiceSpeakFlags.SVSFlagsAsync | SpeechVoiceSpeakFlags.SVSFIsXML | SpeechVoiceSpeakFlags.SVSFParseSsml;
                // ReSharper restore BitwiseOperatorOnEnumWithoutFlags
                var spVoice = new SpVoice();
                var wave = new SpMemoryStream();
                var voices = spVoice.GetVoices();
                try
                {
                    spVoice.Volume = 100;
                    var useVoice = voices.Cast<SpObjectToken>()
                        .FirstOrDefault(voiceToken => voiceToken.GetAttribute("Name") == voice.VoiceName);
                    if (useVoice == null)
                    {
                        useVoice = voices.Item(0);
                        MainWindow.Instance.AddLog($"Using fallback voice for {message.Username}");
                    }
                    spVoice.Voice = useVoice;
                    wave.Format.Type = SpeechAudioFormatType.SAFT48kHz16BitMono;
                    spVoice.AudioOutputStream = wave;
                    var pitch = Math.Clamp(voice.Pitch / 10, -10, 10);
                    var cultureInfo = new CultureInfo(int.Parse(useVoice.GetAttribute("Language"), NumberStyles.HexNumber));
                    var speakXml =
                        "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\">" +
                        $"<voice name=\"{voice.VoiceName}\" xml:lang=\"{cultureInfo.Name}\">" +
                        $"<prosody rate=\"{Math.Pow(0.0001125 * voice.Rate, 2) + 0.01875 * voice.Rate + 1.000}\" pitch=\"{(pitch >= 0 ? "+" + pitch : pitch )}st\">{SecurityElement.Escape(message.SpeakableText)}</prosody>" +
                        "</voice>" +
                        "</speak>";
                    spVoice.Speak(speakXml, speechFlags);
                    spVoice.WaitUntilDone(Timeout.Infinite);
                    
                    var bytes = (byte[]) wave.GetData();
                    IWaveProvider provider = new RawSourceWaveStream(bytes, 0, bytes.Length, new WaveFormat(48000, 16, 1));
                    var entry = new TtsEntry(provider);
                    return entry;
                }
                finally
                {
                    Marshal.ReleaseComObject(voices);
                    Marshal.ReleaseComObject(wave);
                    Marshal.ReleaseComObject(spVoice);
                }
            });
        }
    }
}