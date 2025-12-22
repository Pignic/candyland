using Candyland.Core;
using Candyland.Entities;
using Candyland.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Candyland.Scenes;

internal class GameMenuScene : Scene {

	private GameMenu _gameMenu;

	private NavigationController _navController;
	private bool _isNavigatingInventory = false;

	private int _currentTabIndex = 0;
	private int TAB_COUNT = MenuTab.Values.Count;

	public GameMenuScene(ApplicationContext appContext) : base(appContext, exclusive: true) {
		_navController = new NavigationController {
			Mode = NavigationMode.Index,
			WrapAround = true
		};

		_gameMenu = new GameMenu(
			appContext.graphicsDevice,
			appContext.Font,
			appContext.gameState.Player,
			appContext.Display.VirtualWidth,
			appContext.Display.VirtualHeight,
			appContext.Display.Scale,
			appContext.gameState.QuestManager
		);
		_gameMenu.IsOpen = true;

		_gameMenu.OnScaleChanged += OnScaleChanged;
		_gameMenu.OnFullscreenChanged += OnFullscreenChanged;
	}
	private void OnScaleChanged(int newScale) {
		System.Diagnostics.Debug.WriteLine($"[GAMEMENUSCENE] Scale changed to: {newScale}");

		// Request resolution change through ApplicationContext
		int newWidth = appContext.Display.VirtualWidth * newScale;
		int newHeight = appContext.Display.VirtualHeight * newScale;

		appContext.RequestResolutionChange(newWidth, newHeight);

		// Update GameMenu's internal scale
		_gameMenu.SetScale(newScale);
	}

	private void OnFullscreenChanged(bool isFullscreen) {
		System.Diagnostics.Debug.WriteLine($"[GAMEMENUSCENE] Fullscreen changed to: {isFullscreen}");

		appContext.RequestFullscreenChange(isFullscreen);
	}

	public override void Update(GameTime time) {
		var input = appContext.Input.GetCommands();

		if(input.CancelPressed) {
			appContext.CloseScene();
			return;
		}
		if(appContext.Input.IsActionPressed(GameAction.TabLeft)) {
			_currentTabIndex--;
			if(_currentTabIndex < 0) _currentTabIndex = TAB_COUNT - 1;
			SwitchToTab(_currentTabIndex);
		}
		if(appContext.Input.IsActionPressed(GameAction.TabRight)) {
			_currentTabIndex++;
			if(_currentTabIndex >= TAB_COUNT) _currentTabIndex = 0;
			SwitchToTab(_currentTabIndex);
		}
		if(_currentTabIndex == 1) {  // Inventory tab
			UpdateInventoryNavigation(input);
		}
		_gameMenu.Update(time);
	}
	private void SwitchToTab(int tabIndex) {
		_currentTabIndex = tabIndex;

		System.Diagnostics.Debug.WriteLine($"[MENU] Switched to tab {tabIndex}");

		// Update navigation mode based on tab
		if(tabIndex == 1) {
			// Inventory tab - use spatial navigation for grid
			_navController.Mode = NavigationMode.Spatial;
			_navController.GridSize = new Point(5, 3);  // Example: 5x3 inventory grid
			_navController.Reset();
			_isNavigatingInventory = true;
		} else {
			// Other tabs - use index navigation for lists
			_navController.Mode = NavigationMode.Index;
			_navController.ItemCount = 0;  // Update based on content
			_isNavigatingInventory = false;
		}
	}
	private void UpdateInventoryNavigation(InputCommands input) {
		if(!_isNavigatingInventory) return;

		// Update navigation
		_navController.Update(input);

		// Get selected inventory slot
		Point selectedSlot = _navController.SelectedGridPosition;

		// Highlight selected slot visually
		// (You'll need to expose this in GameMenu or inventory UI)

		// Equip/use item with Interact button
		if(input.InteractPressed) {
			TryEquipOrUseItem(selectedSlot);
		}
	}
	private void TryEquipOrUseItem(Point slot) {
		// Convert grid position to inventory index
		int index = _navController.GridPositionToIndex(slot);

		System.Diagnostics.Debug.WriteLine($"[MENU] Trying to equip/use item at slot {index}");

		var inventory = appContext.gameState.Player.Inventory;
		if(index < inventory.Items.Count) {
			var item = inventory.Items[index];
			if(item is Equipment equip) {
				_gameMenu.EquipItem(equip);
			}
		}
	}

	public override void Draw(SpriteBatch spriteBatch) {
		// End previous scene's batch
		spriteBatch.End();

		// Begin fresh for menu
		spriteBatch.Begin(samplerState: SamplerState.PointClamp);

		_gameMenu.Draw(spriteBatch);
		if(_currentTabIndex == 1) {
			// Inventory tab - show inventory controls
			appContext.InputLegend.Draw(
				spriteBatch,
				appContext.Display.VirtualWidth,
				appContext.Display.VirtualHeight,
				(GameAction.Interact, "Equip/Use"),
				(GameAction.Cancel, "Close"),
				(GameAction.TabLeft, "Prev Tab"),
				(GameAction.TabRight, "Next Tab")
			);
		} else {
			// Other tabs - show general controls
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

	public override void Dispose() {
		base.Dispose();
	}
}
