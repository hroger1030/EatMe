using EatMeLibrary;
using Microsoft.Xna.Framework;

namespace EatMeClient
{
    public static class ColorHelper
    {
        public static Color GenerateRandomColor()
        {
            var r = RandHelper.Int(255);
            var g = RandHelper.Int(255);
            var b = RandHelper.Int(255);

            return new Color(r, g, b);
        }

        /// <summary>
        /// For use when we need a light tint, ex for atmospheres and glow effects.
        /// </summary>
        public static Color GenerateRandomColorLight()
        {
            var r = RandHelper.Int(128, 255);
            var g = RandHelper.Int(128, 255);
            var b = RandHelper.Int(128, 255);

            return new Color(r, g, b);
        }

        public static Color GenerateRandomRichHueColor()
        {
            var hue = RandHelper.UnitFloat() * 360f;
            var saturation = 0.75f + (RandHelper.UnitFloat() * 0.25f);
            var value = 0.7f + (RandHelper.UnitFloat() * 0.3f);

            return ClientColors.ToColor(FastColor.FromHsv(hue, saturation, value));
        }
    }
}