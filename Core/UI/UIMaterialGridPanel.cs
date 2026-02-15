using EldmeresTale.Core.UI.Element;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Core.UI;

public class UIMaterialGridPanel : UIPanel {
	public Dictionary<string, UIMaterialGridItem> GridItems { get; } = [];
	public readonly Inventory Inventory;

	private UIMaterialGridItem _selectedItem;
	private readonly ApplicationContext _applicationContext;

	public UIMaterialGridItem SelectedGridItem { get { return _selectedItem; } }

	public string SelectedItemId { get { return _selectedItem?.MaterialId; } }

	public event Action<UIMaterialGridItem> OnItemClicked;

	public UIMaterialGridPanel(ApplicationContext applicationContext, Inventory inventory) {
		_applicationContext = applicationContext;
		Inventory = inventory;
		Layout = LayoutMode.Grid;

	}

	protected override void OnUpdate(GameTime gameTime) {
		Dictionary<string, int> materials = Inventory.MaterialItems;

		// Remove items that are no longer in inventory
		List<string> toRemove = [];
		foreach (string materialId in GridItems.Keys) {
			if (!materials.TryGetValue(materialId, out int value) || value <= 0) {
				toRemove.Add(materialId);
			}
		}

		foreach (string materialId in toRemove) {
			UIMaterialGridItem item = GridItems[materialId];
			if (item == _selectedItem) {
				_selectedItem = null;
			}
			RemoveChild(item);
			GridItems.Remove(materialId);
		}

		// Add or update items
		foreach (KeyValuePair<string, int> kvp in materials) {
			string materialId = kvp.Key;
			int quantity = kvp.Value;

			if (quantity <= 0) {
				continue;
			}

			if (!GridItems.TryGetValue(materialId, out UIMaterialGridItem value)) {
				// Create new item
				Texture2D icon = _applicationContext.AssetManager.LoadTexture($"Assets/Sprites/Materials/{materialId}.png");
				UIMaterialGridItem item = new UIMaterialGridItem(materialId, icon);

				// Wire up events
				item.OnClicked += HandleItemClicked;

				AddChild(item);
				value = item;
				GridItems[materialId] = value;
			}
			value.SetQuantity(quantity);
		}
		base.OnUpdate(gameTime);
	}

	private void HandleItemClicked(UIMaterialGridItem item) {

		// Deselect previous
		_selectedItem?.SetSelected(false);

		// Select new
		_selectedItem = item;
		_selectedItem.SetSelected(true);

		OnItemClicked?.Invoke(item);
	}

	public void Deselect() {
		_selectedItem?.SetSelected(false);
	}
}
