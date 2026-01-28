using EldmeresTale.Entities;
using EldmeresTale.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EldmeresTale.Core.UI;

public class UIInventoryPanel : UIPanel {
	private readonly Player _player;


	private readonly NavigationController _navController;

	// Sub-panels
	private UIPanel _inventoryItemsPanel;  // Left: scrollable item grid
	private UIPanel _inventoryGridPanel;   // The actual grid inside scroll area
	private UIPanel _equipmentSlotsPanel;  // Right: equipped items

	readonly ApplicationContext _appContext;

	public event Action<Equipment, bool, UIElement> OnItemHover;

	// Navigation
	private int _lastMouseHoveredIndex = -1;
	private Equipment _hoveredItem;
	private UIElement _hoveredElement;
	private Point? _keyboardTooltipPosition = null;

	// Tooltip tracking
	private float _tooltipTimer = 0f;
	private const float TOOLTIP_DELAY = 0.2f;

	public Equipment GetHoveredItem() { return _hoveredItem; }

	public UIInventoryPanel(ApplicationContext appcContext, Player player) {
		_appContext = appcContext;
		_player = player;

		_navController = new NavigationController {
			Mode = NavigationMode.Spatial,
			WrapAround = true
		};

		Width = -1;
		Height = -1;
		SetPadding(5);
		Layout = LayoutMode.Horizontal;

		BuildLayout();

		int itemCount = GetNavigableCount();
		const int COLUMNS = 2;
		int rows = itemCount > 0 ? (int)System.Math.Ceiling((double)itemCount / COLUMNS) : 1;
		_navController.GridSize = new Point(COLUMNS, rows);
		_navController.Reset();
	}

	private void BuildLayout() {
		// Left panel - scrollable item list (60% width)
		_inventoryItemsPanel = new UIPanel() {
			Width = 400,
			EnableScrolling = false,
			Layout = LayoutMode.Vertical,
			Spacing = 5,
			BackgroundColor = new Color(30, 30, 30, 200)
		};
		_inventoryItemsPanel.SetPadding(5);

		// Right panel - equipment slots (40% width)
		_equipmentSlotsPanel = new UIPanel() {
			Width = -1,
			BackgroundColor = new Color(30, 30, 30, 200)
		};
		_equipmentSlotsPanel.SetPadding(10);

		AddChild(_inventoryItemsPanel);
		AddChild(_equipmentSlotsPanel);

		RefreshContent();
	}

	public void RefreshContent() {
		RefreshInventoryItems();
		RefreshEquipmentSlots();
	}

	public override bool HandleMouse(MouseState mouse, MouseState previousMouse) {
		InputCommands input = _appContext.Input.GetCommands();
		UpdateInventoryNavigation(input);
		return base.HandleMouse(mouse, previousMouse);
	}

	private void RefreshInventoryItems() {
		_inventoryItemsPanel.ClearChildren();

		// Header
		UILabel header = new UILabel("INVENTORY") {
			TextColor = Color.Yellow
		};
		header.UpdateSize();
		_inventoryItemsPanel.AddChild(header);

		// Item count
		int itemCount = _player.Inventory.GetItemCount();
		int maxSize = _player.Inventory.MaxSize;
		string countText = maxSize > 0 ? $"({itemCount}/{maxSize})" : $"({itemCount})";
		UILabel countLabel = new UILabel(countText) {
			TextColor = Color.Gray
		};
		countLabel.UpdateSize();
		_inventoryItemsPanel.AddChild(countLabel);

		AddSpacer(_inventoryItemsPanel, 5);

		// Create scrollable grid for items
		_inventoryGridPanel = new UIPanel() {
			X = 0,
			Y = 0,
			Width = -1,
			Height = -1,
			EnableScrolling = true,
			Layout = LayoutMode.Grid,
			Spacing = 5,
			BackgroundColor = new Color(30, 30, 30, 200)
		};
		_inventoryItemsPanel.AddChild(_inventoryGridPanel);

		// Add inventory items to grid
		int lineHeight = Font.GetHeight(2);
		foreach (Equipment item in _player.Inventory.EquipmentItems) {
			AddInventoryItemButton(item, lineHeight);
		}
	}

	public override void Draw(SpriteBatch spriteBatch) {
		if (_tooltipTimer >= TOOLTIP_DELAY) {
			DrawTooltip(spriteBatch, GetHoveredItem());
		}
		base.Draw(spriteBatch);
	}

