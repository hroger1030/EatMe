using System;

namespace EatMeLibrary
{
    /// <summary>
    /// How loud a game event's sound plays. Global events (flag changes and a team about to win) are heard at full
    /// volume wherever they happen; anything else fades with distance from the board's centre (the player's own pac)
    /// and can't be heard once it is off the board.
    /// </summary>
    public static class SoundMath
    {
        /// <summary>
        /// Whether everyone hears an event at full volume, wherever it happens.
        /// </summary>
        public static bool IsGlobalEvent(eGameEvent eventType)
        {
            return eventType is eGameEvent.FlagCaptured or eGameEvent.FlagLost or eGameEvent.FlagReturned or eGameEvent.AboutToWin;
        }

        /// <summary>
        /// Volume, 0 to 1, of a sound offsetX, offsetY pixels from the centre of a board width by height pixels: 1 at
        /// the centre, falling in a straight line to 0 at the board's edge, and 0 beyond it.
        /// </summary>
        public static float GetOnScreenVolume(float offsetX, float offsetY, float width, float height)
        {
            if (width <= 0f || height <= 0f)
                return 0f;

            var edge = Math.Max(Math.Abs(offsetX) / (width * 0.5f), Math.Abs(offsetY) / (height * 0.5f));
            return Math.Max(0f, 1f - edge);
        }

        /// <summary>
        /// Volume, 0 to 1, of an event's sound: 1 for a global event, otherwise GetOnScreenVolume.
        /// </summary>
        public static float GetEventVolume(eGameEvent eventType, float offsetX, float offsetY, float width, float height)
        {
            return IsGlobalEvent(eventType) ? 1f : GetOnScreenVolume(offsetX, offsetY, width, height);
        }
    }
}
