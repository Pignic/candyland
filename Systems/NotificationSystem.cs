using EldmeresTale.Core.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Systems;

/// <summary>
/// Displays on-screen notifications for quests, items, etc.
/// Similar to VFXSystem but for UI notifications
/// </summary>
public class NotificationSystem : GameSystem {
	private readonly BitmapFont _font;
	private readonly List<Notification> _notifications;
	private readonly int _screenWidth;
	private readonly int _screenHeight;

	// Notification positioning
	private const int NOTIFICATION_Y = 40;  // From top of screen
	private const int NOTIFICATION_SPACING = 50;

	public NotificationSystem(BitmapFont font, int screenWidth, int screenHeight) : base() {
		_font = font;
		_notifications = new List<Notification>();
		_screenWidth = screenWidth;
		_screenHeight = screenHeight;
		Enabled = true;
		Visible = true;
	}

	public override void Initialize() {
		System.Diagnostics.Debug.WriteLine("[NOTIFICATION SYSTEM] Initialized");
		base.Initialize();
	}

	/// <summary>
	/// Show a quest started notification
	/// </summary>
	public void ShowQuestStarted(string questName) {
		var notification = new Notification(
			"NEW QUEST",
			questName,
			Color.Yellow,
			_font,
			_screenWidth
		);
		AddNotification(notification);
		System.Diagnostics.Debug.WriteLine($"[NOTIFICATION] Quest Started: {questName}");
	}

	/// <summary>
	/// Show a quest completed notification with rewards
	/// </summary>
	public void ShowQuestCompleted(string questName, int xpReward, int coinReward) {
		string rewards = "";
		if(xpReward > 0 && coinReward > 0) {
			rewards = $"+{xpReward} XP, +{coinReward} coins";
		} else if(xpReward > 0) {
			rewards = $"+{xpReward} XP";
		} else if(coinReward > 0) {
			rewards = $"+{coinReward} coins";
		}

		var notification = new Notification(
			"QUEST COMPLETE!",
			$"{questName}\n{rewards}",
			Color.Gold,
			_font,
			_screenWidth,
			duration: 4.0f  // Longer for quest complete
		);
		AddNotification(notification);
		System.Diagnostics.Debug.WriteLine($"[NOTIFICATION] Quest Completed: {questName}");
	}

	/// <summary>
	/// Show a quest objective progress update
	/// </summary>
	public void ShowQuestProgress(string questName, string objectiveText) {
		var notification = new Notification(
			questName,
			objectiveText,
			Color.LightGray,
			_font,
			_screenWidth,
			duration: 2.5f  // Shorter for progress
		);
		AddNotification(notification);
		System.Diagnostics.Debug.WriteLine($"[NOTIFICATION] Quest Progress: {questName} - {objectiveText}");
	}

	/// <summary>
	/// Show an item pickup notification
	/// </summary>
	public void ShowItemPickup(string itemName, int quantity = 1) {
		string text = quantity > 1 ? $"{itemName} x{quantity}" : itemName;
		var notification = new Notification(
			"ITEM ACQUIRED",
			text,
			Color.Cyan,
			_font,
			_screenWidth,
			duration: 2.0f
		);
		AddNotification(notification);
		System.Diagnostics.Debug.WriteLine($"[NOTIFICATION] Item Pickup: {text}");
	}

	/// <summary>
	/// Show a generic notification
	/// </summary>
	public void ShowNotification(string title, string message, Color color, float duration = 3.0f) {
		var notification = new Notification(title, message, color, _font, _screenWidth, duration);
		AddNotification(notification);
	}

	private void AddNotification(Notification notification) {
		// Position based on existing notifications
		int yOffset = NOTIFICATION_Y + (_notifications.Count * NOTIFICATION_SPACING);
		notification.Position = new Vector2(_screenWidth / 2, yOffset);
		_notifications.Add(notification);
	}

	public override void Update(GameTime gameTime) {
		if(!Enabled) return;

		// Update all notifications
		foreach(var notification in _notifications) {
			notification.Update(gameTime);
		}

		// Remove expired notifications
		_notifications.RemoveAll(n => n.IsExpired);

		// Reposition remaining notifications (stack them nicely)
		for(int i = 0; i < _notifications.Count; i++) {
			int targetY = NOTIFICATION_Y + (i * NOTIFICATION_SPACING);
			_notifications[i].Position = new Vector2(_screenWidth / 2, targetY);
		}

		base.Update(gameTime);
	}

	public override void Draw(SpriteBatch spriteBatch) {
		if(!Visible) return;

		foreach(var notification in _notifications) {
			notification.Draw(spriteBatch);
		}

		base.Draw(spriteBatch);
	}

	public override void Dispose() {
		_notifications.Clear();
		System.Diagnostics.Debug.WriteLine("[NOTIFICATION SYSTEM] Disposed");
		base.Dispose();
	}

	/// <summary>
	/// Clear all active notifications
	/// </summary>
	public void Clear() {
		_notifications.Clear();
	}

	public int NotificationCount => _notifications.Count;
}

/// <summary>
/// Individual notification instance
/// </summary>
public class Notification {
	public Vector2 Position { get; set; }
	public bool IsExpired { get; private set; }

	private readonly string _title;
	private readonly string _message;
	private readonly Color _color;
	private readonly BitmapFont _font;
	private readonly int _screenWidth;
	private readonly float _duration;

	private float _timer;
	private float _alpha;

	// Animation
	private const float FADE_IN_TIME = 0.3f;
	private const float FADE_OUT_TIME = 0.5f;

	public Notification(string title, string message, Color color, BitmapFont font, int screenWidth, float duration = 3.0f) {
		_title = title;
		_message = message;
		_color = color;
		_font = font;
		_screenWidth = screenWidth;
		_duration = duration;
		_timer = 0f;
		_alpha = 0f;
		IsExpired = false;
	}

	public void Update(GameTime gameTime) {
		if(IsExpired) return;

		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
		_timer += deltaTime;

		// Calculate alpha based on lifetime
		if(_timer < FADE_IN_TIME) {
			// Fade in
			_alpha = _timer / FADE_IN_TIME;
		} else if(_timer > _duration - FADE_OUT_TIME) {
			// Fade out
			float fadeOutProgress = (_timer - (_duration - FADE_OUT_TIME)) / FADE_OUT_TIME;
			_alpha = 1.0f - fadeOutProgress;
		} else {
			// Fully visible
			_alpha = 1.0f;
		}

		// Mark as expired
		if(_timer >= _duration) {
			IsExpired = true;
		}
	}

	public void Draw(SpriteBatch spriteBatch) {
		if(IsExpired || _alpha <= 0) return;

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