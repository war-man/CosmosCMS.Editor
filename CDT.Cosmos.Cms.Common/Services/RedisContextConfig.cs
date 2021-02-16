using System;

namespace CDT.Cosmos.Cms.Common.Services
{
    /// <summary>
    /// Redis configuration context
    /// </summary>
    public class RedisContextConfig
    {
        /// <summary>
        ///     Each Cosmos CMS should have its own cache ID
        /// </summary>
        public Guid CacheId { get; set; }

        /// <summary>
        /// Server host name
        /// </summary>
        public string Host { get; set; }
        /// <summary>
        /// Port to communicate on (default 6380)
        /// </summary>
        public int Port { get; set; } = 6380;
        /// <summary>
        /// Password
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// Use SSL/TLS connection (default true)
        /// </summary>
        public bool Ssl { get; set; } = true;

        /// <summary>
        /// Abort connection (default false)
        /// </summary>
        public bool AbortConnect { get; set; } = false;

        /// <summary>
        ///     Number of seconds to cache data in memory (1 minute default).
        /// </summary>
        public int CacheDuration { get; set; } = 60;
    }
}