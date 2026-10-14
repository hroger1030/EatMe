using System.Globalization;

namespace EatMeLibrary
{
    /// <summary>
    /// The client version text shown in the top right corner of the menu screens, and where it goes.
    /// </summary>
    public static class VersionLabelMath
    {
        /// <summary>
        /// "v" followed by the version, always with a dot whatever the player's locale.
        /// </summary>
        public static string GetVersionText(float version)
        {
            return "v" + version.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The text's left edge, in whole pixels, so its right edge sits margin pixels in from the right of a screen
        /// screenWidth wide.
        /// </summary>
        public static int GetTextX(float screenWidth, float textWidth, float margin)
        {
            return (int)(screenWidth - textWidth - margin);
        }
    }
}
