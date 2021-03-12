using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Models
{
    public class KendoFileBrowserEntry
    {
        public KendoFileBrowserEntry()
        {
        }

        public KendoFileBrowserEntry(FileBrowserEntry entry)
        {
            name = entry.Name;
            type = entry.EntryType == FileBrowserEntryType.Directory ? "d" : "f";
            size = entry.Size.ToString();
        }

        /// <summary>
        /// File Name
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// f (file) or d (directory)
        /// </summary>
        public string type { get; set; }

        /// <summary>
        /// Size in bytes
        /// </summary>
        public string size { get; set; }
    }
}
