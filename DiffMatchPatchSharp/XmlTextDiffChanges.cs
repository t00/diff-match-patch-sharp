using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace DiffMatchPatchSharp
{
    public class XmlTextDiffChanges: MarkupDiffChanges<XNode>
    {
        public bool TrimInterElementWhitespaces { get; set; } = true;

        public new void AddChange(DiffMatchPatch dmp, string xml1, string xml2)
        {
            var doc1 = XDocument.Parse(xml1);
            var doc2 = XDocument.Parse(xml2);
            AddChange(dmp, doc1, doc2);
        }

        public void AddChange(DiffMatchPatch dmp, XContainer doc1, XContainer doc2)
        {
            var texts1 = GetElementTexts(doc1);
            var texts2 = GetElementTexts(doc2);
            var text1 = AddTextElements(TextElements1, texts1);
            var text2 = AddTextElements(TextElements2, texts2);
            base.AddChange(dmp, text1, text2);
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

        protected IEnumerable<(XNode node1, string text1)> GetElementTexts(XContainer doc)
        {
            foreach (var e in doc.DescendantNodes())
            {
                var text = e == null ? null : GetElementText(e);

                if(TrimInterElementWhitespaces && e?.NodeType == XmlNodeType.Text && e.PreviousNode?.NodeType == XmlNodeType.Element && string.IsNullOrEmpty(text?.Trim()))
                {
                    continue;
                }
                if (text != null)
                {
                    yield return (e, text);
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

        protected override void ReplaceNode(TextElement textElement, IList<TextPart> parts)
        {
            textElement.Node.ReplaceWith(parts.Select(n => new XText(n.Text)));
        }
    }
}
