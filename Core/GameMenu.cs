namespace Candyland.Core;

using Candyland.Core.UI;
using Candyland.Entities;
using Candyland.Quests;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class MenuTab {

	public int index;
	public string label { get; }

	private static int currentIndex = 0;

	public UIPanel rootPanel;

	private MenuTab(string label) {
		index = currentIndex++;
		this.label = label;
	}

	public static readonly MenuTab Stats = new MenuTab("menu.stats.tab.label");
	public static readonly MenuTab Inventory = new MenuTab("menu.inventory.tab.label");
	public static readonly MenuTab Quests = new MenuTab("menu.quests.tab.label");
	public static readonly MenuTab Options = new MenuTab("menu.options.tab.label");

	public static IReadOnlyList<MenuTab> Values { get; } =
		new List<MenuTab> { Stats, Inventory, Quests, Options }.AsReadOnly();

	public static explicit operator MenuTab(int v) {
		return Values[v];
	}

	public static explicit operator int(MenuTab v) {
		return v.index;
	}
}

public class GameMenu {
	private Player _player;
	private BitmapFont _font;
	private GraphicsDevice _graphicsDevice;
	private int _scale;

	// UI Root
	private UIPanel _rootPanel;
	private UIPanel _backgroundOverlay;

	// Tabs
	private UIPanel _tabContainer;
	private UIButton[] _tabButtons;
	private MenuTab _currentTab = MenuTab.Stats;

	// Tab content panels
	private UIPanel _statsPanel;
	private UIPanel _inventoryPanel;
	private UIPanel _questsPanel;
	private UIPanel _optionsPanel;

	// Inventory sub-panels
	private UIPanel _inventoryItemsPanel;
	private UIPanel _inventoryEquipmentPanel;

	// Instructions
	private UILabel _instructionsLabel;

	// Tooltip
	private Equipment _hoveredItem;
	private UIElement _hoveredElement;
	private float _tooltipTimer = 0f;
	private const float TOOLTIP_DELAY = 0.2f;

	// Input tracking
	private KeyboardState _previousKeyState;
	private MouseState _previousMouseState;

	private QuestManager _questManager;

	public bool IsOpen { get; set; }

	// Option Menu

	private UISlider _scaleSlider;
	private UICheckbox _fullscreenCheckbox;

	public event Action<int> OnScaleChanged;
	public event Action<bool> OnFullscreenChanged;

	private Point screenSize;

	public GameMenu(GraphicsDevice graphicsDevice, BitmapFont font, Player player,
					  int screenWidth, int screenHeight, int scale, QuestManager questManager) {
		_graphicsDevice = graphicsDevice;
		_font = font;
		_scale = scale;
		screenSize = new Point(screenWidth, screenHeight);
		if(player != null && questManager != null) {
			SetGameData(player, questManager);
		}
	}

	public void SetGameData(Player player, QuestManager questManager) {
		_player = player;
		_questManager = questManager;

		BuildUI();

		if(_questManager != null) {
			_questManager.OnQuestStarted += OnQuestChanged;
			_questManager.OnQuestStarted += OnQuestChanged;
			_questManager.OnQuestCompleted += OnQuestChanged;
			_questManager.OnObjectiveUpdated += OnObjectiveChanged;
			_questManager.OnNodeAdvanced += OnQuestChanged;
		}
	}

	public void SetScale(int newScale) {
		_scale = newScale;
	}

	private void OnQuestChanged(Quest quest) {
		if(_currentTab == MenuTab.Quests && _questsPanel.Visible) {
			UpdateQuestsPanel();
		}
	}

	private void OnObjectiveChanged(Quest quest, QuestObjective objective) {
		if(_currentTab == MenuTab.Quests && _questsPanel.Visible) {
			UpdateQuestsPanel();
		}
	}

