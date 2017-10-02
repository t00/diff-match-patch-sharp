using System.Text;
using NUnit.Framework;

namespace DiffMatchPatchSharp.Tests
{
    [TestFixture]
    public class DiffChangeTest
    {
        [Test]
        public void TestSingleStringProcess()
        {
            const string text1 = "There is a bird in a bush";
            const string text2 = "There is bird in the bush and it is dead";
            var dc = new TextDiffChanges();
            dc.AddChange(new DiffMatchPatch(), text1, text2);

            var sb1 = new StringBuilder();
            dc.Process1((ch, text, idx) => { ProcesTest(sb1, ch, text); });
            Assert.AreEqual("There is -a -bird in *a* bush", sb1.ToString());

            var sb2 = new StringBuilder();
            dc.Process2((ch, text, idx) => { ProcesTest(sb2, ch, text); });
            Assert.AreEqual("There is bird in *the* bush+ and it is dead+", sb2.ToString());
        }

        [Test]
        public void TestMultipleStringProcess()
        {
            var texts1 = new[] { "There is a bird in a bush which is green.", "Another bird is eating cherries.", "A crazy dog is barking at the tree." };
            var texts2 = new[] { "There is one bird in a bush which is green and is eating cherries.", "Another bird is flying around.", "The dog is at the wrong tree." };
            var dc = new TextDiffChanges();
            dc.AddChanges(new DiffMatchPatch(), texts1, texts2);

            var sb1 = new StringBuilder();
            dc.Process1((ch, text, idx) => { ProcesTest(sb1, ch, text, idx); });
            Assert.AreEqual("There is *a*0 bird in a bush which is green.Another bird is *eat*1ing *cherries*1.*A crazy*2 dog is -barking -2at the tree.", sb1.ToString());

            var sb2 = new StringBuilder();
            dc.Process2((ch, text, idx) => { ProcesTest(sb2, ch, text, idx); });
            Assert.AreEqual("There is *one*0 bird in a bush which is green+ and is eating cherries+0.Another bird is *fly*1ing *around*1.*The*2 dog is at the+ wrong+2 tree.", sb2.ToString());
        }

        [Test]
        public void TestMultipleStringParallel()
        {
            var texts1 = new[] { "There is a bird in a bush which is green.", "Another bird is eating cherries.", "A crazy dog is barking at the tree." };
            var texts2 = new[] { "There is one bird in a bush which is green and is eating cherries.", "Another bird is flying around.", "The dog is at the wrong tree." };
            var dc = new TextDiffChanges();
            dc.AddChangesParallel(new DiffMatchPatch(), texts1, texts2);

            var sb1 = new StringBuilder();
            dc.Process1((ch, text, idx) => { ProcesTest(sb1, ch, text, idx); });
            Assert.AreEqual("There is *a*0 bird in a bush which is green.Another bird is *eat*1ing *cherries*1.*A crazy*2 dog is -barking -2at the tree.", sb1.ToString());

            var sb2 = new StringBuilder();
            dc.Process2((ch, text, idx) => { ProcesTest(sb2, ch, text, idx); });
            Assert.AreEqual("There is *one*0 bird in a bush which is green+ and is eating cherries+0.Another bird is *fly*1ing *around*1.*The*2 dog is at the+ wrong+2 tree.", sb2.ToString());
        }

        public static void ProcesTest(StringBuilder sb, DiffChanges.Change ch, string text, int? index = null)
        {
            switch (ch)
            {
                case DiffChanges.Change.None:
                    sb.Append(text);
                    break;
                case DiffChanges.Change.Added:
                    sb.Append('+').Append(text).Append('+').Append(index);
                    break;
                case DiffChanges.Change.Changed:
                    sb.Append('*').Append(text).Append('*').Append(index);
                    break;
                case DiffChanges.Change.Deleted:
                    sb.Append('-').Append(text).Append('-').Append(index);
                    break;
            }
        }
    }
}
