namespace EatMeLibrary
{
    public static partial class Const
    {
        /// <summary>
        /// How much larger is the title than the menu items?
        /// </summary>

        public const string GAME_TITLE = "Eat Me";
        public const string CLIENT_INSTANCE_MUTEX_NAME = "Local\\EatMeClient";
        public const string STRING_STORY = "Story";

        // Attract screen section titles
        public const string STRING_ENEMIES = "Enemies";
        public const string STRING_ATTRACT_POWER_UPS = "Power-Ups";
        public const string STRING_STARRING = "Starring";
        public const string STRING_HIGH_SCORES = "High Scores";
        public const string STRING_GAME_SETTINGS = "Game Settings";
        public const string STRING_QUIT = "Quit";
        public const string MOTD_DEFAULT = "Find Friends, Play Games. Visit https://GamrTable.com";
        public const string STRING_BACK = "Back To Menu";
        public const string STRING_MUSIC_VOLUME = "Music Volume";
        public const string STRING_SOUND_VOLUME = "Sound Volume";
        public const string STRING_CONTROLS = "Controls";
        public const string STRING_BRIGHTNESS = "Brightness";
        public const string STRING_BLOOM = "Color Bloom";
        public const string STRING_CONTROLS_MOUSE_KEYBOARD = "Mouse + Keyboard";
        public const string STRING_CONTROLS_GAME_PAD = "Game Pad";
        public const string STRING_DISPLAY = "Display";
        public const string STRING_DISPLAY_FULL_SCREEN = "Full Screen";
        public const string STRING_DISPLAY_WINDOWED = "Windowed";
        public const string STRING_PAC_SHAPE = "Pac";
        public const string STRING_RANK = "Rank";
        public const string STRING_NAME = "Name";
        public const string STRING_SCORE = "Score";
        public const string STRING_DATE = "Date";
        public const string STRING_RESUME_GAME = "Resume Game";
        public const string STRING_QUIT_GAME = "Disconnect from server";

        // Join game screen
        public const string STRING_JOIN_GAME = "Join Game";
        public const string STRING_PLAYER_NAME = "Name";
        public const string STRING_TEAM = "Preferred Team";
        public const string STRING_TEAM_RED = "Red";
        public const string STRING_TEAM_BLUE = "Blue";
        public const string STRING_TEAM_EITHER = "Either";
        public const string STRING_SERVER_ADDRESS = "Server Address";
        public const string STRING_JOIN = "Join";
        public const string STRING_LAN_GAMES = "LAN Games";
        public const string STRING_SEARCHING = "Searching...";
        public const string STRING_INVALID_NAME = "Name must be 1-20 letters or digits";
        public const string STRING_INVALID_ADDRESS = "Enter a server address";

        // Game screen
        public const string STRING_CONNECTING = "Connecting...";
        public const string STRING_JOINING = "Joining...";
        public const string STRING_PRESS_ESCAPE = "Press Escape to go back";
        public const string STRING_REJECT_SERVER_FULL = "Server is full";
        public const string STRING_REJECT_INVALID_NAME = "Server refused your name";
        public const string STRING_REJECT_VERSION = "Server runs a different version";
        public const string STRING_REJECT_KICKED = "You were kicked from the server";
        public const string STRING_RED_WINS = "Red team wins!";
        public const string STRING_BLUE_WINS = "Blue team wins!";
        public const string STRING_NEXT_LEVEL_IN = "Next Level in";
        public const string STRING_LEVEL = "Level";
        public const string STRING_POWER_UP_NONE = "No power up";
        public const string STRING_POWER_PILL = "Power Pill";
        public const string STRING_CHERRY_TWIN = "Cherry Twin";
        public const string STRING_SWEET_TOOTH = "Sweet Tooth";
        public const string STRING_VITAMIN_C = "Vitamin C";
        public const string STRING_DOCTOR_AWAY = "Doctor Away";
        public const string STRING_MELON_BELLY = "Melon Belly";
        public const string STRING_WARP_SPEED = "Warp Speed";
        public const string STRING_DINNER_BELL = "Dinner Bell";
        public const string STRING_LOCKDOWN = "Lockdown";
        public const string STRING_TWISTER = "Twister";
        public const string STRING_INVINCIBLE = "Invincible";
        public const string STRING_REINFORCEMENTS = "Reinforce";
        public const string STRING_BOMB = "Bomb";
        public const string STRING_RACCOON_TAIL = "Raccoon Tail";
        public const string STRING_SWORD_SWING = "Throw Sword";
        public const string STRING_HOLY_FIRE = "Holy Fire";
        public const string STRING_FIREBALL = "Fireball";
        public const string STRING_WARP = "Warp";
        public const string STRING_BANANA_PEEL = "Banana Peel";
        public const string STRING_BLOCK_DROP = "Block Drop";
        public const string STRING_HAMMER_TIME = "Hammer Time";
        public const string STRING_BFG = "BFG 9000";
        public const string STRING_SUPER_SIZE = "Super Size";
        public const string STRING_ENERGY_TANK = "Energy Tank";
        public const string STRING_SCREW_ATTACK = "Screw Attack";
        public const string STRING_PEPPER = "Pepper";
        public const string STRING_SPREAD_SHOT = "Spread Shot";
        public const string STRING_FORCE_FIELD = "Force Field";
        public const string STRING_POW = "POW";
        public const string STRING_FULL_HEAL = "Full Heal";
        public const string STRING_GOTCHA = "Gotcha";
        public const string STRING_BLUE_SHELL = "Blue Shell";

        // Respawn countdown, shown while the own pac is dead
        public const string STRING_RESPAWNING_IN = "Respawning in";
        public const string STRING_SECONDS = "seconds";
        public const string STRING_LOCKED_OUT = "Locked out!";
        public const string STRING_VICTORY = "Victory!";
        public const string STRING_STARTING_IN = "Starting in...";

        // Game screen stats bar
        public const string STRING_STATS = "Stats:";
        public const string STRING_SIZE = "Size";
        public const string STRING_STUNNED = "Stunned!";
        public const string STRING_FLAGS = "Flags";
        public const string STRING_GHOSTS = "Ghosts";
        public const string STRING_CHOMPS = "Chomps";
        public const string STRING_RESCUES = "Rescues";
        public const string STRING_POWER_UPS = "Power Ups";
        public const string STRING_PING = "Ping";
        public const string STRING_PING_UNKNOWN = "--";
        public const string STRING_MS = "ms";
        public const string STRING_PLAYER_LIST_BOT = "<bot>";

        // Performance overlay (F1), string.Format templates filled in by ClientPerf
        public const string STRING_PERF_FPS = "FPS {0:0.0}  frame avg {1:0.00} max {2:0.00} ms";
        public const string STRING_PERF_UPDATE = "Update avg {0:0.00} p99 {1:0.00} max {2:0.00} ms";
        public const string STRING_PERF_DRAW = "Draw avg {0:0.00} p99 {1:0.00} max {2:0.00} ms";
        public const string STRING_PERF_MEMORY = "GC {0}  alloc {1:0.00} MB/s  heap {2:0.0} MB";
        public const string STRING_PERF_NETWORK = "Snapshots {0:0.0}/s  in {1:0.0} KB/s  out {2:0.0} KB/s";
    }
}
