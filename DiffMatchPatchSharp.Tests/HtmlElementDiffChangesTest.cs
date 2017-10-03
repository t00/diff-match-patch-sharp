using System.Xml.Linq;
using NUnit.Framework;

namespace DiffMatchPatchSharp.Tests
{
    [TestFixture]
    public class HtmlElementDiffChangesTest
    {
        [Test]
        public void TestHtmlChange()
        {
            const string html1 = "<p><span>First text added in the paragraph.</span><span>More text added later</span>Hello</p>";
            const string html2 = "<p><span>First text in the html page.</span><span>More text coming later.</span>Hell</p>";

            var xc = new HtmlElementDiffChanges();
            var dmp = new DiffMatchPatch();

            var doc1 = XDocument.Parse(html1);
            var doc2 = XDocument.Parse(html2);
            xc.AddChanges(dmp, doc1, doc2);
            xc.ProcessChanges();

            var e1 = xc.GetElementText((XElement)doc1.FirstNode);
            Assert.AreEqual("<p><span>First text <span style=\"background-color: #FF6347\">added </span>in the <span style=\"background-color: #FFFF00\">paragraph</span>.</span><span>More text <span style=\"background-color: #FFFF00\">added</span> later</span>Hell<span style=\"background-color: #FF6347\">o</span></p>", e1);
            var e2 = xc.GetElementText((XElement)doc2.FirstNode);
            Assert.AreEqual("<p><span>First text in the <span style=\"background-color: #FFFF00\">html page</span>.</span><span>More text <span style=\"background-color: #FFFF00\">coming</span> later<span style=\"background-color: #9ACD32\">.</span></span>Hell</p>", e2);
        }
    }
}
