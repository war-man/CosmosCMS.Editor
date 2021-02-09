using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CDT.Cosmos.Cms.Models
{
    public class FileManagerViewModel
    {
        public int? TeamId { get; set; }
        public IEnumerable<SelectListItem> TeamFolders { get; set; }
    }
}