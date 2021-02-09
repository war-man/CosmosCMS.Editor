using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CDT.Cosmos.Cms.Models.Interfaces;

namespace CDT.Cosmos.Cms.Models
{
    public class FileManagerEditCodeViewModel : ICodeEditorViewModel
    {
        public string Path { get; set; }

        [DataType(DataType.Html)] public string Content { get; set; }

        public EditorMode EditorMode { get; set; }

        [Key] public int Id { get; set; }

        public string EditingField { get; set; }

        public string EditorTitle { get; set; }
        public IEnumerable<EditorField> EditorFields { get; set; }
        public IEnumerable<string> CustomButtons { get; set; }
    }
}