	private void BuildUI() {
		int MENU_WIDTH = screenSize.X - 20;
		int MENU_HEIGHT = screenSize.Y - 20;

		int screenWidth = screenSize.X;
		int screenHeight = screenSize.Y;

		int menuX = (screenWidth - MENU_WIDTH) / 2;
		int menuY = (screenHeight - MENU_HEIGHT) / 2;

		// === BACKGROUND OVERLAY ===
		_backgroundOverlay = new UIPanel(_graphicsDevice) {
			X = 0,
			Y = 0,
			Width = screenWidth,
			Height = screenHeight,
			BackgroundColor = Color.Black * 0.7f
		};

		// === ROOT PANEL ===
		_rootPanel = new UIPanel(_graphicsDevice) {
			X = menuX,
			Y = menuY,
			Width = MENU_WIDTH,
			Height = MENU_HEIGHT,
			BackgroundColor = Color.DarkSlateGray,
			BorderColor = Color.White,
			BorderWidth = 3
		};
		_rootPanel.SetPadding(0);

		// === TAB BUTTONS ===
		_tabContainer = new UIPanel(_graphicsDevice) {
			X = 0,
			Y = 0,
			Width = MENU_WIDTH,
			Height = 22,
			Layout = UIPanel.LayoutMode.Horizontal,
			Spacing = 0
		};

		_tabButtons = new UIButton[MenuTab.Values.Count];

		for(int i = 0; i < MenuTab.Values.Count; i++) {
			int tabIndex = i; // Capture for lambda
			_tabButtons[i] = new UIButton(_graphicsDevice, _font, MenuTab.Values[i].label) {
				Width = MENU_WIDTH / MenuTab.Values.Count,
				Height = 22,
				BorderWidth = 2,
				OnClick = () => SwitchTab((MenuTab)tabIndex)
			};
			_tabContainer.AddChild(_tabButtons[i]);
		}

		_rootPanel.AddChild(_tabContainer);

		// === TAB CONTENT PANELS ===
		BuildStatsTab();
		BuildInventoryTab();
		BuildQuestsTab();
		BuildOptionsTab();

		// === INSTRUCTIONS ===
		_instructionsLabel = new UILabel(_font) {
			X = 20,
			Y = MENU_HEIGHT - 25,
			Width = MENU_WIDTH - 40,
			TextColor = Color.Gray,
			Alignment = UILabel.TextAlignment.Center
		};
		_rootPanel.AddChild(_instructionsLabel);

		// Show stats tab by default
		SwitchTab(MenuTab.Stats);
	}

	private void BuildStatsTab() {
		_statsPanel = new UIPanel(_graphicsDevice) {
			X = 10,
			Y = 32,
			Width = 600,
			Height = 253,
			EnableScrolling = true,
			Layout = UIPanel.LayoutMode.Vertical,
			Spacing = 0,
			Visible = false
		};
		_statsPanel.SetPadding(10);
		MenuTab.Stats.rootPanel = _statsPanel;

		// Title
		var title = new UILabel(_font, "PLAYER STATISTICS") {
			TextColor = Color.Yellow
		};
		title.UpdateSize();
		_statsPanel.AddChild(title);

		AddSpacer(_statsPanel, 10);

		// Core Stats Section
		AddSectionHeader(_statsPanel, "-- CORE --", Color.Cyan);
		AddStatLine(_statsPanel, "Level", () => _player.Level.ToString());
		AddStatLine(_statsPanel, "Health", () => $"{_player.health} / {_player.Stats.MaxHealth}");
		AddStatLine(_statsPanel, "XP", () => $"{_player.XP} / {_player.XPToNextLevel}");
		AddStatLine(_statsPanel, "Coins", () => _player.Coins.ToString());

		AddSpacer(_statsPanel, 10);

		// Offense Section
		AddSectionHeader(_statsPanel, "-- OFFENSE --", Color.Orange);
		AddStatLine(_statsPanel, "Attack Damage", () => _player.Stats.AttackDamage.ToString());
		AddStatLine(_statsPanel, "Attack Speed", () => $"{_player.Stats.AttackSpeed:F2} attacks/sec");
		AddStatLine(_statsPanel, "Crit Chance", () => $"{_player.Stats.CritChance * 100:F0}%");
		AddStatLine(_statsPanel, "Crit Multiplier", () => $"{_player.Stats.CritMultiplier:F2}x");
		if(_player.Stats.LifeSteal > 0)
			AddStatLine(_statsPanel, "Life Steal", () => $"{_player.Stats.LifeSteal * 100:F0}%");

		AddSpacer(_statsPanel, 10);

		// Defense Section
		AddSectionHeader(_statsPanel, "-- DEFENSE --", Color.LightBlue);
		AddStatLine(_statsPanel, "Defense", () => _player.Stats.Defense.ToString());
		if(_player.Stats.Defense > 0) {
			float reduction = (float)_player.Stats.Defense / (_player.Stats.Defense + 100);
			AddStatLine(_statsPanel, "Damage Reduction", () => $"{reduction * 100:F1}%");
		}
		if(_player.Stats.DodgeChance > 0)
			AddStatLine(_statsPanel, "Dodge Chance", () => $"{_player.Stats.DodgeChance * 100:F0}%");
		if(_player.Stats.HealthRegen > 0)
			AddStatLine(_statsPanel, "Health Regen", () => $"{_player.Stats.HealthRegen:F1}/sec");

		AddSpacer(_statsPanel, 10);

		// Mobility Section
		AddSectionHeader(_statsPanel, "-- MOBILITY --", Color.LightGreen);
		AddStatLine(_statsPanel, "Speed", () => _player.Stats.Speed.ToString("F0"));

		_rootPanel.AddChild(_statsPanel);
	}

