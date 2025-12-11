namespace Candyland.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Candyland.Entities;
using System;
using System.Collections.Generic;
using Candyland.Core.UI;

public sealed class MenuTab {

	public int index;
	public string label { get; }

	private static int currentIndex = 0;

	private MenuTab(string label) {
		index = currentIndex++;
		this.label = label;
	}

	public static readonly MenuTab Stats = new MenuTab("menu.label.stats");
	public static readonly MenuTab Inventory = new MenuTab("menu.label.inventory");
	public static readonly MenuTab Quests = new MenuTab("menu.label.quests");
	public static readonly MenuTab Options = new MenuTab("menu.label.options");

	public static IReadOnlyList<MenuTab> Values { get; } =
		new List<MenuTab> { Stats, Inventory, Quests, Options }.AsReadOnly();
}

public class GameMenu {
	public bool isOpen { get; set; }
	public MenuTab currentTab { get; private set; }

	private BitmapFont font;
	private Texture2D pixelTexture;
	private Player player;

	// Input tracking
	private KeyboardState previousKeyState;
	private MouseState previousMouseState;
	private int scale;

	// Menu dimensions
	private Rectangle menuBounds;
	private Rectangle[] tabButtons;
	private Rectangle contentArea;
	private const int MENU_WIDTH = 620;
	private const int MENU_HEIGHT = 320;
	private const int MENU_PADDING_H = 20;
	private const int MENU_PADDING_V = 10;
	private const int MENU_INSTRUCTIONS_HEIGHT = 30;
	private const int TAB_HEIGHT = 22;

	// Scrolling
	private float scrollOffset = 0f;
	private float contentHeight = 0f;
	private float maxScrollOffset = 0f;
	private const float SCROLL_SPEED = 30f; // Pixels per scroll tick
	private const int SCROLLBAR_WIDTH = 10;

	// Mouse scroll tracking
	private int previousScrollWheelValue = 0;

	// Inventory-specific scrolling (left panel only)
	private float inventoryScrollOffset = 0f;
	private float inventoryContentHeight = 0f;
	private float inventoryMaxScrollOffset = 0f;
	private Rectangle inventoryScrollArea = Rectangle.Empty;

	// Inventory interaction
	private Equipment hoveredItem = null;
	private Equipment selectedItem = null;
	private bool isDraggingItem = false;
	private Vector2 dragOffset = Vector2.Zero;
	private List<Rectangle> inventoryItemBounds = new List<Rectangle>();
	private Dictionary<EquipmentSlot, Rectangle> equipmentSlotBounds = new Dictionary<EquipmentSlot, Rectangle>();
	private List<Equipment> inventoryItemsList = new List<Equipment>();

	// Tooltip
	private const float TOOLTIP_DELAY = 0.2f; // seconds
	private float tooltipTimer = 0f;
	private UIToolTip tooltip;

	private int tabCount = MenuTab.Values.Count;

