using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DiffMatchPatchSharp
{
    public class HtmlElementDiffChanges: XmlElementDiffChanges
    {
        public void ProcessChanges()
        {
            var leftDict = new Dictionary<int, IList<XNode>>();
            var rightDict = new Dictionary<int, IList<XNode>>();
            Process1(state => { ProcessElement(leftDict, state); });
            Process2(state => { ProcessElement(rightDict, state); });
            ApplyChanges(TextElements.Select(e => e.change1).ToList(), leftDict);
            ApplyChanges(TextElements.Select(e => e.change2).ToList(), rightDict);
        }

        protected virtual void ProcessElement(IDictionary<int, IList<XNode>> dict, DiffState state)
        {
            if (!dict.TryGetValue(state.ChangeIndex, out var nodes))
            {
                nodes = new List<XNode>();
                dict.Add(state.ChangeIndex, nodes);
            }
            var partNode = CreateHtmlChangeElement(state.Change, state.Diff.Text);
            nodes.Add(partNode);
        }

        protected virtual void ApplyChanges(IList<TextElement> toList, Dictionary<int, IList<XNode>> dict)
        {
            for (var idx = toList.Count - 1; idx >= 0; idx--)
            {
                if (dict.TryGetValue(idx, out var changes))
                {
                    toList[idx].Node.ReplaceWith(changes);
                }
            }
        }
    }
}
