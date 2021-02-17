using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    /// <summary>
    /// Hyperlink model
    /// </summary>
    public class HyperLinkViewModel
    {
        /// <summary>
        /// URI/URI of the link (href)
        /// </summary>
        [Display(Name = "URL", Description = "URL/URI of the link.")]
        [Required(AllowEmptyStrings = false)]
        public string Url { get; set; }

        /// <summary>
        /// Link text
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Link Text")]
        public string Text { get; set; }

        /// <summary>
        /// ID can be an anchor point
        /// </summary>
        [Display(Name = "Link Id")]
        public string Id { get; set; }

        /// <summary>
        /// Accessible Rich Internet Applications (ARIA) Label for link
        /// </summary>
        [Display(Name = "Aria Label")]
        public string AriaLabel { get; set; }

        /// <summary>
        /// Target where web browser will open the link
        /// </summary>
        [Display(Name = "Target")]
        public Target Target { get; set; } = Target.Self;
    }

    [CustomEnumUtility.CustomEnumAttribute(true)]
    public enum Target
    {
        [CustomEnumUtility.TextValueAttribute(textValue: "_blank")]
        Blank,
        [CustomEnumUtility.TextValueAttribute(textValue: "_parent")]
        Parent,
        [CustomEnumUtility.TextValueAttribute(textValue: "_self")]
        Self,
        [CustomEnumUtility.TextValueAttribute(textValue: "_top")]
        Top,
    }
}