	public GameMenu(BitmapFont font, Texture2D pixelTexture, Player player, int screenWidth, int screenHeight, int scale) {
		this.font = font;
		this.pixelTexture = pixelTexture;
		this.player = player;
		this.scale = scale;
		isOpen = false;
		currentTab = MenuTab.Stats;

		// Center the menu
		int menuX = (screenWidth - MENU_WIDTH) / 2;
		int menuY = (screenHeight - MENU_HEIGHT) / 2;
		menuBounds = new Rectangle(menuX, menuY, MENU_WIDTH, MENU_HEIGHT);

		// Create tab buttons
		tabButtons = new Rectangle[tabCount];
		int tabWidth = MENU_WIDTH / tabButtons.Length;
		for(int i = 0; i < tabButtons.Length; i++) {
			tabButtons[i] = new Rectangle(
				menuX + i * tabWidth,
				menuY,
				tabWidth,
				TAB_HEIGHT
			);
		}

		// Define content area (below tabs, with padding for scrollbar)
		contentArea = new Rectangle(
			menuX + MENU_PADDING_H,
			menuY + TAB_HEIGHT + MENU_PADDING_V,
			MENU_WIDTH - (MENU_PADDING_H * 2) - SCROLLBAR_WIDTH - MENU_PADDING_V, // Leave space for scrollbar
			MENU_HEIGHT - TAB_HEIGHT - (MENU_INSTRUCTIONS_HEIGHT + MENU_PADDING_V) // Leave space for instructions at bottom
		);


		tooltip = new UIToolTip(font, 0, 0);
		tooltip.renderContent = ((equipment, container, spriteBatch) => {

			if(equipment == null) return;
			Equipment tooltipItem = (Equipment)equipment;

			Point mousePos = scaleMousePosition(Mouse.GetState().Position);
			int tooltipX = mousePos.X + 15;
			int tooltipY = mousePos.Y + 15;

			// Build tooltip text
			List<string> lines = new List<string>();
			lines.Add(tooltipItem.Name);
			lines.Add($"[{tooltipItem.Rarity}]");
			lines.Add(tooltipItem.Slot.ToString());

			if(tooltipItem.RequiredLevel > 1)
				lines.Add($"Requires Level {tooltipItem.RequiredLevel}");

			lines.Add(""); // Blank line

			if(!string.IsNullOrEmpty(tooltipItem.Description)) {
				lines.Add(tooltipItem.Description);
				lines.Add("");
			}

			// Add stats
			if(tooltipItem.MaxHealthBonus != 0)
				lines.Add($"+{tooltipItem.MaxHealthBonus} Max Health");
			if(tooltipItem.AttackDamageBonus != 0)
				lines.Add($"+{tooltipItem.AttackDamageBonus} Attack Damage");
			if(tooltipItem.DefenseBonus != 0)
				lines.Add($"+{tooltipItem.DefenseBonus} Defense");
			if(tooltipItem.SpeedBonus != 0)
				lines.Add($"+{tooltipItem.SpeedBonus:F0} Speed");
			if(tooltipItem.AttackSpeedBonus != 0)
				lines.Add($"+{tooltipItem.AttackSpeedBonus:F1} Attack Speed");
			if(tooltipItem.CritChanceBonus != 0)
				lines.Add($"+{(tooltipItem.CritChanceBonus * 100):F0}% Crit Chance");
			if(tooltipItem.CritMultiplierBonus != 0)
				lines.Add($"+{tooltipItem.CritMultiplierBonus:F1}x Crit Multiplier");
			if(tooltipItem.HealthRegenBonus != 0)
				lines.Add($"+{tooltipItem.HealthRegenBonus:F1} HP/sec Regen");
			if(tooltipItem.LifeStealBonus != 0)
				lines.Add($"+{(tooltipItem.LifeStealBonus * 100):F0}% Life Steal");
			if(tooltipItem.DodgeChanceBonus != 0)
				lines.Add($"+{(tooltipItem.DodgeChanceBonus * 100):F0}% Dodge");

			// Calculate tooltip size
			int lineHeight = font.getHeight();
			int padding = 10;
			int maxWidth = 0;
			foreach(var line in lines) {
				int width = font.measureString(line);
				if(width > maxWidth) maxWidth = width;
			}

			int tooltipWidth = maxWidth + padding * 2;
			int tooltipHeight = lines.Count * lineHeight + padding * 2;

			// Keep tooltip on screen
			if(tooltipX + tooltipWidth > MENU_WIDTH) tooltipX = MENU_WIDTH - tooltipWidth;
			if(tooltipY + tooltipHeight > MENU_HEIGHT) tooltipY = MENU_HEIGHT - tooltipHeight;

			Rectangle tooltipBounds = new Rectangle(tooltipX, tooltipY, tooltipWidth, tooltipHeight);

			// Draw tooltip background
			spriteBatch.Draw(pixelTexture, tooltipBounds, Color.Black * 0.9f);
			drawBorder(spriteBatch, tooltipBounds, tooltipItem.GetRarityColor(), 2);

			// Draw text
			int yPos = tooltipY + padding;
			for(int i = 0; i < lines.Count; i++) {
				Color textColor = Color.White;
				if(i == 0) textColor = tooltipItem.GetRarityColor(); // Name
				else if(i == 1) textColor = tooltipItem.GetRarityColor(); // Rarity
				else if(i == 2) textColor = Color.Cyan; // Slot
				else if(lines[i].StartsWith("+")) textColor = Color.LightGreen; // Stats

				font.drawText(spriteBatch, lines[i], new Vector2(tooltipX + padding, yPos), textColor);
				yPos += lineHeight;
			}
		});

		previousKeyState = Keyboard.GetState();
		previousMouseState = Mouse.GetState();
	}

	private Point scaleMousePosition(Point displayMousePos) {
		return new Point(
			displayMousePos.X / scale,
			displayMousePos.Y / scale
		);
	}

