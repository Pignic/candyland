using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Candyland.Core;
using System.Collections.Generic;

namespace Candyland.Dialog
{
    /// <summary>
    /// Main UI controller for the dialog system
    /// </summary>
    public class DialogUI
    {
        private DialogManager _dialogManager;
        private BitmapFont _font;
        private Texture2D _pixelTexture;

        // UI Components
        private DialogBox _dialogBox;
        private DialogResponsePanel _responsePanel;
        private PortraitRenderer _portraitRenderer;

        // Layout constants
        private const int DIALOG_BOX_HEIGHT = 200;
        private const int DIALOG_BOX_MARGIN = 20;
        private const int PORTRAIT_SIZE = 100;
        private const int PORTRAIT_MARGIN = 10;

        // Screen dimensions
        private int _screenWidth;
        private int _screenHeight;

        // Input tracking
        private MouseState _previousMouseState;
        private KeyboardState _previousKeyState;

        // Selected response (for keyboard navigation)
        private int _selectedResponseIndex = 0;

        public bool IsActive => _dialogManager.IsDialogActive;

        public DialogUI(DialogManager dialogManager, BitmapFont font, Texture2D pixelTexture, int screenWidth, int screenHeight)
        {
            _dialogManager = dialogManager;
            _font = font;
            _pixelTexture = pixelTexture;
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;

            // Calculate dialog box bounds (bottom of screen)
            Rectangle dialogBoxBounds = new Rectangle(
                DIALOG_BOX_MARGIN,
                _screenHeight - DIALOG_BOX_HEIGHT - DIALOG_BOX_MARGIN,
                _screenWidth - (DIALOG_BOX_MARGIN * 2),
                DIALOG_BOX_HEIGHT
            );

            // Calculate portrait bounds (left side of dialog box)
            Rectangle portraitBounds = new Rectangle(
                dialogBoxBounds.X + PORTRAIT_MARGIN,
                dialogBoxBounds.Y + PORTRAIT_MARGIN,
                PORTRAIT_SIZE,
                PORTRAIT_SIZE
            );

            // Calculate text area (right of portrait)
            Rectangle textBounds = new Rectangle(
                portraitBounds.Right + PORTRAIT_MARGIN,
                portraitBounds.Y,
                dialogBoxBounds.Width - PORTRAIT_SIZE - (PORTRAIT_MARGIN * 3),
                PORTRAIT_SIZE
            );

            // Calculate response panel bounds (below text)
            Rectangle responseBounds = new Rectangle(
                textBounds.X,
                textBounds.Bottom + 10,
                textBounds.Width,
                dialogBoxBounds.Bottom - (textBounds.Bottom + 10) - PORTRAIT_MARGIN
            );

            // Create components
            _dialogBox = new DialogBox(dialogBoxBounds, _font, _pixelTexture);
            _responsePanel = new DialogResponsePanel(responseBounds, _font, _pixelTexture);
            _portraitRenderer = new PortraitRenderer(portraitBounds, _pixelTexture);

            _previousMouseState = Mouse.GetState();
            _previousKeyState = Keyboard.GetState();
        }

        /// <summary>
        /// Update dialog UI
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!IsActive)
                return;

            var currentNode = _dialogManager.GetCurrentNode();
            if (currentNode == null)
                return;

            // Get localized text
            string npcText = _dialogManager.Localization.GetString(currentNode.TextKey);
            var npc = _dialogManager.GetCurrentNPC();
            string npcName = npc != null ? _dialogManager.Localization.GetString(npc.NameKey) : "???";

            // Update dialog box with typewriter effect
            _dialogBox.SetText(npcName, npcText);
            _dialogBox.Update(gameTime);

            // Get available responses
            var responses = _dialogManager.GetAvailableResponses();
            List<string> responseTexts = new List<string>();
            foreach (var response in responses)
            {
                responseTexts.Add(_dialogManager.Localization.GetString(response.TextKey));
            }

            // Update response panel
            _responsePanel.SetResponses(responseTexts);
            _responsePanel.Update(gameTime);

