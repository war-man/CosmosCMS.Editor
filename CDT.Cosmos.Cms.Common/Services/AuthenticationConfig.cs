namespace CDT.Cosmos.Cms.Common.Services
{
    /// <summary>
    /// Authentication provider configurations
    /// </summary>
    public class AuthenticationConfig
    {
        public Microsoft Microsoft { get; set; }
        public Google Google { get; set; }
        public Facebook Facebook { get; set; }
        public bool AllowLocalRegistration { get; set; } = true;
    }
    /// <summary>
    /// Microsoft configuration
    /// </summary>
    public class Microsoft
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
    /// <summary>
    /// Google configuration
    /// </summary>
    public class Google
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
    /// <summary>
    /// Facebook configuration
    /// </summary>
    public class Facebook
    {
        public string AppId { get; set; }
        public string AppSecret { get; set; }
    }
}