namespace CDT.Cosmos.Cms.Models
{
    public class UserRolesViewModel
    {
        public UserRolesViewModel()
        {
            Administrator = false;
            Editor = false;
            Author = false;
            Reviewer = false;
            NoRole = false;
            RemoveAccount = false;
            TeamMember = false;
        }

        public string UserEmailAddress { get; set; }
        public string UserId { get; set; }
        public bool Administrator { get; set; }
        public bool Editor { get; set; }
        public bool Author { get; set; }
        public bool Reviewer { get; set; }
        public bool TeamMember { get; set; }
        public bool NoRole { get; set; }
        public bool RemoveAccount { get; set; }

        public string UserRole { get; set; }
    }
}