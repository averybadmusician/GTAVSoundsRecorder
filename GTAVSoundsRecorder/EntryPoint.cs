using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using Rage;
using static Rage.Native.NativeFunction;

[assembly: Rage.Attributes.Plugin("GTA V Sounds Recorder",
    Description = "",
    Author = "BadMusician")]

namespace GTAVSoundsRecorder
{
    public class EntryPoint
    {
        static int defaultMode = 1;
        static List<string> availableModes = new List<string> { "Sounds", "Speeches" };
        static string Mode = "";
        static string Raw = "";
        static int Index = 0;
        static int Total = 0;
        static bool First25 = false;
        static bool Overwrite = false;

        static string txt = "";
        static bool running = false;
        static bool stopping = false;

        static Vector3 oldPos = Vector3.Zero;
        static float oldHead = 0;

        public static void Main()
        {
            oldPos = Game.LocalPlayer.Character.Position;
            oldHead = Game.LocalPlayer.Character.Heading;
            Game.LocalPlayer.Character.Position = new Vector3(-5000, 2000, 2500);
            Game.LocalPlayer.Character.Heading = 200;
            Game.LocalPlayer.Character.IsPositionFrozen = true;
        start:
            Natives.x00DC833F2568DBF6(true, "", "", availableModes[defaultMode], "", "", "", 33);
            int res = Natives.x0CF2B696BBF945AE<int>();
            while (res == 0)
            {
                GameFiber.Yield();
                res = Natives.x0CF2B696BBF945AE<int>();
            }
            if (res == 2) return;
            Mode = Natives.x8362B09B91893647<string>();
            Game.LogTrivial($"Mode: {Mode}");
            if (!availableModes.Contains(Mode)) goto start;
            Natives.x00DC833F2568DBF6(true, "", "", $"Raw//{Mode}.txt", "", "", "", 33);
            res = Natives.x0CF2B696BBF945AE<int>();
            while (res == 0)
            {
                GameFiber.Yield();
                res = Natives.x0CF2B696BBF945AE<int>();
            }
            if (res == 2) return;
            Raw = Natives.x8362B09B91893647<string>();
            Game.LogTrivial($"Raw: {Raw}");
            First25 = YesNo("Record ~b~only first 25 files~w~?", true, false);
            Game.LogTrivial($"First25: {First25}");
            Overwrite = YesNo("Overwrite ~b~existing files~w~?", true, false);
            Game.LogTrivial($"Overwrite: {Overwrite}");
            GameFiber.StartNew(delegate
            {
                while (true)
                {
                    GameFiber.Yield();
                    Natives.xAC3A74E8384A9919(0); //SET_WIND
                    Natives.xEE09ECEDBABE47FC(0); //SET_WIND_SPEED
                    if (running && !stopping && Game.IsKeyDown(System.Windows.Forms.Keys.NumPad5))
                    {
                        stopping = true;
                        break;
                    }
                }
            });
            Game.RawFrameRender += Game_RawFrameRender;
            object[] bin = Parse();
            int count = bin.Length;
            while (true)
            {
                GameFiber.Yield();
                Game.DisplayHelp($"Mode: ~b~{Mode}~n~~w~Raw: ~b~{Raw}~w~~n~First25: ~b~{First25}~w~~n~Overwrite: ~b~{Overwrite}~w~~n~Press ~y~Num 0~w~ to start");
                if (Game.IsKeyDown(System.Windows.Forms.Keys.NumPad0)) break;
            }
            string outputFolder = $"Plugins/SoundsRecorder/{Mode}";
            Directory.CreateDirectory(outputFolder);
            int forCount = First25 ? 25 : count;
            Total = forCount;
            running = true;
            for (int i = 0; i < forCount && i < count; i++)
            {
                Index = i;
                Record(bin.GetValue(i), Mode, out string filename, out Func<DateTime, (bool invalid, object x)> play, out Action<object> after);
                string outputFilePath = Path.Combine(outputFolder, filename);
                if (File.Exists(outputFilePath))
                {
                    if (Overwrite)
                    {
                        File.Delete(outputFilePath);
                    }
                    else
                    {
                        continue;
                    }
                }
                var capture = new WasapiLoopbackCapture();
                var writer = new WaveFileWriter(outputFilePath, capture.WaveFormat);
                float maxvolume = 0;
                capture.DataAvailable += (cs, ca) =>
                {
                    writer.Write(ca.Buffer, 0, ca.BytesRecorded);

                    float max = 0;
                    var buffer = new WaveBuffer(ca.Buffer);
                    // interpret as 32 bit floating point audio
                    for (int index = 0; index < ca.BytesRecorded / 4; index++)
                    {
                        var sample = buffer.FloatBuffer[index];

                        // absolute value 
                        if (sample < 0) sample = -sample;
                        // is this the max value?
                        if (sample > max) max = sample;
                    }
                    maxvolume = max;
                };
                capture.RecordingStopped += (cs, ca) =>
                {
                    writer.Dispose();
                    writer = null;
                    capture.Dispose();
                };
                //Disable Idle Cam
                Rage.Native.NativeFunction.Natives.xF4F2C0D4EE209E20();
                Rage.Native.NativeFunction.Natives.x9E4CFFF989258472();
                capture.StartRecording();
                DateTime s = DateTime.Now;
                var playResult = play(s);
                bool invalid = playResult.invalid;
                object x = playResult.x;
                int d = (int)((DateTime.Now - s).TotalMilliseconds);
                if (d <= 25)
                {
                    d = 0;
                    invalid = true;
                }
                string log = $"";
                if (invalid)
                {
                    log = $"[{i + 1}/{count}] INVALID ({filename})";
                }
                else
                {
                    log = $"[{i + 1}/{count}] {filename} Duration: {d} Volume: {maxvolume:0.00000000}";
                }
                txt = log;
                File.AppendAllText($"Plugins/SoundsRecorder/_SoundsRecorder.log", log + Environment.NewLine);
                File.AppendAllText($"Plugins/SoundsRecorder/{Mode}.log", log + Environment.NewLine);
                if (!invalid)
                {
                    Game.LogTrivial($"[OK] [{i + 1}/{count}] {filename},{d},{maxvolume:0.00000000}");
                    File.AppendAllText($"Plugins/SoundsRecorder/{Mode}.txt", $"{filename},{d},{maxvolume:0.00000000}" + Environment.NewLine);
                }
                else
                {
                    Game.LogTrivial($"[INVALID] [{i + 1}/{count}] {filename.Replace(".wav", "")}");
                    File.AppendAllText($"Plugins/SoundsRecorder/{Mode}_Invalid.txt", $"{filename.Replace(".wav", "")}" + Environment.NewLine);
                }
                s = DateTime.Now;
                while (!invalid && (DateTime.Now - s).TotalSeconds <= 1)
                {
                    GameFiber.Yield();
                }
                after(x);
                capture.StopRecording();
                if (i + 1 % 10 == 0)
                {
                    _ = GetAsync($"https://badmusician.ru/dev/soundcheck/set/?a={i + 1}&b={count}");
                }
                if (stopping) break;
            }
            Game.LogTrivial("Done");
            Game.LocalPlayer.Character.Position = oldPos;
            Game.LocalPlayer.Character.Heading = oldHead;
            Game.LocalPlayer.Character.IsPositionFrozen = false;
            Game.LocalPlayer.Character.NeedsCollision = true;
            Game.UnloadActivePlugin();
        }