	private void BuildInventoryTab() {
		_inventoryPanel = new UIPanel(_graphicsDevice) {
			X = 10,
			Y = 32,
			Width = 600,
			Height = 253,
			Visible = false
		};
		_inventoryPanel.SetPadding(5);
		MenuTab.Inventory.rootPanel = _inventoryPanel;

		// Left panel - scrollable item list (60% width)
		_inventoryItemsPanel = new UIPanel(_graphicsDevice) {
			X = 0,
			Y = 0,
			Width = 360,
			Height = 243,
			EnableScrolling = false,
			Layout = UIPanel.LayoutMode.Vertical,
			Spacing = 5,
			BackgroundColor = new Color(30, 30, 30, 200)
		};
		_inventoryItemsPanel.SetPadding(5);

		// Right panel - compact equipment grid (40% width)
		_inventoryEquipmentPanel = new UIPanel(_graphicsDevice) {
			X = 370,
			Y = 0,
			Width = 225,
			Height = 243,
			BackgroundColor = new Color(30, 30, 30, 200)
		};
		_inventoryEquipmentPanel.SetPadding(10);

		_inventoryPanel.AddChild(_inventoryItemsPanel);
		_inventoryPanel.AddChild(_inventoryEquipmentPanel);

		_rootPanel.AddChild(_inventoryPanel);
	}

	private void BuildQuestsTab() {
		_questsPanel = new UIPanel(_graphicsDevice) {
			X = 10,
			Y = 32,
			Width = 600,
			Height = 253,
			EnableScrolling = true,
			Layout = UIPanel.LayoutMode.Vertical,  // ← ADD THIS
			Spacing = 5,  // ← ADD THIS
			Visible = false
		};
		_questsPanel.SetPadding(10);
		MenuTab.Quests.rootPanel = _questsPanel;

		// Will be populated by UpdateQuestsPanel()
		_rootPanel.AddChild(_questsPanel);
	}

	private void BuildOptionsTab() {
		_optionsPanel = new UIPanel(_graphicsDevice) {
			X = 10,
			Y = 32,
			Width = 600,
			Height = 253,
			EnableScrolling = true,
			Layout = UIPanel.LayoutMode.Vertical,
			Spacing = 10,
			Visible = false
		};
		_optionsPanel.SetPadding(10);
		MenuTab.Options.rootPanel = _optionsPanel;

		// === TITLE ===
		var title = new UILabel(_font, "OPTIONS") {
			TextColor = Color.Yellow
		};
		title.UpdateSize();
		_optionsPanel.AddChild(title);

		AddSpacer(_optionsPanel, 10);

		// === VIDEO SETTINGS SECTION ===
		AddSectionHeader(_optionsPanel, "-- VIDEO --", Color.Cyan);
		AddSpacer(_optionsPanel, 5);

		// Scale Slider
		_scaleSlider = new UISlider(_graphicsDevice, _font, "Window Scale", 1, 3, _scale) {
			Width = 300,
			IsNavigable= true,
		};
		_scaleSlider.OnValueChanged += (value) => {
			System.Diagnostics.Debug.WriteLine($"[OPTIONS] Scale changed to: {value}");
			OnScaleChanged?.Invoke(value);
		};
		_optionsPanel.AddChild(_scaleSlider);

		AddSpacer(_optionsPanel, 5);

		// Fullscreen Checkbox
		_fullscreenCheckbox = new UICheckbox(_graphicsDevice, _font, "Fullscreen",
			_graphicsDevice.PresentationParameters.IsFullScreen) {
			Width = 300,
			IsNavigable= true,
		};
		_fullscreenCheckbox.OnValueChanged += (value) => {
			System.Diagnostics.Debug.WriteLine($"[OPTIONS] Fullscreen changed to: {value}");
			OnFullscreenChanged?.Invoke(value);
		};
		_optionsPanel.AddChild(_fullscreenCheckbox);

		AddSpacer(_optionsPanel, 20);

		// === CONTROLS SECTION ===
		AddSectionHeader(_optionsPanel, "-- CONTROLS --", Color.Orange);
		AddSpacer(_optionsPanel, 5);

		AddInfoLine(_optionsPanel, "WASD / Arrows - Move");
		AddInfoLine(_optionsPanel, "Space - Attack");
		AddInfoLine(_optionsPanel, "E - Interact / Talk");
		AddInfoLine(_optionsPanel, "Tab - Menu");
		AddInfoLine(_optionsPanel, "M - Map Editor");
		AddInfoLine(_optionsPanel, "Esc - Quit");

		AddSpacer(_optionsPanel, 10);

		// === DEBUG SECTION (Optional) ===
		AddSectionHeader(_optionsPanel, "-- DEBUG --", Color.Red);
		AddSpacer(_optionsPanel, 5);

		_rootPanel.AddChild(_optionsPanel);
	}

