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
    }
}