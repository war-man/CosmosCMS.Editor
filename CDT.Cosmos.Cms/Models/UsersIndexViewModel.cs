using System.ComponentModel.DataAnnotations;

namespace CDT.Cosmos.Cms.Models
{
    public class UsersIndexViewModel
    {
        [Key] public string UserId { get; set; }

        [Display(Name = "Email Address")]
        [EmailAddress]
        public string EmailAddress { get; set; }

        [Display(Name = "Role")] public string Role { get; set; }

        [Display(Name = "Telephone #")]
        [Phone]
        public string PhoneNumber { get; set; }

        [Display(Name = "Email Confirmed")] public bool EmailConfirmed { get; set; }

        [Display(Name = "Login Provider")] public string LoginProvider { get; set; }
    }
}