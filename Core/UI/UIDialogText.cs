using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Core.UI;

/// <summary>
/// Dialog text display with typewriter effect
/// Replaces UIDialogBox, extends UIElement for hierarchy
/// </summary>
public class UIDialogText : UIElement {
	private BitmapFont _font;

	// Text content
	private string _npcName;
	private string _fullText;
	private string _displayedText;

	// Typewriter effect
	private float _typewriterTimer;
	private const float CHARS_PER_SECOND = 30f;
	private int _currentCharIndex;

	// Layout
	private const int NAME_HEIGHT = 14;
	private const int LINE_HEIGHT = 14;

	public bool isTextComplete => _currentCharIndex >= _fullText.Length;

	public UIDialogText(BitmapFont font) {
		_font = font;
		_fullText = "";
		_displayedText = "";
		_npcName = "";
	}

	public void setText(string npcName, string text) {
		_npcName = npcName;
		_fullText = text ?? "";
		resetTypewriter();
	}

	public void resetTypewriter() {
		_typewriterTimer = 0f;
		_currentCharIndex = 0;
		_displayedText = "";
	}

	public void completeText() {
		_currentCharIndex = _fullText.Length;
		_displayedText = _fullText;
	}

	public void update(GameTime gameTime) {
		if(_currentCharIndex < _fullText.Length) {
			float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			_typewriterTimer += deltaTime * CHARS_PER_SECOND;

			int charsToShow = (int)_typewriterTimer;
			_currentCharIndex = System.Math.Min(charsToShow, _fullText.Length);
			_displayedText = _fullText.Substring(0, _currentCharIndex);
		}
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		var globalPos = GlobalPosition;

		// Draw NPC name
		_font.drawText(spriteBatch, _npcName,
					  new Vector2(globalPos.X, globalPos.Y), Color.Yellow);

		// Draw separator line under name
		int separatorY = globalPos.Y + NAME_HEIGHT + 2;
		var pixelTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
		pixelTexture.SetData(new[] { Color.White });
		spriteBatch.Draw(pixelTexture,
						new Rectangle(globalPos.X, separatorY, Width, 1), Color.Gray);

		// Draw dialog text with word wrapping
		int textY = separatorY + 4;
		drawWordWrappedText(spriteBatch, _displayedText, globalPos.X, textY, Width, Color.White);
	}

	private void drawWordWrappedText(SpriteBatch spriteBatch, string text, int x, int y, int maxWidth, Color color) {
		if(string.IsNullOrEmpty(text)) return;

		string[] words = text.Split(' ');
		string currentLine = "";
		int currentY = y;

		foreach(string word in words) {
			string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
			int lineWidth = _font.measureString(testLine);

			if(lineWidth > maxWidth && !string.IsNullOrEmpty(currentLine)) {
				// Draw current line and start new one
				_font.drawText(spriteBatch, currentLine, new Vector2(x, currentY), color);
				currentY += LINE_HEIGHT;
				currentLine = word;

				// Stop if we exceed vertical bounds
				if(currentY + LINE_HEIGHT > GlobalPosition.Y + Height)
					break;
			} else {
				currentLine = testLine;
			}
		}

		// Draw remaining text
		if(!string.IsNullOrEmpty(currentLine) && currentY + LINE_HEIGHT <= GlobalPosition.Y + Height) {
			_font.drawText(spriteBatch, currentLine, new Vector2(x, currentY), color);
		}
	}
}