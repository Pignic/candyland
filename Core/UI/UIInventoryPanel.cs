using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace EldmeresTale.Core.UI;

public class UIInventoryPanel : UIPanel {
	private readonly Player _player;

	// Sub-panels
	private UIPanel _inventoryItemsPanel;  // Left: scrollable item grid
	private UIPanel _inventoryGridPanel;   // The actual grid inside scroll area
	private UIPanel _equipmentSlotsPanel;  // Right: equipped items

	// Events for parent scene to handle
	public event System.Action<Equipment> OnItemEquip;
	public event System.Action<EquipmentSlot> OnItemUnequip;
	public event System.Action<Equipment, bool, UIElement> OnItemHover;

	public UIInventoryPanel(Player player) {
		_player = player;

		Width = -1;
		Height = -1;
		SetPadding(5);
		Layout = LayoutMode.Horizontal;

		BuildLayout();
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
			Width = _inventoryItemsPanel.Width,
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

	private void AddInventoryItemButton(Equipment item, int lineHeight) {
		UIInventoryItemButton button = new UIInventoryItemButton(item, lineHeight) {
			Width = (_inventoryGridPanel.Width / 2) - 20,
			Height = lineHeight * 3,
			IsNavigable = true,
			OnClick = () => OnItemEquip?.Invoke(item),
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
					OnItemUnequip?.Invoke(slot);
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
}