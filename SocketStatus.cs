namespace WorldManager
{
    using JSON = Newtonsoft.Json;

    /// <summary>
    /// Socket status event
    /// </summary>
    public class SocketStatus
    {
        /// <summary>
        /// Gets or sets state
        /// </summary>
        [JSON.JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets current render speed or up/down progress
        /// </summary>
        [JSON.JsonProperty("other")]
        public string Other { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets current progress
        /// </summary>
        [JSON.JsonProperty("progress")]
        public double Progress { get; set; }

        /// <summary>
        /// Gets or sets current rotation number
        /// </summary>
        [JSON.JsonProperty("rotationNumber")]
        public int RotationNumber { get; set; }

        /// <summary>
        /// Gets or sets total number of rotations
        /// </summary>
        [JSON.JsonProperty("rotationTotal")]
        public int RotationTotal { get; set; }

        /// <summary>
        /// Gets or sets world name
        /// </summary>
        [JSON.JsonProperty("world")]
        public string World { get; set; }

        /// <summary>
        /// Gets or sets current world number
        /// </summary>
        [JSON.JsonProperty("worldNumber")]
        public int WorldNumber { get; set; }

        /// <summary>
        /// Gets or sets total number of worlds
        /// </summary>
        [JSON.JsonProperty("worldTotal")]
        public int WorldTotal { get; set; }
    }
}