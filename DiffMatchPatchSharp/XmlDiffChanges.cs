using System.Collections.Generic;
using System.Xml.Linq;

namespace DiffMatchPatchSharp
{
    public class XmlDiffChanges: DiffChanges
    {
        public IList<(XElement element1, XElement element2)> Elements { get; } = new List<(XElement element1, XElement element2)>();

        public void AddChanges(DiffMatchPatch dmp, XContainer doc1, XContainer doc2, bool cleanupSemantics)
        {
            var texts = GetElementTexts(doc1, doc2);
            foreach (var text in texts)
            {
                Elements.Add((text.element1, text.element2));
                AddChange(dmp, text.text1, text.text2, cleanupSemantics);
            }
        }

        /// <summary>
        /// Gets plain text from elements, returns null if element has no text
        /// </summary>
        /// <param name="element">Element to get text from</param>
        /// <returns>Element's text</returns>
        protected virtual string GetElementText(XElement element)
        {
            if (element != null && !element.IsEmpty && !element.HasElements)
            {
                return element.Value;
            }
            return null;
        }

        private IEnumerable<(XElement element1, string text1, XElement element2, string text2)> GetElementTexts(XContainer doc1, XContainer doc2)
        {
            var pairs = GetElements(doc1, doc2);
            foreach (var e in pairs)
            {
                var text1 = GetElementText(e.element1);
                var text2 = GetElementText(e.element2);

                if (text1 != null || text2 != null)
                {
                    yield return (e.element1, text1, e.element2, text2);
                }
            }
        }

        private static IEnumerable<(XElement element1, XElement element2)> GetElements(XContainer doc1, XContainer doc2)
        {
            using (var e1 = doc1.Descendants().GetEnumerator())
            {
                using (var e2 = doc2.Descendants().GetEnumerator())
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
                                yield return (e1.Current, null);
                            }
                            while (e1.MoveNext());
                            yield break;
                        }
                    }
                    if (e2.MoveNext())
                    {
                        do
                        {
                            yield return (null, e2.Current);
                        } while (e2.MoveNext());
                    }
                }
            }
        }
    }
}
