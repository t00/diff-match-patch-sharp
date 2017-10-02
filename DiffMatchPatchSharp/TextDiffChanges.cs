using System.Text;

namespace DiffMatchPatchSharp
{
    public class TextDiffChanges: DiffChanges
    {
        public string GetHtmlChange1()
        {
            var r = new StringBuilder();
            Process1((change, text, index) => Mark(r, text, change));
            return r.ToString();
        }

        public string GetHtmlChange2()
        {
            var r = new StringBuilder();
            Process2((change, text, index) => Mark(r, text, change));
            return r.ToString();
        }
        protected virtual string Mark(string text, Change change)
        {
            var color = GetColor(change);
            return $"<span style=\"background-color: {DiffHtmlExtensions.GetHtmlColor(color)}\">{text}</span>";
        }

        private void Mark(StringBuilder sb, string text, Change change)
        {
            if (change == Change.None)
            {
                sb.Append(text);
            }
            else
            {
                sb.Append(Mark(text, change));
            }
        }
    }
}