	// === HELPER METHODS ===

	private void AddSectionHeader(UIPanel panel, string text, Color color) {
		var label = new UILabel(_font, text) {
			TextColor = color
		};
		label.UpdateSize();
		panel.AddChild(label);
	}

	private void AddStatLine(UIPanel panel, string label, Func<string> getValue) {
		var container = new UIPanel(_graphicsDevice) {
			Width = panel.Width - 20,
			Height = _font.getHeight(2),
			Layout = UIPanel.LayoutMode.Horizontal
		};

		var labelText = new UILabel(_font, label + ":") {
			Width = 200,
			TextColor = Color.LightGray
		};
		labelText.UpdateSize();

		var valueText = new UILabel(_font, "", getValue) {
			TextColor = Color.White
		};
		valueText.UpdateSize();

		container.AddChild(labelText);
		container.AddChild(valueText);
		panel.AddChild(container);
	}

	private void AddInfoLine(UIPanel panel, string text) {
		var label = new UILabel(_font, "  " + text) {
			TextColor = Color.White
		};
		label.UpdateSize();
		panel.AddChild(label);
	}
	public void SetTooltipItem(Equipment item) {
		if(item == null) {
			ClearTooltip();
			return;
		}

		_hoveredItem = item;
		_tooltipTimer = TOOLTIP_DELAY;
	}

	public void ClearTooltip() {
		_hoveredItem = null;
		_tooltipTimer = 0f;
	}

	private void AddSpacer(UIPanel panel, int height) {
		var spacer = new UIPanel(_graphicsDevice) {
			Height = height,
			Width = panel.Width
		};
		panel.AddChild(spacer);
	}
	public void SwitchTabByIndex(int index) {
		if(index >= 0 && index < MenuTab.Values.Count) {
			SwitchTab((MenuTab)index);
		}
	}

	public int GetCurrentTabNavigableCount() {
		return MenuTab.Values[_currentTab.index].rootPanel.GetNavigableChildCount();
	}

	public UIElement GetNavigableElement(int index) {
		return MenuTab.Values[_currentTab.index].rootPanel.GetNavigableChild(index);
	}
	public int GetInventoryItemCount() {
		// Find the grid panel inside _inventoryItemsPanel
		var gridPanel = _inventoryItemsPanel.Children
			.FirstOrDefault(c => c is UIPanel p && p.Layout == UIPanel.LayoutMode.Grid);

		if(gridPanel is UIPanel grid) {
			return grid.Children.Count(c => c.IsNavigable);
		}
		return 0;
	}

	public UIElement GetInventoryItem(int index) {
		var gridPanel = _inventoryItemsPanel.Children
			.FirstOrDefault(c => c is UIPanel p && p.Layout == UIPanel.LayoutMode.Grid);

		if(gridPanel is UIPanel grid) {
			var navigable = grid.Children.Where(c => c.IsNavigable).ToList();
			if(index >= 0 && index < navigable.Count) {
				return navigable[index];
			}
		}
		return null;
	}

