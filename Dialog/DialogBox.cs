using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Candyland.Core;

namespace Candyland.Dialog
{
    /// <summary>
    /// Displays NPC name and text with typewriter effect
    /// </summary>
    public class DialogBox
    {
        private Rectangle _bounds;
        private BitmapFont _font;
        private Texture2D _pixelTexture;

        // Text content
        private string _npcName;
        private string _fullText;
        private string _displayedText;

        // Typewriter effect
        private float _typewriterTimer;
        private const float CHARS_PER_SECOND = 30f;
        private int _currentCharIndex;

        public bool IsTextComplete => _currentCharIndex >= _fullText.Length;

        public DialogBox(Rectangle bounds, BitmapFont font, Texture2D pixelTexture)
        {
            _bounds = bounds;
            _font = font;
            _pixelTexture = pixelTexture;
            _fullText = "";
            _displayedText = "";
            _npcName = "";
        }

        /// <summary>
        /// Set new text to display
        /// </summary>
        public void SetText(string npcName, string text)
        {
            _npcName = npcName;
            _fullText = text ?? "";
            _currentCharIndex = 0;
            _displayedText = "";
            _typewriterTimer = 0f;
        }

        /// <summary>
        /// Reset typewriter effect
        /// </summary>
        public void ResetTypewriter()
        {
            _currentCharIndex = 0;
            _displayedText = "";
            _typewriterTimer = 0f;
        }

        /// <summary>
        /// Complete text immediately (skip typewriter)
        /// </summary>
        public void CompleteText()
        {
            _currentCharIndex = _fullText.Length;
            _displayedText = _fullText;
        }

        /// <summary>
        /// Update typewriter effect
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (IsTextComplete)
                return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _typewriterTimer += deltaTime;

            // Calculate how many characters to show
            int targetCharIndex = (int)(_typewriterTimer * CHARS_PER_SECOND);

            if (targetCharIndex > _currentCharIndex)
            {
                _currentCharIndex = System.Math.Min(targetCharIndex, _fullText.Length);
                _displayedText = _fullText.Substring(0, _currentCharIndex);
            }
        }

        /// <summary>
        /// Draw the dialog box
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw background
            spriteBatch.Draw(_pixelTexture, _bounds, Color.Black * 0.85f);

            // Draw border
            DrawBorder(spriteBatch, _bounds, Color.White, 3);

            // Draw NPC name
            int nameY = _bounds.Y + 10;
            _font.DrawText(spriteBatch, _npcName, new Vector2(_bounds.X + 120, nameY), Color.Yellow);

            // Draw text with word wrapping
            int textY = nameY + 25;
            int textX = _bounds.X + 120;
            int maxWidth = _bounds.Width - 130;

            DrawWrappedText(spriteBatch, _displayedText, textX, textY, maxWidth);
        }

        /// <summary>
        /// Draw text with word wrapping
        /// </summary>
        private void DrawWrappedText(SpriteBatch spriteBatch, string text, int x, int y, int maxWidth)
        {
            if (string.IsNullOrEmpty(text))
                return;

            string[] words = text.Split(' ');
            string currentLine = "";
            int lineHeight = 20;
            int currentY = y;

            foreach (string word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                int lineWidth = _font.MeasureString(testLine);

                if (lineWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
                {
                    // Draw current line and start new one
                    _font.DrawText(spriteBatch, currentLine, new Vector2(x, currentY), Color.White);
                    currentY += lineHeight;
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }

            // Draw remaining text
            if (!string.IsNullOrEmpty(currentLine))
            {
                _font.DrawText(spriteBatch, currentLine, new Vector2(x, currentY), Color.White);
            }
        }

        /// <summary>
        /// Draw border around rectangle
        /// </summary>
        private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color, int thickness)
        {
            // Top
            spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), color);
            // Bottom
            spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), color);
            // Left
            spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), color);
            // Right
            spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), color);
        }
    }
}