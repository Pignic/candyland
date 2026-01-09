using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EldmeresTale.Core.UI.Tabs;

public class InventoryTab : IMenuTab {
	private readonly GraphicsDevice _graphicsDevice;
	private readonly BitmapFont _font;
	private readonly Player _player;

	// Root panels
	public UIPanel RootPanel { get; private set; }
	private UIPanel _leftPanel;   // Inventory items
	private UIPanel _rightPanel;  // Equipment slots

	// Inventory slots (created once, updated with data)
	private List<UIInventorySlot> _inventorySlots;
	private UIPanel _inventoryGrid;

	// Equipment slots
	private Dictionary<EquipmentSlot, UIEquipmentSlot> _equipmentSlots;

	// UI Elements
	private UILabel _inventoryHeader;
	private UILabel _itemCountLabel;

	// State
	public bool IsVisible {
		get => RootPanel.Visible;
		set => RootPanel.Visible = value;
	}

	// Events
	public event Action<Equipment> OnItemEquipped;
	public event Action<Equipment, Point> OnItemHovered;  // Item + tooltip position
	public event Action OnHoverCleared;

	private Equipment _hoveredItem;
	private const int INVENTORY_CAPACITY = 50;

	public InventoryTab(GraphicsDevice graphicsDevice, BitmapFont font, Player player) {
		_graphicsDevice = graphicsDevice;
		_font = font;
		_player = player;
	}

	public void Initialize() {
		// Create root panel
		RootPanel = new UIPanel(_graphicsDevice) {
			X = 10,
			Y = 32,
			Width = 600,
			Height = 253,
			Visible = false,
			BackgroundColor = Color.Transparent
		};

		// Left panel - inventory items (60% width)
		_leftPanel = new UIPanel(_graphicsDevice) {
			X = 0,
			Y = 0,
			Width = 360,
			Height = 243,
			Layout = UIPanel.LayoutMode.Vertical,
			Spacing = 5,
			EnableScrolling = true,
			BackgroundColor = new Color(30, 30, 30, 200)
		};
		_leftPanel.SetPadding(5);

		// Header
		_inventoryHeader = new UILabel(_font, "INVENTORY") {
			TextColor = Color.Yellow
		};
		_inventoryHeader.UpdateSize();
		_leftPanel.AddChild(_inventoryHeader);

		// Item count label
		_itemCountLabel = new UILabel(_font, "(0/50)") {
			TextColor = Color.Gray
		};
		_itemCountLabel.UpdateSize();
		_leftPanel.AddChild(_itemCountLabel);

		// Add spacer
		AddSpacer(_leftPanel, 5);

		// Create inventory grid (2 columns)
		_inventoryGrid = new UIPanel(_graphicsDevice) {
			Width = _leftPanel.Width - 10,
			Height = -1,
			Layout = UIPanel.LayoutMode.Grid,
			Spacing = 5,
			BackgroundColor = Color.Transparent,
			EnableScrolling = false  // Parent panel handles scrolling
		};

		// Pre-create all inventory slots
		_inventorySlots = [];
		for (int i = 0; i < INVENTORY_CAPACITY; i++) {
			UIInventorySlot slot = new UIInventorySlot(_graphicsDevice, _font, i);

			// Wire up events
			slot.OnSlotClicked += OnInventorySlotClicked;
			slot.OnSlotHovered += OnInventorySlotHovered;

			_inventorySlots.Add(slot);
			_inventoryGrid.AddChild(slot);
		}

		_leftPanel.AddChild(_inventoryGrid);
		RootPanel.AddChild(_leftPanel);

		// Right panel - equipment slots (40% width)
		_rightPanel = new UIPanel(_graphicsDevice) {
			X = 370,
			Y = 0,
			Width = 225,
			Height = 243,
			BackgroundColor = new Color(30, 30, 30, 200)
		};
		_rightPanel.SetPadding(10);

		// Create equipment slots
		CreateEquipmentSlots();

		RootPanel.AddChild(_rightPanel);

		System.Diagnostics.Debug.WriteLine($"[INVENTORY TAB] Initialized with {_inventorySlots.Count} slots");
	}

	private void CreateEquipmentSlots() {
		_equipmentSlots = [];

		// Layout positions (matching your original design)
		const int ICON_SIZE = 32;
		const int SPACING = 10;
		const int COL_1 = 10;
		const int COL_2 = COL_1 + ICON_SIZE + SPACING;
		const int COL_3 = COL_2 + ICON_SIZE + SPACING;

		int currentY = 10;

		// Row 1: Helmet (center), Amulet (right)
		AddEquipmentSlot(EquipmentSlot.Helmet, COL_2, currentY);
		AddEquipmentSlot(EquipmentSlot.Amulet, COL_3, currentY);
		currentY += ICON_SIZE + SPACING + 2;

		// Row 2: Weapon (left), Armor (center), Gloves (right)
		AddEquipmentSlot(EquipmentSlot.Weapon, COL_1, currentY);
		AddEquipmentSlot(EquipmentSlot.Armor, COL_2, currentY);
		AddEquipmentSlot(EquipmentSlot.Gloves, COL_3, currentY);
		currentY += ICON_SIZE + SPACING + 2;

		// Row 3: Ring (left), Belt (center)
		AddEquipmentSlot(EquipmentSlot.Ring, COL_1, currentY);
		AddEquipmentSlot(EquipmentSlot.Belt, COL_2, currentY);
		currentY += ICON_SIZE + SPACING + 2;

		// Row 4: Pants (center)
		AddEquipmentSlot(EquipmentSlot.Pants, COL_2, currentY);
		currentY += ICON_SIZE + SPACING + 2;

		// Row 5: Boots (center)
		AddEquipmentSlot(EquipmentSlot.Boots, COL_2, currentY);
	}

