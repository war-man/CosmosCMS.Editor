using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CDT.Cosmos.Cms.Common.Data
{
    /// <summary>
    ///     Article activity log entry
    /// </summary>
    public class ArticleLog
    {
        /// <summary>
        ///     Identity key of the entity
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        ///     User ID of the person who triggered the activity
        /// </summary>

        public string IdentityUserId { get; set; }

        /// <summary>
        ///     Identity User assocated with this activity
        /// </summary>
        [ForeignKey("IdentityUserId")]
        public IdentityUser IdentityUser { get; set; }

        /// <summary>
        ///     Notes regarding what happened.
        /// </summary>
        public string ActivityNotes { get; set; }

        /// <summary>
        ///     Date and Time (UTC by default)
        /// </summary>
        public DateTime DateTimeStamp { get; set; } = DateTime.Now;

        #region NAVIGATION

        /// <summary>
        ///     ID of the Article associated with this event.
        /// </summary>
        public int ArticleId { get; set; }

        /// <summary>
        ///     The article associated with this event.
        /// </summary>
        [ForeignKey("ArticleId")]
        public Article Article { get; set; }

        #endregion
    }
}