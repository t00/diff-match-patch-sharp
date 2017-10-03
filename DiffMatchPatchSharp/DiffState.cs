namespace DiffMatchPatchSharp
{
    public class DiffState
    {
        public Diff Diff { get; internal set; }

        public DiffChange Change { get; internal set; }

        public int ChangeIndex { get; internal set; }

        public int Offset { get; internal set; }
    }
}
