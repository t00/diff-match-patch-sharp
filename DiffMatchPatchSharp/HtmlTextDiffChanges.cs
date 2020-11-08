using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace DiffMatchPatchSharp
{
    public class HtmlTextDiffChanges: XmlTextDiffChanges
    {
        public bool PreserveNewlines { get; set; } = true;
        
        public bool DefineHtmlEntities { get; set; }

        public (string, string) CompareHtml(string html1, string html2)
        {
            var dmp = new DiffMatchPatch();
            var doc1 = XDocument.Parse(ParseHtml(html1, true));
            var doc2 = XDocument.Parse(ParseHtml(html2, true));
            AddChange(dmp, doc1, doc2);
            ProcessChanges();
            var removed = ParseHtml(GetElementText((XElement)doc1.FirstNode), false);
            var added = ParseHtml(GetElementText((XElement)doc2.FirstNode), false);
            return (removed, added);
        }

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

        protected virtual string ParseHtml(string html, bool input)
        {
            if (!DefineHtmlEntities || html == null)
            {
                return html;
            }

            return input ? html.Replace("&", "&amp;") : html.Replace("&amp;", "&").Replace("&amp;", "&");
        }
        
        protected virtual void MarkHtmlChange(XElement element, DiffChange change)
        {
            var style = DiffHtmlExtensions.CreateStyle(GetColor(change));
            DiffHtmlExtensions.SetStyle(element, style);
        }

        protected override string GetElementText(XNode node)
        {
            var text = base.GetElementText(node);
            if (text == null && IsNewlineParagraph(node, out var _))
            {
                return Environment.NewLine;
            }
            return text;
        }

        protected override void ReplaceNode(TextElement textElement, IList<TextPart> parts)
        {
            textElement.Node.ReplaceWith(parts.Select(part => GetChangeNodes(part, textElement.Node)));
        }

        private object GetChangeNodes(TextPart n, XNode originalNode)
        {
            if (IsNewlineParagraph(originalNode, out var element) && n.Text == Environment.NewLine)
            {
                element.Add(GetChangeNode(n.Change, new XElement(XName.Get("br"))));
                return element;
            }

            return GetChangeNode(n.Change, new XText(n.Text));
        }

        private object GetChangeNode(DiffChange change, object text)
        {
            if (change != DiffChange.None)
            {
                var span = CreateHtmlChangeElement(change);
                span.Add(text);
                return span;
            }
            return text;
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

        private bool IsNewlineParagraph(XNode node, out XElement element)
        {
            if (PreserveNewlines && node.NodeType == XmlNodeType.Element)
            {
                element = (XElement) node;
                // treat empty paragraph as a newline
                if ("p".Equals(element.Name.LocalName, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (element.IsEmpty)
                    {
                        return true;
                    }
                }
            }
            element = null;
            return false;
        }
    }
}
