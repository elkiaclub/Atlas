namespace WorldManager
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using WS = WebSocketSharp;

    /// <summary>
    /// Main class
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Render console output
        /// </summary>
        private static readonly List<string> RenderAppOutput = new List<string>();

        /// <summary>
        /// Application settings
        /// </summary>
        private static Settings appSettings;

        /// <summary>
        /// Web socket client
        /// </summary>
        private static WS.WebSocket client = null;

        /// <summary>
        /// Render world counter
        /// </summary>
        private static int worldCounter = 0;

        /// <summary>
        /// Program entry point
        /// </summary>
        /// <param name="args">Program arguments</param>
        public static void Main(string[] args)
        {
            if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0]))
            {
                Console.WriteLine("Bad argument!");
                Environment.Exit(-1);
            }

            Program.appSettings = Settings.Load();

            // Save log on crash
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                try
                {
                    using (FileStream file = File.Create(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "log.txt")))
                    {
                        if (RenderAppOutput != null)
                        {
                            RenderAppOutput.ForEach(line => file.Write(line.ToCharArray().Cast<byte>().ToArray()));
                        }

                        file.Write(e.ExceptionObject.ToString().ToCharArray().Cast<byte>().ToArray());
                    }
                }
                catch (Exception ex)
                {
                    // Fatal error
                    ex.ToString();
                }
            };

            try
            {
                Program.client = new WS.WebSocket(Program.appSettings.WebsocketServerAddress);
                Program.client.Connect();
            }
            catch (Exception ex)
            {
                Program.client = null;
                ex.ToString();
            }

            // Download world
            Stopwatch watch = new Stopwatch();
            watch.Start();

            Program.SendStatus("Initializing...", 0.0, string.Empty);
            Console.WriteLine();
            Program.DownloadWorld(args[0]);

            // DEBUG
            Console.WriteLine();
            Console.WriteLine("[DEBUG] Download time: " + watch.Elapsed.ToString(@"hh\:mm\:ss"));
            watch.Restart();

            // Start render
            Program.SendStatus("Initializing world rendering...", 25.0, string.Empty);
            "unbuffer".Bash("mapcrafter -c " + Program.appSettings.MapCrafterConfig + " -j " + Program.appSettings.MapcrafterCores, Program.ConsoleOut);

            // DEBUG
            Console.WriteLine();
            Console.WriteLine("[DEBUG] Render time: " + watch.Elapsed.ToString(@"hh\:mm\:ss"));
            watch.Restart();

            // Upload render
            Program.SendStatus("Preparing for upload...", 75.0, string.Empty);
            Console.WriteLine();
            Program.UploadRender();

            // DEBUG
            Console.WriteLine();
            Console.WriteLine("[DEBUG] Upload time: " + watch.Elapsed.ToString(@"hh\:mm\:ss"));
            watch.Stop();

            // Everything done
            Program.SendStatus("Done...", 100.0, string.Empty);
        }

        /// <summary>
        /// Read console output from Map crafter
        /// </summary>
        /// <param name="sender">Running process</param>
        /// <param name="e">Console data</param>
        private static void ConsoleOut(object sender, DataReceivedEventArgs e)
        {
            if (e == null || e.Data == null)
            {
                return;
            }

            string line = e.Data.Trim();

            if (Program.RenderAppOutput.Any() && line.StartsWith("[") && !Program.RenderAppOutput.Last().StartsWith("["))
            {
                Console.WriteLine();
                Program.worldCounter++;
            }

            if (line.StartsWith("["))
            {
                // Parse progress
                string[] values = line.Split(" ", StringSplitOptions.RemoveEmptyEntries).Select(item => item.Replace("-nan", "100.0")).ToArray();

                string percent = values.First(item => item.Contains("%"));
                int valueStartIndex = Array.IndexOf(values, percent);
                double percentValue = double.Parse(percent.Replace("%", string.Empty), NumberStyles.Any, CultureInfo.InvariantCulture);

                double offset = 25.0 + ((50.0 / (double)Program.appSettings.WorldCount) * (Program.worldCounter - 1));
                double status = offset + ((percentValue / 100.0) * (50.0 / (double)Program.appSettings.WorldCount));
                Program.SendStatus("Rendering world...", status, values[valueStartIndex + 2]);
            }

            if (line.Length > 5)
            {
                Program.RenderAppOutput.Add(line);
            }
        }

        /// <summary>
        /// Download world from server
        /// </summary>
        /// <param name="remotePath">Remote path</param>
        private static void DownloadWorld(string remotePath)
        {
            // Delete old world files
            Directory.Delete(Program.appSettings.WorldFolder, true);

            if (!Directory.Exists(Program.appSettings.WorldFolder))
            {
                Directory.CreateDirectory(Program.appSettings.WorldFolder);
            }

            Action<object, DataReceivedEventArgs> update = new Action<object, DataReceivedEventArgs>((sender, e) =>
            {
                if (e == null || string.IsNullOrWhiteSpace(e.Data))
                {
                    return;
                }

                string[] values = e.Data.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                string speed = values.FirstOrDefault(value => value.Contains("/s"));
                string progressText = values.FirstOrDefault(value => value.Contains('%'));

                if (!string.IsNullOrWhiteSpace(progressText))
                {
                    double progress = 0.0;
                    string valueText = progressText.Trim().Remove(progressText.Length - 1);

                    if (!double.TryParse(valueText, out progress))
                    {
                        int progressInt = 0;
                        int.TryParse(valueText, out progressInt);
                        progress = progressInt;
                    }

                    Program.SendStatus("Downloading world...", 25.0 * (progress / 100.0), string.IsNullOrWhiteSpace(speed) ? string.Empty : speed.Trim());
                }
            });

            ("unbuffer sshpass -p '" + Program.appSettings.RemotePassword + "' rsync -Iazt --no-p --no-g --no-i-r --info=progress2 --recursive " +
            Program.appSettings.RemoteName + "@" + Program.appSettings.RemoteAddress + ":" + remotePath + "/* " + Program.appSettings.WorldFolder + "/").BashScript(update);
        }

        /// <summary>
        /// Send status to server
        /// </summary>
        /// <param name="message">Current state name</param>
        /// <param name="progress">Current progress</param>
        /// <param name="otherInfo">Other info</param>
        /// <param name="eventName">Event name</param>
        private static void SendStatus(string message, double progress, string otherInfo, string eventName = "progressUpdate")
        {
            string eventMessage = new SocketEvent
            {
                Event = eventName,
                Data = new SocketStatus
                {
                    Message = message,
                    Progress = progress,
                    Other = otherInfo
                }
            }.ToJson();

            // Send to server
            Program.WriteToSockets(eventMessage);

            // Console out
            Console.Write("\r" + new string(' ', Console.WindowWidth));
            Console.Write("\r" + message + " - " + progress.ToString("0.0") + "% - " + otherInfo);
        }

        /// <summary>
        /// Upload render to server
        /// </summary>
        private static void UploadRender()
        {
            Action<object, DataReceivedEventArgs> update = new Action<object, DataReceivedEventArgs>((sender, e) =>
            {
                if (e == null || string.IsNullOrWhiteSpace(e.Data))
                {
                    return;
                }

                string[] values = e.Data.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                string speed = values.FirstOrDefault(value => value.Contains("/s"));
                string progressText = values.FirstOrDefault(value => value.Contains('%'));

                if (!string.IsNullOrWhiteSpace(progressText))
                {
                    double progress = 0.0;
                    string valueText = progressText.Trim().Remove(progressText.Length - 1);

                    if (!double.TryParse(valueText, out progress))
                    {
                        int progressInt = 0;
                        int.TryParse(valueText, out progressInt);
                        progress = progressInt;
                    }

                    Program.SendStatus("Uploading world...", 75.0 + (25.0 * (progress / 100.0)), string.IsNullOrWhiteSpace(speed) ? string.Empty : speed.Trim());
                }
            });

            ("unbuffer sshpass -p '" + Program.appSettings.RemotePassword + "' rsync -Iazt --delete-before --force --no-i-r --info=progress2 --recursive " + Program.appSettings.RenderFolder + "/* " +
            Program.appSettings.RemoteName + "@" + Program.appSettings.RemoteAddress + ":" + Program.appSettings.RemoteRenderFolder + "/").BashScript(update);
        }

        /// <summary>
        /// Write message to Web sockets
        /// </summary>
        /// <param name="message">The message</param>
        private static void WriteToSockets(string message)
        {
            try
            {
                if (Program.client != null && Program.client.ReadyState == WS.WebSocketState.Open)
                {
                    CancellationTokenSource source = new CancellationTokenSource();
                    CancellationToken token = source.Token;
                    Program.client.Send(message.Select(letter => (byte)letter).ToArray());
                    source.Dispose();
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }
    }
}