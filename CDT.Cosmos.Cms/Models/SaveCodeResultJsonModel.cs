using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CDT.Cosmos.Cms.Models
{
    public class SaveCodeResultJsonModel
    {
        public SaveCodeResultJsonModel()
        {
            Errors = new List<ModelStateEntry>();
        }

        public bool IsValid { get; set; }
        public int ErrorCount { get; set; }
        public bool HasReachedMaxErrors { get; set; }
        public ModelValidationState ValidationState { get; set; }
        public List<ModelStateEntry> Errors { get; set; }

    }
}