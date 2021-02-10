using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CDT.Cosmos.Cms.Common.Data
{
    /// <summary>
    ///     Represents a menu item in the layout navigation menu
    /// </summary>
    public class MenuItem
    {
        /// <summary>
        ///     Identity key for this entity
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        ///     Used for cache management.  No need to scaffold in forms.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public Guid Guid { get; set; }

        /// <summary>
        ///     Determins where in the sort order does this menu item fall
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        ///     Menu item text or label
        /// </summary>
        [MaxLength(100)]
        [MinLength(2)]
        [StringLength(100)]
        [Display(Name = "Menu Text (keep short!)")]
        public string MenuText { get; set; }

        /// <summary>
        ///     URL for this menu item
        /// </summary>
        [Display(Name = "Url")]
        [MaxLength(256)]
        [StringLength(256)]
        public string Url { get; set; }

        /// <summary>
        ///     Icon CSS code for this menu item
        /// </summary>
        [Display(Name = "Icon")]
        [MaxLength(256)]
        [StringLength(256)]
        public string IconCode { get; set; }

        #region NAVIGATION

        /// <summary>
        ///     Menu supports a hierarchy, this is the parent menu item
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        ///     Navitation to the parent item
        /// </summary>
        [ForeignKey("ParentId")]
        public MenuItem ParentItem { get; set; }

        /// <summary>
        ///     Items below this one that are "children"
        /// </summary>
        public ICollection<MenuItem> ChildItems { get; set; }

        /// <summary>
        ///     Indicates if this menu item has children
        /// </summary>
        public bool HasChildren { get; set; } = false;

        #endregion
    }
}