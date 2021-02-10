using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CDT.Cosmos.Cms.Common.Services
{
    /// <summary>
    ///     Redis cache service
    /// </summary>
    public class RedisCacheService
    {
        /// <summary>
        ///     Cache Options
        /// </summary>
        public enum CacheOptions
        {
            /// <summary>
            ///     Use this option to denote caching HTML output on views.
            /// </summary>
            Html = 0,

            /// <summary>
            ///     Use this to indicate caching of database calls.
            /// </summary>
            Database = 1,

            /// <summary>
            ///     This indicates the menu database cache
            /// </summary>
            Menu = 2,

            /// <summary>
            ///     Indicates this is the Google Supported Language List
            /// </summary>
            GoogleLanguages = 3
        }

        /// <summary>
        ///     Private field holding Redis config
        /// </summary>
        private static IOptions<RedisContextConfig> _config;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="config"></param>
        public RedisCacheService(IOptions<RedisContextConfig> config)
        {
            _config = config ?? throw new Exception("IOptions<RedisContextConfig> cannot be null!");
        }

        /// <summary>
        ///     Gets the Redis
        /// </summary>
        /// <returns></returns>
        private IServer GetServer()
        {
            var redisOptions = new ConfigurationOptions
            {
                Password = _config.Value.Password,
                Ssl = true,
                SslProtocols = SslProtocols.Tls12,
                AbortOnConnectFail = _config.Value.AbortConnect
            };
            redisOptions.EndPoints.Add(_config.Value.Host, 6380);
            redisOptions.ConnectTimeout = 2000;
            redisOptions.ConnectRetry = 3;
            var connection = ConnectionMultiplexer.Connect(redisOptions);
            return connection.GetServer(_config.Value.Host, 6380);
        }

        /// <summary>
        ///     Article ID (Null if home page)
        /// </summary>
        /// <param name="cacheId"></param>
        /// <param name="lang"></param>
        /// <param name="option"></param>
        /// <param name="urlPath"></param>
        /// <returns></returns>
        public static string GetPageCacheKey(Guid cacheId, string lang, CacheOptions option, string urlPath)
        {
            return $"{cacheId}{(string.IsNullOrEmpty(urlPath) ? "root" : urlPath.ToLower())}{(int) option}{lang}";
        }

        /// <summary>
        ///     Get list of keys in Redis
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public List<RedisKey> GetKeys(string path = "")
        {
            return GetServer().Keys(pattern: $"{_config.Value.CacheId}{path}*").ToList();
        }
    }
}