	public void Update(GameTime gameTime) {
		if(!isOpen) return;

		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
		KeyboardState currentKeyState = Keyboard.GetState();
		MouseState currentMouseState = Mouse.GetState();

		// Store previous tab to reset scroll when switching
		MenuTab previousTab = currentTab;

		// Mouse click on tabs
		if(currentMouseState.LeftButton == ButtonState.Pressed &&
			previousMouseState.LeftButton == ButtonState.Released) {
			Point mousePos = scaleMousePosition(currentMouseState.Position);

			for(int i = 0; i < tabButtons.Length; i++) {
				if(tabButtons[i].Contains(mousePos)) {
					currentTab = MenuTab.Values[i];
					break;
				}
			}
		}

		// Tab switching with number keys
		if(currentKeyState.IsKeyDown(Keys.D1) && previousKeyState.IsKeyUp(Keys.D1))
			currentTab = MenuTab.Stats;
		if(currentKeyState.IsKeyDown(Keys.D2) && previousKeyState.IsKeyUp(Keys.D2))
			currentTab = MenuTab.Inventory;
		if(currentKeyState.IsKeyDown(Keys.D3) && previousKeyState.IsKeyUp(Keys.D3))
			currentTab = MenuTab.Quests;
		if(currentKeyState.IsKeyDown(Keys.D4) && previousKeyState.IsKeyUp(Keys.D4))
			currentTab = MenuTab.Options;

		// Arrow key navigation
		if(currentKeyState.IsKeyDown(Keys.Left) && previousKeyState.IsKeyUp(Keys.Left)) {
			currentTab = MenuTab.Values[((currentTab.index - 1 + tabCount) % tabCount)];
		}
		if(currentKeyState.IsKeyDown(Keys.Right) && previousKeyState.IsKeyUp(Keys.Right)) {
			currentTab = MenuTab.Values[((currentTab.index + 1) % tabCount)];
		}

		// Reset scroll when changing tabs
		if(previousTab != currentTab) {
			scrollOffset = 0f;
			inventoryScrollOffset = 0f;
			hoveredItem = null;
			selectedItem = null;
			isDraggingItem = false;
			tooltipTimer = 0f;
			tooltip.tooltipObject = null;
		}

		// Handle mouse wheel scrolling
		int scrollDelta = currentMouseState.ScrollWheelValue - previousScrollWheelValue;
		if(scrollDelta != 0) {
			// Check if mouse is over inventory left panel
			if(currentTab == MenuTab.Inventory && !inventoryScrollArea.IsEmpty && inventoryScrollArea.Contains(scaleMousePosition(currentMouseState.Position))) {
				// Scroll inventory panel
				inventoryScrollOffset -= (scrollDelta / 120f) * SCROLL_SPEED;
				inventoryScrollOffset = MathHelper.Clamp(inventoryScrollOffset, 0, inventoryMaxScrollOffset);
			} else {
				// Scroll main content
				scrollOffset -= (scrollDelta / 120f) * SCROLL_SPEED;
				clampScrollOffset();
			}
		}

		// Handle keyboard scrolling
		if(currentKeyState.IsKeyDown(Keys.Up) && previousKeyState.IsKeyUp(Keys.Up)) {
			scrollOffset -= SCROLL_SPEED;
			clampScrollOffset();
		}
		if(currentKeyState.IsKeyDown(Keys.Down) && previousKeyState.IsKeyUp(Keys.Down)) {
			scrollOffset += SCROLL_SPEED;
			clampScrollOffset();
		}

		// Inventory-specific interactions
		if(currentTab == MenuTab.Inventory) {
			updateInventoryInteractions(gameTime, currentMouseState);
		}

		previousKeyState = currentKeyState;
		previousMouseState = currentMouseState;
		previousScrollWheelValue = currentMouseState.ScrollWheelValue;
	}

	private void updateInventoryInteractions(GameTime gameTime, MouseState mouseState) {
		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
		Point mousePos = scaleMousePosition(mouseState.Position);

		// Update hovered item
		Equipment previousHovered = hoveredItem;
		hoveredItem = null;

		// Check inventory items
		for(int i = 0; i < inventoryItemBounds.Count; i++) {
			if(inventoryItemBounds[i].Contains(mousePos)) {
				hoveredItem = inventoryItemsList[i];
				break;
			}
		}

		// Check equipment slots if not hovering inventory item
		if(hoveredItem == null) {
			foreach(var kvp in equipmentSlotBounds) {
				if(kvp.Value.Contains(mousePos)) {
					var equipped = player.Inventory.GetEquippedItem(kvp.Key);
					if(equipped != null) {
						hoveredItem = equipped;
						break;
					}
				}
			}
		}

		// Tooltip timer
		if(hoveredItem != null && hoveredItem == previousHovered) {
			tooltipTimer += deltaTime;
			if(tooltipTimer >= TOOLTIP_DELAY) {
				tooltip.tooltipObject = hoveredItem;
			}
		} else {
			tooltipTimer = 0f;
			if(hoveredItem != previousHovered) {
				tooltip.tooltipObject = null;
			}
		}

		// Dragging logic
		if(mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released) {
			if(hoveredItem != null) {
				selectedItem = hoveredItem;
				isDraggingItem = true;
				dragOffset = new Vector2(mousePos.X, mousePos.Y);
			}
		}

		if(isDraggingItem && mouseState.LeftButton == ButtonState.Released) {
			// Drop the item
			if(selectedItem != null) {
				// Check if dropped on equipment slot
				foreach(var kvp in equipmentSlotBounds) {
					if(kvp.Value.Contains(mousePos)) {
						// Try to equip
						if(selectedItem.Slot == kvp.Key) {
							player.Inventory.SwapEquip(selectedItem, player.Stats);
						}
						break;
					}
				}
			}

			isDraggingItem = false;
			selectedItem = null;
		}

		// Left click to equip from inventory
		if(mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released && !isDraggingItem) {
			// Check inventory items
			for(int i = 0; i < inventoryItemBounds.Count; i++) {
				if(inventoryItemBounds[i].Contains(mousePos)) {
					Equipment item = inventoryItemsList[i];
					player.Inventory.SwapEquip(item, player.Stats);
					break;
				}
			}
		}

		// Right click to unequip
		if(mouseState.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released) {
			foreach(var kvp in equipmentSlotBounds) {
				if(kvp.Value.Contains(mousePos)) {
					player.Inventory.Unequip(kvp.Key, player.Stats);
					break;
				}
			}
		}
	}