        private static void Record(object item, string mode, out string filename, out Func<DateTime, (bool tooLong, object x)> play, out Action<object> after)
        {
            int id = availableModes.IndexOf(mode);
            filename = "none";
            play = s => (false, null);
            after = x => { };
            switch (id)
            {
                case 0: //Scripting sounds
                    {
                        var Item = (string[])item;
                        filename = $"{Item[0]},{Item[1]}.wav";
                        play = s =>
                        {
                            GameSFX x = new GameSFX(Item[0], Item[1]);
                            x.Play();
                            while (x.IsPlaying)
                            {
                                GameFiber.Yield();
                                if ((DateTime.Now - s).TotalSeconds >= 15)
                                {
                                    break;
                                }
                            }
                            return (false, x);
                        };
                        after = x =>
                        {
                            (x as GameSFX).Dispose();
                        };
                        break;
                    }
                case 1: //Speeches
                    {
                        var Item = (string[])item;
                        filename = $"{Item[0]},{Item[1]},{Item[2]}.wav";
                        play = s =>
                        {
                            bool invalid = false;
                            bool started = false;
                            Rage.Native.NativeFunction.Natives.x7A73D05A607734C7(Game.LocalPlayer.Character);
                            Rage.Native.NativeFunction.Natives.xB8BEC0CA6F0EDB0F(Game.LocalPlayer.Character);
                            Game.LocalPlayer.Character.PlayAmbientSpeech(Item[0], Item[1], int.Parse(Item[2]), SpeechModifier.ForceFrontend);
                            while (true)
                            {
                                GameFiber.Yield();
                                if (Rage.Native.NativeFunction.Natives.x9072C8B49907BFAD<bool>(Game.LocalPlayer.Character) || Rage.Native.NativeFunction.Natives.x729072355FA39EC9<bool>(Game.LocalPlayer.Character))
                                {
                                    if (!started) started = true;
                                }
                                else
                                {
                                    if (started) break;
                                }
                                if (!started && (DateTime.Now - s).TotalSeconds >= 1f)
                                {
                                    invalid = true;
                                    break;
                                }
                                if ((DateTime.Now - s).TotalSeconds >= 15)
                                {
                                    break;
                                }
                            }
                            return (invalid, $"{Item[0]},{Item[1]},{Item[2]}");
                        };
                        after = x =>
                        {
                        };
                        break;
                    }
            }
        }

