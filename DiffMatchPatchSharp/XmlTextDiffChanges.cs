using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace DiffMatchPatchSharp
{
    public class XmlTextDiffChanges: MarkupDiffChanges<XNode>
    {
        public new void AddChange(DiffMatchPatch dmp, string xml1, string xml2)
        {
            var doc1 = XDocument.Parse(xml1);
            var doc2 = XDocument.Parse(xml2);
            AddChange(dmp, doc1, doc2);
        }

        public void AddChange(DiffMatchPatch dmp, XContainer doc1, XContainer doc2)
        {
            var sb1 = new StringBuilder();
            var sb2 = new StringBuilder();
            var texts = GetElementTexts(doc1, doc2);
            foreach (var text in texts)
            {
                TextElements.Add((
                    new TextElement { Node = text.node1, Offset = sb1.Length, Length = text.text1?.Length ?? 0 }, 
                    new TextElement { Node = text.node2, Offset = sb2.Length, Length = text.text2?.Length ?? 0 }
                ));
                sb1.Append(text.text1);
                sb2.Append(text.text2);
            }
            base.AddChange(dmp, sb1.ToString(), sb2.ToString());
        }

        public virtual string GetElementText(XElement leftElement)
        {
            var builder = new StringBuilder(256);
            using (var sw = new StringWriter(builder))
            {
                using (var htmlWriter = XmlWriter.Create(sw, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = false }))
                {
                    leftElement.Save(htmlWriter);
                }
            }
            return builder.ToString();
        }

        protected IEnumerable<(XNode node1, string text1, XNode node2, string text2)> GetElementTexts(XContainer doc1, XContainer doc2)
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

        protected override XNode CreateChangeNode(DiffChange change, string text)
        {
            return new XText(text);
        }

        protected override void ReplaceNode(TextElement textElement, IList<XNode> nodes)
        {
            textElement.Node.ReplaceWith(nodes);
        }
    }
}
