using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    public class NewHomeViewModel
    {
        [Key] [Display(Name = "Article Key")] public int Id { get; set; }

        [Display(Name = "Article Number")] public int ArticleNumber { get; set; }

        [Display(Name = "Page Title")] public string Title { get; set; }

        [Display(Name = "Make this the new home page")]
        public bool IsNewHomePage { get; set; }

        [Display(Name = "URL Path")] public string UrlPath { get; set; }
    }
}