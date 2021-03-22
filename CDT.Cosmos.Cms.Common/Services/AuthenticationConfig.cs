namespace CDT.Cosmos.Cms.Common.Services
{
    /// <summary>
    /// Authentication provider configurations
    /// </summary>
    public class AuthenticationConfig
    {
        /// <summary>
        /// Microsoft authentication configuration
        /// </summary>
        public Microsoft Microsoft { get; set; }
        /// <summary>
        /// Google authentication configuration
        /// </summary>
        public Google Google { get; set; }
        /// <summary>
        /// Facebook authentication configuration
        /// </summary>
        public Facebook Facebook { get; set; }
        /// <summary>
        /// Allow registration locally on the editor site?
        /// </summary>
        public bool AllowLocalRegistration { get; set; } = true;
    }

    /// <summary>
    /// Microsoft configuration
    /// </summary>
    public class Microsoft
    {
        /// <summary>
        /// Client ID
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// Client Secret
        /// </summary>
        public string ClientSecret { get; set; }
    }

    /// <summary>
    /// Google configuration
    /// </summary>
    public class Google
    {
        /// <summary>
        /// Client ID
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// Client Secret
        /// </summary>
        public string ClientSecret { get; set; }
    }

    /// <summary>
    /// Facebook configuration
    /// </summary>
    public class Facebook
    {
        /// <summary>
        /// Application ID
        /// </summary>
        public string AppId { get; set; }
        /// <summary>
        /// Application Secret
        /// </summary>
        public string AppSecret { get; set; }
    }
}