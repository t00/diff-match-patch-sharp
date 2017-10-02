using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace DiffMatchPatchSharp
{
    public class XmlDiffChanges: DiffChanges
    {
        public IList<(XNode node1, XNode node2)> Elements { get; } = new List<(XNode, XNode)>();

        public void AddChanges(DiffMatchPatch dmp, string xml1, string xml2)
        {
            var doc1 = XDocument.Parse(xml1);
            var doc2 = XDocument.Parse(xml2);
            AddChanges(dmp, doc1, doc2);
        }

        public void AddChanges(DiffMatchPatch dmp, XContainer doc1, XContainer doc2)
        {
            var texts = GetElementTexts(doc1, doc2);
            foreach (var text in texts)
            {
                Elements.Add((text.node1, text.node2));
                AddChange(dmp, text.text1, text.text2);
            }
        }

        /// <summary>
        /// Gets plain text from elements, returns null if element has no text
        /// </summary>
        /// <param name="node">Element to get text from</param>
        /// <returns>Element's text</returns>
        protected virtual string GetElementText(XNode node)
        {
            if (node.NodeType == XmlNodeType.Text)
            {
                return node.ToString(SaveOptions.DisableFormatting);
            }
            return null;
        }

        private IEnumerable<(XNode node1, string text1, XNode node2, string text2)> GetElementTexts(XContainer doc1, XContainer doc2)
        {
            var pairs = GetNodes(doc1.DescendantNodes(), doc2.DescendantNodes());
            foreach (var e in pairs)
            {
                var text1 = e.Item1 == null ? null : GetElementText(e.Item1);
                var text2 = e.Item2 == null ? null : GetElementText(e.Item2);

                if (text1 != null || text2 != null)
                {
                    yield return (e.Item1, text1, e.Item2, text2);
                }
            }
        }

        private static IEnumerable<(TItem, TItem)> GetNodes<TItem>(IEnumerable<TItem> items1, IEnumerable<TItem> items2)
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

        public bool CompareStylesEqual(XElement leftElement, XElement rightElement)
        {
            if (leftElement == null || rightElement == null)
            {
                return false;
            }
            var style1 = DiffHtmlExtensions.ReadInlineStyle(leftElement);
            var style2 = DiffHtmlExtensions.ReadInlineStyle(rightElement);
            if (!DiffHtmlExtensions.AreStylesEqual(style1, style2))
            {
                Mark(leftElement, Change.Changed);
                Mark(rightElement, Change.Changed);
                return false;
            }
            foreach (var e in GetNodes(leftElement.Elements(), rightElement.Elements()))
            {
                if (!CompareStylesEqual(e.Item1, e.Item2))
                {
                    return false;
                }
            }
            return true;
        }

        protected virtual void Mark(XElement element, Change change)
        {
            var style = DiffHtmlExtensions.CreateStyle(GetColor(change));
            DiffHtmlExtensions.SetStyle(element, style);
        }
    }
}
