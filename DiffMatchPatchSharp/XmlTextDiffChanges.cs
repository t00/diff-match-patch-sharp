using System.Text;
using System.Xml.Linq;

namespace DiffMatchPatchSharp
{
    public class XmlTextDiffChanges: XmlDiffChanges
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
                Elements.Add((new TextElement { Node = text.node1, Offset = sb1.Length }, new TextElement { Node = text.node2, Offset = sb2.Length }));
                sb1.Append(text.text1);
                sb2.Append(text.text2);
            }
            base.AddChange(dmp, sb1.ToString(), sb2.ToString());
        }
    }
}
