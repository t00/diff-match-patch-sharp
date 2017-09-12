using System;
using System.Collections.Generic;
using System.Drawing;
using NUnit.Framework;

namespace DiffMatchPatchSharp.Tests
{
    [TestFixture]
    public class DiffHtmlExtensionsTest
    {
        [Test]
        public void TestHtmlColor()
        {
            var col1 = DiffHtmlExtensions.GetHtmlColor(Color.FromArgb(123, 0xbe, 0xef, 0x00));
            Assert.AreEqual("#BEEF00", col1);

            var col2 = DiffHtmlExtensions.GetHtmlColor(Color.FromArgb(88, 0x12, 0x34, 0x56));
            Assert.AreEqual("#123456", col2);
        }

        [Test]
        public void TestCssParse()
        {
            var css = " background-color : #123456; font-weight : 'bold' ;";
            var style = DiffHtmlExtensions.ReadInlineStyle(css);
            var dict = new Dictionary<string,string>{ { "background-color", "#123456" }, { "font-weight", "bold"} };
            CollectionAssert.AreEquivalent(dict, style);
        }

        [Test]
        public void TestCssParseApostropheSpacing()
        {
            var css = "before:don't;font-weight:' bo ld '";
            var style = DiffHtmlExtensions.ReadInlineStyle(css);
            var dict = new Dictionary<string, string> { { "before", "don't" }, { "font-weight", " bo ld " } };
            CollectionAssert.AreEquivalent(dict, style);
        }

        [Test]
        public void TestCssParseDoubleSemicolon()
        {
            var css = "background-color:#123456;;font-weight:'bo ld';;";
            var style = DiffHtmlExtensions.ReadInlineStyle(css);
            var dict = new Dictionary<string, string> { { "background-color", "#123456" }, { "font-weight", "bo ld" } };
            CollectionAssert.AreEquivalent(dict, style);
        }

        [Test]
        public void TestSetStyleText()
        {
            var css = "background-color:#123456;;font-weight:'bo ld';;";
            var style = DiffHtmlExtensions.SetStyle(css, new Dictionary<string, string> {{"font-weight", "bold"}, {"font-face", "Verdana"}});
            Assert.AreEqual("background-color:#123456;font-weight:bold;font-face:Verdana;", style);
        }

        [Test]
        public void AreStylesEqualTest()
        {
            Assert.IsFalse(DiffHtmlExtensions.AreStylesEqual(null, null));
            Assert.IsFalse(DiffHtmlExtensions.AreStylesEqual(null, new Dictionary<string, string>()));
            Assert.IsTrue(DiffHtmlExtensions.AreStylesEqual(new Dictionary<string, string>(), new Dictionary<string, string>()));
            var css1 = "background-color:#beef17;font-weight:bold;font-face:Verdana;";
            var css2 = "font-weight: bold;background-color: '#BEEF17'; font-face: 'verdana';";
            Assert.IsTrue(DiffHtmlExtensions.AreStylesEqual(DiffHtmlExtensions.ReadInlineStyle(css1), DiffHtmlExtensions.ReadInlineStyle(css2), null, StringComparison.InvariantCultureIgnoreCase));
            Assert.IsFalse(DiffHtmlExtensions.AreStylesEqual(DiffHtmlExtensions.ReadInlineStyle(css1), DiffHtmlExtensions.ReadInlineStyle(css2), null, StringComparison.InvariantCulture));
            var css3 = "font-weight: bold;background-color: '#BEEF18'; font-face: 'verdana';";
            Assert.IsFalse(DiffHtmlExtensions.AreStylesEqual(DiffHtmlExtensions.ReadInlineStyle(css1), DiffHtmlExtensions.ReadInlineStyle(css3)));
            Assert.IsTrue(DiffHtmlExtensions.AreStylesEqual(DiffHtmlExtensions.ReadInlineStyle(css1), DiffHtmlExtensions.ReadInlineStyle(css3), new List<string> { "font-weight", "font-face" }));
            Assert.IsFalse(DiffHtmlExtensions.AreStylesEqual(DiffHtmlExtensions.ReadInlineStyle(css1), DiffHtmlExtensions.ReadInlineStyle(css3), new List<string> { "font-weight", "background-color" }));
        }
    }
}
