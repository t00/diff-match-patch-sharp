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
            var html1 = "<p><span>First text in the paragraph.</span><span>More text added later</span>Hello</p>";
            var html2 = "<p><span>First text </span><span>in the paragraph. A new text inserted.</span><span>More text coming later.</span>Hell</p>";

            var xc = new HtmlTextDiffChanges();
            var dmp = new DiffMatchPatch();
            var doc1 = XDocument.Parse(html1);
            var doc2 = XDocument.Parse(html2);
            xc.AddChange(dmp, doc1, doc2);
            xc.ProcessChanges();

            var e1 = xc.GetElementText((XElement)doc1.FirstNode);
            Assert.AreEqual("<p><span>First text in the paragraph.</span><span>More text <span style=\"background-color: #FFFF00\">added</span> later</span>Hell<span style=\"background-color: #FF6347\">o</span></p>", e1);
            var e2 = xc.GetElementText((XElement)doc2.FirstNode);
            Assert.AreEqual("<p><span>First text </span><span>in the paragraph.<span style=\"background-color: #9ACD32\"> A new text inserted.</span></span>Hell</p>", e2);
        }

        [Test]
        public void TestProcessHtmlSplit()
        {
            var html1 = "<p><span>First text</span><span>, second Text</span>,third text</p>";
            var html2 = "<p><span>First text</span><span>, second Text</span>,third text, last text</p>";

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
