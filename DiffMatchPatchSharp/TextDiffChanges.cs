using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiffMatchPatchSharp;

public class TextDiffChanges: DiffChanges
{
    public new void AddChange(DiffMatchPatch dmp, string text1, string text2)
    {
        base.AddChange(dmp, text1, text2);
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

    public string GetHtmlChange1()
    {
        var r = new StringBuilder();
        Process1(state => Mark(r, state));
        return r.ToString();
    }

    public string GetHtmlChange2()
    {
        var r = new StringBuilder();
        Process2(state => Mark(r, state));
        return r.ToString();
    }

    protected virtual string Mark(string text, DiffChange change)
    {
        var color = GetColor(change);
        return $"<span style=\"background-color: {DiffHtmlExtensions.GetHtmlColor(color)}\">{text}</span>";
    }

    private void Mark(StringBuilder sb, DiffState state)
    {
        if (state.Change == DiffChange.None)
        {
            sb.Append(state.Diff.Text);
        }
        else
        {
            sb.Append(Mark(state.Diff.Text, state.Change));
        }
    }
}