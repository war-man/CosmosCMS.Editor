using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CDT.Cosmos.Cms.Models
{
    public class LayoutIndexViewModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Default website layout?")]
        public bool IsDefault { get; set; } = false;

        [Display(Name = "Layout Name")]
        [StringLength(128)]
        public string LayoutName { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.Html)]
        public string Notes { get; set; }
    }
}