	private void AddInventoryItemButton(Equipment item, int lineHeight) {
		UIInventoryItemButton button = new UIInventoryItemButton(item, lineHeight) {
			Width = (_inventoryGridPanel.Width / 2) - 20,
			Height = lineHeight * 3,
			IsNavigable = true,
			OnClick = () => OnItemEquip(_player, item),
			OnHover = (hovered, element) => OnItemHover?.Invoke(item, hovered, element)
		};

		_inventoryGridPanel.AddChild(button);
	}

	private void RefreshEquipmentSlots() {
		_equipmentSlotsPanel.ClearChildren();

		// Icon grid layout - positioned manually for visual appeal
		const int ICON_SIZE = 32;
		const int SPACING = 10;
		const int COL_1 = 10;
		const int COL_2 = COL_1 + ICON_SIZE + SPACING;
		const int COL_3 = COL_2 + ICON_SIZE + SPACING;

		int currentY = 10;

		// Row 1: Helmet center, Amulet right
		AddEquipmentSlot(EquipmentSlot.Helmet, COL_2, currentY);
		AddEquipmentSlot(EquipmentSlot.Amulet, COL_3, currentY);
		currentY += ICON_SIZE + SPACING + 2;

		// Row 2: Weapon left, Armor center, Gloves right
		AddEquipmentSlot(EquipmentSlot.Weapon, COL_1, currentY);
		AddEquipmentSlot(EquipmentSlot.Armor, COL_2, currentY);
		AddEquipmentSlot(EquipmentSlot.Gloves, COL_3, currentY);
		currentY += ICON_SIZE + SPACING + 2;

		// Row 3: Ring left, Belt center
		AddEquipmentSlot(EquipmentSlot.Ring, COL_1, currentY);
		AddEquipmentSlot(EquipmentSlot.Belt, COL_2, currentY);
		currentY += ICON_SIZE + SPACING + 2;

		// Row 4: Pants center
		AddEquipmentSlot(EquipmentSlot.Pants, COL_2, currentY);
		currentY += ICON_SIZE + SPACING + 2;

		// Row 5: Boots center
		AddEquipmentSlot(EquipmentSlot.Boots, COL_2, currentY);
	}

	private void AddEquipmentSlot(EquipmentSlot slot, int x, int y) {
		Equipment equipped = _player.Inventory.GetEquippedItem(slot);

		UIEquipmentSlotIcon slotIcon = new UIEquipmentSlotIcon(slot, equipped) {
			X = x,
			Y = y,
			OnClick = () => {
				if (equipped != null) {
					OnItemUnequip(_player, slot);
				}
			},
			OnHover = (hovered, element) => {
				if (equipped != null) {
					OnItemHover?.Invoke(equipped, hovered, element);
				}
			}
		};

		_equipmentSlotsPanel.AddChild(slotIcon);
	}

	private void AddSpacer(UIPanel panel, int height) {
		UIPanel spacer = new UIPanel() {
			Height = height,
			Width = panel.Width
		};
		panel.AddChild(spacer);
	}

	public int GetNavigableCount() {
		if (_inventoryGridPanel == null) {
			return 0;
		}
		return _inventoryGridPanel.Children.Count(c => c.IsNavigable);
	}

	public UIElement GetNavigableElement(int index) {
		if (_inventoryGridPanel == null) {
			return null;
		}
		List<UIElement> navigable = _inventoryGridPanel.Children
			.Where(c => c.IsNavigable)
			.ToList();
		if (index >= 0 && index < navigable.Count) {
			return navigable[index];
		}
		return null;
	}

	public void OnHide() {
		// Clear tooltips when leaving inventory
		_hoveredItem = null;
		_hoveredElement = null;
		_keyboardTooltipPosition = null;
		_tooltipTimer = 0f;
		_lastMouseHoveredIndex = -1;
	}

	public override void Update(GameTime gameTime) {
		// Update tooltip timer
		if (GetHoveredItem() != null) {
			_tooltipTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
		}
		base.Update(gameTime);
	}

