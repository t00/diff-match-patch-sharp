using System.Collections.Generic;
using System.Linq;
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

        protected override void ReplaceNode(TextElement textElement, IList<TextPart> parts)
        {
            textElement.Node.ReplaceWith(parts.Select(n =>
            {
                if (n.Change != DiffChange.None)
                {
                    var span = CreateHtmlChangeElement(n.Change);
                    span.Value = n.Text;
                    return (object)span;
                }
                return new XText(n.Text);
            }));
        }

        protected virtual XElement CreateHtmlChangeElement(DiffChange change)
        {
            var span = new XElement(XName.Get("span"));
            {
                span.SetAttributeValue("style", $"background-color: {DiffHtmlExtensions.GetHtmlColor(GetColor(change))}");
            }
            return span;
        }

        protected static IEnumerable<(TItem, TItem)> GetNodes<TItem>(IEnumerable<TItem> items1, IEnumerable<TItem> items2)
        {
            using (var e1 = items1.GetEnumerator())
            {
                using (var e2 = items2.GetEnumerator())
                {
                    while (e1.MoveNext())
                    {
                        if (e2.MoveNext())
                        {
                            yield return (e1.Current, e2.Current);
                        }
                        else
                        {
                            do
                            {
                                yield return (e1.Current, default(TItem));
                            }
                            while (e1.MoveNext());
                            yield break;
                        }
                    }
                    if (e2.MoveNext())
                    {
                        do
                        {
                            yield return (default(TItem), e2.Current);
                        } while (e2.MoveNext());
                    }
                }
            }
        }
    }
}
