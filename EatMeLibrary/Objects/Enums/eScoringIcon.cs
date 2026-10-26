namespace EatMeLibrary
{
    /// <summary>
    /// The sprite drawn before an item on the game screen's scoring bar (ScoringBar.GetItems).
    /// </summary>
    public enum eScoringIcon : byte
    {
        /// <summary>The enemy team's flag.</summary>
        EnemyFlag,

        /// <summary>The player's own team's flag.</summary>
        OwnFlag,

        /// <summary>An enemy pac, in the default shape.</summary>
        EnemyPac,

        /// <summary>The Norm ghost.</summary>
        Ghost,
    }
}
