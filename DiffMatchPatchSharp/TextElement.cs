using System.Xml.Linq;

namespace DiffMatchPatchSharp
{
    public class TextElement
    {
        public XNode Node { get; internal set; }

        public int Offset { get; internal set; }
    }
}
