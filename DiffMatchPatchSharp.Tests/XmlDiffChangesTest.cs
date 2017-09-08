using System.Text;
using System.Xml.Linq;
using NUnit.Framework;

namespace DiffMatchPatchSharp.Tests
{
    [TestFixture]
    public class XmlDiffChangesTest
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
            doc1.Add(p1);

            var doc2 = new XDocument();
            var p2 = new XElement(XName.Get("p"));
            var span21 = new XElement(XName.Get("span")) { Value = "First text in the html page." };
            var span22 = new XElement(XName.Get("span")) { Value = "More text coming later." };
            p2.Add(span21);
            p2.Add(span22);
            doc2.Add(p2);

            var xc = new XmlDiffChanges();
            var dmp = new DiffMatchPatch();
            xc.AddChanges(dmp, doc1, doc2, true);

            var sb1 = new StringBuilder();
            xc.Process1((ch, text, idx) => { DiffChangeTest.ProcesTest(sb1, ch, text, idx); });
            Assert.AreEqual(2, xc.Elements.Count);
            Assert.AreEqual("First text added in the paragraph.", xc.Elements[0].element1.Value);
            Assert.AreEqual("More text added later", xc.Elements[1].element1.Value);
            Assert.AreEqual("First text in the html page.", xc.Elements[0].element2.Value);
            Assert.AreEqual("More text coming later.", xc.Elements[1].element2.Value);
            Assert.AreEqual("First text -added -0in the *paragraph*0.More text *added*1 later", sb1.ToString());

            var sb2 = new StringBuilder();
            xc.Process2((ch, text, idx) => { DiffChangeTest.ProcesTest(sb2, ch, text, idx); });
            Assert.AreEqual("First text in the *html page*0.More text *coming*1 later+.+1", sb2.ToString());
        }
    }
}