	private void AddEquipmentSlot(EquipmentSlot slot, int x, int y) {
		UIEquipmentSlot equipSlot = new UIEquipmentSlot(_graphicsDevice, _font, slot) {
			X = x,
			Y = y
		};

		equipSlot.OnSlotClicked += () => OnEquipmentSlotClicked(slot);
		equipSlot.OnSlotHovered += () => OnEquipmentSlotHovered(slot);

		_equipmentSlots[slot] = equipSlot;
		_rightPanel.AddChild(equipSlot);
	}

	/// <summary>
	/// Refresh content - update data without recreating UI
	/// </summary>
	public void RefreshContent() {
		// Update inventory slots with current items
		List<Equipment> items = _player.Inventory.EquipmentItems;

		for (int i = 0; i < _inventorySlots.Count; i++) {
			if (i < items.Count) {
				_inventorySlots[i].SetItem(items[i]);
			} else {
				_inventorySlots[i].Clear();
			}
		}

		// Update item count label
		int itemCount = items.Count;
		_itemCountLabel.SetText($"({itemCount}/{INVENTORY_CAPACITY})");

		// Update equipment slots
		foreach (KeyValuePair<EquipmentSlot, UIEquipmentSlot> kvp in _equipmentSlots) {
			Equipment equipped = _player.Inventory.GetEquippedItem(kvp.Key);
			kvp.Value.SetItem(equipped);
		}

		System.Diagnostics.Debug.WriteLine($"[INVENTORY TAB] Refreshed - {itemCount} items");
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

	// Event handlers
	private void OnInventorySlotClicked(UIInventorySlot slot) {
		if (slot.HasItem) {
			Equipment item = slot.Item;
			System.Diagnostics.Debug.WriteLine($"[INVENTORY TAB] Equipping {item.Name}");

			// Equip the item
			_player.Inventory.Equip(item, _player.Stats);

			// Refresh to show changes
			RefreshContent();

			OnItemEquipped?.Invoke(item);
		}
	}

	private void OnInventorySlotHovered(UIInventorySlot slot) {
		if (slot.HasItem && slot.GlobalBounds.Contains(Mouse.GetState().Position)) {
			_hoveredItem = slot.Item;
			// Calculate tooltip position (right side of slot)
			Point tooltipPos = new Point(
				slot.GlobalBounds.Right + 10,
				slot.GlobalBounds.Y
			);
			OnItemHovered?.Invoke(slot.Item, tooltipPos);
		} else {
			OnHoverCleared?.Invoke();
		}
	}

	private void OnEquipmentSlotClicked(EquipmentSlot slot) {
		Equipment equipped = _player.Inventory.GetEquippedItem(slot);
		if (equipped != null) {
			System.Diagnostics.Debug.WriteLine($"[INVENTORY TAB] Unequipping {equipped.Name}");

			// Unequip the item
			_player.Inventory.Unequip(slot, _player.Stats);

			// Refresh to show changes
			RefreshContent();
		}
	}

	private void OnEquipmentSlotHovered(EquipmentSlot slot) {
		Equipment equipped = _player.Inventory.GetEquippedItem(slot);
		if (equipped != null) {
			_hoveredItem = equipped;
			// Calculate tooltip position
			UIEquipmentSlot slotUI = _equipmentSlots[slot];
			Point tooltipPos = new Point(
				slotUI.GlobalBounds.Right + 10,
				slotUI.GlobalBounds.Y
			);
			OnItemHovered?.Invoke(equipped, tooltipPos);
		} else {
			OnHoverCleared?.Invoke();
		}
	}

	public int GetNavigableCount() {
		// Count visible inventory slots + equipment slots
		int count = _inventorySlots.Count(s => s.HasItem);
		count += _equipmentSlots.Count;
		return count;
	}

	public UIElement GetNavigableElement(int index) {
		// First return inventory slots with items
		List<UIInventorySlot> filledSlots = _inventorySlots.Where(s => s.HasItem).ToList();

		if (index < filledSlots.Count) {
			return filledSlots[index];
		}

		// Then equipment slots
		int equipIndex = index - filledSlots.Count;
		if (equipIndex < _equipmentSlots.Count) {
			return _equipmentSlots.Values.ElementAt(equipIndex);
		}

		return null;
	}

	/// <summary>
	/// Highlight a slot for keyboard navigation (FIX #2)
	/// </summary>
	public void HighlightSlot(int index) {
		// Clear all highlights first
		foreach (UIInventorySlot slot in _inventorySlots) {
			slot.ForceHoverState(false);
		}

		// Highlight selected slot
		if (index >= 0 && index < _inventorySlots.Count) {
			_inventorySlots[index].ForceHoverState(true);

			// Trigger hover event to show tooltip
			if (_inventorySlots[index].HasItem) {
				OnInventorySlotHovered(_inventorySlots[index]);
			}
		}
	}

	private void AddSpacer(UIPanel panel, int height) {
		UIPanel spacer = new UIPanel(_graphicsDevice) {
			Height = height,
			Width = panel.Width
		};
		panel.AddChild(spacer);
	}

	public void Dispose() {
		// Cleanup if needed
		System.Diagnostics.Debug.WriteLine("[INVENTORY TAB] Disposed");
	}
}