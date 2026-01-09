using EldmeresTale.Core;
using EldmeresTale.Core.UI;
using EldmeresTale.Entities;
using EldmeresTale.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace EldmeresTale.Scenes;

public class GameMenuScene : Scene {
	private readonly GameServices _gameServices;
	private readonly int _scale;

	// UI
	private UIPanel _backgroundOverlay;
	private UIPanel _rootPanel;
	private UITabContainer _tabContainer;
	private UIOptionsPanel _optionsPanel;
	private UIQuestsPanel _questsPanel;
	private UIInventoryPanel _inventoryPanel;

	// Tooltip tracking
	private Equipment _hoveredItem;
	private UIElement _hoveredElement;
	private float _tooltipTimer = 0f;
	private const float TOOLTIP_DELAY = 0.2f;
	private Point? _keyboardTooltipPosition = null;

	// Navigation
	private readonly NavigationController _navController;
	private MouseState _previousMouseState;

	public GameMenuScene(ApplicationContext appContext, GameServices gameServices)
		: base(appContext, exclusive: true) {
		_gameServices = gameServices;
		_scale = appContext.Display.Scale;

		_navController = new NavigationController {
			Mode = NavigationMode.Index,
			WrapAround = true
		};

		BuildUI();

		System.Diagnostics.Debug.WriteLine("[GAME MENU SCENE] Initialized with UITabContainer");
	}

	private void BuildUI() {
		int screenWidth = appContext.Display.VirtualWidth;
		int screenHeight = appContext.Display.VirtualHeight;
		int menuWidth = screenWidth - 20;
		int menuHeight = screenHeight - 20;
		int menuX = (screenWidth - menuWidth) / 2;
		int menuY = (screenHeight - menuHeight) / 2;

		// Background overlay
		_backgroundOverlay = new UIPanel(appContext.GraphicsDevice) {
			X = 0,
			Y = 0,
			Width = screenWidth,
			Height = screenHeight,
			BackgroundColor = Color.Black * 0.7f
		};

		// Root panel
		_rootPanel = new UIPanel(appContext.GraphicsDevice) {
			X = menuX,
			Y = menuY,
			Width = menuWidth,
			Height = menuHeight,
			BackgroundColor = Color.DarkSlateGray,
			BorderColor = Color.White,
			BorderWidth = 3
		};
		_rootPanel.SetPadding(0);

		// Create tab content panels
		UIStatsPanel statsPanel = new UIStatsPanel(
			appContext.GraphicsDevice,
			appContext.Font,
			_gameServices.Player
		);

		// Options panel with event wiring
		_optionsPanel = new UIOptionsPanel(
			appContext.GraphicsDevice,
			appContext.Font,
			_scale,
			appContext.GraphicsDevice.PresentationParameters.IsFullScreen
		);
		_optionsPanel.OnMusicVolumeChanged += OnMusicVolumeChanged;
		_optionsPanel.OnSfxVolumeChanged += OnSfxVolumeChanged;
		_optionsPanel.OnScaleChanged += OnScaleChanged;
		_optionsPanel.OnFullscreenChanged += OnFullscreenChanged;
		_optionsPanel.OnCameraShakeChanged += OnCameraShakeChanged;

		// Quests panel - displays active/completed quests
		_questsPanel = new UIQuestsPanel(
			appContext.GraphicsDevice,
			appContext.Font,
			_gameServices.QuestManager
		);

		// Inventory panel - items and equipment with tooltips
		_inventoryPanel = new UIInventoryPanel(
			appContext.GraphicsDevice,
			appContext.Font,
			_gameServices.Player
		);
		_inventoryPanel.OnItemEquip += OnItemEquip;
		_inventoryPanel.OnItemUnequip += OnItemUnequip;
		_inventoryPanel.OnItemHover += OnItemHover;

		// Create tab configs with OnShow callbacks
		TabConfig[] tabs = [
			new TabConfig {
				Name = "STATS",
				Content = statsPanel,
				OnShow = () => {
					_navController.Mode = NavigationMode.None;
					_navController.ItemCount = 0;
					System.Diagnostics.Debug.WriteLine("[MENU] Stats tab shown");
				}
			},
			new TabConfig {
				Name = "INVENTORY",
				Content = _inventoryPanel,
				OnShow = () => {
					// Inventory tab - spatial grid navigation (2 columns)
					_navController.Mode = NavigationMode.Spatial;
					int itemCount = _inventoryPanel.GetNavigableCount();
					const int COLUMNS = 2;
					int rows = itemCount > 0 ? (int)System.Math.Ceiling((double)itemCount / COLUMNS) : 1;
					_navController.GridSize = new Point(COLUMNS, rows);
					_navController.Reset();
					// Refresh inventory display
					_inventoryPanel.RefreshContent();

					System.Diagnostics.Debug.WriteLine($"[MENU] Inventory tab shown ({itemCount} items, grid: {COLUMNS}x{rows})");
				},
				OnHide = () => {
					// Clear tooltips when leaving inventory
					_hoveredItem = null;
					_hoveredElement = null;
					_keyboardTooltipPosition = null;
					_tooltipTimer = 0f;
				}
			},
			new TabConfig {
				Name = "QUESTS",
				Content = _questsPanel,
				OnShow = () => {
					// Quests tab - no navigation, just scrollable display
					_navController.Mode = NavigationMode.None;
					_navController.ItemCount = 0;
					// Refresh quest display in case anything changed
					_questsPanel.RefreshContent();

					System.Diagnostics.Debug.WriteLine("[MENU] Quests tab shown");
				}
			},
			new TabConfig {
				Name = "OPTIONS",
				Content = _optionsPanel,
				OnShow = () => {
					// Options tab - index navigation for sliders/checkboxes
					_navController.Mode = NavigationMode.Index;
					_navController.ItemCount = _optionsPanel.GetNavigableCount();
					_navController.Reset();
					System.Diagnostics.Debug.WriteLine($"[MENU] Options tab shown (navigable: {_navController.ItemCount})");
				}
			}
		];

		// Create tab container
		_tabContainer = new UITabContainer(
			appContext.GraphicsDevice,
			appContext.Font,
			tabs
		) {
			X = 0,
			Y = 0,
			Width = menuWidth,
			Height = menuHeight - 30 // Leave space for instructions
		};
		_tabContainer.UpdateButtonWidths();

		_rootPanel.AddChild(_tabContainer);

		// Instructions at bottom
		UILabel instructions = new UILabel(appContext.Font, "TAB: Close   Q/E: Switch Tabs") {
			X = 20,
			Y = menuHeight - 25,
			Width = menuWidth - 40,
			TextColor = Color.Gray,
			Alignment = UILabel.TextAlignment.Center
		};
		instructions.UpdateSize();
		_rootPanel.AddChild(instructions);
	}