        private static object[] Parse()
        {
            List<object> list = new List<object> { };
            foreach (string line in File.ReadLines($"Plugins//SoundsRecorder//{Raw}")) list.Add(line.Split(','));
            return list.ToArray();
        }

        private static void Game_RawFrameRender(object sender, GraphicsEventArgs e)
        {
            if (txt != "")
            {
                e.Graphics.DrawText(txt, "Arial", 18, new System.Drawing.PointF(100, 100), System.Drawing.Color.White);
            }
            if (running)
            {
                if (stopping)
                {
                    e.Graphics.DrawText("STOPPING", "Arial", 32, new System.Drawing.PointF(100, 200), System.Drawing.Color.Red);
                }
                else
                {
                    e.Graphics.DrawText("RUNNING", "Arial", 32, new System.Drawing.PointF(100, 200), System.Drawing.Color.Green);
                    e.Graphics.DrawText($"{Index + 1} / {Total}", "Arial", 32, new System.Drawing.PointF(100, 250), System.Drawing.Color.White);
                    e.Graphics.DrawText($"NumPad 5 to STOP", "Arial", 32, new System.Drawing.PointF(100, 300), System.Drawing.Color.White);
                }
            }
        }

        public static async Task<string> GetAsync(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                Game.LogTrivial($"Web Progress: OK");
                return await reader.ReadToEndAsync();
            }
        }

        public static T YesNo<T>(string message, T yes, T no)
        {
            while (true)
            {
                GameFiber.Yield();
                Game.DisplayHelp(message + " ~w~(~g~Y~w~/~r~N~w~)");
                if (Game.IsKeyDown(System.Windows.Forms.Keys.Y)) return yes;
                if (Game.IsKeyDown(System.Windows.Forms.Keys.N)) return no;
            }
        }
    }
}
