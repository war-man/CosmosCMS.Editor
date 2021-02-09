using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    public class SetupViewModel
    {
        [Display(Name = "Icons")] public bool IconsSetup { get; set; } = false;

        [Display(Name = "User Roles")] public bool RolesSetup { get; set; } = false;

        [Display(Name = "Administrator Setup")]
        public bool SetupAdmin { get; set; } = false;

        [Display(Name = "REDIS Cache Detected")]
        public bool RedisSetup { get; set; } = false;

        [Display(Name = "Email")] public bool EmailSetup { get; set; } = false;

        [Display(Name = "Database")] public bool DataSetup { get; set; } = false;
    }
}