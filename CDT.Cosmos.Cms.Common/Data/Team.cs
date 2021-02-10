using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CDT.Cosmos.Cms.Common.Data
{
    /// <summary>
    ///     Team entity
    /// </summary>
    public class Team
    {
        /// <summary>
        ///     Identity key for this entity
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        ///     Friendly name of this team
        /// </summary>
        [MaxLength(64)]
        public string TeamName { get; set; }

        /// <summary>
        ///     Description of who this team is and what they are responsible for
        /// </summary>
        [MaxLength(1024)]
        [DataType(DataType.Html)]
        public string TeamDescription { get; set; }

        /// <summary>
        ///     Team member list
        /// </summary>
        public ICollection<TeamMember> Members { get; set; }

        /// <summary>
        ///     Articles managed by this team
        /// </summary>
        public ICollection<Article> Articles { get; set; }
    }
}