	private void clampScrollOffset() {
		scrollOffset = MathHelper.Clamp(scrollOffset, 0f, maxScrollOffset);
	}

	public void draw(SpriteBatch spriteBatch) {
		if(!isOpen) return;

		// Draw semi-transparent background overlay
		Rectangle fullScreen = new Rectangle(0, 0, 800, 600);
		spriteBatch.Draw(pixelTexture, fullScreen, Color.Black * 0.7f);

		// Draw menu background
		spriteBatch.Draw(pixelTexture, menuBounds, Color.DarkSlateGray);

		// Draw menu border
		drawBorder(spriteBatch, menuBounds, Color.White, 3);

		// Draw tabs
		drawTabs(spriteBatch);

		// Calculate content height for current tab
		contentHeight = calculateContentHeight(currentTab);
		maxScrollOffset = Math.Max(0, contentHeight - contentArea.Height);

		// Draw content with scissor test (clipping)
		drawScrollableContent(spriteBatch);

		// Draw scrollbar if needed
		if(maxScrollOffset > 0) {
			drawScrollbar(spriteBatch);
		}

		// Draw instructions at bottom
		string instructions = "TAB: Close   Click/1/2/3/Arrows: Switch Tabs";
		if(maxScrollOffset > 0) {
			instructions += "   Scroll: Mouse Wheel / Up/Down";
		}
		if(currentTab == MenuTab.Inventory) {
			instructions = "Click: Equip   Right-Click: Unequip   Drag: Move";
		}
		int textWidth = font.measureString(instructions);
		Vector2 instructPos = new Vector2(
			menuBounds.X + (menuBounds.Width - textWidth) / 2,
			menuBounds.Bottom - 25
		);
		font.drawText(spriteBatch, instructions, instructPos, Color.Gray);

		// Draw tooltip if active
		if(tooltip.tooltipObject != null) {
			tooltip.draw(spriteBatch);
		}
	}

	private void drawScrollableContent(SpriteBatch spriteBatch) {
		// End current batch
		spriteBatch.End();

		// Inventory tab handles its own scrolling, so we draw it differently
		if(currentTab == MenuTab.Inventory) {
			// Inventory doesn't use the main scroll, just draw it directly
			spriteBatch.Begin(samplerState: SamplerState.PointClamp);
			drawInventoryTab(spriteBatch, contentArea);
			spriteBatch.End();
			spriteBatch.Begin(samplerState: SamplerState.PointClamp);
			return;
		}

		// Set up scissor rectangle for clipping (for Stats and Options)
		Rectangle scissorRect = new Rectangle(
			contentArea.X,
			contentArea.Y,
			contentArea.Width + SCROLLBAR_WIDTH + 10, // Include scrollbar area
			contentArea.Height
		);

		var rasterizerState = new RasterizerState {
			ScissorTestEnable = true
		};

		spriteBatch.Begin(
			samplerState: SamplerState.PointClamp,
			rasterizerState: rasterizerState
		);

		// Set scissor rectangle
		spriteBatch.GraphicsDevice.ScissorRectangle = scissorRect;

		// Draw content with offset
		int offsetY = (int)scrollOffset;
		Rectangle adjustedContentArea = new Rectangle(
			contentArea.X,
			contentArea.Y - offsetY,
			contentArea.Width,
			(int)contentHeight + 100 // Give extra space for rendering
		);

		switch(currentTab.index) {
			case var _ when currentTab == MenuTab.Stats:
				drawStatsTab(spriteBatch, adjustedContentArea);
				break;
			case var _ when currentTab == MenuTab.Options:
				drawOptionsTab(spriteBatch, adjustedContentArea);
				break;
		}

		spriteBatch.End();

		// Resume normal rendering
		spriteBatch.Begin(samplerState: SamplerState.PointClamp);
	}

