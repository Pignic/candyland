using EldmeresTale.Core;
using EldmeresTale.Core.UI;
using EldmeresTale.Core.UI.Tabs;
using EldmeresTale.Entities;
using EldmeresTale.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Scenes;

/// <summary>
/// Refactored Game Menu with all regressions fixed
/// </summary>
public class GameMenuScene : Scene {
	private readonly GameServices _gameServices;
	private readonly int _scale;

	// UI Root
	private UIPanel _backgroundOverlay;
	private UIPanel _rootPanel;
	private UIPanel _tabButtonContainer;
	private UILabel _instructionsLabel;

	// Tabs
	private readonly List<IMenuTab> _tabs;
	private readonly List<UIButton> _tabButtons;
	private int _currentTabIndex = 0;

	// Tab instances
	private StatsTab _statsTab;
	private InventoryTab _inventoryTab;
	private QuestsTab _questsTab;
	private OptionsTab _optionsTab;

	// Navigation
	private readonly NavigationController _navController;
	private MouseState _previousMouseState;
	private bool _isNavigatingInventory = false;
	private int _lastMouseHoveredIndex = -1;

	// Tooltip
	private UIEquipmentTooltip _tooltip;
	private Equipment _hoveredItem;
	private Point _tooltipPosition;
	private float _tooltipTimer = 0f;
	private const float TOOLTIP_DELAY = 0.2f;

	private readonly string[] _tabLabels = { "STATS", "INVENTORY", "QUESTS", "OPTIONS" };

	public GameMenuScene(ApplicationContext appContext, GameServices gameServices)
		: base(appContext, exclusive: true) {
		_gameServices = gameServices;
		_scale = appContext.Display.Scale;
		_tabs = [];
		_tabButtons = [];

		_navController = new NavigationController {
			Mode = NavigationMode.Index,
			WrapAround = true
		};

		BuildUI();
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

		// Tab buttons
		_tabButtonContainer = new UIPanel(appContext.GraphicsDevice) {
			X = 0,
			Y = 0,
			Width = menuWidth,
			Height = 22,
			Layout = UIPanel.LayoutMode.Horizontal,
			Spacing = 0
		};

		int buttonWidth = menuWidth / _tabLabels.Length;
		for (int i = 0; i < _tabLabels.Length; i++) {
			int tabIndex = i; // Capture for lambda
			UIButton button = new UIButton(appContext.GraphicsDevice, appContext.Font, _tabLabels[i]) {
				Width = buttonWidth,
				Height = 22,
				BorderWidth = 2,
				OnClick = () => SwitchToTab(tabIndex)
			};
			_tabButtons.Add(button);
			_tabButtonContainer.AddChild(button);
		}

		_rootPanel.AddChild(_tabButtonContainer);

		// Create tabs
		_statsTab = new StatsTab(appContext.GraphicsDevice, appContext.Font, _gameServices.Player);
		_inventoryTab = new InventoryTab(appContext.GraphicsDevice, appContext.Font, _gameServices.Player);
		_questsTab = new QuestsTab(appContext.GraphicsDevice, appContext.Font, _gameServices.QuestManager);
		_optionsTab = new OptionsTab(appContext.GraphicsDevice, appContext.Font, _scale);

		// Wire up options tab events
		_optionsTab.OnMusicVolumeChanged += (volume) => appContext.MusicPlayer.Volume = volume;
		_optionsTab.OnSfxVolumeChanged += (volume) => appContext.SoundEffects.MasterVolume = volume;
		_optionsTab.OnScaleChanged += OnScaleChanged;
		_optionsTab.OnFullscreenChanged += OnFullscreenChanged;

		// Wire up inventory tab events
		_inventoryTab.OnItemHovered += OnItemHovered;
		_inventoryTab.OnHoverCleared += OnHoverCleared;

		_tabs.Add(_statsTab);
		_tabs.Add(_inventoryTab);
		_tabs.Add(_questsTab);
		_tabs.Add(_optionsTab);

		// Initialize all tabs
		foreach (IMenuTab tab in _tabs) {
			tab.Initialize();
			_rootPanel.AddChild(tab.RootPanel);
		}

		// Create tooltip
		_tooltip = new UIEquipmentTooltip(appContext.GraphicsDevice, appContext.Font);

		// Instructions label
		_instructionsLabel = new UILabel(appContext.Font) {
			X = 20,
			Y = menuHeight - 25,
			Width = menuWidth - 40,
			TextColor = Color.Gray,
			Alignment = UILabel.TextAlignment.Center
		};
		_rootPanel.AddChild(_instructionsLabel);

		// Show first tab
		SwitchToTab(0);

		System.Diagnostics.Debug.WriteLine("[GAME MENU SCENE] UI Built");
	}

