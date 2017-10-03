using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DiffMatchPatchSharp
{
    public class HtmlElementDiffChanges: XmlElementDiffChanges
    {
        public void ProcessChanges()
        {
            var leftDict = new Dictionary<int, IList<TextElement>>();
            var rightDict = new Dictionary<int, IList<TextElement>>();
            Process1(state => { ProcessElement(leftDict, state); });
            Process2(state => { ProcessElement(rightDict, state); });
            ApplyChanges(Elements.Select(e => e.change1).ToList(), leftDict);
            ApplyChanges(Elements.Select(e => e.change2).ToList(), rightDict);
        }

        protected virtual void ProcessElement(IDictionary<int, IList<TextElement>> dict, DiffState state)
        {
            XNode partNode;
            if (state.Change != DiffChange.None)
            {
                var span = CreateHtmlChangeElement(state.Change);
                span.Value = state.Diff.Text;
                partNode = span;
            }
            else
            {
                partNode = new XText(state.Diff.Text);
            }
            if (!dict.TryGetValue(state.ChangeIndex, out var nodes))
            {
                nodes = new List<TextElement>();
                dict.Add(state.ChangeIndex, nodes);
            }
            nodes.Add(new TextElement { Node = partNode, Offset = state.Offset });
        }

        protected virtual void ApplyChanges(IList<TextElement> toList, Dictionary<int, IList<TextElement>> dict)
        {
            for (var idx = toList.Count - 1; idx >= 0; idx--)
            {
                if (dict.TryGetValue(idx, out var changes))
                {
                    toList[idx].Node.ReplaceWith(changes.Select(x => x.Node));
                }
            }
        }
    }
}
