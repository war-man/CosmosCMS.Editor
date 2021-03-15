using System.Collections.Generic;

namespace CDT.Cosmos.Cms.Models.Interfaces
{
    public interface ICodeEditorViewModel
    {
        public int Id { get; set; }
        public string EditingField { get; set; }
        public string EditorTitle { get; set; }

        public bool IsValid { get; set; }

        public IEnumerable<EditorField> EditorFields { get; set; }
        public IEnumerable<string> CustomButtons { get; set; }
    }
}