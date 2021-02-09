namespace CDT.Cosmos.Cms.Services
{
    public class AuthenticationConfig
    {
        public Microsoft Microsoft { get; set; }
        public Google Google { get; set; }
        public Facebook Facebook { get; set; }
        public bool AllowLocalRegistration { get; set; } = true;
    }

    public class Microsoft
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }

    public class Google
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }

    public class Facebook
    {
        public string AppId { get; set; }
        public string AppSecret { get; set; }
    }
}