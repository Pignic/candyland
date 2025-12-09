using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Candyland.Dialog
{
    /// <summary>
    /// Renders NPC portraits
    /// </summary>
    public class PortraitRenderer
    {
        private Rectangle _bounds;
        private Texture2D _pixelTexture;
        private Dictionary<string, Texture2D> _portraits;

        public PortraitRenderer(Rectangle bounds, Texture2D pixelTexture)
        {
            _bounds = bounds;
            _pixelTexture = pixelTexture;
            _portraits = new Dictionary<string, Texture2D>();
        }

        /// <summary>
        /// Load a portrait texture
        /// </summary>
        public void LoadPortrait(string key, Texture2D texture)
        {
            _portraits[key] = texture;
        }

        /// <summary>
        /// Draw the portrait
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, string portraitKey)
        {
            // Draw border/frame
            DrawFrame(spriteBatch);

            // Draw portrait if available
            if (_portraits.ContainsKey(portraitKey))
            {
                Texture2D portrait = _portraits[portraitKey];
                spriteBatch.Draw(portrait, _bounds, Color.White);
            }
            else
            {
                // Draw placeholder if no portrait loaded
                DrawPlaceholder(spriteBatch, portraitKey);
            }
        }

        /// <summary>
        /// Draw portrait frame/border
        /// </summary>
        private void DrawFrame(SpriteBatch spriteBatch)
        {
            int thickness = 2;

            // Top
            spriteBatch.Draw(_pixelTexture, new Rectangle(_bounds.X - thickness, _bounds.Y - thickness, _bounds.Width + thickness * 2, thickness), Color.Gold);
            // Bottom
            spriteBatch.Draw(_pixelTexture, new Rectangle(_bounds.X - thickness, _bounds.Bottom, _bounds.Width + thickness * 2, thickness), Color.Gold);
            // Left
            spriteBatch.Draw(_pixelTexture, new Rectangle(_bounds.X - thickness, _bounds.Y - thickness, thickness, _bounds.Height + thickness * 2), Color.Gold);
            // Right
            spriteBatch.Draw(_pixelTexture, new Rectangle(_bounds.Right, _bounds.Y - thickness, thickness, _bounds.Height + thickness * 2), Color.Gold);
        }

        /// <summary>
        /// Draw placeholder portrait with colored background
        /// </summary>
        private void DrawPlaceholder(SpriteBatch spriteBatch, string portraitKey)
        {
            // Generate a color based on the portrait key
            Color placeholderColor = GetColorFromString(portraitKey);

            // Draw colored background
            spriteBatch.Draw(_pixelTexture, _bounds, placeholderColor);

            // Draw simple face placeholder (optional)
            DrawSimpleFace(spriteBatch);
        }

        /// <summary>
        /// Draw a simple face placeholder
        /// </summary>
        private void DrawSimpleFace(SpriteBatch spriteBatch)
        {
            int centerX = _bounds.X + _bounds.Width / 2;
            int centerY = _bounds.Y + _bounds.Height / 2;

            // Head circle (approximated with rectangles)
            int headSize = _bounds.Width / 2;
            Rectangle head = new Rectangle(
                centerX - headSize / 2,
                centerY - headSize / 2,
                headSize,
                headSize
            );
            spriteBatch.Draw(_pixelTexture, head, Color.White * 0.3f);

            // Eyes
            int eyeSize = 8;
            int eyeOffset = 15;
            Rectangle leftEye = new Rectangle(centerX - eyeOffset, centerY - 10, eyeSize, eyeSize);
            Rectangle rightEye = new Rectangle(centerX + eyeOffset - eyeSize, centerY - 10, eyeSize, eyeSize);
            spriteBatch.Draw(_pixelTexture, leftEye, Color.Black);
            spriteBatch.Draw(_pixelTexture, rightEye, Color.Black);

            // Mouth
            Rectangle mouth = new Rectangle(centerX - 12, centerY + 10, 24, 3);
            spriteBatch.Draw(_pixelTexture, mouth, Color.Black);
        }

        /// <summary>
        /// Generate a consistent color from a string
        /// </summary>
        private Color GetColorFromString(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Color.Gray;

            // Simple hash to color conversion
            int hash = text.GetHashCode();
            byte r = (byte)((hash & 0xFF0000) >> 16);
            byte g = (byte)((hash & 0x00FF00) >> 8);
            byte b = (byte)(hash & 0x0000FF);

            // Ensure colors are not too dark
            r = (byte)(r / 2 + 128);
            g = (byte)(g / 2 + 128);
            b = (byte)(b / 2 + 128);

            return new Color(r, g, b);
        }
    }
}