	public void SwitchTab(MenuTab tab) {
		_currentTab = tab;

		_hoveredItem = null;
		_hoveredElement = null;
		_tooltipTimer = 0f;

		// Hide all tabs
		_statsPanel.Visible = false;
		_inventoryPanel.Visible = false;
		_questsPanel.Visible = false;
		_optionsPanel.Visible = false;

		// Show selected tab
		switch(tab.index) {
			case 0:
				_statsPanel.Visible = true;
				UpdateStatsPanel();
				break;
			case 1:
				_inventoryPanel.Visible = true;
				UpdateInventoryPanel();
				break;
			case 2:
				_questsPanel.Visible = true;
				UpdateQuestsPanel();
				break;
			case 3:
				_optionsPanel.Visible = true;
				break;
		}

		// Update button styles
		for(int i = 0; i < _tabButtons.Length; i++) {
			if(i == (int)tab) {
				_tabButtons[i].BackgroundColor = Color.SlateGray;
				_tabButtons[i].TextColor = Color.Yellow;
			} else {
				_tabButtons[i].BackgroundColor = new Color(60, 60, 60);
				_tabButtons[i].TextColor = Color.LightGray;
			}
		}

		UpdateInstructions();
	}

	private void UpdateStatsPanel() {
		// Stats are updated via lambdas - they'll auto-refresh
	}

	private void UpdateInventoryPanel() {
		// Rebuild inventory items list (LEFT SIDE)
		_inventoryItemsPanel.ClearChildren();

		var header = new UILabel(_font, "INVENTORY") {
			TextColor = Color.Yellow
		};
		header.UpdateSize();
		_inventoryItemsPanel.AddChild(header);

		int itemCount = _player.Inventory.GetItemCount();
		int maxSize = _player.Inventory.MaxSize;
		string countText = maxSize > 0 ? $"({itemCount}/{maxSize})" : $"({itemCount})";
		var countLabel = new UILabel(_font, countText) {
			TextColor = Color.Gray
		};
		countLabel.UpdateSize();
		_inventoryItemsPanel.AddChild(countLabel);

		AddSpacer(_inventoryItemsPanel, 5);

		UIPanel inventoryList = new UIPanel(_graphicsDevice) {
			X = _inventoryItemsPanel.X,
			Y = 0,
			Width = _inventoryItemsPanel.Width,
			Height = -1,
			EnableScrolling = true,
			Layout = UIPanel.LayoutMode.Grid,
			Spacing = 5,
			BackgroundColor = new Color(30, 30, 30, 200)
		};
		_inventoryItemsPanel.AddChild(inventoryList);
		foreach(var item in _player.Inventory.Items) {
			AddInventoryItem(inventoryList, item);
		}

		// Rebuild equipment panel (RIGHT SIDE - ICON GRID)
		_inventoryEquipmentPanel.ClearChildren();

		// No header needed - more compact

		// Icon grid layout
		const int ICON_SIZE = 32;
		const int SPACING = 10; 
		const int COL_1 = 10;             // Left column
		const int COL_2 = COL_1 + ICON_SIZE + SPACING;   // Center column
		const int COL_3 = COL_2 + ICON_SIZE + SPACING;   // Right column

		int currentY = 10;

		AddEquipmentIcon(EquipmentSlot.Helmet, COL_2, currentY);

		AddEquipmentIcon(EquipmentSlot.Amulet, COL_3, currentY);
		currentY += ICON_SIZE + SPACING + 2;

		int row3Y = currentY;
		AddEquipmentIcon(EquipmentSlot.Weapon, COL_1, row3Y);
		AddEquipmentIcon(EquipmentSlot.Armor, COL_2, row3Y);
		AddEquipmentIcon(EquipmentSlot.Gloves, COL_3, row3Y);
		currentY += ICON_SIZE + SPACING + 2;

		AddEquipmentIcon(EquipmentSlot.Belt, COL_2, currentY);
		AddEquipmentIcon(EquipmentSlot.Ring, COL_1, currentY);
		currentY += ICON_SIZE + SPACING + 2;

		AddEquipmentIcon(EquipmentSlot.Pants, COL_2, currentY);
		currentY += ICON_SIZE + SPACING + 2;

		AddEquipmentIcon(EquipmentSlot.Boots, COL_2, currentY);
	}

