using EldmeresTale.Core.UI;
using EldmeresTale.Dialog;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace EldmeresTale.Systems;

public class NotificationSystem : GameSystem {
	private readonly BitmapFont _font;
	private readonly List<Notification> _notifications;
	DisplayManager _display;

	// Notification positioning
	// TODO, use % of screen
	private const int NOTIFICATION_Y = 40;  // From top of screen
	private const int NOTIFICATION_SPACING = 50;

	public NotificationSystem(BitmapFont font, DisplayManager display) : base() {
		_font = font;
		_notifications = new List<Notification>();
		_display = display;
		Enabled = true;
		Visible = true;
	}

	public override void Initialize() {
		System.Diagnostics.Debug.WriteLine("[NOTIFICATION SYSTEM] Initialized");
		base.Initialize();
	}

	public void ShowQuestStarted(string questName) {
		Notification notification = new Notification(
			"NEW QUEST",
			questName,
			Color.Yellow,
			_font
		);
		AddNotification(notification);
		System.Diagnostics.Debug.WriteLine($"[NOTIFICATION] Quest Started: {questName}");
	}

	public void ShowQuestCompleted(string questName, int xpReward, int coinReward) {
		string rewards = "";
		if (xpReward > 0 && coinReward > 0) {
			rewards = $"+{xpReward} XP, +{coinReward} coins";
		} else if (xpReward > 0) {
			rewards = $"+{xpReward} XP";
		} else if (coinReward > 0) {
			rewards = $"+{coinReward} coins";
		}

		Notification notification = new Notification(
			"QUEST COMPLETE!",
			$"{questName}\n{rewards}",
			Color.Gold,
			_font,
			duration: 4.0f  // Longer for quest complete
		);
		AddNotification(notification);
		System.Diagnostics.Debug.WriteLine($"[NOTIFICATION] Quest Completed: {questName}");
	}

	public void ShowQuestProgress(string questName, string objectiveText) {
		Notification notification = new Notification(
			questName,
			objectiveText,
			Color.LightGray,
			_font,
			duration: 2.5f  // Shorter for progress
		);
		AddNotification(notification);
		System.Diagnostics.Debug.WriteLine($"[NOTIFICATION] Quest Progress: {questName} - {objectiveText}");
	}

	public void ShowItemPickup(string itemName, int quantity = 1) {
		string text = quantity > 1 ? $"{itemName} x{quantity}" : itemName;
		Notification notification = new Notification(
			"ITEM ACQUIRED",
			text,
			Color.Cyan,
			_font,
			duration: 2.0f
		);
		AddNotification(notification);
		System.Diagnostics.Debug.WriteLine($"[NOTIFICATION] Item Pickup: {text}");
	}

	public void ShowNotification(string title, string message, Color color, float duration = 3.0f) {
		Notification notification = new Notification(title, message, color, _font, duration);
		AddNotification(notification);
	}

	private void AddNotification(Notification notification) {
		// Position based on existing notifications
		int yOffset = NOTIFICATION_Y + (_notifications.Count * NOTIFICATION_SPACING);
		notification.Position = new Vector2(_display.VirtualWidth / 2, yOffset);
		_notifications.Add(notification);
	}

	public override void Update(GameTime gameTime) {
		if (!Enabled) {
			return;
		}

		// Update all notifications
		foreach (Notification notification in _notifications) {
			notification.Update(gameTime);
		}

		// Remove expired notifications
		_notifications.RemoveAll(n => n.IsExpired);

		// Reposition remaining notifications (stack them nicely)
		for (int i = 0; i < _notifications.Count; i++) {
			int targetY = NOTIFICATION_Y + (i * NOTIFICATION_SPACING);
			_notifications[i].Position = new Vector2(_display.VirtualWidth / 2, targetY);
		}

		base.Update(gameTime);
	}

	public override void Draw(SpriteBatch spriteBatch) {
		if (!Visible) {
			return;
		}

		foreach (Notification notification in _notifications) {
			notification.Draw(spriteBatch);
		}

		base.Draw(spriteBatch);
	}

	public override void Dispose() {
		_notifications.Clear();
		System.Diagnostics.Debug.WriteLine("[NOTIFICATION SYSTEM] Disposed");
		base.Dispose();
	}

	public void Clear() {
		_notifications.Clear();
	}

	public int NotificationCount => _notifications.Count;
}

public class Notification {
	public Vector2 Position { get; set; }
	public bool IsExpired { get; private set; }

	private readonly string _title;
	private readonly string _message;
	private readonly Color _color;
	private readonly BitmapFont _font;
	private readonly float _duration;

	private float _timer;
	private float _alpha;

	// Animation
	private const float FADE_IN_TIME = 0.3f;
	private const float FADE_OUT_TIME = 0.5f;

	public Notification(string title, string message, Color color, BitmapFont font, float duration = 3.0f) {
		_title = title;
		_message = message;
		_color = color;
		_font = font;
		_duration = duration;
		_timer = 0f;
		_alpha = 0f;
		IsExpired = false;
	}

	public void Update(GameTime gameTime) {
		if (IsExpired) {
			return;
		}

		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
		_timer += deltaTime;

		// Calculate alpha based on lifetime
		if (_timer < FADE_IN_TIME) {
			// Fade in
			_alpha = _timer / FADE_IN_TIME;
		} else if (_timer > _duration - FADE_OUT_TIME) {
			// Fade out
			float fadeOutProgress = (_timer - (_duration - FADE_OUT_TIME)) / FADE_OUT_TIME;
			_alpha = 1.0f - fadeOutProgress;
		} else {
			// Fully visible
			_alpha = 1.0f;
		}

		// Mark as expired
		if (_timer >= _duration) {
			IsExpired = true;
		}
	}

	public void Draw(SpriteBatch spriteBatch) {
		if (IsExpired || _alpha <= 0) {
			return;
		}

		int scale = 2;
		int lineHeight = _font.getHeight(scale);

		// Measure text for centering
		Vector2 titleSize = _font.getSize(_title, scale);
		Vector2 messageSize = _font.getSize(_message, scale);

		// Draw title (centered)
		Vector2 titlePos = new Vector2(
			Position.X - (titleSize.X / 2),
			Position.Y
		);
		_font.drawText(spriteBatch, _title, titlePos, _color * _alpha, scale);

		// Draw message (centered, below title)
		Vector2 messagePos = new Vector2(
			Position.X - (messageSize.X / 2),
			Position.Y + lineHeight + 2
		);
		_font.drawText(spriteBatch, _message, messagePos, Color.White * _alpha, scale);
	}
}