using CDT.Cosmos.Cms.Common.Models;

namespace CDT.Cosmos.Cms.Tests
{
    public class JsonResultViewModel
    {
        //System.Boolean,System.Int32,System.Boolean,Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState,CDT.Cosmos.Cms.Models.ArticleViewModel
        //  IsValid = true, ErrorCount = 0, HasReachedMaxErrors = false, ValidationState = Valid, Model = {CDT.Cosmos.Cms.Models.ArticleViewModel}
        public bool IsValid { get; set; }
        public int ErrorCount { get; set; }
        public bool HasReachedMaxErrors { get; set; }
        public string ValidationState { get; set; }
        public ArticleViewModel Model { get; set; }
    }
}