namespace EatMeClient
{
    public static class XnaHelper
    {
        public static Microsoft.Xna.Framework.Vector2 ToXnaVector(this FastG.Point2 v) => new(v.X, v.Y);

        public static FastG.Point2 ToGeometryPoint(this Microsoft.Xna.Framework.Vector2 v) => new(v.X, v.Y);
    }
}
