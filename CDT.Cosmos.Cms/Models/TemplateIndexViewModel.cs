using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    public class TemplateIndexViewModel
    {
        [Key] public int Id { get; set; }

        [Display(Name = "Template Title")]
        [StringLength(128)]
        public string Title { get; set; }

        [Display(Name = "Description/Notes")] public string Description { get; set; }
    }
}