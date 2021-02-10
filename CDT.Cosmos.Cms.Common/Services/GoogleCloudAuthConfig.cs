namespace CDT.Cosmos.Cms.Common.Services
{
    /// <summary>
    ///     Google cloud account information
    /// </summary>
    public class GoogleCloudAuthConfig
    {
        /// <summary>
        ///     Service type
        /// </summary>
        /// <remarks>Default is "service_account"</remarks>
        public string ServiceType { get; set; } = "service_account";

        /// <summary>
        ///     Project id
        /// </summary>
        /// <remarks>For example "translator-oet"</remarks>
        public string ProjectId { get; set; }

        /// <summary>
        ///     Private key Id
        /// </summary>
        public string PrivateKeyId { get; set; }

        /// <summary>
        ///     Google account private key
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        ///     Google account client email
        /// </summary>
        public string ClientEmail { get; set; }

        /// <summary>
        ///     Client ID
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        ///     Google authentication end point
        /// </summary>
        /// <remarks>Default value is "https://accounts.google.com/o/oauth2/auth"</remarks>
        public string AuthUri { get; set; } = "https://accounts.google.com/o/oauth2/auth";

        /// <summary>
        ///     Token end point
        /// </summary>
        /// <remarks>Default value is "https://oauth2.googleapis.com/token"</remarks>
        public string TokenUri { get; set; } = "https://oauth2.googleapis.com/token";

        /// <summary>
        ///     Authentication provider certificate URL
        /// </summary>
        /// <remarks>Default value is "https://www.googleapis.com/oauth2/v1/certs"</remarks>
        public string AuthProviderX509CertUrl { get; set; } = "https://www.googleapis.com/oauth2/v1/certs";

        /// <summary>
        ///     Client certificate URL
        /// </summary>
        public string ClientX509CertificateUrl { get; set; }
    }
}