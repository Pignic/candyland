using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using System;

namespace EldmeresTale.Core.UI;

public class UIStatsPanel : UIPanel {

	private readonly Player _player;

	public UIStatsPanel(Player player) {
		_player = player;

		EnableScrolling = true;
		Layout = LayoutMode.Vertical;
		Spacing = 2;
		SetPadding(10);

		BuildContent();
	}

	private void BuildContent() {
		// Title
		UILabel title = new UILabel("PLAYER STATISTICS") {
			TextColor = Color.Yellow
		};
		title.UpdateSize();
		AddChild(title);

		AddSpacer(10);

		// Core Stats Section
		AddSectionHeader("-- CORE --", Color.Cyan);
		AddStatLine("Level", () => _player.Level.ToString());
		AddStatLine("Health", () => $"{_player.Health} / {_player.Stats.MaxHealth}");
		AddStatLine("XP", () => $"{_player.XP} / {_player.XPToNextLevel}");
		AddStatLine("Coins", () => _player.Coins.ToString());

		AddSpacer(10);

		// Offense Section
		AddSectionHeader("-- OFFENSE --", Color.Orange);
		AddStatLine("Attack Damage", () => _player.Stats.AttackDamage.ToString());
		AddStatLine("Attack Speed", () => $"{_player.Stats.AttackSpeed:F2} attacks/sec");
		AddStatLine("Crit Chance", () => $"{_player.Stats.CritChance * 100:F0}%");
		AddStatLine("Crit Multiplier", () => $"{_player.Stats.CritMultiplier:F2}x");
		if (_player.Stats.LifeSteal > 0) {
			AddStatLine("Life Steal", () => $"{_player.Stats.LifeSteal * 100:F0}%");
		}

		AddSpacer(10);

		// Defense Section
		AddSectionHeader("-- DEFENSE --", Color.LightBlue);
		AddStatLine("Defense", () => _player.Stats.Defense.ToString());
		if (_player.Stats.Defense > 0) {
			float reduction = (float)_player.Stats.Defense / (_player.Stats.Defense + 100);
			AddStatLine("Damage Reduction", () => $"{reduction * 100:F1}%");
		}
		if (_player.Stats.DodgeChance > 0) {
			AddStatLine("Dodge Chance", () => $"{_player.Stats.DodgeChance * 100:F0}%");
		}

		if (_player.Stats.HealthRegen > 0) {
			AddStatLine("Health Regen", () => $"{_player.Stats.HealthRegen:F1}/sec");
		}

		AddSpacer(10);

		// Mobility Section
		AddSectionHeader("-- MOBILITY --", Color.LightGreen);
		AddStatLine("Speed", () => _player.Stats.Speed.ToString("F0"));
	}

	private void AddSectionHeader(string text, Color color) {
		UILabel label = new UILabel(text) {
			TextColor = color
		};
		label.UpdateSize();
		AddChild(label);
	}

	private void AddStatLine(string label, Func<string> getValue) {
		UIPanel container = new UIPanel() {
			Width = Width - 20,
			Height = Font.GetHeight(2),
			Layout = LayoutMode.Horizontal
		};

		UILabel labelText = new UILabel(label + ":") {
			Width = 200,
			TextColor = Color.LightGray
		};
		labelText.UpdateSize();

		UILabel valueText = new UILabel("", getValue) {
			TextColor = Color.White
		};
		valueText.UpdateSize();

		container.AddChild(labelText);
		container.AddChild(valueText);
		AddChild(container);
	}

	private void AddSpacer(int height) {
		UIPanel spacer = new UIPanel() {
			Height = height,
			Width = Width
		};
		AddChild(spacer);
	}
}