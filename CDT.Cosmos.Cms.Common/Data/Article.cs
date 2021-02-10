using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CDT.Cosmos.Cms.Common.Data.Logic;

namespace CDT.Cosmos.Cms.Common.Data
{
    /// <summary>
    ///     Article
    /// </summary>
    /// <remarks>An article is the content for a web page.</remarks>
    public class Article
    {
        /// <summary>
        ///     Unique article entity primary key number (not to be confused with article number)
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        ///     (optional) Layout that this article uses
        /// </summary>
        /// <remarks>If the page uses a layout other than the default, it will be specified here.</remarks>
        public int? LayoutId { get; set; }

        /// <summary>
        ///     (optional) If applicable, the Id of the team managing this web page.
        /// </summary>
        public int? TeamId { get; set; }

        /// <summary>
        ///     Article number
        /// </summary>
        /// <remarks>An article number.</remarks>
        public int ArticleNumber { get; set; }

        /// <summary>
        ///     Status of this article
        /// </summary>
        /// <remarks>See <see cref="StatusCodeEnum" /> enum for code numbers.</remarks>
        public int StatusCode { get; set; } = 0;

        /// <summary>
        ///     This is the URL of the article.
        /// </summary>
        [MaxLength(128)]
        [StringLength(128)]
        public string UrlPath { get; set; }

        /// <summary>
        ///     Version number of the article.
        /// </summary>
        [Display(Name = "Article version")]
        public int VersionNumber { get; set; }

        /// <summary>
        ///     Date/time of when this article is published.
        /// </summary>
        /// <remarks>Null value here means this article is not published.</remarks>
        [Display(Name = "Publish on (date/time):")]
        [DataType(DataType.DateTime)]
        public DateTime? Published { get; set; }

        /// <summary>
        ///     Title of the article
        /// </summary>
        [MaxLength(254)]
        [StringLength(254)]
        [Display(Name = "Article title")]
        public string Title { get; set; }

        /// <summary>
        ///     HTML content of the page.
        /// </summary>
        [DataType(DataType.Html)]
        public string Content { get; set; }

        /// <summary>
        ///     Date/time of when this article was last updated.
        /// </summary>
        [Display(Name = "Article last saved")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime Updated { get; set; }

        /// <summary>
        ///     JavaScript injected into the header of the web page.
        /// </summary>
        [DataType(DataType.Html)]
        public string HeaderJavaScript { get; set; }

        /// <summary>
        ///     JavaScript injected into the footer of the web page.
        /// </summary>
        [DataType(DataType.Html)]
        public string FooterJavaScript { get; set; }

        #region NAVIGATIONS

        /// <summary>
        ///     (optional) The layout of this page if other than the default.
        /// </summary>
        [ForeignKey("LayoutId")]
        public Layout Layout { get; set; }

        /// <summary>
        ///     Activity logs relating to this article
        /// </summary>
        public ICollection<ArticleLog> ArticleLogs { get; set; }

        /// <summary>
        ///     Menu items for this article (soon to be depreciated)
        /// </summary>
        public ICollection<MenuItem> MenuItems { get; set; }

        /// <summary>
        ///     (optional) Icon font Id for this article
        /// </summary>
        public int? FontIconId { get; set; }

        /// <summary>
        ///     (optional) Icon font for this page (will appear in menu if selected)
        /// </summary>
        [ForeignKey("FontIconId")]
        public FontIcon FontIcon { get; set; }

        /// <summary>
        ///     (optional) Team that manages this page.
        /// </summary>
        [ForeignKey("TeamId")]
        public Team Team { get; set; }

        #endregion
    }
}