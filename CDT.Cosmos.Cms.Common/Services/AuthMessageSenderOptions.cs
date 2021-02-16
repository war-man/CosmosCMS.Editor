namespace CDT.Cosmos.Cms.Common.Services
{
    /// <summary>
    /// SendGrid Authentication Options
    /// </summary>
    public class AuthMessageSenderOptions
    {
        /// <summary>
        /// SendGrid key
        /// </summary>
        public string SendGridKey { get; set; }
        /// <summary>
        /// From Email address
        /// </summary>
        public string EmailFrom { get; set; }
    }
}