	private void SwitchToTab(int tabIndex) {
		_currentTabIndex = tabIndex;

		// Hide all tabs
		foreach (IMenuTab tab in _tabs) {
			tab.IsVisible = false;
		}

		// Show selected tab
		_tabs[tabIndex].IsVisible = true;
		_tabs[tabIndex].RefreshContent();

		// Update button styles
		for (int i = 0; i < _tabButtons.Count; i++) {
			if (i == tabIndex) {
				_tabButtons[i].BackgroundColor = Color.SlateGray;
				_tabButtons[i].TextColor = Color.Yellow;
			} else {
				_tabButtons[i].BackgroundColor = new Color(60, 60, 60);
				_tabButtons[i].TextColor = Color.LightGray;
			}
		}

		// Clear tooltip when switching tabs
		_hoveredItem = null;
		_tooltipTimer = 0f;

		// Update navigation mode based on tab
		if (tabIndex == 1) { // Inventory
			_navController.Mode = NavigationMode.Spatial;

			// Get actual inventory count
			int itemCount = _gameServices.Player.Inventory.GetItemCount();
			const int COLUMNS = 2;
			int rows = itemCount > 0 ? (int)Math.Ceiling((double)itemCount / COLUMNS) : 1;

			_navController.GridSize = new Point(COLUMNS, rows);
			_navController.Reset();
			_isNavigatingInventory = true;
			_lastMouseHoveredIndex = -1;
		} else if (tabIndex == 3) { // Options
			_navController.Mode = NavigationMode.Index;
			_navController.ItemCount = _tabs[tabIndex].GetNavigableCount();
			_navController.Reset();
			_isNavigatingInventory = false;
			_lastMouseHoveredIndex = -1;
		} else {
			_navController.Mode = NavigationMode.Index;
			_navController.ItemCount = 0;
			_isNavigatingInventory = false;
		}

		UpdateInstructions();

		System.Diagnostics.Debug.WriteLine($"[GAME MENU SCENE] Switched to tab {tabIndex} ({_tabLabels[tabIndex]})");
	}

	private void UpdateInstructions() {
		string text = _currentTabIndex switch {
			1 => "Click: Equip   Right-Click: Unequip   TAB: Close",
			_ => "TAB: Close   Q/E or Click: Switch Tabs"
		};
		_instructionsLabel.SetText(text);
	}

	public override void Update(GameTime time) {
		InputCommands input = appContext.Input.GetCommands();

		// Close menu
		if (input.CancelPressed) {
			appContext.CloseScene();
			return;
		}

		// Tab switching
		if (appContext.Input.IsActionPressed(GameAction.TabLeft)) {
			_currentTabIndex--;
			if (_currentTabIndex < 0) {
				_currentTabIndex = _tabs.Count - 1;
			}

			SwitchToTab(_currentTabIndex);
		}
		if (appContext.Input.IsActionPressed(GameAction.TabRight)) {
			_currentTabIndex++;
			if (_currentTabIndex >= _tabs.Count) {
				_currentTabIndex = 0;
			}

			SwitchToTab(_currentTabIndex);
		}

		// Update tooltip timer
		if (_hoveredItem != null) {
			_tooltipTimer += (float)time.ElapsedGameTime.TotalSeconds;
		} else {
			_tooltipTimer = 0f;
		}

		// Update current tab
		_tabs[_currentTabIndex].Update(time);

		// Handle keyboard navigation for inventory/options tabs
		if (_currentTabIndex == 1) {
			UpdateInventoryNavigation(input, time);
		} else if (_currentTabIndex == 3) {
			UpdateOptionsNavigation(input);
		}

		// Update UI
		MouseState mouseState = Mouse.GetState();
		MouseState scaledMouse = ScaleMouseState(mouseState);
		MouseState scaledPrevMouse = ScaleMouseState(_previousMouseState);

		// Clear tooltip if mouse leaves menu
		if (!_rootPanel.GlobalBounds.Contains(scaledMouse.Position)) {
			_hoveredItem = null;
			_tooltipTimer = 0f;
		}

		_backgroundOverlay.Update(time);
		_rootPanel.Update(time);
		_rootPanel.HandleMouse(scaledMouse, scaledPrevMouse);

		// Handle tab-specific mouse input
		_tabs[_currentTabIndex].HandleMouse(scaledMouse, scaledPrevMouse);

		_previousMouseState = mouseState;
	}