	private void drawScrollbar(SpriteBatch spriteBatch) {
		int scrollbarX = menuBounds.Right - 20 - SCROLLBAR_WIDTH;
		int scrollbarY = menuBounds.Y + TAB_HEIGHT + 20;
		int scrollbarHeight = contentArea.Height;

		// Draw scrollbar background track
		Rectangle trackRect = new Rectangle(scrollbarX, scrollbarY, SCROLLBAR_WIDTH, scrollbarHeight);
		spriteBatch.Draw(pixelTexture, trackRect, Color.DarkGray);

		// Calculate thumb size and position
		float viewportRatio = contentArea.Height / contentHeight;
		int thumbHeight = (int)(scrollbarHeight * viewportRatio);
		thumbHeight = Math.Max(thumbHeight, 20); // Minimum thumb size

		float scrollRatio = scrollOffset / maxScrollOffset;
		int thumbY = scrollbarY + (int)((scrollbarHeight - thumbHeight) * scrollRatio);

		// Draw scrollbar thumb
		Rectangle thumbRect = new Rectangle(scrollbarX, thumbY, SCROLLBAR_WIDTH, thumbHeight);
		spriteBatch.Draw(pixelTexture, thumbRect, Color.LightGray);

		// Draw thumb border
		drawBorder(spriteBatch, thumbRect, Color.White, 1);
	}

	private float calculateContentHeight(MenuTab tab) {
		// Calculate the height of content for each tab
		// This is approximate - adjust these values based on actual content
		switch(tab) {
			case var _ when currentTab == MenuTab.Stats:
				return calculateStatsHeight();
			case var _ when currentTab == MenuTab.Inventory:
				return calculateInventoryHeight();
			case var _ when currentTab == MenuTab.Quests:
				return 200f;
			case var _ when currentTab == MenuTab.Options:
				return 200f;
			default:
				return 0f;
		}
	}

	private float calculateStatsHeight() {
		int lineHeight = font.getHeight(2);
		int lines = 0;

		// Title
		lines += 2; // Title + spacing

		// Core section
		lines += 1; // Section header
		lines += 4; // Level, Health, XP, Coins
		lines += 2; // Spacing

		// Offense section
		lines += 1; // Section header
		lines += 4; // Attack, Attack Speed, Crit Chance, Crit Mult
		if(player.Stats.LifeSteal > 0) lines += 1;
		lines += 2; // Spacing

		// Defense section
		lines += 1; // Section header
		lines += 1; // Defense
		if(player.Stats.Defense > 0) lines += 1; // Damage reduction
		if(player.Stats.DodgeChance > 0) lines += 1;
		if(player.Stats.HealthRegen > 0) lines += 1;
		lines += 2; // Spacing

		// Mobility section
		lines += 1; // Section header
		lines += 1; // Speed

		return lines * lineHeight;
	}

	private float calculateInventoryHeight() {
		// Inventory left panel handles its own scrolling independently
		// Return minimal height for the main content area
		return 200f;
	}

	private void drawTabs(SpriteBatch spriteBatch) {
		var mouseState = Mouse.GetState();
		Point mousePos = scaleMousePosition(mouseState.Position);

		for(int i = 0; i < MenuTab.Values.Count; i++) {
			bool isActive = currentTab.index == i;
			bool isHovered = tabButtons[i].Contains(mousePos) && isOpen;

			Color tabColor;
			if(isActive) {
				tabColor = Color.SlateGray;
			} else if(isHovered) {
				tabColor = Color.Gray; // Highlight on hover
			} else {
				tabColor = Color.DarkGray;
			}

			Color textColor = isActive ? Color.Yellow : (isHovered ? Color.White : Color.LightGray);

			// Draw tab background
			spriteBatch.Draw(pixelTexture, tabButtons[i], tabColor);

			// Draw tab border
			drawBorder(spriteBatch, tabButtons[i], Color.White, 2);

			// Draw tab text
			int textWidth = font.measureString(MenuTab.Values[i].label);
			Vector2 textPos = new Vector2(
				tabButtons[i].X + (tabButtons[i].Width - textWidth) / 2,
				tabButtons[i].Y + 8
			);
			font.drawText(spriteBatch, MenuTab.Values[i].label, textPos, textColor);
		}
	}

