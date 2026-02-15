using EldmeresTale.Core.UI.Element;
using EldmeresTale.Entities.Definitions;
using EldmeresTale.Entities.Factories;
using Microsoft.Xna.Framework;

namespace EldmeresTale.Core.UI.Panel;

public class UIMaterialPanel : UIPanel {

	private readonly ApplicationContext _appContext;
	private readonly Inventory _inventory;

	private UIMaterialGridPanel _gridPanel;

	private UIPanel _detailPanel;
	private UIPanel _detailIconPanel;
	private UILabel _detailNameLabel;
	private UILabel _detailDescriptionLabel;
	private UIPanel _detailPricePanel;

	public UIMaterialPanel(ApplicationContext appContext, Inventory inventory) {
		_appContext = appContext;
		_inventory = inventory;

		Width = -1;  // Fill parent
		Height = -1;
		Layout = LayoutMode.Horizontal;
		Spacing = 10;
		SetPadding(10);
		BackgroundColor = Color.Transparent;

		BuildLayout();
	}

	private void BuildLayout() {
		_gridPanel = new UIMaterialGridPanel(_appContext, _inventory) {
			Width = -1,
			Height = -1,
			EnableScrolling = true,
			BackgroundColor = new Color(40, 40, 40),
			BorderColor = Color.Gray,
			BorderWidth = 2
		};
		_gridPanel.OnItemClicked += HandleItemClicked;
		_gridPanel.SetPadding(6);
		AddChild(_gridPanel);

		_detailPanel = new UIPanel {
			Width = -1,
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
			Height = 16,
			TextColor = Color.Yellow,
			WordWrap = true,
			Scale = 2f,
		};
		_detailPanel.AddChild(_detailNameLabel);

		// Price panel (horizontal layout with icon + text)
		_detailPricePanel = new UIPanel {
			Width = -1,
			Height = 16,
			Layout = LayoutMode.Horizontal,
			Spacing = 4,
			BackgroundColor = Color.Transparent,
			BorderColor = Color.Transparent
		};
		_detailPanel.AddChild(_detailPricePanel);

		// Description label
		_detailDescriptionLabel = new UILabel("") {
			TextColor = Color.LightGray,
			WordWrap = true,
			Width = -1,
			Height = -1,
		};
		_detailPanel.AddChild(_detailDescriptionLabel);
	}

	protected override void OnUpdate(GameTime gameTime) {
		if (!Visible || !Enabled) {
			return;
		}
		if (Width > 0) {
			int totalWidth = Width - (PaddingLeft + PaddingRight + Spacing);
			_gridPanel.Width = (int)(totalWidth * 0.60f);
			_detailPanel.Width = totalWidth - _gridPanel.Width;
		}
		base.OnUpdate(gameTime);
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
			PaddingTop = 4,
			TextColor = Color.Gold
		};
		priceLabel.UpdateSize();
		_detailPricePanel.AddChild(priceLabel);
	}

	private void HandleItemClicked(UIMaterialGridItem item) {
		UpdateDetailPanel(item.MaterialId);
	}
}