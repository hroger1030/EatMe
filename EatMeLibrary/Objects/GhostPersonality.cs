namespace EatMeLibrary
{
    /// <summary>
    /// Who a ghost is: its name (drawn under it), its body color, and how it behaves (GhostBrain).
    /// The full list is GhostPersonalities.ALL; MapMaker.PickGhostLineup picks each level's.
    /// </summary>
    public class GhostPersonality
    {
        /// <summary>
        /// Index in GhostPersonalities.ALL.
        /// </summary>
        public int Id { get; }

        public string Name { get; }

        /// <summary>
        /// Body color, put on the white ghost sheet with GhostPalette.ReplaceColor. Not used on rainbow levels.
        /// </summary>
        public FastColor Color { get; }

        public eGhostBehavior Behavior { get; }

        /// <summary>
        /// Times GameConfiguration.GhostSpeed.
        /// </summary>
        public float SpeedScale { get; }

        /// <summary>
        /// Times GameConfiguration.GhostVisionDistance, for spotting pacs to chase and powered pacs to run from.
        /// </summary>
        public float VisionScale { get; }

        public GhostPersonality(int id, string name, FastColor color, eGhostBehavior behavior, float speedScale = 1f, float visionScale = 1f)
        {
            Id = id;
            Name = name;
            Color = color;
            Behavior = behavior;
            SpeedScale = speedScale;
            VisionScale = visionScale;
        }
    }
}