	public override void Update(GameTime time) {
		InputCommands input = appContext.Input.GetCommands();

		// Close menu
		if (input.CancelPressed) {
			appContext.CloseScene();
			return;
		}

		// Tab switching with Q/E
		if (appContext.Input.IsActionPressed(GameAction.TabLeft)) {
			int newIndex = _tabContainer.SelectedTabIndex - 1;
			if (newIndex < 0) {
				newIndex = 3; // Wrap to last tab
			}

			_tabContainer.SelectTab(newIndex);
		}
		if (appContext.Input.IsActionPressed(GameAction.TabRight)) {
			int newIndex = _tabContainer.SelectedTabIndex + 1;
			if (newIndex >= 4) {
				newIndex = 0; // Wrap to first tab
			}

			_tabContainer.SelectTab(newIndex);
		}

		// Handle navigation for current tab
		if (_tabContainer.SelectedTabIndex == 1) {  // Inventory tab
			UpdateInventoryNavigation(input);
		} else if (_tabContainer.SelectedTabIndex == 3) {  // Options tab
			UpdateOptionsNavigation(input);
		}

		// Update tooltip timer
		if (_hoveredItem != null) {
			_tooltipTimer += (float)time.ElapsedGameTime.TotalSeconds;
		}

		// Update UI
		MouseState mouseState = Mouse.GetState();
		MouseState scaledMouse = ScaleMouseState(mouseState);
		MouseState scaledPrevMouse = ScaleMouseState(_previousMouseState);

		_backgroundOverlay.Update(time);
		_rootPanel.Update(time);
		_rootPanel.HandleMouse(scaledMouse, scaledPrevMouse);

		_previousMouseState = mouseState;
	}

