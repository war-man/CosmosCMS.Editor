namespace CDT.Cosmos.Cms.Common.Services
{
    /// <summary>
    ///     Startup.ConfigureServices method captures the site customization options found in "secrets" in this
    ///     object.
    /// </summary>
    public class SiteCustomizationsConfig
    {
        /// <summary>
        ///     Sets the mode of the website. True means authoring mode, false means publishing.
        /// </summary>
        public bool ReadWriteMode { get; set; } = false;

        /// <summary>
        ///     Allows a website to go into setup mode. For use only on fresh sites.
        /// </summary>
        public bool AllowSetup { get; set; } = false;

        /// <summary>
        ///     For training purposes, allows a full reset of site to factory settings.
        /// </summary>
        public bool AllowReset { get; set; } = false;

        /// <summary>
        /// Allowed file type extensions
        /// </summary>
        public string AllowedFileTypes { get; set; } = ".js,.css,.htm,.html,.mov,.webm,.avi,.mp4,.mpeg,.ts,.svg,.json";

        /// <summary>
        ///     Enable Azure Signal R (Required if website is behind Azure Front Door
        /// </summary>
        public bool UseAzureSignalR { get; set; } = false;

        /// <summary>
        ///     (Future functionality) Turns on page autoversion
        /// </summary>
        public bool AutoVersion { get; set; } = false;

        /// <summary>
        /// URI/URLURL
        /// of the publisher website.
        /// </summary>
        public string PublisherUrl { get; set; } = "";
    }
}