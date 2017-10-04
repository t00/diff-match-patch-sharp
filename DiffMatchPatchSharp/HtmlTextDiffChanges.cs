using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DiffMatchPatchSharp
{
    public class HtmlTextDiffChanges: XmlTextDiffChanges
    {
        public void ProcessChanges()
        {
            var parts1 = new List<TextPart>();
            var elements1 = TextElements.Select(e => e.change1).ToList();
            var parts2 = new List<TextPart>();
            var elements2 = TextElements.Select(e => e.change2).ToList();
            Process1(state => { ProcessElement(elements1, parts1, state); });
            Process2(state => { ProcessElement(elements2, parts2, state); });
            ApplyChanges(elements1, parts1);
            ApplyChanges(elements2, parts2);
        }

        protected virtual void ProcessElement(IList<TextElement> textElements, IList<TextPart> parts, DiffState state)
        {
            parts.Add(new TextPart { Text = state.Diff.Text, Change = state.Change, Offset = state.Offset });
        }

        protected virtual void ApplyChanges(IList<TextElement> toList, IList<TextPart> parts)
        {
            var newNodes = toList.Where(x => x.Node != null).Select(x => new { Element = x, Nodes = new List<XNode>() }).ToList();
            var partIndex = 0;
            for (var el = 0; el < newNodes.Count; el++)
            {
                if (partIndex >= parts.Count)
                {
                    break;
                }
                var textElement = newNodes[el].Element;
                do
                {
                    var part = parts[partIndex];
                    var partText = part.Text ?? string.Empty;
                    var partEnd = part.Offset + partText.Length;
                    var elementEnd = textElement.Offset + textElement.Length;
                    if (partEnd > elementEnd && el + 1 < newNodes.Count)
                    {
                        newNodes[el].Nodes.Add(CreateHtmlChangeElement(parts[partIndex].Change, partText.Substring(0, partText.Length - (partEnd - elementEnd))));
                        newNodes[el + 1].Nodes.Add(CreateHtmlChangeElement(parts[partIndex].Change, partText.Substring(partText.Length - (partEnd - elementEnd))));
                    }
                    else
                    {
                        newNodes[el].Nodes.Add(CreateHtmlChangeElement(parts[partIndex].Change, partText));
                    }
                    partIndex++;
                } while(partIndex < parts.Count && parts[partIndex].Offset < textElement.Offset + textElement.Length);
            }

            for (var idx = newNodes.Count - 1; idx >= 0; idx--)
            {
                toList[idx].Node.ReplaceWith(newNodes[idx].Nodes);
            }
        }

        protected class TextPart
        {
            public string Text { get; set; }

            public DiffChange Change { get; set; }

            public int Offset { get; set; }

            public override string ToString()
            {
                return $"{Text} ({Change}, {Offset})";
            }
        }
    }
}
