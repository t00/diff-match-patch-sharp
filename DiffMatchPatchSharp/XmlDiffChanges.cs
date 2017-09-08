using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace DiffMatchPatchSharp
{
    public class XmlDiffChanges: DiffChanges
    {
        public IList<(XNode node1, XNode node2)> Elements { get; } = new List<(XNode, XNode)>();

        public void AddChanges(DiffMatchPatch dmp, XContainer doc1, XContainer doc2, bool cleanupSemantics)
        {
            var texts = GetElementTexts(doc1, doc2);
            foreach (var text in texts)
            {
                Elements.Add((text.node1, text.node2));
                AddChange(dmp, text.text1, text.text2, cleanupSemantics);
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
            var pairs = GetNodes(doc1, doc2);
            foreach (var e in pairs)
            {
                var text1 = GetElementText(e.node1);
                var text2 = GetElementText(e.node2);

                if (text1 != null || text2 != null)
                {
                    yield return (e.node1, text1, e.node2, text2);
                }
            }
        }

        private static IEnumerable<(XNode node1, XNode node2)> GetNodes(XContainer doc1, XContainer doc2)
        {
            using (var e1 = doc1.DescendantNodes().GetEnumerator())
            {
                using (var e2 = doc2.DescendantNodes().GetEnumerator())
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
                                yield return (e1.Current, null);
                            }
                            while (e1.MoveNext());
                            yield break;
                        }
                    }
                    if (e2.MoveNext())
                    {
                        do
                        {
                            yield return (null, e2.Current);
                        } while (e2.MoveNext());
                    }
                }
            }
        }
    }
}
