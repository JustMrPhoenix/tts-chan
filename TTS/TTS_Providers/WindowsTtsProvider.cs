using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BespokeFusion;
using Microsoft.Win32;
using NAudio.Wave;
using SpeechLib;
using static System.String;

namespace TTS_Chan.TTS.TTS_Providers
{
    public class WindowsTtsProvider: ITtsProvider
    {
        private SpeechSynthesizer _synthesizer;
        public async Task Initialize()
        {
            await Task.Factory.StartNew(PerformTtsImport);
            _synthesizer = new SpeechSynthesizer();
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
            return "Microsoft";
        }

        public List<string> GetVoices()
        {
            return _synthesizer.GetInstalledVoices().Where(installedVoice => installedVoice.Enabled).Select(voice => voice.VoiceInfo.Name).ToList();
        }

        public async Task<TtsEntry> MakeEntry(string message, string voice, int rate, int pitch)
        {
            return await Task.Factory.StartNew(() =>
            {
                var stream = new MemoryStream();
                Application.Current.Dispatcher.Invoke(delegate
                {
                    _synthesizer.SelectVoice(voice);
                    _synthesizer.SetOutputToWaveFile(@"Z:\sas.wav", new SpeechAudioFormatInfo(44100, AudioBitsPerSample.Sixteen, AudioChannel.Mono));
                    //_synthesizer.SetOutputToAudioStream(stream,
                    //    new SpeechAudioFormatInfo(44100, AudioBitsPerSample.Sixteen, AudioChannel.Mono));
                    _synthesizer.Rate = rate / 10;
                    _synthesizer.Speak(message);
                });

                stream.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                IWaveProvider provider = new RawSourceWaveStream(stream, new WaveFormat(44100, 16, 1));
                return new TtsEntry(provider, pitch);
            });
        }

        public new static byte[] GetSound(int rate, int volume, string voiceName, string text)
        {
            const SpeechVoiceSpeakFlags speechFlags = SpeechVoiceSpeakFlags.SVSFlagsAsync;
            var synth = new SpVoice();
            var wave = new SpMemoryStream();
            var voices = synth.GetVoices();
            try
            {
                // synth setup
                synth.Volume = Math.Max(1, Math.Min(100, volume));
                synth.Rate = Math.Max(-10, Math.Min(10, rate));
                foreach (SpObjectToken voice in voices)
                {
                    if (voice.GetAttribute("Name") == voiceName)
                    {
                        synth.Voice = voice;
                    }
                }
                wave.Format.Type = SpeechAudioFormatType.SAFT22kHz16BitMono;
                synth.AudioOutputStream = wave;
                synth.Speak(text, speechFlags);
                synth.WaitUntilDone(Timeout.Infinite);

                var waveFormat = new WaveFormat(22050, 16, 1);
                using (var ms = new MemoryStream((byte[])wave.GetData()))
                using (var reader = new RawSourceWaveStream(ms, waveFormat))
                using (var outStream = new MemoryStream())
                using (var writer = new WaveFileWriter(outStream, waveFormat))
                {
                    reader.CopyTo(writer);
                    return outStream.GetBuffer();
                }
            }
            finally
            {
                Marshal.ReleaseComObject(voices);
                Marshal.ReleaseComObject(wave);
                Marshal.ReleaseComObject(synth);
            }
        }
    }
}