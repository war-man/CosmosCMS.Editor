using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CDT.Cosmos.Cms.Models
{
    public class CreatePageViewModel
    {
        public CreatePageViewModel()
        {
            Templates = new List<SelectListItem>();
        }

        [Key] public int Id { get; set; }

        [Display(Name = "Page Title")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Pages must have a title.")]
        public string Title { get; set; }

        [Display(Name = "Page template (optional)")]
        public int? TemplateId { get; set; }

        public List<SelectListItem> Templates { get; set; }

        [Display(Name = "Team Name")] public int? TeamId { get; set; }
    }
}