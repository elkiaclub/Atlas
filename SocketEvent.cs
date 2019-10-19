namespace WorldManager
{
    using System;
    using JSON = Newtonsoft.Json;

    /// <summary>
    /// Web socket event
    /// </summary>
    public class SocketEvent
    {
        /// <summary>
        /// Gets or sets data
        /// </summary>
        [JSON.JsonProperty("data")]
        public object Data { get; set; }

        /// <summary>
        /// Gets or sets event name
        /// </summary>
        [JSON.JsonProperty("event")]
        public string Event { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets message ID
        /// </summary>
        [JSON.JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Convert to JSON string
        /// </summary>
        /// <returns>JSON string</returns>
        public string ToJson()
        {
            return JSON.JsonConvert.SerializeObject(this, JSON.Formatting.Indented);
        }
    }
}