	// ================================================================
	// New helper for icon-based equipment slots
	// ================================================================

	private void AddEquipmentIcon(EquipmentSlot slot, int x, int y) {
		var equipped = _player.Inventory.GetEquippedItem(slot);

		var slotIcon = new UIEquipmentSlotIcon(_graphicsDevice, _font, slot, equipped) {
			X = x,
			Y = y,
			OnClick = () => UnequipItem(slot),
			OnHover = (hovered, element) =>
			{
				if(hovered && equipped != null) {
					_hoveredItem = equipped;
					_hoveredElement = element;
					_tooltipTimer = 0f;
				} else if(_hoveredElement == element) {
					_hoveredItem = null;
					_hoveredElement = null;
				}
			}
		};

		_inventoryEquipmentPanel.AddChild(slotIcon);
	}

	private void AddInventoryItem(UIPanel panel, Equipment item) {
		int lineHeight = _font.getHeight(2);

		var itemButton = new UIInventoryItemButton(_graphicsDevice, _font, item, lineHeight) {
			Width = (panel.Width/2) - 20,
			Height = lineHeight * 3,
			OnClick = () => EquipItem(item),
			OnHover = (hovered, element) =>
			{
				if(hovered) {
					_hoveredItem = item;
					_tooltipTimer = 0f;
					_hoveredElement = element;
				} else if(_hoveredElement == element) {
					_hoveredItem = null;
					_hoveredElement = null;
				}
			}
		};

		panel.AddChild(itemButton);
	}

	private void UpdateQuestsPanel() {
		_questsPanel.ClearChildren();

		// Title
		var title = new UILabel(_font, "QUESTS") {
			TextColor = Color.Yellow
		};
		title.UpdateSize();
		_questsPanel.AddChild(title);

		AddSpacer(_questsPanel, 10);

		// Active Quests Section
		var activeQuests = _questManager.getActiveQuests();

		if(activeQuests.Count == 0) {
			AddSectionHeader(_questsPanel, "-- ACTIVE --", Color.Cyan);
			var noQuestsLabel = new UILabel(_font, "  No active quests") {
				TextColor = Color.Gray
			};
			noQuestsLabel.UpdateSize();
			_questsPanel.AddChild(noQuestsLabel);
		} else {
			AddSectionHeader(_questsPanel, "-- ACTIVE --", Color.Cyan);
			AddSpacer(_questsPanel, 5);

			foreach(var instance in activeQuests) {
				AddQuestEntry(_questsPanel, instance);
			}
		}

		// TODO: Completed quests section (would need to track completed quests)
		AddSpacer(_questsPanel, 15);
		AddSectionHeader(_questsPanel, "-- COMPLETED --", Color.Green);
		var completedLabel = new UILabel(_font, "  Coming soon...") {
			TextColor = Color.Gray
		};
		completedLabel.UpdateSize();
		_questsPanel.AddChild(completedLabel);
	}

	private void AddQuestEntry(UIPanel panel, QuestInstance instance) {
		// Quest Name
		string questName = _questManager.getQuestName(instance.quest);
		var nameLabel = new UILabel(_font, questName) {
			TextColor = Color.Yellow
		};
		nameLabel.UpdateSize();
		panel.AddChild(nameLabel);

		// Current Node Description
		var currentNode = instance.getCurrentNode();
		if(currentNode != null) {
			// Node description (optional, can be shown)
			// string nodeDesc = _questManager._localization.getString(currentNode.descriptionKey);

			AddSpacer(panel, 3);

			// Objectives with progress
			foreach(var objective in currentNode.objectives) {
				AddObjectiveEntry(panel, instance, objective);
			}
		}

		AddSpacer(panel, 10);
	}