	public void UpdateInventoryNavigation(InputCommands input) {
		_navController.Update(input);
		int itemCount = GetNavigableCount();
		const int COLUMNS = 2;
		int rows = itemCount > 0 ? (int)System.Math.Ceiling((double)itemCount / COLUMNS) : 1;
		_navController.GridSize = new Point(COLUMNS, rows);

		// Check which item mouse is over
		MouseState mouseState = Mouse.GetState();
		Point mouseScaled = _appContext.Display.ScaleMouseState(mouseState).Position;

		int currentMouseHoveredIndex = -1;
		for (int i = 0; i < itemCount; i++) {
			UIElement element = GetNavigableElement(i);
			if (element != null && element.GlobalBounds.Contains(mouseScaled)) {
				currentMouseHoveredIndex = i;
				break;
			}
		}

		// Did mouse ENTER a different item? If yes, move focus to it
		if (currentMouseHoveredIndex != _lastMouseHoveredIndex) {
			_lastMouseHoveredIndex = currentMouseHoveredIndex;

			if (currentMouseHoveredIndex != -1) {
				// Mouse entered an item - move focus to it
				Point gridPos = _navController.IndexToGridPosition(currentMouseHoveredIndex);
				_navController.SetSelectedGridPosition(gridPos);
			}
		}

		// Update keyboard navigation (moves focus if arrow keys pressed)
		_navController.Update(input);

		// Get the ONE focused item
		Point selectedSlot = _navController.SelectedGridPosition;
		int selectedIndex = _navController.GridPositionToIndex(selectedSlot);

		// Update visual highlight for all items
		for (int i = 0; i < itemCount; i++) {
			UIElement element = GetNavigableElement(i);
			if (element is UINavigableElement nav) {
				nav.ForceHoverState(i == selectedIndex);
			}
		}

		// Show tooltip for the focused item
		if (selectedIndex >= 0 && selectedIndex < itemCount) {
			UIElement selectedElement = GetNavigableElement(selectedIndex);
			Equipment item = _player.Inventory.EquipmentItems[selectedIndex];

			_hoveredItem = item;
			_hoveredElement = selectedElement;

			// Position tooltip to right of item (or left if no room)
			Rectangle bounds = selectedElement.GlobalBounds;
			int screenWidth = _appContext.Display.VirtualWidth;
			const int TOOLTIP_WIDTH = 200; // Approximate tooltip width

			int tooltipX;
			if (bounds.Right + TOOLTIP_WIDTH + 10 < screenWidth) {
				// Room on the right
				tooltipX = bounds.Right + 10;
			} else {
				// Position on the left
				tooltipX = bounds.Left - TOOLTIP_WIDTH - 10;
			}

			_keyboardTooltipPosition = new Point(tooltipX, bounds.Y);
			_tooltipTimer = TOOLTIP_DELAY; // Show immediately
		} else {
			_hoveredItem = null;
			_hoveredElement = null;
			_keyboardTooltipPosition = null;
			_tooltipTimer = 0f;
		}

		// Equip item on Space/Enter
		if (input.AttackPressed && selectedIndex >= 0 && selectedIndex < _player.Inventory.EquipmentItems.Count) {
			Equipment item = _player.Inventory.EquipmentItems[selectedIndex];
			OnItemEquip(_player, item);
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
			Point scaledMousePos = _appContext.Display.ScaleMouseState(mouseState).Position;
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
		int lineHeight = _appContext.Font.GetHeight(2);
		int tooltipWidth = 0;
		foreach (string line in lines) {
			int lineWidth = _appContext.Font.MeasureString(line);
			if (lineWidth > tooltipWidth) {
				tooltipWidth = lineWidth;
			}
		}
		tooltipWidth += 20; // Padding
		int tooltipHeight = (lines.Count * lineHeight) + 10;

		// Clamp tooltip to stay within screen bounds
		Rectangle screenBounds = new Rectangle(0, 0,
			_appContext.Display.VirtualWidth,
			_appContext.Display.VirtualHeight);

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
		Texture2D pixelTexture = new Texture2D(_appContext.GraphicsDevice, 1, 1);
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

			_appContext.Font.DrawText(spriteBatch, line, new Vector2(tooltipX + 10, yOffset), lineColor);
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


	// Inventory event handlers
	private void OnItemEquip(Player player, Equipment item) {
		player.Inventory.SwapEquip(item, player.Stats);
		RefreshContent();
		System.Diagnostics.Debug.WriteLine($"[SCENE] Equipped: {item.Name}");
	}

	private void OnItemUnequip(Player player, EquipmentSlot slot) {
		player.Inventory.Unequip(slot, _player.Stats);
		RefreshContent();
		System.Diagnostics.Debug.WriteLine($"[SCENE] Unequipped: {slot}");
	}
}