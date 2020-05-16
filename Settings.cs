namespace WorldManager
{
    using System.IO;
    using JSON = Newtonsoft.Json;

    /// <summary>
    /// Application settings
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Settings file path
        /// </summary>
        public static readonly string SettingsFilePath = Path.Combine(Path.GetDirectoryName(typeof(Settings).Assembly.Location), "WorldManagerSettings.json");

        /// <summary>
        /// Gets or sets Map crafter configuration file
        /// </summary>
        public string MapCrafterConfig { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets How many cores should Map crafter use
        /// </summary>
        public uint MapcrafterCores { get; set; } = 1;

        /// <summary>
        /// Gets or sets Remote address
        /// </summary>
        public string RemoteAddress { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets Remote name
        /// </summary>
        public string RemoteName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets Remote password
        /// </summary>
        public string RemotePassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets Render folder
        /// </summary>
        public string RemoteRenderFolder { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets Render folder
        /// </summary>
        public string RenderFolder { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets Web socket servers
        /// </summary>
        public string WebsocketServerAddress { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets World folder
        /// </summary>
        public string WorldFolder { get; set; } = string.Empty;

        /// <summary>
        /// Load settings
        /// </summary>
        /// <returns>Saved settings</returns>
        public static Settings Load()
        {
            if (!File.Exists(Settings.SettingsFilePath))
            {
                return new Settings().Save();
            }

            return JSON.JsonConvert.DeserializeObject<Settings>(File.ReadAllText(Settings.SettingsFilePath));
        }

        /// <summary>
        /// Save settings
        /// </summary>
        /// <returns>Saved settings</returns>
        public Settings Save()
        {
            File.WriteAllText(Settings.SettingsFilePath, JSON.JsonConvert.SerializeObject(this, JSON.Formatting.Indented));
            return this;
        }
    }
}