	public override void Draw(SpriteBatch spriteBatch) {
		// End previous batch
		spriteBatch.End();

		// Begin fresh
		spriteBatch.Begin(samplerState: SamplerState.PointClamp);

		_backgroundOverlay.Draw(spriteBatch);
		_rootPanel.Draw(spriteBatch);

		// Draw tooltip if hovering and timer elapsed (only on inventory tab)
		if (_tabContainer.SelectedTabIndex == 1 && _hoveredItem != null && _tooltipTimer >= TOOLTIP_DELAY) {
			DrawTooltip(spriteBatch, _hoveredItem);
		}

		// Input legend
		appContext.InputLegend.Draw(
			spriteBatch,
			appContext.Display.VirtualWidth,
			appContext.Display.VirtualHeight,
			(GameAction.Cancel, "Close"),
			(GameAction.TabLeft, "Prev Tab"),
			(GameAction.TabRight, "Next Tab")
		);

		spriteBatch.End();
		spriteBatch.Begin(samplerState: SamplerState.PointClamp);
	}

	private void UpdateInventoryNavigation(InputCommands input) {
		int itemCount = _inventoryPanel.GetNavigableCount();
		const int COLUMNS = 2;
		int rows = itemCount > 0 ? (int)System.Math.Ceiling((double)itemCount / COLUMNS) : 1;
		_navController.GridSize = new Point(COLUMNS, rows);

		_navController.Update(input);

		// Update hover state for all items based on keyboard selection
		Point selectedSlot = _navController.SelectedGridPosition;
		int selectedIndex = _navController.GridPositionToIndex(selectedSlot);

		for (int i = 0; i < itemCount; i++) {
			UIElement element = _inventoryPanel.GetNavigableElement(i);
			if (element is UINavigableElement nav) {
				nav.ForceHoverState(i == selectedIndex);
			}
		}

		// Set keyboard tooltip for selected item
		if (selectedIndex >= 0 && selectedIndex < itemCount) {
			UIElement selectedElement = _inventoryPanel.GetNavigableElement(selectedIndex);
			Equipment item = _gameServices.Player.Inventory.EquipmentItems[selectedIndex];

			// Set keyboard tooltip position
			if (selectedElement != null) {
				Rectangle bounds = selectedElement.GlobalBounds;
				_hoveredItem = item;
				_hoveredElement = selectedElement;
				_keyboardTooltipPosition = new Point(bounds.Right + 10, bounds.Y);
				_tooltipTimer = TOOLTIP_DELAY; // Show immediately for keyboard
			}
		} else {
			_hoveredItem = null;
			_hoveredElement = null;
			_keyboardTooltipPosition = null;
		}

		// Equip item on Space/Enter
		if (input.AttackPressed && selectedIndex >= 0 && selectedIndex < _gameServices.Player.Inventory.EquipmentItems.Count) {
			Equipment item = _gameServices.Player.Inventory.EquipmentItems[selectedIndex];
			OnItemEquip(item); // Use event handler for consistency
		}
	}

	private void UpdateOptionsNavigation(InputCommands input) {
		_navController.Update(input);

		int selected = _navController.SelectedIndex;
		int navigableCount = _optionsPanel.GetNavigableCount();

		// Update hover state for all navigable elements
		for (int i = 0; i < navigableCount; i++) {
			UIElement element = _optionsPanel.GetNavigableElement(i);
			bool isSelected = i == selected;
			if (element is UINavigableElement nav) {
				nav.ForceHoverState(isSelected);
			}
		}

		// Handle input for selected element
		UIElement selectedElement = _optionsPanel.GetNavigableElement(selected);

		if (selectedElement is UISlider slider) {
			// Adjust slider with left/right
			if (input.MoveLeftPressed) {
				slider.Value--;
			}
			if (input.MoveRightPressed) {
				slider.Value++;
			}
		} else if (selectedElement is UICheckbox checkbox) {
			// Toggle checkbox with space/attack
			if (input.AttackPressed) {
				checkbox.IsChecked = !checkbox.IsChecked;
			}
		}
	}

