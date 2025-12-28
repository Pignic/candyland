using EldmeresTale.Core;
using EldmeresTale.Core.UI;
using EldmeresTale.Entities;
using EldmeresTale.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace EldmeresTale.Scenes;

internal class GameMenuScene : Scene {

	private GameMenu _gameMenu;

	private NavigationController _navController;
	private bool _isNavigatingInventory = false;

	private int _currentTabIndex = 0;
	private int TAB_COUNT = MenuTab.Values.Count;

	private int _lastMouseHoveredIndex = -1;
	private int _lastMouseHoveredOptionsIndex = -1;

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
		} else if(_currentTabIndex == 3) {  // Options tab
			UpdateOptionsNavigation(input);
		}
		_gameMenu.Update(time);
	}
	private void SwitchToTab(int tabIndex) {
		_currentTabIndex = tabIndex;

		System.Diagnostics.Debug.WriteLine($"[MENU] Switched to tab {tabIndex}");

		_gameMenu.SwitchTabByIndex(tabIndex);

		// Update navigation mode based on tab
		if(tabIndex == 1) {
			_navController.Mode = NavigationMode.Spatial;

			int itemCount = _gameMenu.GetInventoryItemCount();
			const int COLUMNS = 2;
			int rows = itemCount > 0 ? (int)Math.Ceiling((double)itemCount / COLUMNS) : 1;

			_navController.GridSize = new Point(COLUMNS, rows);
			_navController.Reset();
			_isNavigatingInventory = true;
			_lastMouseHoveredIndex = -1;
		} else {
			// Other tabs - use index navigation for lists
			_navController.Mode = NavigationMode.Index;
			_navController.ItemCount = _gameMenu.GetCurrentTabNavigableCount();
			_isNavigatingInventory = false;
			_lastMouseHoveredIndex = -1;
		}
	}
	private void UpdateInventoryNavigation(InputCommands input) {
		if(!_isNavigatingInventory) return;

		int itemCount = _gameMenu.GetInventoryItemCount();
		const int COLUMNS = 2;
		int rows = itemCount > 0 ? (int)Math.Ceiling((double)itemCount / COLUMNS) : 1;
		_navController.GridSize = new Point(COLUMNS, rows);

		_navController.Update(input);

		//  Find which item mouse is currently over
		MouseState mouseState = Mouse.GetState();
		Point mouseScaled = appContext.Display.ScaleMouseState(mouseState).Position;

		int currentMouseHoveredIndex = -1;
		for(int i = 0; i < itemCount; i++) {
			UIElement element = _gameMenu.GetInventoryItem(i);
			if(element != null && element.GlobalBounds.Contains(mouseScaled)) {
				currentMouseHoveredIndex = i;
				break;
			}
		}

		// Only update selection if mouse moved to a DIFFERENT item
		if(currentMouseHoveredIndex != -1 &&
		   currentMouseHoveredIndex != _lastMouseHoveredIndex) {
			Point gridPos = _navController.IndexToGridPosition(currentMouseHoveredIndex);
			_navController.SetSelectedGridPosition(gridPos);
		}

		// Remember for next frame
		_lastMouseHoveredIndex = currentMouseHoveredIndex;

		// Rest stays the same...
		Point selectedSlot = _navController.SelectedGridPosition;
		int selectedIndex = _navController.GridPositionToIndex(selectedSlot);

		for(int i = 0; i < itemCount; i++) {
			UIElement element = _gameMenu.GetInventoryItem(i);
			if(element is UINavigableElement nav) {
				nav.ForceHoverState(i == selectedIndex);
			}
		}

		var inventory = appContext.gameState.Player.Inventory;
		if(selectedIndex >= 0 && selectedIndex < inventory.Items.Count) {
			UIElement selectedElement = _gameMenu.GetInventoryItem(selectedIndex);
			Rectangle? itemBounds = selectedElement?.GlobalBounds;
			_gameMenu.SetTooltipItem(
				inventory.Items[selectedIndex] as Equipment,
				itemBounds
			);
		} else {
			_gameMenu.ClearTooltip();
		}

		if(input.AttackPressed) {
			TryEquipOrUseItem(selectedSlot);
		}
	}

	private void UpdateOptionsNavigation(InputCommands input) {
		_navController.Update(input);

		int selected = _navController.SelectedIndex;
		int navigableCount = _gameMenu.GetCurrentTabNavigableCount();

		for(int i = 0; i < navigableCount; i++) {
			UIElement element = _gameMenu.GetNavigableElement(i);
			bool isSelected = (i == selected);
			if(element is UINavigableElement) {
				((UINavigableElement)element).ForceHoverState(isSelected);
			}
		}

		UIElement selectedElement = _gameMenu.GetNavigableElement(selected);

		if(selectedElement is UISlider selectedSlider) {
			// Adjust slider with left/right
			if(input.MoveLeftPressed) {
				selectedSlider.Value--;
			}
			if(input.MoveRightPressed) {
				selectedSlider.Value++;
			}
		} else if(selectedElement is UICheckbox selectedCheckbox) {
			// Toggle checkbox with space/attack
			if(input.AttackPressed) {
				selectedCheckbox.IsChecked = !selectedCheckbox.IsChecked;
			}
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
