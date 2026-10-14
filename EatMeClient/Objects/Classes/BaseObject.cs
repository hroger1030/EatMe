using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EatMeClient
{
    public abstract class BaseObject
    {
        public Texture2D Texture { get; set; }
        public Rectangle SpriteSource { get; set; }

        public FastG.Point2 Location { get; set; }
        public float Facing { get; set; }
        public float Scale { get; set; }

        public BaseObject() { }
    } 
}
