using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CDT.Cosmos.Cms.Common.Data;

namespace CDT.Cosmos.Cms.Models
{
    public class MenuItemViewModel
    {
        [Key]
        [ScaffoldColumn(false)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ScaffoldColumn(false)] public bool hasChildren { get; set; }

        [ScaffoldColumn(false)] public int? ParentId { get; set; }

        [Display(Name = "Sort Order")] public int SortOrder { get; set; } = 0;

        [Display(Name = "Menu Text")] public string MenuText { get; set; }

        [UIHint("Url")]
        [Display(Name = "Url or Path")]
        public string Url { get; set; }

        [Display(Name = "Icon")]
        [UIHint("Icon")]
        public string IconCode { get; set; }

        public MenuItem ToEntity()
        {
            return new MenuItem
            {
                Id = Id,
                HasChildren = hasChildren,
                ParentId = ParentId,
                SortOrder = SortOrder,
                MenuText = MenuText,
                Url = Url,
                IconCode = IconCode
            };
        }
    }
}