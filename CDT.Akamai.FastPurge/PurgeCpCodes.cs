using Newtonsoft.Json;

namespace CDT.Akamai.FastPurge
{
    public class PurgeObjects
    {
        /// <summary>
        ///     An array of CP codes to purge.
        /// </summary>
        [JsonProperty("objects")]
        public string[] Objects { get; set; }
    }
}