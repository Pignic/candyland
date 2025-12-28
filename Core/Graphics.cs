
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace EldmeresTale.Core
{
    public class Graphics
    {
        public static Texture2D CreateColoredTexture(GraphicsDevice graphicsDevice, int width, int height, Color color)
        {
            var texture = new Texture2D(graphicsDevice, width, height);
            var colorData = new Color[width * height];
            for (int i = 0; i < colorData.Length; i++)
                colorData[i] = color;
            texture.SetData(colorData);
            return texture;
        }
    }
}
