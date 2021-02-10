using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CDT.Cosmos.Cms.Common.Data
{
    /// <summary>
    ///     Font icon class in the database
    /// </summary>
    public class FontIcon
    {
        /// <summary>
        ///     Identity key for this entity
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        ///     CSS code for this icon
        /// </summary>
        public string IconCode { get; set; }
    }
}