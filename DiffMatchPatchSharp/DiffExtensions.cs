using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace DiffMatchPatchSharp
{
    public class DiffExtensions
    {
        private readonly List<Diff> diff;

        public IReadOnlyList<Diff> Diff => diff.AsReadOnly();

        public Color DeletedColor { get; set; } = Color.Tomato;

        public Color AddedColor { get; set; } = Color.YellowGreen;

        public Color ChangedColor { get; set; } = Color.Yellow;

        public DiffExtensions(DiffMatchPatch dmp, string before, string after, bool cleanupSemantics = true)
        {
            diff = dmp.DiffMain(before, after);
            if (cleanupSemantics && diff.Count > 2)
            {
                dmp.DiffCleanupSemantic(diff);
            }
        }

        public DiffExtensions(List<Diff> diff)
        {
            this.diff = diff;
        }

        public string GetHtmlBefore()
        {
            var r = new StringBuilder();
            for (var i = 0; i < diff.Count; i++)
            {
                if (diff[i].Operation == Operation.Equal)
                {
                    r.Append(diff[i].Text);
                }
                else if (diff[i].Operation == Operation.Delete)
                {
                    if (i + 1 < diff.Count && diff[i + 1].Operation == Operation.Insert)
                    {
                        // changed
                        r.Append(Mark(diff[i].Text, ChangedColor));
                    }
                    else
                    {
                        // deleted
                        r.Append(Mark(diff[i].Text, DeletedColor));
                    }
                }
            }
            return r.ToString();
        }

        public string GetHtmlAfter()
        {
            var r = new StringBuilder();
            for (var i = 0; i < diff.Count; i++)
            {
                if (diff[i].Operation == Operation.Equal)
                {
                    r.Append(diff[i].Text);
                }
                else if (diff[i].Operation == Operation.Insert)
                {
                    if (i > 0 && diff[i - 1].Operation == Operation.Delete)
                    {
                        // changed
                        r.Append(Mark(diff[i].Text, ChangedColor));
                    }
                    else
                    {
                        // added
                        r.Append(Mark(diff[i].Text, AddedColor));
                    }
                }
            }
            return r.ToString();
        }

        protected string GetHtmlColor(Color color)
        {
            return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }

        protected virtual string Mark(string text, Color color)
        {
            return $"<span style=\"background-color: {GetHtmlColor(color)}\">{text}</span>";
        }
    }
}
