namespace EatMeLibrary
{
    /// <summary>
    /// The pieces of the client's frame that ClientPerf times, one PerfSection each.
    /// </summary>
    public enum eClientPerfSection
    {
        /// <summary>
        /// The whole of Game.Update.
        /// </summary>
        Update,

        /// <summary>
        /// The whole of Game.Draw, up to handing the frame to the graphics card.
        /// </summary>
        Draw,

        /// <summary>
        /// Time from one Draw starting to the next: one frame on screen.
        /// </summary>
        Frame,
    }
}
