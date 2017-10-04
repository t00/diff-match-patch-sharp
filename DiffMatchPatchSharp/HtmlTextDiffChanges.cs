using System.Xml.Linq;

namespace DiffMatchPatchSharp
{
    public class HtmlTextDiffChanges: XmlTextDiffChanges
    {
        public bool CompareHtmlStyleEqual(XElement leftElement, XElement rightElement)
        {
            if (leftElement == null || rightElement == null)
            {
                return false;
            }
            var style1 = DiffHtmlExtensions.ReadInlineStyle(leftElement);
            var style2 = DiffHtmlExtensions.ReadInlineStyle(rightElement);
            if (!DiffHtmlExtensions.AreStylesEqual(style1, style2))
            {
                MarkHtmlChange(leftElement, DiffChange.Changed);
                MarkHtmlChange(rightElement, DiffChange.Changed);
                return false;
            }
            foreach (var e in GetNodes(leftElement.Elements(), rightElement.Elements()))
            {
                if (!CompareHtmlStyleEqual(e.Item1, e.Item2))
                {
                    return false;
                }
            }
            return true;
        }

        protected virtual void MarkHtmlChange(XElement element, DiffChange change)
        {
            var style = DiffHtmlExtensions.CreateStyle(GetColor(change));
            DiffHtmlExtensions.SetStyle(element, style);
        }

        protected override XNode CreateChangeNode(DiffChange change, string text)
        {
            if (change != DiffChange.None)
            {
                var span = CreateHtmlChangeElement(change);
                span.Value = text;
                return span;
            }
            else
            {
                return new XText(text);
            }
        }

        protected virtual XElement CreateHtmlChangeElement(DiffChange change)
        {
            var span = new XElement(XName.Get("span"));
            {
                span.SetAttributeValue("style", $"background-color: {DiffHtmlExtensions.GetHtmlColor(GetColor(change))}");
            }
            return span;
        }
    }
}