	private void UpdateInventoryNavigation(InputCommands input, GameTime time) {
		if (!_isNavigatingInventory) {
			return;
		}

		int itemCount = _gameServices.Player.Inventory.GetItemCount();
		if (itemCount == 0) {
			return;
		}

		const int COLUMNS = 2;
		int rows = (int)Math.Ceiling((double)itemCount / COLUMNS);
		_navController.GridSize = new Point(COLUMNS, rows);

		// Update navigation
		_navController.Update(input);

		// Get selected index
		Point selectedSlot = _navController.SelectedGridPosition;
		int selectedIndex = _navController.GridPositionToIndex(selectedSlot);

		// Highlight selected slot in inventory tab (FIX #2)
		_inventoryTab.HighlightSlot(selectedIndex);

		// Equip item on attack press
		if (input.AttackPressed && selectedIndex >= 0 && selectedIndex < itemCount) {
			Equipment selectedItem = _gameServices.Player.Inventory.EquipmentItems[selectedIndex];
			if (selectedItem != null) {
				_gameServices.Player.Inventory.Equip(selectedItem, _gameServices.Player.Stats);
				_inventoryTab.RefreshContent();
			}
		}
	}

	private void UpdateOptionsNavigation(InputCommands input) {
		_navController.Update(input);

		int selected = _navController.SelectedIndex;
		OptionsTab optTab = (OptionsTab)_tabs[3];

		// Clear ALL hover states first (FIX #4)
		for (int i = 0; i < optTab.GetNavigableCount(); i++) {
			UIElement element = optTab.GetNavigableElement(i);
			if (element is UINavigableElement navElement) {
				navElement.ForceHoverState(false);
			}
		}

		// Highlight ONLY selected element
		UIElement selectedElement = optTab.GetNavigableElement(selected);
		if (selectedElement is UINavigableElement selectedNav) {
			selectedNav.ForceHoverState(true);
		}

		// Interact with selected element
		if (selectedElement is UISlider slider) {
			if (input.MoveLeftPressed) {
				slider.Value--;
			}

			if (input.MoveRightPressed) {
				slider.Value++;
			}
		} else if (selectedElement is UICheckbox checkbox) {
			if (input.AttackPressed) {
				checkbox.IsChecked = !checkbox.IsChecked;
			}
		}
	}

	private void OnItemHovered(Equipment item, Point position) {
		_hoveredItem = item;
		_tooltipPosition = position;
		_tooltipTimer = 0f;
	}

	private void OnHoverCleared() {
		_hoveredItem = null;
		_tooltipTimer = 0f;
	}

	private void OnScaleChanged(int newScale) {
		System.Diagnostics.Debug.WriteLine($"[GAME MENU SCENE] Scale changed to: {newScale}");
		appContext.RequestResolutionChange(
			appContext.Display.VirtualWidth * newScale,
			appContext.Display.VirtualHeight * newScale
		);
	}

	private void OnFullscreenChanged(bool isFullscreen) {
		System.Diagnostics.Debug.WriteLine($"[GAME MENU SCENE] Fullscreen changed to: {isFullscreen}");
		appContext.RequestFullscreenChange(isFullscreen);
	}

	public override void Draw(SpriteBatch spriteBatch) {
		spriteBatch.End();
		spriteBatch.Begin(samplerState: SamplerState.PointClamp);

		// Draw background overlay
		_backgroundOverlay.Draw(spriteBatch);

		// Draw root panel (contains everything)
		_rootPanel.Draw(spriteBatch);

		// Draw tooltip if visible (FIX #1 - tooltips now work!)
		if (_hoveredItem != null && _tooltipTimer >= TOOLTIP_DELAY) {
			DrawTooltip(spriteBatch, _hoveredItem);
		}

		// Draw input legend
		if (_currentTabIndex == 1) {
			// Inventory tab
			appContext.InputLegend.Draw(
				spriteBatch,
				appContext.Display.VirtualWidth,
				appContext.Display.VirtualHeight,
				(GameAction.Interact, "Equip"),
				(GameAction.Cancel, "Close"),
				(GameAction.TabLeft, "Prev Tab"),
				(GameAction.TabRight, "Next Tab")
			);
		} else {
			// Other tabs
			appContext.InputLegend.Draw(
				spriteBatch,
				appContext.Display.VirtualWidth,
				appContext.Display.VirtualHeight,
				(GameAction.Cancel, "Close"),
				(GameAction.TabLeft, "Prev Tab"),
				(GameAction.TabRight, "Next Tab")
			);
		}

		spriteBatch.End();
		spriteBatch.Begin(samplerState: SamplerState.PointClamp);
	}

	private void DrawTooltip(SpriteBatch spriteBatch, Equipment item) {
		// Update tooltip item
		_tooltip.Item = item;

		// Update position and clamp to menu bounds
		_tooltip.UpdatePosition(_tooltipPosition, _rootPanel.GlobalBounds);

		// Draw tooltip
		_tooltip.Draw(spriteBatch);
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
		foreach (IMenuTab tab in _tabs) {
			tab.Dispose();
		}
		base.Dispose();
		System.Diagnostics.Debug.WriteLine("[GAME MENU SCENE] Disposed");
	}
}