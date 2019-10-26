namespace WorldManager
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Type extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Run bash command
        /// </summary>
        /// <param name="cmd">Bash command</param>
        /// <param name="parameters">Command parameters</param>
        /// <param name="onData">On data receive</param>
        public static void Bash(this string cmd, string parameters, Action<object, DataReceivedEventArgs> onData = null)
        {
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = cmd,
                    Arguments = parameters,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            if (onData != null)
            {
                process.ErrorDataReceived += (sender, e) => onData(sender, e);
                process.OutputDataReceived += (sender, e) => onData(sender, e);
            }

            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
        }

        /// <summary>
        /// Run bash script
        /// </summary>
        /// <param name="cmd">Bash script</param>
        /// <param name="onData">On data receive</param>
        public static void BashScript(this string cmd, Action<object, DataReceivedEventArgs> onData = null)
        {
            string escapedArgs = cmd.Replace("\"", "\\\"");

            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            if (onData != null)
            {
                process.ErrorDataReceived += (sender, e) => onData(sender, e);
                process.OutputDataReceived += (sender, e) => onData(sender, e);
            }

            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
        }
    }
}