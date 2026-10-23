namespace EatMeLibrary
{
    public static partial class Const
    {
        // Images

        public const string IMAGE_TITLE_BACKGROUND = "Images/title";
        // Animated title logo, magenta keyed out (LogoSprites)
        public const string IMAGE_LOGO = "Images/logo (800x200x4)";
        // White triangle pointing right, magenta color-keyed to transparent
        public const string IMAGE_POINTER = "Images/pointer";
        public const string IMAGE_DOT = "Images/dot0";
        // Floor tiles drawn under the spawn rooms (team colors) and the ghost pen
        public const string IMAGE_PAC_BASE = "Images/base0.png (48x48x6)";
        public const string IMAGE_FLAG0 = "Images/flag0";
        public const string IMAGE_FLAG_BASE = "Images/flagbase0";
        public const string IMAGE_PAC_SHEET = "Images/Pacs (60x60x9)";
        public const string IMAGE_WIN_BLUE = "Images/winblue";
        public const string IMAGE_WIN_RED = "Images/winred";

        // Pac death: eleven frames left to right, played once
        public const string IMAGE_PAC_DEATH_SHEET = "Images/death (60x60x11)";

        // Ghost sheet: four frames looking right, down, left and up, with a white body the client recolors per ghost (GhostPalette)
        public const string IMAGE_GHOST = "Images/ghost(60x60x4)";

        // Bonus items: one 60x60 cell per ePowerUp, in enum order, eight to a row (see PowerUpSprites)
        public const string IMAGE_POWER_UP_SHEET = "Images/powerups(60x60x32)";
        // Projectiles and objects power up effects put in the maze, two frames each (WeaponEffectSprites)
        public const string IMAGE_WEAPON_EFFECTS = "Images/WeaponEffects(60x60x22)";
        // Mini map icons: one 16x16 cell per eMiniMapIcon, in enum order, in one row, white tinted when drawn (MiniMapSprites)
        public const string IMAGE_MINI_MAP_ICONS = "Images/MiniMapIcons(16x16x10)";

        // Wall corner tile, joined below and right; walls are drawn in quarters cut from it (see WallTiles)
        public const string IMAGE_WALL = "Images/wall0";

        // Wall fill mask, laid out like IMAGE_WALL: white inside the tube, tinted with the level's wall fill color
        public const string IMAGE_WALL_FILL = "Images/wallfill";

        // Starfield tiled behind the maze on levels with LevelInfo.BackgroundImage set (Pac Paradise)
        public const string IMAGE_SPACE_FILL = "Images/SpaceFill (256x256x1)";
    }
}
