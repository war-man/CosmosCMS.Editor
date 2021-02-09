using System.Collections.Generic;
using CDT.Cosmos.Cms.Models.Interfaces;

namespace CDT.Cosmos.Cms.Models
{
    public class LayoutCodeViewModel : ICodeEditorViewModel
    {
        public string Head { get; set; }
        public string BodyHtmlAttributes { get; set; }
        public string BodyHeaderHtmlAttributes { get; set; }
        public string HtmlHeader { get; set; }
        public string FooterHtmlAttributes { get; set; }
        public string FooterHtmlContent { get; set; }
        public string PostFooterBlock { get; set; }
        public int Id { get; set; }
        public string EditingField { get; set; }
        public string EditorTitle { get; set; }
        public IEnumerable<EditorField> EditorFields { get; set; }
        public IEnumerable<string> CustomButtons { get; set; }
    }
}