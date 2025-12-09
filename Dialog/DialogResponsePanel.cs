using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Candyland.Core;
using System.Collections.Generic;

namespace Candyland.Dialog
{
    /// <summary>
    /// Displays clickable response options for the player
    /// </summary>
    public class DialogResponsePanel
    {
        private Rectangle _bounds;
        private BitmapFont _font;
        private Texture2D _pixelTexture;

        // Response data
        private List<string> _responses;
        private List<Rectangle> _responseBounds;
        private int _selectedIndex;

        // Layout
        private const int RESPONSE_HEIGHT = 25;
        private const int RESPONSE_PADDING = 5;

        public DialogResponsePanel(Rectangle bounds, BitmapFont font, Texture2D pixelTexture)
        {
            _bounds = bounds;
            _font = font;
            _pixelTexture = pixelTexture;
            _responses = new List<string>();
            _responseBounds = new List<Rectangle>();
            _selectedIndex = 0;
        }

        /// <summary>
        /// Set the list of responses to display
        /// </summary>
        public void SetResponses(List<string> responses)
        {
            _responses = responses;
            _responseBounds.Clear();

            // Calculate bounds for each response
            int y = _bounds.Y;
            for (int i = 0; i < _responses.Count; i++)
            {
                Rectangle responseBounds = new Rectangle(
                    _bounds.X,
                    y,
                    _bounds.Width,
                    RESPONSE_HEIGHT
                );
                _responseBounds.Add(responseBounds);
                y += RESPONSE_HEIGHT + RESPONSE_PADDING;
            }
        }

        /// <summary>
        /// Set the selected response index
        /// </summary>
        public void SetSelectedIndex(int index)
        {
            if (index >= 0 && index < _responses.Count)
            {
                _selectedIndex = index;
            }
        }

        /// <summary>
        /// Get the index of the response under the mouse cursor
        /// </summary>
        public int GetHoveredResponseIndex(Point mousePosition)
        {
            for (int i = 0; i < _responseBounds.Count; i++)
            {
                if (_responseBounds[i].Contains(mousePosition))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Update the response panel
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // Currently no animation, but could add hover effects here
        }

        /// <summary>
        /// Draw the response panel
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (_responses.Count == 0)
                return;

            for (int i = 0; i < _responses.Count; i++)
            {
                bool isSelected = i == _selectedIndex;
                DrawResponse(spriteBatch, _responses[i], _responseBounds[i], isSelected, i);
            }
        }

        /// <summary>
        /// Draw a single response option
        /// </summary>
        private void DrawResponse(SpriteBatch spriteBatch, string text, Rectangle bounds, bool isSelected, int index)
        {
            // Draw background if selected
            if (isSelected)
            {
                spriteBatch.Draw(_pixelTexture, bounds, Color.White * 0.2f);
            }

            // Draw selection indicator
            string prefix = isSelected ? "> " : "  ";
            Color textColor = isSelected ? Color.Yellow : Color.LightGray;

            // Draw text
            _font.DrawText(spriteBatch, prefix + text, new Vector2(bounds.X + 5, bounds.Y + 5), textColor);

            // Draw subtle divider line between responses
            if (index < _responses.Count - 1)
            {
                Rectangle divider = new Rectangle(
                    bounds.X,
                    bounds.Bottom + RESPONSE_PADDING / 2,
                    bounds.Width,
                    1
                );
                spriteBatch.Draw(_pixelTexture, divider, Color.Gray * 0.3f);
            }
        }
    }
}