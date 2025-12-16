using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Candyland.Dialog;

namespace Candyland.Core.UI;

public class UIDialog
{
    private DialogManager _dialogManager;
    private BitmapFont _font;
    private Texture2D _pixelTexture;

    // UI Components
    private UIDialogBox _dialogBox;
    private UIDialogResponsePanel _responsePanel;
    private UIPortrait _portraitRenderer;

    // Layout constants
    private const int DIALOG_BOX_HEIGHT = 120;
    private const int DIALOG_BOX_MARGIN = 10;
    private const int PORTRAIT_SIZE = 60;
    private const int PORTRAIT_MARGIN = 5;
    private const int TEXT_MARGIN = 1;

    // Screen dimensions
    private int _screenWidth;
    private int _screenHeight;
    private int _scale;

    private string _currentNodeId = "";

    // Input tracking
    private MouseState _previousMouseState;
    private KeyboardState _previousKeyState;

    // Selected response (for keyboard navigation)
    private int _selectedResponseIndex = 0;

    public bool IsActive => _dialogManager.isDialogActive;

    public UIDialog(DialogManager dialogManager, BitmapFont font, Texture2D pixelTexture, int screenWidth, int screenHeight, int scale)
    {
        _dialogManager = dialogManager;
        _font = font;
        _pixelTexture = pixelTexture;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _scale = scale;

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
            textBounds.Bottom-2,
            textBounds.Width,
            dialogBoxBounds.Bottom - (textBounds.Bottom-2) - PORTRAIT_MARGIN
        );

        // Create components
        _dialogBox = new UIDialogBox(dialogBoxBounds, _font, _pixelTexture);
        _responsePanel = new UIDialogResponsePanel(responseBounds, _font, _pixelTexture);
        _portraitRenderer = new UIPortrait(portraitBounds, _pixelTexture);

        _previousMouseState = Mouse.GetState();
        _previousKeyState = Keyboard.GetState();
    }

    private Point ScaleMousePosition(Point displayMousePos)
    {
        return new Point(
            displayMousePos.X / _scale,
            displayMousePos.Y / _scale
        );
    }

    /// <summary>
    /// Update dialog UI
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (!IsActive)
            return;

        var currentNode = _dialogManager.getCurrentNode();
        if (currentNode == null)
            return;

        // *** FIX: Only set text if the node changed ***
        // Add a field to track current node
        if (_currentNodeId != currentNode.id)
        {
            _currentNodeId = currentNode.id;

            // Get localized text
            string npcText = _dialogManager.localization.getString(currentNode.textKey);
            var npc = _dialogManager.getCurrentNPC();
            string npcName = npc != null ? _dialogManager.localization.getString(npc.nameKey) : "???";

            // Update dialog box with typewriter effect
            _dialogBox.SetText(npcName, npcText);
        }

        // Always update the typewriter animation
        _dialogBox.Update(gameTime);

        // Get available responses
        var responses = _dialogManager.getAvailableResponses();
        List<string> responseTexts = new List<string>();
        foreach (var response in responses)
        {
            responseTexts.Add(_dialogManager.localization.getString(response.textKey));
        }

        // Update response panel
        _responsePanel.SetResponses(responseTexts);
        _responsePanel.Update(gameTime);

        // Handle input
        HandleInput(responses.Count);
    }

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
        int hoveredIndex = _responsePanel.GetHoveredResponseIndex(ScaleMousePosition(mouseState.Position));
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

    private void ChooseResponse(int index)
    {
        _dialogManager.chooseResponse(index);
        _selectedResponseIndex = 0;
        _responsePanel.SetSelectedIndex(0);

        // Reset typewriter for next node
        _dialogBox.ResetTypewriter();
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive)
            return;

        var currentNode = _dialogManager.getCurrentNode();
        if (currentNode == null)
            return;

        // Draw dialog box background
        _dialogBox.Draw(spriteBatch);

        // Draw portrait
        var npc = _dialogManager.getCurrentNPC();
        string portraitKey = currentNode.portraitKey ?? (npc?.defaultPortrait ?? "default");
        _portraitRenderer.draw(spriteBatch, portraitKey);

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

    private void DrawContinueHint(SpriteBatch spriteBatch)
    {
        string hint = "[Click to skip]";
        int textWidth = _font.measureString(hint);
        Vector2 hintPos = new Vector2(
            _screenWidth - textWidth - DIALOG_BOX_MARGIN - 10,
            _screenHeight - DIALOG_BOX_MARGIN - 25
        );

        // Flashing effect
        float alpha = (float)((System.Math.Sin(System.DateTime.Now.Millisecond * 0.01) + 1) / 2);
        _font.drawText(spriteBatch, hint, hintPos, Color.Gray * alpha);
    }

    public void LoadPortrait(string portraitKey, Texture2D texture)
    {
        _portraitRenderer.loadPortrait(portraitKey, texture);
    }
}