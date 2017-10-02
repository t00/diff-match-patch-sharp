using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace DiffMatchPatchSharp
{
    public abstract class DiffChanges
    {
        public enum Change
        {
            None,
            Added,
            Changed,
            Deleted
        }

        public IList<IList<Diff>> Diffs { get; } = new List<IList<Diff>>();

        public bool CleanupSemantics { get; set; } = true;

        public Color AddedColor { get; set; } = Color.YellowGreen;

        public Color DeletedColor { get; set; } = Color.Tomato;

        public Color ChangedColor { get; set; } = Color.Yellow;

        public int Add(IList<Diff> diff)
        {
            Diffs.Add(diff);
            return Diffs.Count - 1;
        }

        public void AddChange(DiffMatchPatch dmp, string text1, string text2)
        {
            Add(CreateDiff(dmp, text1, text2));
        }

        public void AddChanges(DiffMatchPatch dmp, IEnumerable<string> texts1, IEnumerable<string> texts2)
        {
            foreach (var diff in texts1.Zip(texts2, (a, b) => new { Text1 = a, Text2 = b }))
            {
                AddChange(dmp, diff.Text1, diff.Text2);
            }
        }

        public void AddChangesParallel(DiffMatchPatch dmp, IEnumerable<string> texts1, IEnumerable<string> texts2)
        {
            var texts = texts1.Zip(texts2, (a, b) => new { text1 = a, text2 = b }).Select((t, i) => new { t.text1, t.text2, index = i });
            var bag = new ConcurrentBag<(int index, IList<Diff> diff)>();
            texts.AsParallel().ForAll(text =>
            {
                var change = CreateDiff(dmp, text.text1, text.text2);
                bag.Add((text.index, change));
            });
            foreach (var b in bag.OrderBy(b => b.index))
            {
                Add(b.diff);
            }
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

        protected IList<Diff> CreateDiff(DiffMatchPatch dmp, string text1, string text2)
        {
            var d = dmp.DiffMain(text1 ?? string.Empty, text2 ?? string.Empty);
            DiffCreated(dmp, d);
            return d;
        }

        protected virtual void DiffCreated(DiffMatchPatch dmp, List<Diff> d)
        {
            if (CleanupSemantics && d.Count > 2)
            {
                dmp.DiffCleanupSemantic(d);
            }
        }
    }
}
