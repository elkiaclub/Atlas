namespace WorldManager
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using SSH = Renci.SshNet;

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
        /// Download directory from <see cref="SSH.SftpClient"/>
        /// </summary>
        /// <param name="client">SFTP client</param>
        /// <param name="localPath">Local path</param>
        /// <param name="remotePath">Remote path</param>
        /// <param name="tasks">All started tasks</param>
        /// <param name="fileDownloaded">File was downloaded</param>
        public static void DownloadDirectory(this SSH.SftpClient client, string localPath, string remotePath, List<Task> tasks, Action fileDownloaded = null)
        {
            foreach (SSH.Sftp.SftpFile file in client.ListDirectory(remotePath).OrderBy(item => item.IsDirectory))
            {
                if (!file.IsDirectory && !file.IsSymbolicLink)
                {
                    // Start download file task
                    tasks.Add(
                        Task.Factory.StartNew(
                            () =>
                            {
                                client.DownloadFile(file, localPath);

                                if (fileDownloaded != null)
                                {
                                    fileDownloaded();
                                }
                            },
                            TaskCreationOptions.LongRunning));

                    while (tasks.Count(task => !task.IsCompleted) >= client.ConnectionInfo.MaxSessions)
                    {
                        // Wait here if we are downloading too many files at once
                    }
                }
                else if (file.Name != "." && file.Name != ".." && !file.IsSymbolicLink)
                {
                    Task.WhenAll(tasks).GetAwaiter().GetResult();
                    DirectoryInfo dir = Directory.CreateDirectory(Path.Combine(localPath, file.Name));
                    client.DownloadDirectory(dir.FullName, file.FullName, tasks, fileDownloaded);
                }
            }
        }

        /// <summary>
        /// Download file from <see cref="SSH.SftpClient"/>
        /// </summary>
        /// <param name="client">SFTP client</param>
        /// <param name="file">File to download</param>
        /// <param name="directory">File directory</param>
        public static void DownloadFile(this SSH.SftpClient client, SSH.Sftp.SftpFile file, string directory)
        {
            string path = Path.Combine(directory, file.Name);
            DateTime date = client.GetLastWriteTimeUtc(file.FullName);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (Stream fileStream = File.OpenWrite(path))
            {
                client.DownloadFile(file.FullName, fileStream);
            }

            File.SetLastWriteTimeUtc(path, date);
        }

        /// <summary>
        /// Get count of sub directories in current directory
        /// </summary>
        /// <param name="client">SFTP client</param>
        /// <param name="remotePath">Remote path</param>
        /// <returns>Number of sub directories</returns>
        public static long GetDirectoryTreeFileCount(this SSH.SftpClient client, string remotePath)
        {
            long total = 0;

            foreach (SSH.Sftp.SftpFile file in client.ListDirectory(remotePath))
            {
                if (file.IsDirectory && file.Name != "." && file.Name != ".." && !file.IsSymbolicLink)
                {
                    total += client.GetDirectoryTreeFileCount(file.FullName);
                }
                else if (!file.IsDirectory && !file.IsSymbolicLink)
                {
                    total++;
                }
            }

            return total;
        }

        /// <summary>
        /// Upload directory to server
        /// </summary>
        /// <param name="client">SFTP client</param>
        /// <param name="localPath">Local path</param>
        /// <param name="remotePath">Remote path</param>
        /// <param name="tasks">All started tasks</param>
        /// <param name="fileUploaded">File was uploaded</param>
        public static void UploadDirectory(this SSH.SftpClient client, string localPath, string remotePath, List<Task> tasks, Action fileUploaded = null)
        {
            IEnumerable<FileSystemInfo> infos = new DirectoryInfo(localPath).EnumerateFileSystemInfos();

            foreach (FileSystemInfo info in infos)
            {
                if (info.Attributes.HasFlag(FileAttributes.Directory))
                {
                    string subPath = remotePath + "/" + info.Name;

                    if (!client.Exists(subPath))
                    {
                        client.CreateDirectory(subPath);
                    }

                    Task.WhenAll(tasks).GetAwaiter().GetResult();
                    client.UploadDirectory(info.FullName, remotePath + "/" + info.Name, tasks, fileUploaded);
                }
                else
                {
                    // Start upload file task
                    tasks.Add(
                        Task.Factory.StartNew(
                            () =>
                            {
                                using (Stream fileStream = new FileStream(info.FullName, FileMode.Open))
                                {
                                    client.UploadFile(fileStream, remotePath + "/" + info.Name, true);
                                }

                                if (fileUploaded != null)
                                {
                                    fileUploaded();
                                }
                            },
                            TaskCreationOptions.LongRunning));

                    while (tasks.Count(task => !task.IsCompleted) >= client.ConnectionInfo.MaxSessions)
                    {
                        // Wait here if we are uploading too many files at once
                    }
                }
            }
        }
    }
}