	private void AddObjectiveEntry(UIPanel panel, QuestInstance instance, QuestObjective objective) {
		// Get current progress
		int current = instance.objectiveProgress.ContainsKey(objective)
			? instance.objectiveProgress[objective]
			: 0;
		int required = objective.requiredCount;

		// Get objective text
		string objectiveText = _questManager.getObjectiveDescription(instance, objective);

		// Remove the "(X/Y)" that getObjectiveDescription adds since we'll draw it differently
		if(objectiveText.Contains(" (")) {
			objectiveText = objectiveText.Substring(0, objectiveText.IndexOf(" ("));
		}

		// Objective text
		var textLabel = new UILabel(_font, "  • " + objectiveText) {
			TextColor = current >= required ? Color.LimeGreen : Color.White
		};
		textLabel.UpdateSize();
		panel.AddChild(textLabel);

		// Progress bar (if count > 1)
		if(required > 1) {
			float progress = (float)current / required;

			var progressBarContainer = new UIPanel(_graphicsDevice) {
				Width = panel.Width - 40,
				Height = 12,
				BackgroundColor = Color.Transparent
			};

			var progressBar = new UIProgressBar(_graphicsDevice, _font, () => $"{current}/{required}", () => (float) current / required ) {
				X = 20,
				Y = 0,
				Width = panel.Width - 60,
				Height = 10,
				BackgroundColor = new Color(40, 40, 40),
				ForegroundColor = current >= required ? Color.LimeGreen : Color.Gold,
				BorderColor = Color.Gray,
				BorderWidth = 1,
				TextColor = Color.White
			};

			progressBarContainer.AddChild(progressBar);
			panel.AddChild(progressBarContainer);

			AddSpacer(panel, 3);
		}
	}

	private void UpdateInstructions() {
		string text = "TAB: Close   Click/1/2/3/Arrows: Switch Tabs";

		if(_currentTab == MenuTab.Inventory) {
			text = "Click: Equip   Right-Click: Unequip";
		}

		_instructionsLabel.SetText(text);
	}

	// === UPDATE / DRAW ===

	public void Update(GameTime gameTime) {
		if(!IsOpen) return;

		KeyboardState keyState = Keyboard.GetState();
		MouseState mouseState = Mouse.GetState();

		MouseState scaledMouse = ScaleMouseState(mouseState);
		if(!_rootPanel.GlobalBounds.Contains(scaledMouse.Position)) {
			_hoveredItem = null;
			_hoveredElement = null;
			_tooltipTimer = 0f;
		}

		// Update tooltip timer
		if(_hoveredItem != null) {
			_tooltipTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
		} else {
			_tooltipTimer = 0f;
		}

		// Update UI
		_rootPanel.Update(gameTime);

		// Scale mouse positions for input handling
		MouseState scaledPrevMouse = ScaleMouseState(_previousMouseState);

		// Handle mouse input with scaled positions
		_rootPanel.HandleMouse(scaledMouse, scaledPrevMouse);

		_previousKeyState = keyState;
		_previousMouseState = mouseState;
	}

	public void EquipItem(Equipment item) {
		_player.Inventory.SwapEquip(item, _player.Stats);
		UpdateInventoryPanel(); // Refresh the display
	}

	private void UnequipItem(EquipmentSlot slot) {
		_player.Inventory.Unequip(slot, _player.Stats);
		UpdateInventoryPanel(); // Refresh the display
	}

	public void Draw(SpriteBatch spriteBatch) {
		_backgroundOverlay.Draw(spriteBatch);
		_rootPanel.Draw(spriteBatch);

		// Draw tooltip if hovering and timer elapsed
		if(_hoveredItem != null && _tooltipTimer >= TOOLTIP_DELAY) {
			DrawTooltip(spriteBatch, _hoveredItem);
		}
	}

