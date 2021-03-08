using System.Collections.Generic;

namespace CDT.Cosmos.Cms.Common.Services
{
    /// <summary>
    /// User identity information
    /// </summary>
    public class UserIdentityInfo
    {
        /// <summary>
        /// User is authenticated
        /// </summary>
        public bool IsAuthenticated { get; set; }
        /// <summary>
        /// User email address
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Role membership
        /// </summary>
        public IEnumerable<string> RoleMembership { get; set; }
    }
}
