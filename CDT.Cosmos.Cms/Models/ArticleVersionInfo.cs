using System;
using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    [Serializable]
    public class ArticleVersionInfo
    {
        [Key] public int Id { get; set; }

        public int VersionNumber { get; set; }
        public string Title { get; set; }
        public DateTime Updated { get; set; }
        public DateTime? Published { get; set; }
    }
}