	private void DrawTooltip(SpriteBatch spriteBatch, Equipment item) {
		MouseState mouseState = Mouse.GetState();
		Point scaledMousePos = new Point(mouseState.X / _scale, mouseState.Y / _scale);

		int tooltipX = scaledMousePos.X + 15;
		int tooltipY = scaledMousePos.Y + 15;

		// Build tooltip text
		var lines = new List<string>();
		lines.Add(item.Name);
		lines.Add($"[{item.Rarity}]");
		lines.Add(item.Slot.ToString());

		if(item.RequiredLevel > 1)
			lines.Add($"Requires Level {item.RequiredLevel}");

		lines.Add("");

		if(!string.IsNullOrEmpty(item.Description)) {
			lines.Add(item.Description);
			lines.Add("");
		}

		// Add stats
		if(item.MaxHealthBonus != 0)
			lines.Add($"+{item.MaxHealthBonus} Max Health");
		if(item.AttackDamageBonus != 0)
			lines.Add($"+{item.AttackDamageBonus} Attack Damage");
		if(item.DefenseBonus != 0)
			lines.Add($"+{item.DefenseBonus} Defense");
		if(item.SpeedBonus != 0)
			lines.Add($"+{item.SpeedBonus:F0} Speed");
		if(item.AttackSpeedBonus != 0)
			lines.Add($"+{item.AttackSpeedBonus:F2} Attack Speed");
		if(item.CritChanceBonus != 0)
			lines.Add($"+{item.CritChanceBonus * 100:F0}% Crit Chance");
		if(item.CritMultiplierBonus != 0)
			lines.Add($"+{item.CritMultiplierBonus:F2}x Crit Damage");
		if(item.HealthRegenBonus != 0)
			lines.Add($"+{item.HealthRegenBonus:F1} HP Regen");
		if(item.LifeStealBonus != 0)
			lines.Add($"+{item.LifeStealBonus * 100:F0}% Life Steal");
		if(item.DodgeChanceBonus != 0)
			lines.Add($"+{item.DodgeChanceBonus * 100:F0}% Dodge");

		// Calculate tooltip size
		int lineHeight = _font.getHeight(2);
		int tooltipWidth = 0;
		foreach(var line in lines) {
			int lineWidth = _font.measureString(line);
			if(lineWidth > tooltipWidth)
				tooltipWidth = lineWidth;
		}
		tooltipWidth += 20; // Padding
		int tooltipHeight = lines.Count * lineHeight + 10; // Padding

		Rectangle menuBounds = _rootPanel.GlobalBounds;

		// Clamp X (keep inside menu horizontally)
		if(tooltipX + tooltipWidth > menuBounds.Right) {
			tooltipX = menuBounds.Right - tooltipWidth;
		}
		if(tooltipX < menuBounds.Left) {
			tooltipX = menuBounds.Left;
		}

		// Clamp Y (keep inside menu vertically)
		if(tooltipY + tooltipHeight > menuBounds.Bottom) {
			tooltipY = menuBounds.Bottom - tooltipHeight;
		}
		if(tooltipY < menuBounds.Top) {
			tooltipY = menuBounds.Top;
		}

		// Create tooltip background
		var pixelTexture = Graphics.CreateColoredTexture(_graphicsDevice, 1, 1, Color.White);
		Rectangle tooltipBounds = new Rectangle(tooltipX, tooltipY, tooltipWidth, tooltipHeight);

		// Background
		spriteBatch.Draw(pixelTexture, tooltipBounds, Color.Black * 0.9f);

		// Border
		DrawBorder(spriteBatch, pixelTexture, tooltipBounds, Color.White, 2);

		// Draw text
		int yOffset = tooltipY + 5;
		foreach(var line in lines) {
			Color lineColor = Color.White;

			if(line == item.Name)
				lineColor = item.GetRarityColor();
			else if(line.StartsWith("["))
				lineColor = Color.Yellow;
			else if(line.StartsWith("+"))
				lineColor = Color.LightGreen;
			else if(line == item.Slot.ToString())
				lineColor = Color.Gray;

			_font.drawText(spriteBatch, line, new Vector2(tooltipX + 10, yOffset), lineColor);
			yOffset += lineHeight;
		}
	}

	private void DrawBorder(SpriteBatch spriteBatch, Texture2D texture, Rectangle bounds, Color color, int width) {
		// Top
		spriteBatch.Draw(texture, new Rectangle(bounds.X, bounds.Y, bounds.Width, width), color);
		// Bottom
		spriteBatch.Draw(texture, new Rectangle(bounds.X, bounds.Bottom - width, bounds.Width, width), color);
		// Left
		spriteBatch.Draw(texture, new Rectangle(bounds.X, bounds.Y, width, bounds.Height), color);
		// Right
		spriteBatch.Draw(texture, new Rectangle(bounds.Right - width, bounds.Y, width, bounds.Height), color);
	}

	/// <summary>
	/// Scale mouse position from display resolution to game resolution
	/// </summary>
	private MouseState ScaleMouseState(MouseState original) {
		Point scaledPosition = new Point(
			original.Position.X / _scale,
			original.Position.Y / _scale
		);

		// Create new MouseState with scaled position but same button states
		return new MouseState(
			scaledPosition.X,
			scaledPosition.Y,
			original.ScrollWheelValue,
			original.LeftButton,
			original.MiddleButton,
			original.RightButton,
			original.XButton1,
			original.XButton2
		);
	}
}
