namespace CDT.Cosmos.Cms.Common.Services
{
    /// <summary>
    ///     Azure blob storage config
    /// </summary>
    public class AzureBlobServiceConfig
    {
        /// <summary>
        ///     Blob storage connection string.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        ///     Public URL to the origin of the blob service.
        /// </summary>
        public string BlobServicePublicUrl { get; set; }
    }
}