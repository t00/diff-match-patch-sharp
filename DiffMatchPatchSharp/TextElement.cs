using System.Xml.Linq;

namespace DiffMatchPatchSharp
{
    public class TextElement
    {
        public XNode Node { get; internal set; }

        public int Offset { get; set; }

        public int Length { get; set; }

        public override string ToString()
        {
            return $"{Node} ({Offset} {Length})";
        }
    }
}
