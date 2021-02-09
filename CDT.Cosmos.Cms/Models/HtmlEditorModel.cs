using System;
using CDT.Cosmos.Cms.Common.Models;
using Kendo.Mvc.UI.Fluent;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CDT.Cosmos.Cms.Models
{
    public class HtmlEditorModel : PageModel
    {
        public ArticleViewModel Article { get; set; }
        public ToolSetEnum ToolSet { get; set; }
        public bool EditModeOn { get; private set; }
        public string FieldName { get; private set; }
        public string BackgroundColor { get; set; }
        public string CssClass { get; set; }
        public Action<EditorToolFactory> Tools { get; private set; }

        public static HtmlEditorModel Build(ArticleViewModel model, string fieldName, bool editModeOn,
            ToolSetEnum toolSet, string backgroundColor = "white", string cssClass = "")
        {
            Action<EditorToolFactory> tools;
            switch (toolSet)
            {
                case ToolSetEnum.Full:
                    tools = tools => tools
                        .Clear()
                        .Bold().Italic().Underline().Strikethrough()
                        .JustifyLeft().JustifyCenter().JustifyRight().JustifyFull()
                        .InsertUnorderedList().InsertOrderedList()
                        .Outdent().Indent()
                        .CreateLink().Unlink()
                        .SubScript()
                        .SuperScript()
                        .TableEditing()
                        .ViewHtml()
                        .Formatting()
                        .CleanFormatting()
                        .FormatPainter()
                        .FontName()
                        .FontSize()
                        .ForeColor().BackColor()
                        .Print();
                    break;
                case ToolSetEnum.Map:
                case ToolSetEnum.Limited:
                    tools = tools => tools.Clear().ViewHtml().CleanFormatting();
                    break;
                case ToolSetEnum.Card:
                    tools = tools => tools.Clear().ViewHtml().CleanFormatting()
                        .CustomButton(c =>
                            c.Name("Icon Tool").Tooltip("Create a new card")
                                .Exec(@"function (e) { openCardEdtor($(this).data('kendoEditor')); }"));
                    break;
                default:
                    tools = tools => tools.Clear();
                    break;
            }

            return new HtmlEditorModel
            {
                Article = model,
                FieldName = fieldName,
                EditModeOn = editModeOn,
                Tools = tools,
                BackgroundColor = backgroundColor,
                CssClass = cssClass,
                ToolSet = toolSet
            };
        }
    }
}