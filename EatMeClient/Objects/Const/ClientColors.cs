using EatMeLibrary;
using Microsoft.Xna.Framework;

namespace EatMeClient
{
    public static class ClientColors
    {
        // Colors

        public static readonly Color COLOR_PAC_RED = new(1f, 0f, 0f);
        public static readonly Color COLOR_PAC_BLUE = new(0f, 0.5f, 1f);

        // The player's own pac: lighter shades of the team colors so it stands out in a crowd.

        public static readonly Color COLOR_PAC_RED_OWN = new(255, 110, 110);
        public static readonly Color COLOR_PAC_BLUE_OWN = new(110, 190, 255);

        // Spawn floors: dark but clear of black, with matching luminance (about 0.047) so neither team's stands out more.

        public static readonly Color COLOR_SPAWN_RED = new(255, 80, 110);
        public static readonly Color COLOR_SPAWN_BLUE = new(80, 150, 255);

        public static readonly Color COLOR_PAC_WHITE = new(1f, 1f, 1f);
        public static readonly Color COLOR_PAC_LIGHT_GREY = new(0.6f, 0.6f, 0.6f);

        public static readonly Color COLOR_PAC_YELLOW = new(255, 255, 0);
        public static readonly Color COLOR_PAC_MAZE_BLUE = new(33, 33, 222);
        public static readonly Color COLOR_PAC_DOTS_BLUE = new(130, 180, 255);
        public static readonly Color COLOR_PAC_DOTS_RED = new(255, 140, 140);

        /// <summary>
        /// The empty (greyed out) part of the player state panel's meter.
        /// </summary>
        public static readonly Color COLOR_POWER_UP_BAR_EMPTY = new(70, 70, 70);

        // Mini map markers: warp doors (#777) and power ups (royal purple).

        public static readonly Color COLOR_MINI_MAP_WARP = new(0x77, 0x77, 0x77);
        public static readonly Color COLOR_MINI_MAP_POWER_UP = new(120, 81, 169);

        // ghost body colors are each personality's GhostPersonality.Color (library)
        public static readonly Color COLOR_GHOST_SCARED = new(33, 33, 222);

        // Transparency

        public static readonly Color COLOR_TRANSPARENT_BLUE10 = new(0.0f, 0.0f, 0.2f, 0.1f);

        /// <summary>
        /// Half-transparent black drawn behind text on menu screens so it reads against the background image.
        /// </summary>
        public static readonly Color COLOR_TEXT_PANEL = new(0f, 0f, 0f, 0.5f);

        // Dark menu: text drawn straight onto the bright title background (title menu and every menu screen).

        public static readonly Color COLOR_DARK_MENU_ITEM = new(16, 16, 112);
        public static readonly Color COLOR_DARK_MENU_SELECTED = new(176, 0, 0);
        public static readonly Color COLOR_DARK_MENU_HEADING = new(16, 16, 112);
        public static readonly Color COLOR_DARK_TITLE_ITEM = new(8, 8, 24);
        public static readonly Color COLOR_DARK_MENU_HINT = new(64, 64, 96);
        public static readonly Color COLOR_DARK_MENU_SLIDER_BACKGROUND = new(128, 128, 160);
        public static readonly Color COLOR_DARK_MENU_SLIDER_SELECTED_BACKGROUND = new(224, 144, 144);
        public static readonly Color COLOR_DARK_MENU_FIELD_BACKGROUND = new(128, 128, 160);
        public static readonly Color COLOR_DARK_MENU_FIELD_SELECTED_BACKGROUND = new(224, 144, 144);

        // Light menu: text drawn over the dark game board (the in-game menu).

        public static readonly Color COLOR_LIGHT_MENU_ITEM = new(255, 255, 255);
        public static readonly Color COLOR_LIGHT_MENU_SELECTED = new(255, 255, 0);
        public static readonly Color COLOR_LIGHT_MENU_PANEL = new(0f, 0f, 0f, 0.5f);

        /// <summary>
        /// Flags flicker between yellow and their team's color, showing each for this long.
        /// </summary>
        public const float FLAG_FLICKER_SECONDS = 0.1f;

        /// <summary>
        /// A library FastColor as a MonoGame color.
        /// </summary>
        public static Color ToColor(FastColor color)
        {
            return new Color(color.PackedValue);
        }

        /// <summary>
        /// A MonoGame color as a library FastColor.
        /// </summary>
        public static FastColor ToFastColor(Color color)
        {
            return new FastColor(color.PackedValue);
        }

        public static Color GetTeamColor(eTeam team)
        {
            return team == eTeam.Blue ? COLOR_PAC_BLUE : COLOR_PAC_RED;
        }

        /// <summary>
        /// A pac's color: its team color, or the lighter shade of it when it is the player's own pac.
        /// </summary>
        public static Color GetPacColor(eTeam team, bool isOwn)
        {
            if (!isOwn)
                return GetTeamColor(team);

            return team == eTeam.Blue ? COLOR_PAC_BLUE_OWN : COLOR_PAC_RED_OWN;
        }

        public static Color GetSpawnColor(eTeam team)
        {
            return team == eTeam.Blue ? COLOR_SPAWN_BLUE : COLOR_SPAWN_RED;
        }

        /// <summary>
        /// A flag's tint at animationTime seconds: it flickers between yellow and its team's color.
        /// </summary>
        public static Color GetFlagColor(eTeam team, float animationTime)
        {
            var showTeamColor = animationTime % (FLAG_FLICKER_SECONDS * 2f) < FLAG_FLICKER_SECONDS;
            return showTeamColor ? GetTeamColor(team) : COLOR_PAC_YELLOW;
        }
    }
}