	private void drawStatsTab(SpriteBatch spriteBatch, Rectangle area) {
		int yOffset = area.Y;
		int lineHeight = font.getHeight(2);

		font.drawText(spriteBatch, "PLAYER STATISTICS", new Vector2(area.X, yOffset), Color.Yellow);
		yOffset += lineHeight * 2;

		// === CORE STATS ===
		font.drawText(spriteBatch, "-- CORE --", new Vector2(area.X, yOffset), Color.Cyan);
		yOffset += lineHeight;

		drawStatLine(spriteBatch, "Level", player.Level.ToString(), area.X, yOffset);
		yOffset += lineHeight;

		drawStatLine(spriteBatch, "Health", $"{player.Health} / {player.Stats.MaxHealth}", area.X, yOffset);
		yOffset += lineHeight;

		drawStatLine(spriteBatch, "XP", $"{player.XP} / {player.XPToNextLevel}", area.X, yOffset);
		yOffset += lineHeight;

		drawStatLine(spriteBatch, "Coins", player.Coins.ToString(), area.X, yOffset);
		yOffset += lineHeight * 2;

		// === OFFENSIVE STATS ===
		font.drawText(spriteBatch, "-- OFFENSE --", new Vector2(area.X, yOffset), Color.Orange);
		yOffset += lineHeight;

		drawStatLine(spriteBatch, "Attack Damage", player.Stats.AttackDamage.ToString(), area.X, yOffset);
		yOffset += lineHeight;

		drawStatLine(spriteBatch, "Attack Speed", $"{player.Stats.AttackSpeed:F1} / sec", area.X, yOffset);
		yOffset += lineHeight;

		drawStatLine(spriteBatch, "Crit Chance", $"{(player.Stats.CritChance * 100):F1}%", area.X, yOffset);
		yOffset += lineHeight;

		drawStatLine(spriteBatch, "Crit Multiplier", $"{player.Stats.CritMultiplier:F1}x", area.X, yOffset);
		yOffset += lineHeight;

		if(player.Stats.LifeSteal > 0) {
			drawStatLine(spriteBatch, "Life Steal", $"{(player.Stats.LifeSteal * 100):F1}%", area.X, yOffset);
			yOffset += lineHeight;
		}

		yOffset += lineHeight;

		// === DEFENSIVE STATS ===
		font.drawText(spriteBatch, "-- DEFENSE --", new Vector2(area.X, yOffset), Color.LightBlue);
		yOffset += lineHeight;

		drawStatLine(spriteBatch, "Defense", player.Stats.Defense.ToString(), area.X, yOffset);
		yOffset += lineHeight;

		if(player.Stats.Defense > 0) {
			float damageReduction = player.Stats.Defense / (player.Stats.Defense + 100f);
			drawStatLine(spriteBatch, "Damage Reduction", $"{(damageReduction * 100):F1}%", area.X, yOffset);
			yOffset += lineHeight;
		}

		if(player.Stats.DodgeChance > 0) {
			drawStatLine(spriteBatch, "Dodge Chance", $"{(player.Stats.DodgeChance * 100):F1}%", area.X, yOffset);
			yOffset += lineHeight;
		}

		if(player.Stats.HealthRegen > 0) {
			drawStatLine(spriteBatch, "Health Regen", $"{player.Stats.HealthRegen:F1} / sec", area.X, yOffset);
			yOffset += lineHeight;
		}

		yOffset += lineHeight;

		// === MOBILITY ===
		font.drawText(spriteBatch, "-- MOBILITY --", new Vector2(area.X, yOffset), Color.LightGreen);
		yOffset += lineHeight;

		drawStatLine(spriteBatch, "Speed", player.Stats.Speed.ToString("F0"), area.X, yOffset);
	}

	private void drawInventoryTab(SpriteBatch spriteBatch, Rectangle area) {
		int lineHeight = font.getHeight(3);

		// Clear bounds lists
		inventoryItemBounds.Clear();
		inventoryItemsList.Clear();
		equipmentSlotBounds.Clear();

		// Calculate split (2/3 left for items, 1/3 right for equipment)
		int leftWidth = (int)(area.Width * 0.66f);
		int rightWidth = area.Width - leftWidth - 10;

		Rectangle leftArea = new Rectangle(area.X, area.Y, leftWidth, area.Height);
		Rectangle rightArea = new Rectangle(area.X + leftWidth + 10, area.Y, rightWidth, area.Height);

		// Store scroll area for mouse detection
		inventoryScrollArea = leftArea;

		// === LEFT SIDE: SCROLLABLE INVENTORY ITEMS ===
		// Calculate content height for inventory
		int itemsHeaderHeight = lineHeight * 4; // Title + counter + spacing
		int itemHeight = lineHeight * 3 + 5; // Each item takes 3 lines + spacing
		inventoryContentHeight = itemsHeaderHeight + (player.Inventory.Items.Count * itemHeight);
		inventoryMaxScrollOffset = Math.Max(0, inventoryContentHeight - leftArea.Height);

		// Set up scissor test for left panel only
		var rasterizerState = new RasterizerState { ScissorTestEnable = true };
		spriteBatch.End();
		spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: rasterizerState);
		spriteBatch.GraphicsDevice.ScissorRectangle = leftArea;

		// Draw inventory with scroll offset
		int yOffset = leftArea.Y - (int)inventoryScrollOffset;

		font.drawText(spriteBatch, "INVENTORY", new Vector2(leftArea.X, yOffset), Color.Yellow);
		yOffset += lineHeight + 5;

