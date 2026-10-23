namespace EatMeLibrary
{
    public static partial class Const
    {
        // Music

        public const string SONG_TITLE_THEME = "Music/TitleTheme";

        // Sound effects

        public const string SFX_TICK = "Audio/SoundFX_Tic1";
        // Played once when a level starts
        // Loops while the own pac is eating dots
        public const string SFX_EAT = "Audio/SoundFX_Eat";
        // Loops while the own pac is alive and not eating dots
        public const string SFX_SIREN = "Audio/SoundFX_Siren";
        // Played once when the intermission (results) screen comes up
        public const string SFX_DEATH = "Audio/SoundFX_Death";
        public const string SFX_EAT_GHOST = "Audio/SoundFX_Eatghost";
        public const string SFX_FLAG_CAP = "Audio/SoundFX_FlagCap";
        public const string SFX_LEVEL_LOST = "Audio/SoundFX_LevelLost";
        public const string SFX_LEVEL_WON = "Audio/SoundFX_LevelWon";
        public const string SFX_POWER_PILL = "Audio/SoundFX_PowerPill";
        // Loops while the own pac is growing from standing on its own team's spawn (a rising charge)
        public const string SFX_SPAWN_CHARGE = "Audio/SoundFX_SpawnCharge";
        // A pac ate a power up
        public const string SFX_POWER_UP_EATEN = "Audio/SoundFX_PowerUpEaten";
        // A POW block went off (a low percussive boom)
        public const string SFX_POW = "Audio/SoundFX_Pow";
        // A pac respawned at once thanks to a teammate's Reinforcements (only that player hears it, at full volume)
        public const string SFX_ANGELIC_CHORUS = "Audio/SoundFX_AngelicChorus";
        // A monster ball caught a ghost, or the ghost got out
        public const string SFX_BLOOP = "Audio/SoundFX_Bloop";
        // A BFG ball burst: sizzling, crackling electricity (an in-world sound)
        public const string SFX_BFG_BURST = "Audio/SoundFX_BfgBurst";
        // Loops while any power up is in the level (one loop however many), a global sound heard everywhere at full volume
        public const string SFX_POWER_UP = "Audio/SoundFX_PowerUp";

        // Announcer voice clips, played to everyone at full volume (see Announcements)

        public const string SFX_MSG_RED_FLAG_DROPPED = "Audio/SoundFX_Msg_RedFlagDropped";
        public const string SFX_MSG_BLUE_FLAG_DROPPED = "Audio/SoundFX_Msg_BlueFlagDropped";
        public const string SFX_MSG_RED_FLAG_RETURNED = "Audio/SoundFX_Msg_RedFlagReturned";
        public const string SFX_MSG_BLUE_FLAG_RETURNED = "Audio/SoundFX_Msg_BlueFlagReturned";
        public const string SFX_MSG_RED_FLAG_CAPTURED = "Audio/SoundFX_Msg_RedFlagCaptured";
        public const string SFX_MSG_BLUE_FLAG_CAPTURED = "Audio/SoundFX_Msg_BlueFlagCaptured";
        public const string SFX_MSG_RED_ABOUT_TO_WIN = "Audio/SoundFX_Msg_RedAboutToWin";
        public const string SFX_MSG_BLUE_ABOUT_TO_WIN = "Audio/SoundFX_Msg_BlueAboutToWin";
    }
}
