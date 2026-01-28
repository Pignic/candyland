using EldmeresTale.Core;
using EldmeresTale.Core.UI;
using EldmeresTale.Core.UI.Panel;
using EldmeresTale.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EldmeresTale.Scenes;

public class GameMenuScene : Scene {
	private readonly GameServices _gameServices;
	private readonly int _scale;

	// UI
	private UIPanel _overlay;
	private UIPanel _rootPanel;
	private UITabContainer _tabContainer;
	private UIStatsPanel _statsPanel;
	private UIMaterialPanel _materialsPanel;
	private UIInventoryPanel _inventoryPanel;
	private UIQuestsPanel _questsPanel;
	private UIOptionsPanel _optionsPanel;

	// Navigation
	private MouseState _previousMouseState;

	public GameMenuScene(ApplicationContext appContext, GameServices gameServices)
		: base(appContext, exclusive: true) {
		_gameServices = gameServices;
		_scale = appContext.Display.Scale;

		BuildUI();

		System.Diagnostics.Debug.WriteLine("[GAME MENU SCENE] Initialized with UITabContainer");
	}

	private void BuildUI() {
		int screenWidth = appContext.Display.VirtualWidth;
		int screenHeight = appContext.Display.VirtualHeight;

		// Background overlay
		_overlay = new UIPanel() {
			X = 0,
			Y = 0,
			Width = screenWidth,
			Height = screenHeight,
			BackgroundColor = Color.Black * 0.7f,
			Layout = UIPanel.LayoutMode.Vertical
		};
		_overlay.SetPadding(10);

		// Root panel
		_rootPanel = new UIPanel() {
			Height = -1,
			Width = -1,
			BackgroundColor = Color.DarkSlateGray,
			BorderColor = Color.White,
			BorderWidth = 3,
			Layout = UIPanel.LayoutMode.Vertical
		};
		_rootPanel.SetPadding(10, 10, 0, 10);

		// Create tab content panels
		_statsPanel = new UIStatsPanel(_gameServices.Player);

		_materialsPanel = new UIMaterialPanel(appContext, _gameServices.Player);

		// Inventory panel - items and equipment with tooltips
		_inventoryPanel = new UIInventoryPanel(appContext, _gameServices.Player);

		// Quests panel - displays active/completed quests
		_questsPanel = new UIQuestsPanel(_gameServices.QuestManager);

		// Options panel with event wiring
		_optionsPanel = new UIOptionsPanel(appContext, _scale, appContext.GraphicsDevice.PresentationParameters.IsFullScreen);

		// Create tab configs with OnShow callbacks
		TabConfig[] tabs = [
			new TabConfig {
				Name = "STATS",
				Content = _statsPanel,
				OnShow = () => {
					System.Diagnostics.Debug.WriteLine("[MENU] Stats tab shown");
				}
			},
			new TabConfig {
				Name = "MATERIALS",
				Content = _materialsPanel,
				OnShow = () => {
					System.Diagnostics.Debug.WriteLine("[MENU] Materials tab shown");
				}
			},
			new TabConfig {
				Name = "EQUIPMENT",
				Content = _inventoryPanel,
				OnShow = () => {
					// Refresh inventory display
					_inventoryPanel.RefreshContent();
				},
				OnHide = () => {
					_inventoryPanel.OnHide();
				}
			},
			new TabConfig {
				Name = "QUESTS",
				Content = _questsPanel,
				OnShow = () => {
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
					_optionsPanel.OnShow();
				}
			}
		];

		// Create tab container
		_tabContainer = new UITabContainer(tabs);
		_tabContainer.UpdateButtonWidths();
		_rootPanel.AddChild(_tabContainer);
		_overlay.AddChild(_rootPanel);
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
				newIndex = _tabContainer.TabCount() - 1; // Wrap to last tab
			}

			_tabContainer.SelectTab(newIndex);
		}
		if (appContext.Input.IsActionPressed(GameAction.TabRight)) {
			int newIndex = _tabContainer.SelectedTabIndex + 1;
			if (newIndex >= _tabContainer.TabCount()) {
				newIndex = 0; // Wrap to first tab
			}

			_tabContainer.SelectTab(newIndex);
		}

		// Update UI FIRST - process mouse and visuals
		MouseState mouseState = Mouse.GetState();
		MouseState scaledMouse = appContext.Display.ScaleMouseState(mouseState);
		MouseState scaledPrevMouse = appContext.Display.ScaleMouseState(_previousMouseState);

		_overlay.Update(time);
		_overlay.HandleMouse(scaledMouse, scaledPrevMouse);

		_previousMouseState = mouseState;
	}

	public override void Draw(SpriteBatch spriteBatch) {
		// End previous batch
		spriteBatch.End();

		// Begin fresh
		spriteBatch.Begin(samplerState: SamplerState.PointClamp);

		_overlay.Draw(spriteBatch);

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

	public override void Dispose() {
		base.Dispose();
		System.Diagnostics.Debug.WriteLine("[GAME MENU SCENE] Disposed");
	}
}