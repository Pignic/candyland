using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Core.UI;

public class StatsTab : IMenuTab {
	private readonly GraphicsDevice _graphicsDevice;
	private readonly BitmapFont _font;
	private readonly Player _player;

	public UIPanel RootPanel { get; private set; }

	// UI elements (created once, display data via lambdas)
	private readonly List<UILabel> _statLabels;

	public bool IsVisible {
		get => RootPanel.Visible;
		set => RootPanel.Visible = value;
	}

	public StatsTab(GraphicsDevice graphicsDevice, BitmapFont font, Player player) {
		_graphicsDevice = graphicsDevice;
		_font = font;
		_player = player;
		_statLabels = [];
	}

	public void Initialize() {
		RootPanel = new UIPanel(_graphicsDevice) {
			X = 10,
			Y = 32,
			Width = 600,
			Height = 253,
			EnableScrolling = true,
			Layout = UIPanel.LayoutMode.Vertical,
			Spacing = 0,
			Visible = false
		};
		RootPanel.SetPadding(10);

		// Title
		UILabel title = new UILabel(_font, "PLAYER STATISTICS") {
			TextColor = Color.Yellow
		};
		title.UpdateSize();
		RootPanel.AddChild(title);

		AddSpacer(10);

		// Core Stats
		AddSectionHeader("-- CORE --", Color.Cyan);
		AddStatLine("Level", () => _player.Level.ToString());
		AddStatLine("Health", () => $"{_player.Health} / {_player.Stats.MaxHealth}");
		AddStatLine("XP", () => $"{_player.XP} / {_player.XPToNextLevel}");
		AddStatLine("Coins", () => _player.Coins.ToString());

		AddSpacer(10);

		// Combat Stats
		AddSectionHeader("-- COMBAT --", Color.Orange);
		AddStatLine("Attack Damage", () => _player.Stats.AttackDamage.ToString());
		AddStatLine("Attack Speed", () => _player.Stats.AttackSpeed.ToString("F2"));
		AddStatLine("Attack Range", () => _player.Stats.AttackRange.ToString("F0"));
		AddStatLine("Defense", () => _player.Stats.Defense.ToString());
		AddStatLine("Speed", () => _player.Stats.Speed.ToString("F0"));

		AddSpacer(10);

		// Advanced Stats
		AddSectionHeader("-- ADVANCED --", Color.LightGreen);
		AddStatLine("Crit Chance", () => $"{_player.Stats.CritChance * 100:F1}%");
		AddStatLine("Crit Multiplier", () => $"{_player.Stats.CritMultiplier:F2}x");
		AddStatLine("Dodge Chance", () => $"{_player.Stats.DodgeChance * 100:F1}%");
		AddStatLine("Life Steal", () => $"{_player.Stats.LifeSteal * 100:F1}%");
		AddStatLine("Health Regen", () => $"{_player.Stats.HealthRegen}/s");

		System.Diagnostics.Debug.WriteLine("[STATS TAB] Initialized");
	}

	public void RefreshContent() {
		// Stats update automatically via lambdas - no action needed!
		// This is the beauty of the data-driven approach
	}

	public void Update(GameTime gameTime) {
		if (!IsVisible) {
			return;
		}

		RootPanel.Update(gameTime);
	}

	public void HandleMouse(MouseState mouseState, MouseState previousMouseState) {
		if (!IsVisible) {
			return;
		}

		RootPanel.HandleMouse(mouseState, previousMouseState);
	}

	public void Draw(SpriteBatch spriteBatch) {
		if (!IsVisible) {
			return;
		}

		RootPanel.Draw(spriteBatch);
	}

	public int GetNavigableCount() => 0; // Stats tab has no navigable elements

	public UIElement GetNavigableElement(int index) => null;

	private void AddSectionHeader(string text, Color color) {
		UILabel label = new UILabel(_font, text) {
			TextColor = color
		};
		label.UpdateSize();
		RootPanel.AddChild(label);
	}

	private void AddStatLine(string label, Func<string> getValue) {
		UIPanel container = new UIPanel(_graphicsDevice) {
			Width = RootPanel.Width - 20,
			Height = _font.GetHeight(2),
			Layout = UIPanel.LayoutMode.Horizontal
		};

		UILabel labelText = new UILabel(_font, label + ":") {
			Width = 200,
			TextColor = Color.LightGray
		};
		labelText.UpdateSize();

		UILabel valueText = new UILabel(_font, "", getValue) {
			TextColor = Color.White
		};
		valueText.UpdateSize();

		container.AddChild(labelText);
		container.AddChild(valueText);
		RootPanel.AddChild(container);

		_statLabels.Add(labelText);
		_statLabels.Add(valueText);
	}

	private void AddSpacer(int height) {
		UIPanel spacer = new UIPanel(_graphicsDevice) {
			Height = height,
			Width = RootPanel.Width
		};
		RootPanel.AddChild(spacer);
	}

	public void Dispose() {
		System.Diagnostics.Debug.WriteLine("[STATS TAB] Disposed");
	}
}