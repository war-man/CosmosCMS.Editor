using System;

namespace CDT.Cosmos.Cms.Common.Services
{
    public class RedisContextConfig
    {
        /// <summary>
        ///     Each Cosmos CMS should have its own cache ID
        /// </summary>
        public Guid CacheId { get; set; }

        public string Host { get; set; }
        public int Port { get; set; } = 6380;
        public string Password { get; set; }
        public bool Ssl { get; set; } = true;
        public bool AbortConnect { get; set; } = false;

        /// <summary>
        ///     Number of seconds to cache data in memory (1 minute default).
        /// </summary>
        public int CacheDuration { get; set; } = 60;
    }
}