using EldmeresTale.Core;
using EldmeresTale.Core.UI;
using EldmeresTale.Core.UI.Element;
using EldmeresTale.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static EldmeresTale.Core.UI.Element.UIPanel;

namespace EldmeresTale.Scenes;

public class InventoryExchangeScene : Scene {
	private readonly Inventory _inventorySource;
	private readonly Inventory _inventoryTarget;

	private UIPanel _overlay;
	private UIPanel _rootPanel;
	private UIMaterialGridPanel _sourceInventoryPanel;
	private UIPanel _interactionPanel;
	private UIMaterialGridPanel _targetInventoryPanel;

	private UIButton _buySellButton;
	private UILabel _buySellLabel;

	private MouseState _previousMouseState;

	public InventoryExchangeScene(ApplicationContext appContext, Inventory inventorySource, Inventory inventoryTarget) : base(appContext, true) {
		_inventorySource = inventorySource;
		_inventoryTarget = inventoryTarget;
		BuildUI();
	}

	public void BuildUI() {
		int screenWidth = appContext.Display.VirtualWidth;
		int screenHeight = appContext.Display.VirtualHeight;
		_overlay = new UIPanel() {
			X = 0,
			Y = 0,
			Width = screenWidth,
			Height = screenHeight,
			BackgroundColor = Color.Black * 0.7f,
			Layout = UIPanel.LayoutMode.Horizontal
		};
		_overlay.SetPadding(10);

		// Root panel
		_rootPanel = new UIPanel() {
			Height = -1,
			Width = -1,
			BackgroundColor = Color.DarkSlateGray,
			BorderColor = Color.White,
			BorderWidth = 3,
			Layout = UIPanel.LayoutMode.Horizontal
		};
		_rootPanel.SetPadding(10, 10, 10, 10);

		_sourceInventoryPanel = new UIMaterialGridPanel(appContext, _inventorySource) {
			Width = -1,
			Height = -1,
			EnableScrolling = true,
			BackgroundColor = new Color(40, 40, 40),
			BorderColor = Color.Gray,
			BorderWidth = 2
		};

		_sourceInventoryPanel.OnItemClicked += HandleSourceItemClicked;

		_targetInventoryPanel = new UIMaterialGridPanel(appContext, _inventoryTarget) {
			Width = -1,
			Height = -1,
			EnableScrolling = true,
			BackgroundColor = new Color(40, 40, 40),
			BorderColor = Color.Gray,
			BorderWidth = 2
		};

		_targetInventoryPanel.OnItemClicked += HandleTargetItemClicked;

		_buySellButton = new UIButton("") { Width = -1, Alignment = UIElement.TextAlignment.Center };
		_buySellLabel = new UILabel("") { Width = -1, Alignment = UIElement.TextAlignment.Center };

		_buySellButton.OnClick += BuySell;

		UIPanel interactionWrapperPanel = new UIPanel {
			Width = 50,
			Height = -1,
			Allign = AllignMode.Center,
			BackgroundColor = new Color(40, 40, 40),
			Layout = LayoutMode.Vertical
		};
		_interactionPanel = new UIPanel {
			Width = 50,
			Height = -1,
			Allign = AllignMode.Center,
			BackgroundColor = new Color(40, 40, 40),
			Layout = LayoutMode.Vertical,
			Visible = false
		};
		_interactionPanel.AddChild(_buySellButton);
		_interactionPanel.AddChild(_buySellLabel);

		interactionWrapperPanel.AddChild(_interactionPanel);

		_rootPanel.AddChild(_sourceInventoryPanel);
		_rootPanel.AddChild(interactionWrapperPanel);
		_rootPanel.AddChild(_targetInventoryPanel);
		_overlay.AddChild(_rootPanel);
	}

	public override void Update(GameTime time) {

		MouseState mouseState = Mouse.GetState();
		MouseState scaledMouse = appContext.Display.ScaleMouseState(mouseState);
		MouseState scaledPrevMouse = appContext.Display.ScaleMouseState(_previousMouseState);

		_overlay.Update(time);
		int width = _overlay.LocalBounds.Width;
		_sourceInventoryPanel.Width = (width - 100) / 2;
		_targetInventoryPanel.Width = (width - 100) / 2;
		_rootPanel.UpdateLayout();
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

	private void HandleSourceItemClicked(UIMaterialGridItem item) {
		_targetInventoryPanel.Deselect();
		_buySellButton.Text = "-Sell->";
		_interactionPanel.Visible = true;
	}

	private void HandleTargetItemClicked(UIMaterialGridItem item) {
		_sourceInventoryPanel.Deselect();
		_buySellButton.Text = "<-Buy--";
		_interactionPanel.Visible = true;
	}

	private void BuySell() {
		bool isBuying = false;
		string materialId = _sourceInventoryPanel.SelectedItemId;

		if (materialId == null) {
			isBuying = true;
			materialId = _targetInventoryPanel.SelectedItemId;
		}
		UIMaterialGridPanel buyerInventory = isBuying ? _sourceInventoryPanel : _targetInventoryPanel;
		UIMaterialGridPanel sellerInventory = !isBuying ? _sourceInventoryPanel : _targetInventoryPanel;
		buyerInventory.Inventory.AddItem(materialId, 1);
		if (sellerInventory.Inventory.AddItem(materialId, -1) <= 0) {
			sellerInventory.Deselect();
			_interactionPanel.Visible = false;
		}
	}
}
