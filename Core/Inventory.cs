using System.Collections.Generic;
using Candyland.Entities;

namespace Candyland.Core {
	public class Inventory {
		// All items the player owns (not equipped)
		public List<Equipment> Items { get; private set; }

		// Equipped items by slot
		public Dictionary<EquipmentSlot, Equipment> EquippedItems { get; private set; }

		// Maximum inventory size (0 = unlimited)
		public int MaxSize { get; set; }

		public Inventory(int maxSize = 0) {
			Items = new List<Equipment>();
			EquippedItems = new Dictionary<EquipmentSlot, Equipment>();
			MaxSize = maxSize;

			// Initialize all equipment slots as empty
			EquippedItems[EquipmentSlot.Weapon] = null;
			EquippedItems[EquipmentSlot.Helmet] = null;
			EquippedItems[EquipmentSlot.Amulet] = null;
			EquippedItems[EquipmentSlot.Armor] = null;
			EquippedItems[EquipmentSlot.Gloves] = null;
			EquippedItems[EquipmentSlot.Belt] = null;
			EquippedItems[EquipmentSlot.Pants] = null;
			EquippedItems[EquipmentSlot.Boots] = null;
			EquippedItems[EquipmentSlot.Ring] = null;
		}

		// Add item to inventory
		public bool AddItem(Equipment item) {
			if(MaxSize > 0 && Items.Count >= MaxSize)
				return false; // Inventory full

			Items.Add(item);
			return true;
		}

		// Remove item from inventory
		public bool RemoveItem(Equipment item) {
			return Items.Remove(item);
		}

		// Equip an item (from inventory or directly)
		public bool Equip(Equipment item, PlayerStats stats) {
			if(item == null)
				return false;

			// Check if already equipped
			if(EquippedItems[item.Slot] == item)
				return false;

			// Unequip current item in that slot if any
			if(EquippedItems[item.Slot] != null) {
				Unequip(item.Slot, stats);
			}

			// Remove from inventory if it's there
			Items.Remove(item);

			// Equip the item
			EquippedItems[item.Slot] = item;
			item.ApplyTo(stats);

			return true;
		}

		// Unequip an item (returns to inventory)
		public bool Unequip(EquipmentSlot slot, PlayerStats stats) {
			if(EquippedItems[slot] == null)
				return false;

			Equipment item = EquippedItems[slot];

			// Remove stats
			item.RemoveFrom(stats);

			// Return to inventory
			Items.Add(item);

			// Clear slot
			EquippedItems[slot] = null;

			return true;
		}

		// Swap equipped item with another (instant equip from inventory)
		public void SwapEquip(Equipment newItem, PlayerStats stats) {
			if(newItem == null)
				return;

			// If there's an item in that slot, unequip it first
			if(EquippedItems[newItem.Slot] != null) {
				Equipment oldItem = EquippedItems[newItem.Slot];
				oldItem.RemoveFrom(stats);
				Items.Add(oldItem);
			}

			// Equip new item
			Items.Remove(newItem);
			EquippedItems[newItem.Slot] = newItem;
			newItem.ApplyTo(stats);
		}

		// Get all items of a specific slot type
		public List<Equipment> GetItemsBySlot(EquipmentSlot slot) {
			return Items.FindAll(item => item.Slot == slot);
		}

		// Check if a slot is empty
		public bool IsSlotEmpty(EquipmentSlot slot) {
			return EquippedItems[slot] == null;
		}

		// Get equipped item in a slot
		public Equipment GetEquippedItem(EquipmentSlot slot) {
			return EquippedItems[slot];
		}

		// Count items in inventory
		public int GetItemCount() {
			return Items.Count;
		}

		// Check if inventory is full
		public bool IsFull() {
			return MaxSize > 0 && Items.Count >= MaxSize;
		}
		public void Clear() {
			Items.Clear();

			// Clear all equipped slots
			foreach(EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot))) {
				EquippedItems[slot] = null;
			}
		}
	}
}