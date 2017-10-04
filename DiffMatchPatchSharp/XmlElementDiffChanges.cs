using System.Xml.Linq;

namespace DiffMatchPatchSharp
{
    public class XmlElementDiffChanges: XmlDiffChanges
    {
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
                TextElements.Add((new TextElement { Node = text.node1 }, new TextElement { Node = text.node2 }));
                AddChange(dmp, text.text1, text.text2);
            }
        }
    }
}
