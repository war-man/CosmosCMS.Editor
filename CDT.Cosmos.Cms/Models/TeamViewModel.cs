using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    public class TeamViewModel
    {
        [Key] [Display(Name = "Team ID")] public int Id { get; set; }

        [MaxLength(64)]
        [Display(Name = "Team Name")]
        public string TeamName { get; set; }

        [MaxLength(1024)]
        [DataType(DataType.Html)]
        [Display(Name = "Team Description")]
        public string TeamDescription { get; set; }
    }
}