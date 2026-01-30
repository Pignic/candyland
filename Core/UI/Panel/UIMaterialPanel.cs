using EldmeresTale.Core.UI.Element;
using EldmeresTale.Entities;
using EldmeresTale.Entities.Definitions;
using EldmeresTale.Entities.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace EldmeresTale.Core.UI.Panel;

public class UIMaterialPanel : UIPanel {

	private readonly ApplicationContext _appContext;
	private readonly Player _player;

	private UIMaterialGridItem _selectedItem;


	private readonly Dictionary<string, UIMaterialGridItem> _gridItems = [];

	private UIPanel _gridPanel;

	private UIPanel _detailPanel;
	private UIPanel _detailIconPanel;
	private UILabel _detailNameLabel;
	private UILabel _detailDescriptionLabel;
	private UIPanel _detailPricePanel;

	public UIMaterialPanel(ApplicationContext appContext, Player player) {
		_appContext = appContext;
		_player = player;

		Width = -1;  // Fill parent
		Height = -1;
		Layout = LayoutMode.Horizontal;
		Spacing = 10;
		SetPadding(10);
		BackgroundColor = Color.Transparent;

		BuildLayout();
	}

	private void BuildLayout() {
		// Left panel - Grid (75% width)
		_gridPanel = new UIPanel {
			Width = -1,  // Will calculate as 75% in UpdateLayout
			Height = -1,
			EnableScrolling = true,
			BackgroundColor = new Color(40, 40, 40),
			BorderColor = Color.Gray,
			BorderWidth = 2,
			Layout = LayoutMode.Grid  // Manual grid positioning
		};
		_gridPanel.SetPadding(6);
		AddChild(_gridPanel);

		// Right panel - Detail (25% width)
		_detailPanel = new UIPanel {
			Width = -1,  // Will calculate as 25% in UpdateLayout
			Height = -1,
			BackgroundColor = new Color(40, 40, 40),
			BorderColor = Color.Gray,
			BorderWidth = 2,
			Layout = LayoutMode.Vertical,
			Spacing = 10
		};
		_detailPanel.SetPadding(10);
		BuildDetailPanel();
		AddChild(_detailPanel);
	}

	private void BuildDetailPanel() {
		_detailPanel.ClearChildren();

		// Icon container (centered)
		_detailIconPanel = new UIPanel {
			Width = -1,
			Height = 74,
			BackgroundColor = Color.Transparent,
			BorderColor = Color.Transparent,
			PaddingBottom = 10,
		};
		_detailPanel.AddChild(_detailIconPanel);

		// Name label
		_detailNameLabel = new UILabel("") {
			TextColor = Color.Yellow,
			Scale = 1.5f,
			PaddingBottom = 5,
		};
		_detailPanel.AddChild(_detailNameLabel);


		// Price panel (horizontal layout with icon + text)
		_detailPricePanel = new UIPanel {
			Width = -1,
			Height = 16,
			Layout = LayoutMode.Horizontal,
			Spacing = 4,
			PaddingBottom = 5,
			BackgroundColor = Color.Transparent,
			BorderColor = Color.Transparent
		};
		_detailPanel.AddChild(_detailPricePanel);

		// Description label
		_detailDescriptionLabel = new UILabel("") {
			TextColor = Color.LightGray,
			//WordWrap = true,
			Width = -1,
			Height = -1,
		};
		_detailPanel.AddChild(_detailDescriptionLabel);
	}

	protected override void OnUpdate(GameTime gameTime) {
		if (!Visible || !Enabled) {
			return;
		}
		// Calculate panel widths (75% / 25% split)
		if (Width > 0) {
			int totalWidth = Width - (PaddingLeft + PaddingRight + Spacing);
			_gridPanel.Width = (int)(totalWidth * 0.75f);
			_detailPanel.Width = totalWidth - _gridPanel.Width;
		}
		RefreshContent();
		base.OnUpdate(gameTime);
	}

	public void RefreshContent() {
		Dictionary<string, int> materials = _player.Inventory.MaterialItems;

		// Remove items that are no longer in inventory
		List<string> toRemove = [];
		foreach (string materialId in _gridItems.Keys) {
			if (!materials.TryGetValue(materialId, out int value) || value <= 0) {
				toRemove.Add(materialId);
			}
		}

		foreach (string materialId in toRemove) {
			UIMaterialGridItem item = _gridItems[materialId];
			if (item == _selectedItem) {
				_selectedItem = null;
			}
			_gridPanel.RemoveChild(item);
			_gridItems.Remove(materialId);
		}

		// Add or update items
		foreach (KeyValuePair<string, int> kvp in materials) {
			string materialId = kvp.Key;
			int quantity = kvp.Value;

			if (quantity <= 0) {
				continue;
			}

			if (!_gridItems.TryGetValue(materialId, out UIMaterialGridItem value)) {
				// Create new item
				Texture2D icon = _appContext.AssetManager.LoadTexture($"Assets/Sprites/Materials/{materialId}.png");
				UIMaterialGridItem item = new UIMaterialGridItem(materialId, icon);

				// Wire up events
				item.OnClicked += HandleItemClicked;
				item.OnHoverEnter += HandleItemHoverEnter;
				item.OnHoverExit += HandleItemHoverExit;

				_gridPanel.AddChild(item);
				value = item;
				_gridItems[materialId] = value;
			}

			value.SetQuantity(quantity);

		}
	}

	private void UpdateDetailPanel(string materialId) {
		if (!MaterialFactory.Catalog.TryGetValue(materialId, out MaterialDefinition def)) {
			System.Diagnostics.Debug.WriteLine($"[UIMaterialPanel] Material definition not found: {materialId}");
			return;
		}

		// Update icon
		_detailIconPanel.ClearChildren();
		UIImage iconImage = new UIImage(_appContext.AssetManager.LoadTexture($"Assets/Sprites/Materials/{materialId}.png")) {
			Width = 64,
			Height = 64,
			X = (_detailIconPanel.Width - 64) / 2 // Center
		};
		_detailIconPanel.AddChild(iconImage);

		// Update name
		_detailNameLabel.SetText(def.Name);
		_detailNameLabel.UpdateSize();

		// Update description
		_detailDescriptionLabel.SetText(def.Description);
		_detailDescriptionLabel.UpdateSize();

		// Update price
		_detailPricePanel.ClearChildren();

		UIImage coinIcon = new UIImage(_appContext.AssetManager.LoadTexture("Assets/Sprites/Pickups/BigCoin.png")) {
			Width = 16,
			Height = 16
		};
		_detailPricePanel.AddChild(coinIcon);

		UILabel priceLabel = new UILabel(def.Price.ToString()) {
			TextColor = Color.Gold
		};
		priceLabel.UpdateSize();
		_detailPricePanel.AddChild(priceLabel);
	}
	private void HandleItemClicked(UIMaterialGridItem item) {
		// Deselect previous
		_selectedItem?.SetSelected(false);

		// Select new
		_selectedItem = item;
		_selectedItem.SetSelected(true);

		UpdateDetailPanel(item.MaterialId);

		System.Diagnostics.Debug.WriteLine($"[UIMaterialPanel] Selected: {item.MaterialId} (x{item.Quantity})");
	}

	private void HandleItemHoverEnter(UIMaterialGridItem item) {
		System.Diagnostics.Debug.WriteLine($"[UIMaterialPanel] Hover: {item.MaterialId}");
		// TODO: Update detail panel on hover (or keep selected on click?)
	}

	private void HandleItemHoverExit(UIMaterialGridItem item) {
		// TODO: Maybe restore detail panel to selected item?
	}
}