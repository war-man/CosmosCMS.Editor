using System;
using System.ComponentModel.DataAnnotations;
using System.Web;
using CDT.Cosmos.Cms.Common.Data;

namespace CDT.Cosmos.Cms.Common.Models
{
    /// <summary>
    ///     VSiew model used on layout list page
    /// </summary>
    [Serializable]
    public class LayoutViewModel
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public LayoutViewModel()
        {
            var starterLayout = LayoutDefaults.GetOceanside();
            Head = starterLayout.Head;
            BodyHtmlAttributes = starterLayout.BodyHtmlAttributes;
            BodyHeaderHtmlAttributes = starterLayout.BodyHeaderHtmlAttributes;
            HtmlHeader = starterLayout.HtmlHeader;
            FooterHtmlAttributes = starterLayout.FooterHtmlAttributes;
            FooterHtmlContent = starterLayout.FooterHtmlContent;
            PostFooterBlock = starterLayout.PostFooterBlock;
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="layout"></param>
        public LayoutViewModel(Layout layout)
        {
            Id = layout.Id;
            LayoutName = layout.LayoutName;
            Notes = layout.Notes;
            Head = layout.Head;
            BodyHtmlAttributes = layout.BodyHtmlAttributes;
            BodyHeaderHtmlAttributes = layout.BodyHeaderHtmlAttributes;
            HtmlHeader = layout.HtmlHeader;
            FooterHtmlAttributes = layout.FooterHtmlAttributes;
            FooterHtmlContent = layout.FooterHtmlContent;
            PostFooterBlock = layout.PostFooterBlock;
        }

        /// <summary>
        ///     Identity key of the entity
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        ///     Indicates if this is the default layout of the site
        /// </summary>
        [Display(Name = "Is default layout?")]
        public bool IsDefault { get; set; } = false;

        /// <summary>
        ///     Friendly name of layout
        /// </summary>
        [Display(Name = "Layout Name")]
        [StringLength(128)]
        public string LayoutName { get; set; }

        /// <summary>
        ///     Notes regarding this layout
        /// </summary>
        [Display(Name = "Notes")]
        [DataType(DataType.Html)]
        public string Notes { get; set; }

        /// <summary>
        ///     Content injected into the head tag
        /// </summary>
        [Display(Name = "HEAD Content")]
        [DataType(DataType.Html)]
        public string Head { get; set; }

        /// <summary>
        ///     Body tag attribute
        /// </summary>
        [Display(Name = "BODY Html Attributes", GroupName = "Body")]
        [StringLength(256)]
        public string BodyHtmlAttributes { get; set; }

        /// <summary>
        ///     Page header attributes
        /// </summary>
        [Display(Name = "Header Html Attributes", GroupName = "Header")]
        [StringLength(256)]
        public string BodyHeaderHtmlAttributes { get; set; }

        /// <summary>
        ///     Content injected into page header
        /// </summary>
        [Display(Name = "Header Html Content", GroupName = "Header")]
        [DataType(DataType.Html)]
        public string HtmlHeader { get; set; }

        /// <summary>
        ///     Footer html attributes
        /// </summary>
        [Display(Name = "Footer Html Attributes", GroupName = "Footer")]
        [StringLength(256)]
        public string FooterHtmlAttributes { get; set; }

        /// <summary>
        ///     Content injected into the page footer
        /// </summary>
        [Display(Name = "Footer Html Content", GroupName = "Footer")]
        [DataType(DataType.Html)]
        public string FooterHtmlContent { get; set; }

        /// <summary>
        ///     Content injected into post footer block
        /// </summary>
        [Display(Name = "Post-Footer Code Bloc", GroupName = "Footer")]
        [DataType(DataType.Html)]
        public string PostFooterBlock { get; set; }

        /// <summary>
        ///     Gets a detached entity.
        /// </summary>
        /// <returns></returns>
        public Layout GetLayout(bool decode = false)
        {
            if (decode)
                return new Layout
                {
                    Id = Id,
                    IsDefault = IsDefault,
                    LayoutName = LayoutName,
                    Notes = HttpUtility.HtmlDecode(Notes),
                    Head = HttpUtility.HtmlDecode(Head),
                    BodyHtmlAttributes = BodyHtmlAttributes,
                    BodyHeaderHtmlAttributes = BodyHeaderHtmlAttributes,
                    HtmlHeader = HttpUtility.HtmlDecode(HtmlHeader),
                    FooterHtmlAttributes = FooterHtmlAttributes,
                    FooterHtmlContent = HttpUtility.HtmlDecode(FooterHtmlContent),
                    PostFooterBlock = HttpUtility.HtmlDecode(PostFooterBlock)
                };
            return new Layout
            {
                Id = Id,
                IsDefault = IsDefault,
                LayoutName = LayoutName,
                Notes = Notes,
                Head = Head,
                BodyHtmlAttributes = BodyHtmlAttributes,
                BodyHeaderHtmlAttributes = BodyHeaderHtmlAttributes,
                HtmlHeader = HtmlHeader,
                FooterHtmlAttributes = FooterHtmlAttributes,
                FooterHtmlContent = FooterHtmlContent,
                PostFooterBlock = PostFooterBlock
            };
        }
    }
}