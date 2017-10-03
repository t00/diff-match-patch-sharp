using System.Xml.Linq;
using NUnit.Framework;

namespace DiffMatchPatchSharp.Tests
{
    [TestFixture]
    public class HtmlTextDiffChangesTest
    {
        [Test]
        public void TestProcessHtml()
        {
            var html1 = "<p><span>First text added in the paragraph.</span><span>More text added later</span>Hello</p>";
            var html2 = "<p><span>First text in the paragraph.</span><span>A new paragraph inserted.</span><span>More text coming later.</span>Hell</p>";

            var xc = new HtmlTextDiffChanges();
            var dmp = new DiffMatchPatch();
            var doc1 = XDocument.Parse(html1);
            var doc2 = XDocument.Parse(html2);
            xc.AddChange(dmp, doc1, doc2);
            xc.ProcessChanges();

            var e1 = xc.GetElementText((XElement)doc1.FirstNode);
            var e2 = xc.GetElementText((XElement)doc2.FirstNode);
        }
    }
}
