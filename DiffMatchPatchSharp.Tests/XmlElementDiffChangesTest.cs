using System.Text;
using System.Xml.Linq;
using NUnit.Framework;

namespace DiffMatchPatchSharp.Tests
{
    [TestFixture]
    public class XmlElementDiffChangesTest
    {
        [Test]
        public void TestXmlChange()
        {
            var doc1 = new XDocument();
            var p1 = new XElement(XName.Get("p"));
            var span11 = new XElement(XName.Get("span")) { Value = "First text added in the paragraph." };
            var span12 = new XElement(XName.Get("span")) { Value = "More text added later" };
            p1.Add(span11);
            p1.Add(span12);
            span12.AddAfterSelf(new XText("Hello"));
            doc1.Add(p1);

            var doc2 = new XDocument();
            var p2 = new XElement(XName.Get("p"));
            var span21 = new XElement(XName.Get("span")) { Value = "First text in the html page." };
            var span22 = new XElement(XName.Get("span")) { Value = "More text coming later." };
            p2.Add(span21);
            p2.Add(span22);
            span22.AddAfterSelf(new XText("Hell"));
            doc2.Add(p2);

            var xc = new XmlElementDiffChanges();
            var dmp = new DiffMatchPatch();
            xc.AddChanges(dmp, doc1, doc2);

            TestCommonOutput(xc);
        }

        private static void TestCommonOutput(XmlElementDiffChanges xc)
        {
            var sb1 = new StringBuilder();
            xc.Process1(state => { TextDiffChangeTest.ProcesTest(sb1, state); });
            Assert.AreEqual(3, xc.Elements.Count);
            Assert.AreEqual("First text added in the paragraph.", xc.Elements[0].change1.Node.ToString());
            Assert.AreEqual("More text added later", xc.Elements[1].change1.Node.ToString());
            Assert.AreEqual("Hello", xc.Elements[2].change1.Node.ToString());
            Assert.AreEqual("First text in the html page.", xc.Elements[0].change2.Node.ToString());
            Assert.AreEqual("More text coming later.", xc.Elements[1].change2.Node.ToString());
            Assert.AreEqual("Hell", xc.Elements[2].change2.Node.ToString());
            Assert.AreEqual("First text -added -0in the *paragraph*0.More text *added*1 laterHell-o-2", sb1.ToString());

            var sb2 = new StringBuilder();
            xc.Process2(state => { TextDiffChangeTest.ProcesTest(sb2, state); });
            Assert.AreEqual("First text in the *html page*0.More text *coming*1 later+.+1Hell", sb2.ToString());
        }

        [Test]
        public void TestHtmlChange()
        {
            var html1 = "<p><span>First text added in the paragraph.</span><span>More text added later</span>Hello</p>";
            var html2 = "<p><span>First text in the html page.</span><span>More text coming later.</span>Hell</p>";

            var xc = new XmlElementDiffChanges();
            var dmp = new DiffMatchPatch();
            xc.AddChanges(dmp, html1, html2);

            TestCommonOutput(xc);
        }
    }
}