		int itemCount = player.Inventory.GetItemCount();
		int maxSize = player.Inventory.MaxSize;
		string countText = maxSize > 0 ? $"({itemCount} / {maxSize})" : $"({itemCount})";
		font.drawText(spriteBatch, countText, new Vector2(leftArea.X, yOffset), Color.Gray);
		yOffset += lineHeight * 2;

		if(player.Inventory.Items.Count == 0) {
			font.drawText(spriteBatch, "No items", new Vector2(leftArea.X + 10, yOffset), Color.Gray);
		} else {
			// Draw each item in inventory
			foreach(var item in player.Inventory.Items) {
				int itemStartY = yOffset;
				int itemDisplayHeight = lineHeight * 3 + 5;

				// Create bounds for this item (in screen space for click detection)
				Rectangle itemBounds = new Rectangle(
					leftArea.X,
					itemStartY + (int)inventoryScrollOffset, // Adjust for scroll
					leftArea.Width,
					itemDisplayHeight
				);
				inventoryItemBounds.Add(itemBounds);
				inventoryItemsList.Add(item);

				// Only draw if visible in scroll area
				if(itemStartY + itemDisplayHeight >= leftArea.Y && itemStartY <= leftArea.Bottom) {
					// Highlight if hovered or selected
					bool isHovered = hoveredItem == item;
					bool isSelected = selectedItem == item;

					if(isSelected) {
						Rectangle highlightBounds = new Rectangle(leftArea.X, itemStartY, leftArea.Width, itemDisplayHeight);
						drawItemHighlight(spriteBatch, highlightBounds, Color.Yellow * 0.3f);
					} else if(isHovered) {
						Rectangle highlightBounds = new Rectangle(leftArea.X, itemStartY, leftArea.Width, itemDisplayHeight);
						drawItemHighlight(spriteBatch, highlightBounds, Color.White * 0.2f);
					}

					// Item name with rarity color
					Color nameColor = isHovered ? Color.White : item.GetRarityColor();
					font.drawText(spriteBatch, item.Name, new Vector2(leftArea.X, yOffset), nameColor);
					yOffset += lineHeight;

					// Item slot type
					string slotText = $"  [{item.Slot}]";
					font.drawText(spriteBatch, slotText, new Vector2(leftArea.X + 10, yOffset), Color.LightGray);
					yOffset += lineHeight;

					// Quick stats preview
					string statsPreview = getItemStatsPreview(item);
					if(!string.IsNullOrEmpty(statsPreview)) {
						font.drawText(spriteBatch, $"  {statsPreview}", new Vector2(leftArea.X + 10, yOffset), Color.Gray);
						yOffset += lineHeight;
					}

					yOffset += 5;
				} else {
					yOffset += itemDisplayHeight; // Still advance offset even if not drawn
				}
			}
		}

		// Draw scrollbar for left panel if needed
		if(inventoryMaxScrollOffset > 0) {
			drawInventoryScrollbar(spriteBatch, leftArea);
		}

		// End scissor test
		spriteBatch.End();
		spriteBatch.Begin(samplerState: SamplerState.PointClamp);

		// === RIGHT SIDE: EQUIPPED ITEMS (NO SCROLL) ===
		int equipYOffset = rightArea.Y;
		font.drawText(spriteBatch, "EQUIPPED", new Vector2(rightArea.X, equipYOffset), Color.Yellow);
		equipYOffset += lineHeight * 2;

		// Draw equipment slots
		drawEquipmentSlot(spriteBatch, rightArea.X, equipYOffset, EquipmentSlot.Helmet, "HELMET", rightArea.Width);
		equipYOffset += lineHeight * 3;

		drawEquipmentSlot(spriteBatch, rightArea.X, equipYOffset, EquipmentSlot.Armor, "ARMOR", rightArea.Width);
		equipYOffset += lineHeight * 3;

		drawEquipmentSlot(spriteBatch, rightArea.X, equipYOffset, EquipmentSlot.Weapon, "WEAPON", rightArea.Width);
		equipYOffset += lineHeight * 3;

		drawEquipmentSlot(spriteBatch, rightArea.X, equipYOffset, EquipmentSlot.Boots, "BOOTS", rightArea.Width);
		equipYOffset += lineHeight * 3;

		drawEquipmentSlot(spriteBatch, rightArea.X, equipYOffset, EquipmentSlot.Accessory1, "RING 1", rightArea.Width);
		equipYOffset += lineHeight * 3;

