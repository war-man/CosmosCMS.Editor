using System.Collections.Generic;
using CDT.Cosmos.Cms.Common.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CDT.Cosmos.Cms.Models
{
    public class SaveResultJsonModel
    {
        public bool IsValid { get; set; }
        public int ErrorCount { get; set; }
        public bool HasReachedMaxErrors { get; set; }
        public ModelValidationState ValidationState { get; set; }
        public ArticleViewModel Model { get; set; }
        public IEnumerable<ModelStateEntry> Errors { get; set; }
    }
}