	// Event handlers
	private void OnMusicVolumeChanged(float volume) {
		appContext.MusicPlayer.Volume = volume;
		System.Diagnostics.Debug.WriteLine($"[SCENE] Music volume: {volume:F2}");
	}

	private void OnSfxVolumeChanged(float volume) {
		appContext.SoundEffects.MasterVolume = volume;
		System.Diagnostics.Debug.WriteLine($"[SCENE] SFX volume: {volume:F2}");
	}

	private void OnScaleChanged(int newScale) {
		int newWidth = appContext.Display.VirtualWidth * newScale;
		int newHeight = appContext.Display.VirtualHeight * newScale;
		appContext.RequestResolutionChange(newWidth, newHeight);
		System.Diagnostics.Debug.WriteLine($"[SCENE] Scale: {newScale}");
	}

	private void OnFullscreenChanged(bool isFullscreen) {
		appContext.RequestFullscreenChange(isFullscreen);
		System.Diagnostics.Debug.WriteLine($"[SCENE] Fullscreen: {isFullscreen}");
	}

	private void OnCameraShakeChanged(bool enabled) {
		// Camera shake is handled via GameSettings, no additional action needed
		System.Diagnostics.Debug.WriteLine($"[SCENE] Camera Shake: {enabled}");
	}

	// Inventory event handlers
	private void OnItemEquip(Equipment item) {
		_gameServices.Player.Inventory.SwapEquip(item, _gameServices.Player.Stats);
		_inventoryPanel.RefreshContent();
		System.Diagnostics.Debug.WriteLine($"[SCENE] Equipped: {item.Name}");
	}

	private void OnItemUnequip(EquipmentSlot slot) {
		_gameServices.Player.Inventory.Unequip(slot, _gameServices.Player.Stats);
		_inventoryPanel.RefreshContent();
		System.Diagnostics.Debug.WriteLine($"[SCENE] Unequipped: {slot}");
	}

	private void OnItemHover(Equipment item, bool hovered, UIElement element) {
		if (hovered) {
			_hoveredItem = item;
			_hoveredElement = element;
			_tooltipTimer = 0f;
			_keyboardTooltipPosition = null; // Mouse hover uses mouse position
		} else if (_hoveredElement == element) {
			_hoveredItem = null;
			_hoveredElement = null;
			_tooltipTimer = 0f;
		}
	}

