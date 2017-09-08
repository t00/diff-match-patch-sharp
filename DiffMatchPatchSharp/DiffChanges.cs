﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace DiffMatchPatchSharp
{
    public class DiffChanges
    {
        public enum Change
        {
            None,
            Added,
            Changed,
            Deleted
        }

        public IList<List<Diff>> Diffs { get;  } = new List<List<Diff>>();

        public Color AddedColor { get; set; } = Color.YellowGreen;

        public Color DeletedColor { get; set; } = Color.Tomato;

        public Color ChangedColor { get; set; } = Color.Yellow;

        public DiffChanges(DiffMatchPatch dmp, string text1, string text2, bool cleanupSemantics = true)
        {
            AddDiff(dmp, text1, text2, cleanupSemantics);
        }

        public DiffChanges(DiffMatchPatch dmp, IEnumerable<string> texts1, IEnumerable<string> texts2, bool cleanupSemantics = true)
        {
            foreach (var diff in texts1.Zip(texts2, (a, b) => new { Text1 = a, Text2 = b }))
            {
                AddDiff(dmp, diff.Text1, diff.Text2, cleanupSemantics);
            }
        }

        public DiffChanges(List<Diff> diff)
        {
            Diffs.Add(diff);
        }

        public void AddDiff(DiffMatchPatch dmp, string text1, string text2, bool cleanupSemantics)
        {
            var d = dmp.DiffMain(text1, text2);
            if (cleanupSemantics && d.Count > 2)
            {
                dmp.DiffCleanupSemantic(d);
            }
            this.Diffs.Add(d);
        }

        public void Process1(Action<Change, string, int> action)
        {
            for (var diffIndex = 0; diffIndex < Diffs.Count; diffIndex++)
            {
                var diff = Diffs[diffIndex];
                for (var opIndex = 0; opIndex < diff.Count; opIndex++)
                {
                    if (diff[opIndex].Operation == Operation.Equal)
                    {
                        action(Change.None, diff[opIndex].Text, diffIndex);
                    }
                    else if (diff[opIndex].Operation == Operation.Delete)
                    {
                        if (opIndex + 1 < diff.Count && diff[opIndex + 1].Operation == Operation.Insert)
                        {
                            // changed
                            action(Change.Changed, diff[opIndex].Text, diffIndex);
                        }
                        else
                        {
                            // deleted
                            action(Change.Deleted, diff[opIndex].Text, diffIndex);
                        }
                    }
                }
            }
        }

        public void Process2(Action<Change, string, int> action)
        {
            for (var diffIndex = 0; diffIndex < Diffs.Count; diffIndex++)
            {
                var diff = Diffs[diffIndex];
                for (var opIndex = 0; opIndex < diff.Count; opIndex++)
                {
                    if (diff[opIndex].Operation == Operation.Equal)
                    {
                        action(Change.None, diff[opIndex].Text, diffIndex);
                    }
                    else if (diff[opIndex].Operation == Operation.Insert)
                    {
                        if (opIndex > 0 && diff[opIndex - 1].Operation == Operation.Delete)
                        {
                            // changed
                            action(Change.Changed, diff[opIndex].Text, diffIndex);
                        }
                        else
                        {
                            // added
                            action(Change.Added, diff[opIndex].Text, diffIndex);
                        }
                    }
                }
            }
        }

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

        public Color GetColor(Change change)
        {
            switch (change)
            {
                case Change.Changed:
                    return ChangedColor;
                case Change.Added:
                    return AddedColor;
                case Change.Deleted:
                    return DeletedColor;
                default:
                    return Color.Empty;
            }
        }

        protected virtual string Mark(StringBuilder sb, string text, Color color)
        {
            return $"<span style=\"background-color: {GetHtmlColor(color)}\">{text}</span>";
        }

        protected string GetHtmlColor(Color color)
        {
            return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }

        private void Mark(StringBuilder sb, string text, Change change)
        {
            if (change == Change.None)
            {
                sb.Append(text);
            }
            else
            {
                Mark(sb, text, GetColor(change));
            }
        }
    }
}