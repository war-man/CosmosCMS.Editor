using System;
using System.ComponentModel.DataAnnotations;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Data.Logic;

namespace CDT.Cosmos.Cms.Models
{
    public class TeamArticleViewModel
    {
        public TeamArticleViewModel()
        {
        }

        public TeamArticleViewModel(Article article)
        {
            Id = article.Id;
            StatusCode = (StatusCodeEnum) article.StatusCode;
        }

        [Key] public int Id { get; set; }

        public StatusCodeEnum StatusCode { get; set; }

        public int ArticleNumber { get; set; }

        [MaxLength(128)] [StringLength(128)] public string UrlPath { get; set; }

        [Display(Name = "Article version")] public int VersionNumber { get; set; }

        [Display(Name = "Publish on date/time (PST):")]
        [DataType(DataType.DateTime)]
        public DateTime? Published { get; set; }

        [MaxLength(80)]
        [StringLength(80)]
        [Display(Name = "Article title")]
        public string Title { get; set; }

        [Display(Name = "Article last saved")] public DateTime Updated { get; set; }
    }
}