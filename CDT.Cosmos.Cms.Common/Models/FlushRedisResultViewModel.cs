using System.Collections.Generic;

namespace CDT.Cosmos.Cms.Common.Models
{
    /// <summary>
    ///     Redis cache flush result
    /// </summary>
    public class FlushRedisResultViewModel
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public FlushRedisResultViewModel()
        {
            CacheConnected = false;
            UrlPath = string.Empty;
            Keys = new List<string>();
        }

        /// <summary>
        ///     Indicates that Redis is connected and in use
        /// </summary>
        public bool CacheConnected { get; set; }

        /// <summary>
        ///     URL to flush
        /// </summary>
        public string UrlPath { get; set; }

        /// <summary>
        ///     List of keys flushed (if applicable)
        /// </summary>
        public List<string> Keys { get; set; }
    }
}