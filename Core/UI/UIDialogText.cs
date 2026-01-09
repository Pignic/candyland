using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Core.UI;

public class UIDialogText : UIElement {
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

	public bool IsTextComplete => _currentCharIndex >= _fullText.Length;

	public UIDialogText() : base() {
		_fullText = "";
		_displayedText = "";
		_npcName = "";
	}

	public void SetText(string npcName, string text) {
		_npcName = npcName;
		_fullText = text ?? "";
		ResetTypewriter();
	}

	public void ResetTypewriter() {
		_typewriterTimer = 0f;
		_currentCharIndex = 0;
		_displayedText = "";
	}

	public void CompleteText() {
		_currentCharIndex = _fullText.Length;
		_displayedText = _fullText;
	}

	public override void Update(GameTime gameTime) {
		if (_currentCharIndex < _fullText.Length) {
			float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			_typewriterTimer += deltaTime * CHARS_PER_SECOND;

			int charsToShow = (int)_typewriterTimer;
			_currentCharIndex = System.Math.Min(charsToShow, _fullText.Length);
			_displayedText = _fullText.Substring(0, _currentCharIndex);
		}
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		Point globalPos = GlobalPosition;

		// Draw NPC name
		_font.DrawText(spriteBatch, _npcName,
					  new Vector2(globalPos.X, globalPos.Y), Color.Yellow);

		// Draw separator line under name
		int separatorY = globalPos.Y + NAME_HEIGHT + 2;
		spriteBatch.Draw(_defaultTexture,
						new Rectangle(globalPos.X, separatorY, Width, 1), Color.Gray);

		// Draw dialog text with word wrapping
		int textY = separatorY + 4;
		DrawWordWrappedText(spriteBatch, _displayedText, globalPos.X, textY, Width, Color.White);
	}

	private void DrawWordWrappedText(SpriteBatch spriteBatch, string text, int x, int y, int maxWidth, Color color) {
		if (string.IsNullOrEmpty(text)) {
			return;
		}

		string[] words = text.Split(' ');
		string currentLine = "";
		int currentY = y;

		foreach (string word in words) {
			string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
			int lineWidth = _font.MeasureString(testLine);

			if (lineWidth > maxWidth && !string.IsNullOrEmpty(currentLine)) {
				// Draw current line and start new one
				_font.DrawText(spriteBatch, currentLine, new Vector2(x, currentY), color);
				currentY += LINE_HEIGHT;
				currentLine = word;

				// Stop if we exceed vertical bounds
				if (currentY + LINE_HEIGHT > GlobalPosition.Y + Height) {
					break;
				}
			} else {
				currentLine = testLine;
			}
		}

		// Draw remaining text
		if (!string.IsNullOrEmpty(currentLine) && currentY + LINE_HEIGHT <= GlobalPosition.Y + Height) {
			_font.DrawText(spriteBatch, currentLine, new Vector2(x, currentY), color);
		}
	}
}