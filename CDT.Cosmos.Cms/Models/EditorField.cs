namespace CDT.Cosmos.Cms.Models
{
    public class EditorField
    {
        public string FieldId { get; set; }
        public string FieldName { get; set; }
        public EditorMode EditorMode { get; set; }

        public string IconUrl { get; set; } = "";
    }

    public enum EditorMode
    {
        JavaScript = 0,
        Html = 1,
        Css = 2
    }
}