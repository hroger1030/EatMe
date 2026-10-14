namespace EatMeLibrary
{
    /// <summary>
    /// What the level music does when the player leaves the in-game menu (MusicPlaylist.GetResumeMusic).
    /// </summary>
    public enum eResumeMusic
    {
        /// <summary>
        /// Carry on with the track that was paused.
        /// </summary>
        Resume,

        /// <summary>
        /// A level started while paused, so start that level's track.
        /// </summary>
        PlayLevelTrack,

        /// <summary>
        /// The results are showing, which have no music.
        /// </summary>
        StaySilent,
    }
}