		drawEquipmentSlot(spriteBatch, rightArea.X, equipYOffset, EquipmentSlot.Accessory2, "RING 2", rightArea.Width);
	}

	private void drawInventoryScrollbar(SpriteBatch spriteBatch, Rectangle scrollArea) {
		// Scrollbar background (track)
		Rectangle scrollbarTrack = new Rectangle(
			scrollArea.Right - SCROLLBAR_WIDTH,
			scrollArea.Y,
			SCROLLBAR_WIDTH,
			scrollArea.Height
		);
		spriteBatch.Draw(pixelTexture, scrollbarTrack, Color.DarkGray * 0.5f);

		// Calculate thumb size and position
		float contentRatio = scrollArea.Height / inventoryContentHeight;
		int thumbHeight = Math.Max(20, (int)(scrollArea.Height * contentRatio));

		float scrollPercentage = inventoryMaxScrollOffset > 0 ? inventoryScrollOffset / inventoryMaxScrollOffset : 0;
		int thumbY = scrollArea.Y + (int)((scrollArea.Height - thumbHeight) * scrollPercentage);

		// Scrollbar thumb
		Rectangle scrollbarThumb = new Rectangle(
			scrollArea.Right - SCROLLBAR_WIDTH,
			thumbY,
			SCROLLBAR_WIDTH,
			thumbHeight
		);
		spriteBatch.Draw(pixelTexture, scrollbarThumb, Color.LightGray * 0.8f);
	}

	private void drawItemHighlight(SpriteBatch spriteBatch, Rectangle bounds, Color color) {
		spriteBatch.Draw(pixelTexture, bounds, color);
	}

	private void drawEquipmentSlot(SpriteBatch spriteBatch, int x, int y, EquipmentSlot slot, string slotName, int width) {
		int lineHeight = font.getHeight(2);
		int slotHeight = lineHeight * 2;

		// Store bounds for click detection
		Rectangle slotBounds = new Rectangle(x, y, width, slotHeight);
		equipmentSlotBounds[slot] = slotBounds;

		// Get equipped item
		var equippedItem = player.Inventory.GetEquippedItem(slot);

		// Highlight if hovered
		bool isHovered = hoveredItem == equippedItem && equippedItem != null;
		if(isHovered) {
			drawItemHighlight(spriteBatch, slotBounds, Color.White * 0.2f);
		}

		// Slot label
		font.drawText(spriteBatch, slotName, new Vector2(x, y), Color.Cyan);
		y += lineHeight;

		if(equippedItem == null) {
			font.drawText(spriteBatch, "  [Empty]", new Vector2(x, y), Color.DarkGray);
		} else {
			Color itemColor = isHovered ? Color.White : equippedItem.GetRarityColor();
			font.drawText(spriteBatch, $"  {equippedItem.Name}", new Vector2(x, y), itemColor);
		}
	}

	private string getItemStatsPreview(Equipment item) {
		// Show the most relevant stat for quick scanning
		List<string> stats = new List<string>();

		if(item.AttackDamageBonus > 0)
			stats.Add($"+{item.AttackDamageBonus} ATK");
		if(item.DefenseBonus > 0)
			stats.Add($"+{item.DefenseBonus} DEF");
		if(item.MaxHealthBonus > 0)
			stats.Add($"+{item.MaxHealthBonus} HP");
		if(item.SpeedBonus > 0)
			stats.Add($"+{item.SpeedBonus:F0} SPD");
		if(item.CritChanceBonus > 0)
			stats.Add($"+{(item.CritChanceBonus * 100):F0}% CRIT");

		if(stats.Count == 0)
			return "";

		// Return first 2 stats
		return string.Join(", ", stats.GetRange(0, System.Math.Min(2, stats.Count)));
	}

	private void drawOptionsTab(SpriteBatch spriteBatch, Rectangle area) {
		int yOffset = area.Y;

		font.drawText(spriteBatch, "OPTIONS", new Vector2(area.X, yOffset), Color.Yellow);
		yOffset += 50;

		font.drawText(spriteBatch, "Controls:", new Vector2(area.X, yOffset), Color.LightGray);
		yOffset += 30;

		font.drawText(spriteBatch, "WASD / Arrows - Move", new Vector2(area.X + 20, yOffset), Color.White);
		yOffset += 25;
		font.drawText(spriteBatch, "Space - Attack", new Vector2(area.X + 20, yOffset), Color.White);
		yOffset += 25;
		font.drawText(spriteBatch, "Tab - Menu", new Vector2(area.X + 20, yOffset), Color.White);
		yOffset += 25;
		font.drawText(spriteBatch, "Esc - Quit", new Vector2(area.X + 20, yOffset), Color.White);
	}

	private void drawStatLine(SpriteBatch spriteBatch, string label, string value, int x, int y) {
		font.drawText(spriteBatch, label + ":", new Vector2(x, y), Color.LightGray);
		font.drawText(spriteBatch, value, new Vector2(x + 200, y), Color.White);
	}

	private void drawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color, int thickness) {
		// Top
		spriteBatch.Draw(pixelTexture, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), color);
		// Bottom
		spriteBatch.Draw(pixelTexture, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), color);
		// Left
		spriteBatch.Draw(pixelTexture, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), color);
		// Right
		spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), color);
	}
}