	private void DrawTooltip(SpriteBatch spriteBatch, Equipment item) {
		// Determine tooltip position
		int tooltipX;
		int tooltipY;

		if (_keyboardTooltipPosition.HasValue) {
			// Position tooltip next to keyboard-selected element
			tooltipX = _keyboardTooltipPosition.Value.X;
			tooltipY = _keyboardTooltipPosition.Value.Y;
		} else {
			// Use mouse position for mouse hover
			MouseState mouseState = Mouse.GetState();
			Point scaledMousePos = ScaleMouseState(mouseState).Position;
			tooltipX = scaledMousePos.X + 15;
			tooltipY = scaledMousePos.Y + 15;
		}

		// Build tooltip content
		List<string> lines = [
			item.Name,
			$"[{item.Rarity}]",
			item.Slot.ToString()
		];

		if (item.RequiredLevel > 1) {
			lines.Add($"Requires Level {item.RequiredLevel}");
		}

		lines.Add("");

		if (!string.IsNullOrEmpty(item.Description)) {
			lines.Add(item.Description);
			lines.Add("");
		}

		// Add stats
		if (item.MaxHealthBonus != 0) {
			lines.Add($"+{item.MaxHealthBonus} Max Health");
		}

		if (item.AttackDamageBonus != 0) {
			lines.Add($"+{item.AttackDamageBonus} Attack Damage");
		}

		if (item.DefenseBonus != 0) {
			lines.Add($"+{item.DefenseBonus} Defense");
		}

		if (item.SpeedBonus != 0) {
			lines.Add($"+{item.SpeedBonus:F0} Speed");
		}

		if (item.AttackSpeedBonus != 0) {
			lines.Add($"+{item.AttackSpeedBonus:F2} Attack Speed");
		}

		if (item.CritChanceBonus != 0) {
			lines.Add($"+{item.CritChanceBonus * 100:F0}% Crit Chance");
		}

		if (item.CritMultiplierBonus != 0) {
			lines.Add($"+{item.CritMultiplierBonus:F2}x Crit Damage");
		}

		if (item.HealthRegenBonus != 0) {
			lines.Add($"+{item.HealthRegenBonus:F1} HP Regen");
		}

		if (item.LifeStealBonus != 0) {
			lines.Add($"+{item.LifeStealBonus * 100:F0}% Life Steal");
		}

		if (item.DodgeChanceBonus != 0) {
			lines.Add($"+{item.DodgeChanceBonus * 100:F0}% Dodge");
		}

		// Calculate tooltip size
		int lineHeight = appContext.Font.GetHeight(2);
		int tooltipWidth = 0;
		foreach (string line in lines) {
			int lineWidth = appContext.Font.MeasureString(line);
			if (lineWidth > tooltipWidth) {
				tooltipWidth = lineWidth;
			}
		}
		tooltipWidth += 20; // Padding
		int tooltipHeight = (lines.Count * lineHeight) + 10;

		// Clamp tooltip to stay within screen bounds
		Rectangle screenBounds = new Rectangle(0, 0,
			appContext.Display.VirtualWidth,
			appContext.Display.VirtualHeight);

		if (tooltipX + tooltipWidth > screenBounds.Right) {
			tooltipX = screenBounds.Right - tooltipWidth;
		}
		if (tooltipX < screenBounds.Left) {
			tooltipX = screenBounds.Left;
		}
		if (tooltipY + tooltipHeight > screenBounds.Bottom) {
			tooltipY = screenBounds.Bottom - tooltipHeight;
		}
		if (tooltipY < screenBounds.Top) {
			tooltipY = screenBounds.Top;
		}

		// Draw tooltip background
		Texture2D pixelTexture = new Texture2D(appContext.GraphicsDevice, 1, 1);
		pixelTexture.SetData([Color.White]);
		Rectangle tooltipBounds = new Rectangle(tooltipX, tooltipY, tooltipWidth, tooltipHeight);
		spriteBatch.Draw(pixelTexture, tooltipBounds, Color.Black * 0.9f);

		// Draw border
		DrawBorder(spriteBatch, pixelTexture, tooltipBounds, Color.White, 2);

		// Draw text
		int yOffset = tooltipY + 5;
		foreach (string line in lines) {
			Color lineColor = Color.White;

			// Color specific lines
			if (line == item.Name) {
				lineColor = item.GetRarityColor();
			} else if (line.StartsWith('[')) {
				lineColor = Color.Yellow;
			} else if (line.StartsWith('+')) {
				lineColor = Color.LightGreen;
			} else if (line == item.Slot.ToString()) {
				lineColor = Color.Gray;
			}

			appContext.Font.DrawText(spriteBatch, line, new Vector2(tooltipX + 10, yOffset), lineColor);
			yOffset += lineHeight;
		}

		pixelTexture.Dispose();
	}

	private static void DrawBorder(SpriteBatch spriteBatch, Texture2D texture, Rectangle bounds, Color color, int width) {
		// Top
		spriteBatch.Draw(texture, new Rectangle(bounds.X, bounds.Y, bounds.Width, width), color);
		// Bottom
		spriteBatch.Draw(texture, new Rectangle(bounds.X, bounds.Bottom - width, bounds.Width, width), color);
		// Left
		spriteBatch.Draw(texture, new Rectangle(bounds.X, bounds.Y, width, bounds.Height), color);
		// Right
		spriteBatch.Draw(texture, new Rectangle(bounds.Right - width, bounds.Y, width, bounds.Height), color);
	}

	private MouseState ScaleMouseState(MouseState mouseState) {
		return new MouseState(
			mouseState.X / _scale,
			mouseState.Y / _scale,
			mouseState.ScrollWheelValue,
			mouseState.LeftButton,
			mouseState.MiddleButton,
			mouseState.RightButton,
			mouseState.XButton1,
			mouseState.XButton2
		);
	}

	public override void Dispose() {
		base.Dispose();
		System.Diagnostics.Debug.WriteLine("[GAME MENU SCENE] Disposed");
	}
}