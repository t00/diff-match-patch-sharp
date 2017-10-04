using System.Collections.Generic;
using System.Linq;

namespace DiffMatchPatchSharp
{
    public abstract class MarkupDiffChanges<TNode>: DiffChanges
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

        protected IList<(TextElement change1, TextElement change2)> TextElements { get; } = new List<(TextElement, TextElement)>();

        protected static IEnumerable<(TItem, TItem)> GetNodes<TItem>(IEnumerable<TItem> items1, IEnumerable<TItem> items2)
        {
            using (var e1 = items1.GetEnumerator())
            {
                using (var e2 = items2.GetEnumerator())
                {
                    while (e1.MoveNext())
                    {
                        if (e2.MoveNext())
                        {
                            yield return (e1.Current, e2.Current);
                        }
                        else
                        {
                            do
                            {
                                yield return (e1.Current, default(TItem));
                            }
                            while (e1.MoveNext());
                            yield break;
                        }
                    }
                    if (e2.MoveNext())
                    {
                        do
                        {
                            yield return (default(TItem), e2.Current);
                        } while (e2.MoveNext());
                    }
                }
            }
        }

        protected virtual void ProcessElement(IList<TextElement> textElements, IList<TextPart> parts, DiffState state)
        {
            parts.Add(new TextPart { Text = state.Diff.Text, Change = state.Change, Offset = state.Offset });
        }

        protected virtual void ApplyChanges(IList<TextElement> textElements, IList<TextPart> parts)
        {
            var newNodes = textElements.Where(x => x.Node != null).Select(x => new { Element = x, Nodes = new List<TNode>() }).ToList();
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
                        newNodes[elementIndex++].Nodes.Add(CreateChangeNode(parts[partIndex].Change, partText));
                        partOffset += partLength;
                    }
                    else
                    {
                        newNodes[elementIndex].Nodes.Add(CreateChangeNode(parts[partIndex].Change, partText));
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

        protected abstract TNode CreateChangeNode(DiffChange change, string partText);

        protected abstract void ReplaceNode(TextElement textElement, IList<TNode> nodes);

        protected class TextElement
        {
            public TNode Node { get; internal set; }

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
