namespace EatMeLibrary
{
    /// <summary>
    /// One item on the scoring bar: a label, a sprite (or none) and the value after it.
    /// </summary>
    public struct ScoringBarItem
    {
        public string Label;
        public eScoringIcon Icon;
        public string Value;

        public ScoringBarItem(string label, eScoringIcon icon, string value)
        {
            Label = label;
            Icon = icon;
            Value = value;
        }
    }
}
