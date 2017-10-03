using System.Text;
using System.Xml.Linq;
using NUnit.Framework;

namespace DiffMatchPatchSharp.Tests
{
    [TestFixture]
    public class XmlTextDiffChangesTest
    {
        [Test]
        public void TestCustomProcessHtml()
        {
            var html1 = "<p><span>First text added in the paragraph.</span><span>More text added later</span>Hello</p>";
            var html2 = "<p><span>First text in the html page.</span><span>More text coming later.</span>Hell</p>";

            var xc = new XmlTextDiffChanges();
            var dmp = new DiffMatchPatch();
            xc.AddChange(dmp, html1, html2);

            var sb1 = new StringBuilder();
            xc.Process1(state => { TextDiffChangeTest.ProcesTest(sb1, state); });
            Assert.AreEqual("First text -added -0in the *paragraph*0.More text *added*0 laterHell-o-0", sb1.ToString());

            var sb2 = new StringBuilder();
            xc.Process2(state => { TextDiffChangeTest.ProcesTest(sb2, state); });
            Assert.AreEqual("First text in the *html page*0.More text *coming*0 later+.+0Hell", sb2.ToString());

        }
    }
}
