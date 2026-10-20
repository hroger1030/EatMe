namespace EatMeClient
{
    /// <summary>
    /// The layers the game screen is drawn in, bottom to top: MultiPlayerGameScreen.Draw draws them in this order, so
    /// anything in a layer is drawn over everything in the layers before it. Give anything new the layer it belongs in.
    /// </summary>
    public enum eDrawLayer
    {
        /// <summary>
        /// Behind the game: the board's background color and image. Scenery only, nothing in it is part of the game.
        /// </summary>
        Background,

        /// <summary>
        /// The maze: room floors, flag bases, walls, dots and power pills.
        /// </summary>
        Maze,

        /// <summary>
        /// Things lying in the maze: loose flags, power ups and weapon effects.
        /// </summary>
        Items,

        /// <summary>
        /// Death animations, under every pac and ghost so a pac dies beneath whatever killed it.
        /// </summary>
        Deaths,

        /// <summary>
        /// Pacs (with their trails, carried flags and names) and ghosts.
        /// </summary>
        Actors,

        /// <summary>
        /// Short lived effects over the actors: wall debris.
        /// </summary>
        Effects,

        /// <summary>
        /// Above everything in the maze, for things that fly over the walls: the eyes of dead ghosts.
        /// </summary>
        Overhead,

        /// <summary>
        /// The score and stats bars (or the status text) and the flag pointers.
        /// </summary>
        Hud,

        /// <summary>
        /// The top most layer, nothing is drawn over it: the mini map, the player state panel, the respawn countdown
        /// and the in-game menu.
        /// </summary>
        Ui,
    }
}
