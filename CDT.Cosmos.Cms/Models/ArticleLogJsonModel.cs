using System;
using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    public class ArticleLogJsonModel
    {
        [Key] public int Id { get; set; }

        public string ActivityNotes { get; set; }

        /// <summary>
        ///     Date and Time (UTC by default)
        /// </summary>
        public DateTime DateTimeStamp { get; set; }

        public string Title { get; set; }
        public string Email { get; set; }
    }
}