using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace DiffMatchPatchSharp
{
    public class FlowDocumentDiffChanges: MarkupDiffChanges<Run>
    {
        public void AddChange(DiffMatchPatch dmp, object blocks1, object blocks2)
        {
            var texts1 = GetElementTexts(blocks1);
            var texts2 = GetElementTexts(blocks2);
            var text1 = AddTextElements(TextElements1, texts1);
            var text2 = AddTextElements(TextElements2, texts2);
            base.AddChange(dmp, text1, text2);
        }

        protected IEnumerable<(Run node, string text)> GetElementTexts(object element)
        {
            foreach(var run in GetElementContent(element, true).OfType<Run>())
            {
                yield return (run, run.Text ?? string.Empty);
            }
        }

        protected override void ReplaceNode(TextElement textElement, IList<TextPart> parts)
        {
            var run = textElement.Node;
            switch(run.Parent)
            {
                case Paragraph p:
                    foreach(var part in parts.Reverse()) {
                        p.Inlines.InsertAfter(run, CreateChangeRun(run, part.Change, part.Text));
                    }
                    p.Inlines.Remove(run);
                    break;
            }
        }

        private Inline CreateChangeRun(Run run, DiffChange change, string text)
        {
            var r = CreateRun(run);
            r.Text = text;
            var c = GetColor(change);
            if (change != DiffChange.None)
            {
                r.Background = new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B));
            }
            return r;
        }

        public class RunPart
        {
            public string Text { get; set; }

            public DiffChange Change { get; set; }
        }

        public static IEnumerable<object> GetElementContent(object item, bool recursive, bool includeParent = true, bool includeCollections = false)
        {
            switch (item)
            {
                case ICollection collection:
                    if (includeCollections)
                    {
                        yield return collection;
                    }
                    foreach (var contentItem in collection)
                    {
                        foreach (var contentSubItem in GetElementContent(contentItem, recursive, true, includeCollections))
                        {
                            yield return contentSubItem;
                        }
                    }
                    break;
                default:
                    if (includeParent)
                    {
                        yield return item;
                    }
                    if (recursive || !includeParent)
                    {
                        foreach (var subItem in Visit(item))
                        {
                            foreach (var contentItem in GetElementContent(subItem, recursive, true, includeCollections))
                            {
                                yield return contentItem;
                            }
                        }
                    }
                    break;
            }
        }

        public static bool AreElementsEqual(object left, object right)
        {
            var leftObj = left as DependencyObject;
            var rightObj = right as DependencyObject;
            if (leftObj == null || rightObj == null || left.GetType() != right.GetType())
            {
                return false;
            }
            foreach (var property in TextElementProperties)
            {
                var leftValue = leftObj.GetValue(property);
                var rightValue = rightObj.GetValue(property);
                if (leftValue != DependencyProperty.UnsetValue || rightValue != DependencyProperty.UnsetValue)
                {
                    if (!AreValuesEqual(leftValue, rightValue))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool AreValuesEqual(object leftValue, object rightValue)
        {
            if (Equals(leftValue, rightValue))
            {
                return true;
            }
            if (leftValue is TextEffectCollection && rightValue is TextEffectCollection)
            {
                var leftCol = leftValue as TextEffectCollection;
                var rightCol = rightValue as TextEffectCollection;
                if (leftCol.Count != rightCol.Count)
                {
                    return false;
                }
                foreach (var both in leftCol.OrderBy(c => c.GetHashCode()).Zip(rightCol.OrderBy(c => c.GetHashCode()), (a, b) => new { Left = a, Right = b }))
                {
                    if (!AreValuesEqual(both.Left, both.Right))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private static Run CreateRun(Run copyFrom)
        {
            var newRun = new Run();
            CopyProperty(copyFrom, newRun, TextElementProperties);
            return newRun;
        }

        private static void CopyProperty(DependencyObject original, DependencyObject copy, IEnumerable<DependencyProperty> properties)
        {
            foreach (var property in properties)
            {
                var oldValue = original.ReadLocalValue(property);
                if (oldValue != DependencyProperty.UnsetValue)
                {
                    copy.SetValue(property, oldValue);
                }
            }
        }

        private static IEnumerable<object> Visit(object item)
        {
            switch (item)
            {
                case FlowDocument doc:
                    yield return doc.Blocks;
                    break;
                case Section section:
                    yield return section.Blocks;
                    break;
                case BlockUIContainer blockUI:
                    yield return blockUI.Child;
                    break;
                case InlineUIContainer inlineUI:
                    yield return inlineUI.Child;
                    break;
                case Span span:
                    yield return span.Inlines;
                    break;
                case AnchoredBlock block:
                    yield return block.Blocks;
                    break;
                case Paragraph paragraph:
                    yield return paragraph.Inlines;
                    break;
                case Table table:
                    yield return table.RowGroups;
                    yield return table.Columns;
                    break;
                case TableRowGroup rowGroup:
                    yield return rowGroup.Rows;
                    break;
                case TableRow tableRow:
                    yield return tableRow.Cells;
                    break;
                case TableCell cell:
                    yield return cell.Blocks;
                    break;
                case List list:
                    yield return list.ListItems;
                    break;
                case ListItem listItem:
                    yield return listItem.Blocks;
                    break;
                default:
                    yield break;
            }
        }

        private static readonly ICollection<DependencyProperty> TextElementProperties = new[]
        {
            Paragraph.TextIndentProperty,
            Block.TextAlignmentProperty,
            Block.MarginProperty,
            System.Windows.Documents.TextElement.FontFamilyProperty,
            System.Windows.Documents.TextElement.FontSizeProperty,
            System.Windows.Documents.TextElement.FontStretchProperty,
            System.Windows.Documents.TextElement.FontStyleProperty,
            System.Windows.Documents.TextElement.FontWeightProperty,
            System.Windows.Documents.TextElement.BackgroundProperty,
            System.Windows.Documents.TextElement.TextEffectsProperty,
            System.Windows.Documents.TextElement.ForegroundProperty
        };
    }
}
