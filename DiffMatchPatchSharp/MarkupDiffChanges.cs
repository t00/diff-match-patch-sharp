using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiffMatchPatchSharp
{
    public abstract class MarkupDiffChanges<TNode>: DiffChanges
    {
        public void ProcessChanges()
        {
            var parts1 = new List<TextPart>();
            var parts2 = new List<TextPart>();
            Process1(state => { ProcessElement(TextElements1, parts1, state); });
            Process2(state => { ProcessElement(TextElements2, parts2, state); });
            ApplyChanges(TextElements1, parts1);
            ApplyChanges(TextElements2, parts2);
        }

        protected string AddTextElements(IList<TextElement> textElements, IEnumerable<(TNode, string)> texts)
        {
            var sb = new StringBuilder();
            foreach (var text in texts)
            {
                textElements.Add(new TextElement
                {
                    Node = text.Item1,
                    Offset = sb.Length,
                    Length = text.Item2?.Length ?? 0
                });
                if (text.Item2 != null)
                {
                    sb.Append(text.Item2);
                }
            }
            return sb.ToString();
        }

        protected IList<TextElement> TextElements1 { get; } = new List<TextElement>();

        protected IList<TextElement> TextElements2 { get; } = new List<TextElement>();

        protected virtual void ProcessElement(IList<TextElement> textElements, IList<TextPart> parts, DiffState state)
        {
            parts.Add(new TextPart { Text = state.Diff.Text, Change = state.Change, Offset = state.Offset });
        }

        protected virtual void ApplyChanges(IList<TextElement> textElements, IList<TextPart> parts)
        {
            var newNodes = textElements.Where(x => x.Node != null).Select(x => new { Element = x, Nodes = new List<TextPart>() }).ToList();
            var partIndex = 0;
            var partOffset = 0;
            var elementIndex = 0;
            while (elementIndex < newNodes.Count && partIndex < parts.Count)
            {
                var textElement = newNodes[elementIndex].Element;
                do
                {
                    var part = parts[partIndex];
                    var partText = part.Text?.Substring(partOffset) ?? string.Empty;
                    var partEnd = part.Offset + partOffset + partText.Length;
                    var elementEnd = textElement.Offset + textElement.Length;
                    if (partEnd > elementEnd)
                    {
                        var partLength = partText.Length - (partEnd - elementEnd);
                        partText = partText.Substring(0, partLength);
                        newNodes[elementIndex++].Nodes.Add(CreateChangePart(parts[partIndex].Change, partText, partOffset));
                        partOffset += partLength;
                    }
                    else
                    {
                        newNodes[elementIndex].Nodes.Add(CreateChangePart(parts[partIndex].Change, partText, partOffset));
                        partIndex++;
                        partOffset = 0;
                    }
                    textElement = newNodes[elementIndex].Element;
                } while (elementIndex < newNodes.Count && partIndex < parts.Count && parts[partIndex].Offset < textElement.Offset + textElement.Length);
                elementIndex++;
            }

            for (var idx = newNodes.Count - 1; idx >= 0; idx--)
            {
                ReplaceNode(textElements[idx], newNodes[idx].Nodes);
            }
        }

        protected virtual TextPart CreateChangePart(DiffChange change, string partText, int offset)
        {
            return new TextPart
            {
                Change = change,
                Text = partText,
                Offset = offset
            };
        }

        protected abstract void ReplaceNode(TextElement textElement, IList<TextPart> parts);

        protected class TextElement
        {
            public TNode Node { get; set; }

            public int Offset { get; set; }

            public int Length { get; set; }

            public override string ToString()
            {
                return $"{Node} ({Offset} {Length})";
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
