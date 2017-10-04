using System;
using System.Collections.Generic;
using System.Drawing;

namespace DiffMatchPatchSharp
{
    public abstract class DiffChanges
    {
        public IList<IList<Diff>> Diffs { get; } = new List<IList<Diff>>();

        public bool CleanupSemantics { get; set; } = true;

        public Color AddedColor { get; set; } = Color.YellowGreen;

        public Color DeletedColor { get; set; } = Color.Tomato;

        public Color ChangedColor { get; set; } = Color.Yellow;

        protected int Add(IList<Diff> diff)
        {
            Diffs.Add(diff);
            return Diffs.Count - 1;
        }

        protected void AddChange(DiffMatchPatch dmp, string text1, string text2)
        {
            Add(CreateDiff(dmp, text1, text2));
        }

        public void Process1(Action<DiffState> action)
        {
            var state = new DiffState();
            for (var diffIndex = 0; diffIndex < Diffs.Count; diffIndex++)
            {
                state.ChangeIndex = diffIndex;
                var diff = Diffs[diffIndex];
                for (var opIndex = 0; opIndex < diff.Count; opIndex++)
                {
                    state.Diff = diff[opIndex];
                    if (diff[opIndex].Operation == Operation.Equal)
                    {
                        state.Change = DiffChange.None;
                        action(state);
                        state.Offset += state.Diff.Text.Length;
                    }
                    else if (diff[opIndex].Operation == Operation.Delete)
                    {
                        if (opIndex + 1 < diff.Count && diff[opIndex + 1].Operation == Operation.Insert)
                        {
                            state.Change = DiffChange.Changed;
                            action(state);
                        }
                        else
                        {
                            state.Change = DiffChange.Deleted;
                            action(state);
                        }
                        state.Offset += state.Diff.Text.Length;
                    }
                }
            }
        }

        public void Process2(Action<DiffState> action)
        {
            var state = new DiffState();
            for (var diffIndex = 0; diffIndex < Diffs.Count; diffIndex++)
            {
                state.ChangeIndex = diffIndex;
                var diff = Diffs[diffIndex];
                for (var opIndex = 0; opIndex < diff.Count; opIndex++)
                {
                    state.Diff = diff[opIndex];
                    if (diff[opIndex].Operation == Operation.Equal)
                    {
                        state.Change = DiffChange.None;
                        action(state);
                        state.Offset += state.Diff.Text.Length;
                    }
                    else if (diff[opIndex].Operation == Operation.Insert)
                    {
                        if (opIndex > 0 && diff[opIndex - 1].Operation == Operation.Delete)
                        {
                            state.Change = DiffChange.Changed;
                            action(state);
                        }
                        else
                        {
                            state.Change = DiffChange.Added;
                            action(state);
                        }
                        state.Offset += state.Diff.Text.Length;
                    }
                }
            }
        }

        public Color GetColor(DiffChange change)
        {
            switch (change)
            {
                case DiffChange.Changed:
                    return ChangedColor;
                case DiffChange.Added:
                    return AddedColor;
                case DiffChange.Deleted:
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