            // Handle input
            HandleInput(responses.Count);
        }

        /// <summary>
        /// Handle keyboard and mouse input
        /// </summary>
        private void HandleInput(int responseCount)
        {
            if (responseCount == 0)
                return;

            var mouseState = Mouse.GetState();
            var keyState = Keyboard.GetState();

            // Skip if text is still typing
            if (!_dialogBox.IsTextComplete)
            {
                // Allow skipping typewriter effect
                if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                {
                    _dialogBox.CompleteText();
                }

                _previousMouseState = mouseState;
                _previousKeyState = keyState;
                return;
            }

            // Keyboard navigation (Up/Down arrows)
            if (keyState.IsKeyDown(Keys.Down) && _previousKeyState.IsKeyUp(Keys.Down))
            {
                _selectedResponseIndex = (_selectedResponseIndex + 1) % responseCount;
                _responsePanel.SetSelectedIndex(_selectedResponseIndex);
            }
            if (keyState.IsKeyDown(Keys.Up) && _previousKeyState.IsKeyUp(Keys.Up))
            {
                _selectedResponseIndex = (_selectedResponseIndex - 1 + responseCount) % responseCount;
                _responsePanel.SetSelectedIndex(_selectedResponseIndex);
            }

            // Keyboard selection (Enter/Space)
            if ((keyState.IsKeyDown(Keys.Enter) || keyState.IsKeyDown(Keys.Space)) &&
                (_previousKeyState.IsKeyUp(Keys.Enter) && _previousKeyState.IsKeyUp(Keys.Space)))
            {
                ChooseResponse(_selectedResponseIndex);
            }

            // Mouse selection
            int hoveredIndex = _responsePanel.GetHoveredResponseIndex(mouseState.Position);
            if (hoveredIndex >= 0)
            {
                _selectedResponseIndex = hoveredIndex;
                _responsePanel.SetSelectedIndex(_selectedResponseIndex);

                if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                {
                    ChooseResponse(hoveredIndex);
                }
            }

            _previousMouseState = mouseState;
            _previousKeyState = keyState;
        }

        /// <summary>
        /// Choose a response and advance dialog
        /// </summary>
        private void ChooseResponse(int index)
        {
            _dialogManager.ChooseResponse(index);
            _selectedResponseIndex = 0;
            _responsePanel.SetSelectedIndex(0);

            // Reset typewriter for next node
            _dialogBox.ResetTypewriter();
        }

        /// <summary>
        /// Draw the dialog UI
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsActive)
                return;

            var currentNode = _dialogManager.GetCurrentNode();
            if (currentNode == null)
                return;

            // Draw dialog box background
            _dialogBox.Draw(spriteBatch);

            // Draw portrait
            var npc = _dialogManager.GetCurrentNPC();
            string portraitKey = currentNode.PortraitKey ?? (npc?.DefaultPortrait ?? "default");
            _portraitRenderer.Draw(spriteBatch, portraitKey);

            // Draw response panel
            if (_dialogBox.IsTextComplete)
            {
                _responsePanel.Draw(spriteBatch);
            }
            else
            {
                // Show "Press to continue" hint
                DrawContinueHint(spriteBatch);
            }
        }

        /// <summary>
        /// Draw continue hint while text is typing
        /// </summary>
        private void DrawContinueHint(SpriteBatch spriteBatch)
        {
            string hint = "[Click to skip]";
            int textWidth = _font.MeasureString(hint);
            Vector2 hintPos = new Vector2(
                _screenWidth - textWidth - DIALOG_BOX_MARGIN - 10,
                _screenHeight - DIALOG_BOX_MARGIN - 25
            );

            // Flashing effect
            float alpha = (float)((System.Math.Sin(System.DateTime.Now.Millisecond * 0.01) + 1) / 2);
            _font.DrawText(spriteBatch, hint, hintPos, Color.Gray * alpha);
        }

        /// <summary>
        /// Load portrait textures
        /// </summary>
        public void LoadPortrait(string portraitKey, Texture2D texture)
        {
            _portraitRenderer.LoadPortrait(portraitKey, texture);
        }
    }
}