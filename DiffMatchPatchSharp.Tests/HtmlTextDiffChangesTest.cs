using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
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
            Assert.AreEqual("<p><span>First text </span><span>in the paragraph.<span style=\"background-color: #9ACD32\"> A new text inserted.</span></span><span>More text <span style=\"background-color: #FFFF00\">coming</span> later<span style=\"background-color: #9ACD32\">.</span></span>Hell</p>", e2);
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
            Assert.AreEqual(html1, e1);
            var e2 = xc.GetElementText((XElement)doc2.FirstNode);
            Assert.AreEqual("<p><span>First text</span><span>, second Text</span>,third text<span style=\"background-color: #9ACD32\">, last text</span></p>", e2);
        }

        [Test]
        public void TestCompareHtml()
        {
            var html1 = "<p><span>First text in the paragraph.</span><span>More text added later</span>Hello</p>";
            var html2 = "<p><span>First text </span><span>in the paragraph. A new text inserted.</span><span>More text coming later.</span>Hell</p>";
            var dc = new HtmlTextDiffChanges { AddedColor = Color.Aqua };
            var (removed, added) = dc.CompareHtml(html1, html2);
            Assert.AreEqual("<p><span>First text in the paragraph.</span><span>More text <span style=\"background-color: #FFFF00\">added</span> later</span>Hell<span style=\"background-color: #FF6347\">o</span></p>", removed);
            Assert.AreEqual("<p><span>First text </span><span>in the paragraph.<span style=\"background-color: #00FFFF\"> A new text inserted.</span></span><span>More text <span style=\"background-color: #FFFF00\">coming</span> later<span style=\"background-color: #00FFFF\">.</span></span>Hell</p>", added);
        }

        [Test]
        public void TestCompareTables()
        {
            var html1 = GetResource("TableBefore.html");
            var html2 = GetResource("TableAfter.html");
            var dc = new HtmlTextDiffChanges { DefineHtmlEntities = true };
            var (removed, added) = dc.CompareHtml(html1, html2);
            var expectedRemoved = GetResource("TableRemoved.html");
            var expectedAdded = GetResource("TableAdded.html");
            Assert.AreEqual(expectedRemoved, removed);
            Assert.AreEqual(expectedAdded, added);
        }

        private static string GetResource(string suffix)
        {
            var fullName = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(x => x.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullName);
            using var reader = new StreamReader(stream ?? throw new InvalidDataException(nameof(stream)));
            return reader.ReadToEnd();

        }
    }
}
