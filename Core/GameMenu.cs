using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Candyland.Entities;
using System;
using System.Collections.Generic;

namespace Candyland.Core
{
    public enum MenuTab
    {
        Stats,
        Inventory,
        Options
    }

    public class GameMenu
    {
        public bool IsOpen { get; set; }
        public MenuTab CurrentTab { get; private set; }

        private BitmapFont _font;
        private Texture2D _pixelTexture;
        private Player _player;

        // Input tracking
        private KeyboardState _previousKeyState;
        private MouseState _previousMouseState;

        // Menu dimensions
        private Rectangle _menuBounds;
        private Rectangle[] _tabButtons;
        private Rectangle _contentArea;
        private const int MENU_WIDTH = 750;
        private const int MENU_HEIGHT = 550;
        private const int TAB_HEIGHT = 30;

        // Scrolling
        private float _scrollOffset = 0f;
        private float _contentHeight = 0f;
        private float _maxScrollOffset = 0f;
        private const float SCROLL_SPEED = 30f; // Pixels per scroll tick
        private const int SCROLLBAR_WIDTH = 10;

        // Mouse scroll tracking
        private int _previousScrollWheelValue = 0;

        // Inventory-specific scrolling (left panel only)
        private float _inventoryScrollOffset = 0f;
        private float _inventoryContentHeight = 0f;
        private float _inventoryMaxScrollOffset = 0f;
        private Rectangle _inventoryScrollArea = Rectangle.Empty;

        // Inventory interaction
        private Equipment _hoveredItem = null;
        private Equipment _selectedItem = null;
        private bool _isDraggingItem = false;
        private Vector2 _dragOffset = Vector2.Zero;
        private List<Rectangle> _inventoryItemBounds = new List<Rectangle>();
        private Dictionary<EquipmentSlot, Rectangle> _equipmentSlotBounds = new Dictionary<EquipmentSlot, Rectangle>();
        private List<Equipment> _inventoryItemsList = new List<Equipment>();

        // Tooltip
        private const float TOOLTIP_DELAY = 0.5f; // seconds
        private float _tooltipTimer = 0f;
        private Equipment _tooltipItem = null;

        public GameMenu(BitmapFont font, Texture2D pixelTexture, Player player, int screenWidth, int screenHeight)
        {
            _font = font;
            _pixelTexture = pixelTexture;
            _player = player;
            IsOpen = false;
            CurrentTab = MenuTab.Stats;

            // Center the menu
            int menuX = (screenWidth - MENU_WIDTH) / 2;
            int menuY = (screenHeight - MENU_HEIGHT) / 2;
            _menuBounds = new Rectangle(menuX, menuY, MENU_WIDTH, MENU_HEIGHT);

            // Create tab buttons
            _tabButtons = new Rectangle[3];
            int tabWidth = MENU_WIDTH / 3;
            for (int i = 0; i < 3; i++)
            {
                _tabButtons[i] = new Rectangle(
                    menuX + i * tabWidth,
                    menuY,
                    tabWidth,
                    TAB_HEIGHT
                );
            }

            // Define content area (below tabs, with padding for scrollbar)
            _contentArea = new Rectangle(
                menuX + 20,
                menuY + TAB_HEIGHT + 20,
                MENU_WIDTH - 40 - SCROLLBAR_WIDTH - 10, // Leave space for scrollbar
                MENU_HEIGHT - TAB_HEIGHT - 60 // Leave space for instructions at bottom
            );

            _previousKeyState = Keyboard.GetState();
            _previousMouseState = Mouse.GetState();
        }

        public void Update(GameTime gameTime)
        {
            if (!IsOpen) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState currentKeyState = Keyboard.GetState();
            MouseState currentMouseState = Mouse.GetState();

            // Store previous tab to reset scroll when switching
            MenuTab previousTab = CurrentTab;

            // Mouse click on tabs
            if (currentMouseState.LeftButton == ButtonState.Pressed &&
                _previousMouseState.LeftButton == ButtonState.Released)
            {
                Point mousePos = currentMouseState.Position;

                for (int i = 0; i < _tabButtons.Length; i++)
                {
                    if (_tabButtons[i].Contains(mousePos))
                    {
                        CurrentTab = (MenuTab)i;
                        break;
                    }
                }
            }

            // Tab switching with number keys
            if (currentKeyState.IsKeyDown(Keys.D1) && _previousKeyState.IsKeyUp(Keys.D1))
                CurrentTab = MenuTab.Stats;
            if (currentKeyState.IsKeyDown(Keys.D2) && _previousKeyState.IsKeyUp(Keys.D2))
                CurrentTab = MenuTab.Inventory;
            if (currentKeyState.IsKeyDown(Keys.D3) && _previousKeyState.IsKeyUp(Keys.D3))
                CurrentTab = MenuTab.Options;

            // Arrow key navigation
            if (currentKeyState.IsKeyDown(Keys.Left) && _previousKeyState.IsKeyUp(Keys.Left))
            {
                CurrentTab = (MenuTab)(((int)CurrentTab - 1 + 3) % 3);
            }
            if (currentKeyState.IsKeyDown(Keys.Right) && _previousKeyState.IsKeyUp(Keys.Right))
            {
                CurrentTab = (MenuTab)(((int)CurrentTab + 1) % 3);
            }

            // Reset scroll when changing tabs
            if (previousTab != CurrentTab)
            {
                _scrollOffset = 0f;
                _inventoryScrollOffset = 0f;
                _hoveredItem = null;
                _selectedItem = null;
                _isDraggingItem = false;
                _tooltipTimer = 0f;
                _tooltipItem = null;
            }

            // Handle mouse wheel scrolling
            int scrollDelta = currentMouseState.ScrollWheelValue - _previousScrollWheelValue;
            if (scrollDelta != 0)
            {
                // Check if mouse is over inventory left panel
                if (CurrentTab == MenuTab.Inventory && !_inventoryScrollArea.IsEmpty && _inventoryScrollArea.Contains(currentMouseState.Position))
                {
                    // Scroll inventory panel
                    _inventoryScrollOffset -= (scrollDelta / 120f) * SCROLL_SPEED;
                    _inventoryScrollOffset = MathHelper.Clamp(_inventoryScrollOffset, 0, _inventoryMaxScrollOffset);
                }
                else
                {
                    // Scroll main content
                    _scrollOffset -= (scrollDelta / 120f) * SCROLL_SPEED;
                    ClampScrollOffset();
                }
            }

            // Handle keyboard scrolling
            if (currentKeyState.IsKeyDown(Keys.Up) && _previousKeyState.IsKeyUp(Keys.Up))
            {
                _scrollOffset -= SCROLL_SPEED;
                ClampScrollOffset();
            }
            if (currentKeyState.IsKeyDown(Keys.Down) && _previousKeyState.IsKeyUp(Keys.Down))
            {
                _scrollOffset += SCROLL_SPEED;
                ClampScrollOffset();
            }

            // Inventory-specific interactions
            if (CurrentTab == MenuTab.Inventory)
            {
                UpdateInventoryInteractions(gameTime, currentMouseState);
            }

            _previousKeyState = currentKeyState;
            _previousMouseState = currentMouseState;
            _previousScrollWheelValue = currentMouseState.ScrollWheelValue;
        }

        private void UpdateInventoryInteractions(GameTime gameTime, MouseState mouseState)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Point mousePos = mouseState.Position;

            // Update hovered item
            Equipment previousHovered = _hoveredItem;
            _hoveredItem = null;

            // Check inventory items
            for (int i = 0; i < _inventoryItemBounds.Count; i++)
            {
                if (_inventoryItemBounds[i].Contains(mousePos))
                {
                    _hoveredItem = _inventoryItemsList[i];
                    break;
                }
            }

            // Check equipment slots if not hovering inventory item
            if (_hoveredItem == null)
            {
                foreach (var kvp in _equipmentSlotBounds)
                {
                    if (kvp.Value.Contains(mousePos))
                    {
                        var equipped = _player.Inventory.GetEquippedItem(kvp.Key);
                        if (equipped != null)
                        {
                            _hoveredItem = equipped;
                            break;
                        }
                    }
                }
            }

            // Tooltip timer
            if (_hoveredItem != null && _hoveredItem == previousHovered)
            {
                _tooltipTimer += deltaTime;
                if (_tooltipTimer >= TOOLTIP_DELAY)
                {
                    _tooltipItem = _hoveredItem;
                }
            }
            else
            {
                _tooltipTimer = 0f;
                if (_hoveredItem != previousHovered)
                {
                    _tooltipItem = null;
                }
            }

            // Dragging logic
            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                if (_hoveredItem != null)
                {
                    _selectedItem = _hoveredItem;
                    _isDraggingItem = true;
                    _dragOffset = new Vector2(mousePos.X, mousePos.Y);
                }
            }

            if (_isDraggingItem && mouseState.LeftButton == ButtonState.Released)
            {
                // Drop the item
                if (_selectedItem != null)
                {
                    // Check if dropped on equipment slot
                    foreach (var kvp in _equipmentSlotBounds)
                    {
                        if (kvp.Value.Contains(mousePos))
                        {
                            // Try to equip
                            if (_selectedItem.Slot == kvp.Key)
                            {
                                _player.Inventory.SwapEquip(_selectedItem, _player.Stats);
                            }
                            break;
                        }
                    }
                }

                _isDraggingItem = false;
                _selectedItem = null;
            }

            // Left click to equip from inventory
            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released && !_isDraggingItem)
            {
                // Check inventory items
                for (int i = 0; i < _inventoryItemBounds.Count; i++)
                {
                    if (_inventoryItemBounds[i].Contains(mousePos))
                    {
                        Equipment item = _inventoryItemsList[i];
                        _player.Inventory.SwapEquip(item, _player.Stats);
                        break;
                    }
                }
            }

            // Right click to unequip
            if (mouseState.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
            {
                foreach (var kvp in _equipmentSlotBounds)
                {
                    if (kvp.Value.Contains(mousePos))
                    {
                        _player.Inventory.Unequip(kvp.Key, _player.Stats);
                        break;
                    }
                }
            }
        }

        private void ClampScrollOffset()
        {
            _scrollOffset = MathHelper.Clamp(_scrollOffset, 0f, _maxScrollOffset);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsOpen) return;

            // Draw semi-transparent background overlay
            Rectangle fullScreen = new Rectangle(0, 0, 800, 600);
            spriteBatch.Draw(_pixelTexture, fullScreen, Color.Black * 0.7f);

            // Draw menu background
            spriteBatch.Draw(_pixelTexture, _menuBounds, Color.DarkSlateGray);

            // Draw menu border
            DrawBorder(spriteBatch, _menuBounds, Color.White, 3);

            // Draw tabs
            DrawTabs(spriteBatch);

            // Calculate content height for current tab
            _contentHeight = CalculateContentHeight(CurrentTab);
            _maxScrollOffset = Math.Max(0, _contentHeight - _contentArea.Height);

            // Draw content with scissor test (clipping)
            DrawScrollableContent(spriteBatch);

            // Draw scrollbar if needed
            if (_maxScrollOffset > 0)
            {
                DrawScrollbar(spriteBatch);
            }

            // Draw instructions at bottom
            string instructions = "TAB: Close   Click/1/2/3/Arrows: Switch Tabs";
            if (_maxScrollOffset > 0)
            {
                instructions += "   Scroll: Mouse Wheel / Up/Down";
            }
            if (CurrentTab == MenuTab.Inventory)
            {
                instructions = "Click: Equip   Right-Click: Unequip   Drag: Move";
            }
            int textWidth = _font.MeasureString(instructions);
            Vector2 instructPos = new Vector2(
                _menuBounds.X + (_menuBounds.Width - textWidth) / 2,
                _menuBounds.Bottom - 25
            );
            _font.DrawText(spriteBatch, instructions, instructPos, Color.Gray);

            // Draw tooltip if active
            if (_tooltipItem != null)
            {
                DrawTooltip(spriteBatch);
            }
        }

        private void DrawTooltip(SpriteBatch spriteBatch)
        {
            if (_tooltipItem == null) return;

            var mouseState = Mouse.GetState();
            int tooltipX = mouseState.X + 15;
            int tooltipY = mouseState.Y + 15;

            // Build tooltip text
            List<string> lines = new List<string>();
            lines.Add(_tooltipItem.Name);
            lines.Add($"[{_tooltipItem.Rarity}]");
            lines.Add(_tooltipItem.Slot.ToString());

            if (_tooltipItem.RequiredLevel > 1)
                lines.Add($"Requires Level {_tooltipItem.RequiredLevel}");

            lines.Add(""); // Blank line

            if (!string.IsNullOrEmpty(_tooltipItem.Description))
            {
                lines.Add(_tooltipItem.Description);
                lines.Add("");
            }

            // Add stats
            if (_tooltipItem.MaxHealthBonus != 0)
                lines.Add($"+{_tooltipItem.MaxHealthBonus} Max Health");
            if (_tooltipItem.AttackDamageBonus != 0)
                lines.Add($"+{_tooltipItem.AttackDamageBonus} Attack Damage");
            if (_tooltipItem.DefenseBonus != 0)
                lines.Add($"+{_tooltipItem.DefenseBonus} Defense");
            if (_tooltipItem.SpeedBonus != 0)
                lines.Add($"+{_tooltipItem.SpeedBonus:F0} Speed");
            if (_tooltipItem.AttackSpeedBonus != 0)
                lines.Add($"+{_tooltipItem.AttackSpeedBonus:F1} Attack Speed");
            if (_tooltipItem.CritChanceBonus != 0)
                lines.Add($"+{(_tooltipItem.CritChanceBonus * 100):F0}% Crit Chance");
            if (_tooltipItem.CritMultiplierBonus != 0)
                lines.Add($"+{_tooltipItem.CritMultiplierBonus:F1}x Crit Multiplier");
            if (_tooltipItem.HealthRegenBonus != 0)
                lines.Add($"+{_tooltipItem.HealthRegenBonus:F1} HP/sec Regen");
            if (_tooltipItem.LifeStealBonus != 0)
                lines.Add($"+{(_tooltipItem.LifeStealBonus * 100):F0}% Life Steal");
            if (_tooltipItem.DodgeChanceBonus != 0)
                lines.Add($"+{(_tooltipItem.DodgeChanceBonus * 100):F0}% Dodge");

            // Calculate tooltip size
            int lineHeight = 16;
            int padding = 10;
            int maxWidth = 0;
            foreach (var line in lines)
            {
                int width = _font.MeasureString(line);
                if (width > maxWidth) maxWidth = width;
            }

            int tooltipWidth = maxWidth + padding * 2;
            int tooltipHeight = lines.Count * lineHeight + padding * 2;

            // Keep tooltip on screen
            if (tooltipX + tooltipWidth > 800) tooltipX = 800 - tooltipWidth;
            if (tooltipY + tooltipHeight > 600) tooltipY = 600 - tooltipHeight;

            Rectangle tooltipBounds = new Rectangle(tooltipX, tooltipY, tooltipWidth, tooltipHeight);

            // Draw tooltip background
            spriteBatch.Draw(_pixelTexture, tooltipBounds, Color.Black * 0.9f);
            DrawBorder(spriteBatch, tooltipBounds, _tooltipItem.GetRarityColor(), 2);

            // Draw text
            int yPos = tooltipY + padding;
            for (int i = 0; i < lines.Count; i++)
            {
                Color textColor = Color.White;
                if (i == 0) textColor = _tooltipItem.GetRarityColor(); // Name
                else if (i == 1) textColor = _tooltipItem.GetRarityColor(); // Rarity
                else if (i == 2) textColor = Color.Cyan; // Slot
                else if (lines[i].StartsWith("+")) textColor = Color.LightGreen; // Stats

                _font.DrawText(spriteBatch, lines[i], new Vector2(tooltipX + padding, yPos), textColor);
                yPos += lineHeight;
            }
        }

        private void DrawScrollableContent(SpriteBatch spriteBatch)
        {
            // End current batch
            spriteBatch.End();

            // Inventory tab handles its own scrolling, so we draw it differently
            if (CurrentTab == MenuTab.Inventory)
            {
                // Inventory doesn't use the main scroll, just draw it directly
                spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                DrawInventoryTab(spriteBatch, _contentArea);
                spriteBatch.End();
                spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                return;
            }

            // Set up scissor rectangle for clipping (for Stats and Options)
            Rectangle scissorRect = new Rectangle(
                _contentArea.X,
                _contentArea.Y,
                _contentArea.Width + SCROLLBAR_WIDTH + 10, // Include scrollbar area
                _contentArea.Height
            );

            var rasterizerState = new RasterizerState
            {
                ScissorTestEnable = true
            };

            spriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                rasterizerState: rasterizerState
            );

            // Set scissor rectangle
            spriteBatch.GraphicsDevice.ScissorRectangle = scissorRect;

            // Draw content with offset
            int offsetY = (int)_scrollOffset;
            Rectangle adjustedContentArea = new Rectangle(
                _contentArea.X,
                _contentArea.Y - offsetY,
                _contentArea.Width,
                (int)_contentHeight + 100 // Give extra space for rendering
            );

            switch (CurrentTab)
            {
                case MenuTab.Stats:
                    DrawStatsTab(spriteBatch, adjustedContentArea);
                    break;
                case MenuTab.Options:
                    DrawOptionsTab(spriteBatch, adjustedContentArea);
                    break;
            }

            // End scissor test batch
            spriteBatch.End();

            // Resume normal rendering
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        }

        private void DrawScrollbar(SpriteBatch spriteBatch)
        {
            int scrollbarX = _menuBounds.Right - 20 - SCROLLBAR_WIDTH;
            int scrollbarY = _menuBounds.Y + TAB_HEIGHT + 20;
            int scrollbarHeight = _contentArea.Height;

            // Draw scrollbar background track
            Rectangle trackRect = new Rectangle(scrollbarX, scrollbarY, SCROLLBAR_WIDTH, scrollbarHeight);
            spriteBatch.Draw(_pixelTexture, trackRect, Color.DarkGray);

            // Calculate thumb size and position
            float viewportRatio = _contentArea.Height / _contentHeight;
            int thumbHeight = (int)(scrollbarHeight * viewportRatio);
            thumbHeight = Math.Max(thumbHeight, 20); // Minimum thumb size

            float scrollRatio = _scrollOffset / _maxScrollOffset;
            int thumbY = scrollbarY + (int)((scrollbarHeight - thumbHeight) * scrollRatio);

            // Draw scrollbar thumb
            Rectangle thumbRect = new Rectangle(scrollbarX, thumbY, SCROLLBAR_WIDTH, thumbHeight);
            spriteBatch.Draw(_pixelTexture, thumbRect, Color.LightGray);

            // Draw thumb border
            DrawBorder(spriteBatch, thumbRect, Color.White, 1);
        }

        private float CalculateContentHeight(MenuTab tab)
        {
            // Calculate the height of content for each tab
            // This is approximate - adjust these values based on actual content
            switch (tab)
            {
                case MenuTab.Stats:
                    return CalculateStatsHeight();
                case MenuTab.Inventory:
                    return CalculateInventoryHeight();
                case MenuTab.Options:
                    return 200f; // Options is relatively short
                default:
                    return 0f;
            }
        }

        private float CalculateStatsHeight()
        {
            int lineHeight = 20;
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
            if (_player.Stats.LifeSteal > 0) lines += 1;
            lines += 2; // Spacing

            // Defense section
            lines += 1; // Section header
            lines += 1; // Defense
            if (_player.Stats.Defense > 0) lines += 1; // Damage reduction
            if (_player.Stats.DodgeChance > 0) lines += 1;
            if (_player.Stats.HealthRegen > 0) lines += 1;
            lines += 2; // Spacing

            // Mobility section
            lines += 1; // Section header
            lines += 1; // Speed

            return lines * lineHeight;
        }

        private float CalculateInventoryHeight()
        {
            // Inventory left panel handles its own scrolling independently
            // Return minimal height for the main content area
            return 200f;
        }

        private void DrawTabs(SpriteBatch spriteBatch)
        {
            string[] tabNames = { "STATS", "INVENTORY", "OPTIONS" };
            var mouseState = Mouse.GetState();
            Point mousePos = mouseState.Position;

            for (int i = 0; i < 3; i++)
            {
                bool isActive = (int)CurrentTab == i;
                bool isHovered = _tabButtons[i].Contains(mousePos) && IsOpen;

                Color tabColor;
                if (isActive)
                {
                    tabColor = Color.SlateGray;
                }
                else if (isHovered)
                {
                    tabColor = Color.Gray; // Highlight on hover
                }
                else
                {
                    tabColor = Color.DarkGray;
                }

                Color textColor = isActive ? Color.Yellow : (isHovered ? Color.White : Color.LightGray);

                // Draw tab background
                spriteBatch.Draw(_pixelTexture, _tabButtons[i], tabColor);

                // Draw tab border
                DrawBorder(spriteBatch, _tabButtons[i], Color.White, 2);

                // Draw tab text
                int textWidth = _font.MeasureString(tabNames[i]);
                Vector2 textPos = new Vector2(
                    _tabButtons[i].X + (_tabButtons[i].Width - textWidth) / 2,
                    _tabButtons[i].Y + 8
                );
                _font.DrawText(spriteBatch, tabNames[i], textPos, textColor);
            }
        }

        private void DrawStatsTab(SpriteBatch spriteBatch, Rectangle area)
        {
            int yOffset = area.Y;
            int lineHeight = 20;

            _font.DrawText(spriteBatch, "PLAYER STATISTICS", new Vector2(area.X, yOffset), Color.Yellow);
            yOffset += lineHeight * 2;

            // === CORE STATS ===
            _font.DrawText(spriteBatch, "-- CORE --", new Vector2(area.X, yOffset), Color.Cyan);
            yOffset += lineHeight;

            DrawStatLine(spriteBatch, "Level", _player.Level.ToString(), area.X, yOffset);
            yOffset += lineHeight;

            DrawStatLine(spriteBatch, "Health", $"{_player.Health} / {_player.Stats.MaxHealth}", area.X, yOffset);
            yOffset += lineHeight;

            DrawStatLine(spriteBatch, "XP", $"{_player.XP} / {_player.XPToNextLevel}", area.X, yOffset);
            yOffset += lineHeight;

            DrawStatLine(spriteBatch, "Coins", _player.Coins.ToString(), area.X, yOffset);
            yOffset += lineHeight * 2;

            // === OFFENSIVE STATS ===
            _font.DrawText(spriteBatch, "-- OFFENSE --", new Vector2(area.X, yOffset), Color.Orange);
            yOffset += lineHeight;

            DrawStatLine(spriteBatch, "Attack Damage", _player.Stats.AttackDamage.ToString(), area.X, yOffset);
            yOffset += lineHeight;

            DrawStatLine(spriteBatch, "Attack Speed", $"{_player.Stats.AttackSpeed:F1} / sec", area.X, yOffset);
            yOffset += lineHeight;

            DrawStatLine(spriteBatch, "Crit Chance", $"{(_player.Stats.CritChance * 100):F1}%", area.X, yOffset);
            yOffset += lineHeight;

            DrawStatLine(spriteBatch, "Crit Multiplier", $"{_player.Stats.CritMultiplier:F1}x", area.X, yOffset);
            yOffset += lineHeight;

            if (_player.Stats.LifeSteal > 0)
            {
                DrawStatLine(spriteBatch, "Life Steal", $"{(_player.Stats.LifeSteal * 100):F1}%", area.X, yOffset);
                yOffset += lineHeight;
            }

            yOffset += lineHeight;

            // === DEFENSIVE STATS ===
            _font.DrawText(spriteBatch, "-- DEFENSE --", new Vector2(area.X, yOffset), Color.LightBlue);
            yOffset += lineHeight;

            DrawStatLine(spriteBatch, "Defense", _player.Stats.Defense.ToString(), area.X, yOffset);
            yOffset += lineHeight;

            if (_player.Stats.Defense > 0)
            {
                float damageReduction = _player.Stats.Defense / (_player.Stats.Defense + 100f);
                DrawStatLine(spriteBatch, "Damage Reduction", $"{(damageReduction * 100):F1}%", area.X, yOffset);
                yOffset += lineHeight;
            }

            if (_player.Stats.DodgeChance > 0)
            {
                DrawStatLine(spriteBatch, "Dodge Chance", $"{(_player.Stats.DodgeChance * 100):F1}%", area.X, yOffset);
                yOffset += lineHeight;
            }

            if (_player.Stats.HealthRegen > 0)
            {
                DrawStatLine(spriteBatch, "Health Regen", $"{_player.Stats.HealthRegen:F1} / sec", area.X, yOffset);
                yOffset += lineHeight;
            }

            yOffset += lineHeight;

            // === MOBILITY ===
            _font.DrawText(spriteBatch, "-- MOBILITY --", new Vector2(area.X, yOffset), Color.LightGreen);
            yOffset += lineHeight;

            DrawStatLine(spriteBatch, "Speed", _player.Stats.Speed.ToString("F0"), area.X, yOffset);
        }

        private void DrawInventoryTab(SpriteBatch spriteBatch, Rectangle area)
        {
            int lineHeight = 22;

            // Clear bounds lists
            _inventoryItemBounds.Clear();
            _inventoryItemsList.Clear();
            _equipmentSlotBounds.Clear();

            // Calculate split (2/3 left for items, 1/3 right for equipment)
            int leftWidth = (int)(area.Width * 0.66f);
            int rightWidth = area.Width - leftWidth - 10;

            Rectangle leftArea = new Rectangle(area.X, area.Y, leftWidth, area.Height);
            Rectangle rightArea = new Rectangle(area.X + leftWidth + 10, area.Y, rightWidth, area.Height);

            // Store scroll area for mouse detection
            _inventoryScrollArea = leftArea;

            // === LEFT SIDE: SCROLLABLE INVENTORY ITEMS ===
            // Calculate content height for inventory
            int itemsHeaderHeight = lineHeight * 4; // Title + counter + spacing
            int itemHeight = lineHeight * 3 + 5; // Each item takes 3 lines + spacing
            _inventoryContentHeight = itemsHeaderHeight + (_player.Inventory.Items.Count * itemHeight);
            _inventoryMaxScrollOffset = Math.Max(0, _inventoryContentHeight - leftArea.Height);

            // Set up scissor test for left panel only
            var rasterizerState = new RasterizerState { ScissorTestEnable = true };
            spriteBatch.End();
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: rasterizerState);
            spriteBatch.GraphicsDevice.ScissorRectangle = leftArea;

            // Draw inventory with scroll offset
            int yOffset = leftArea.Y - (int)_inventoryScrollOffset;

            _font.DrawText(spriteBatch, "INVENTORY", new Vector2(leftArea.X, yOffset), Color.Yellow);
            yOffset += lineHeight + 5;

            int itemCount = _player.Inventory.GetItemCount();
            int maxSize = _player.Inventory.MaxSize;
            string countText = maxSize > 0 ? $"({itemCount} / {maxSize})" : $"({itemCount})";
            _font.DrawText(spriteBatch, countText, new Vector2(leftArea.X, yOffset), Color.Gray);
            yOffset += lineHeight * 2;

            if (_player.Inventory.Items.Count == 0)
            {
                _font.DrawText(spriteBatch, "No items", new Vector2(leftArea.X + 10, yOffset), Color.Gray);
            }
            else
            {
                // Draw each item in inventory
                foreach (var item in _player.Inventory.Items)
                {
                    int itemStartY = yOffset;
                    int itemDisplayHeight = lineHeight * 3 + 5;

                    // Create bounds for this item (in screen space for click detection)
                    Rectangle itemBounds = new Rectangle(
                        leftArea.X,
                        itemStartY + (int)_inventoryScrollOffset, // Adjust for scroll
                        leftArea.Width,
                        itemDisplayHeight
                    );
                    _inventoryItemBounds.Add(itemBounds);
                    _inventoryItemsList.Add(item);

                    // Only draw if visible in scroll area
                    if (itemStartY + itemDisplayHeight >= leftArea.Y && itemStartY <= leftArea.Bottom)
                    {
                        // Highlight if hovered or selected
                        bool isHovered = _hoveredItem == item;
                        bool isSelected = _selectedItem == item;

                        if (isSelected)
                        {
                            Rectangle highlightBounds = new Rectangle(leftArea.X, itemStartY, leftArea.Width, itemDisplayHeight);
                            DrawItemHighlight(spriteBatch, highlightBounds, Color.Yellow * 0.3f);
                        }
                        else if (isHovered)
                        {
                            Rectangle highlightBounds = new Rectangle(leftArea.X, itemStartY, leftArea.Width, itemDisplayHeight);
                            DrawItemHighlight(spriteBatch, highlightBounds, Color.White * 0.2f);
                        }

                        // Item name with rarity color
                        Color nameColor = isHovered ? Color.White : item.GetRarityColor();
                        _font.DrawText(spriteBatch, item.Name, new Vector2(leftArea.X, yOffset), nameColor);
                        yOffset += lineHeight;

                        // Item slot type
                        string slotText = $"  [{item.Slot}]";
                        _font.DrawText(spriteBatch, slotText, new Vector2(leftArea.X + 10, yOffset), Color.LightGray);
                        yOffset += lineHeight;

                        // Quick stats preview
                        string statsPreview = GetItemStatsPreview(item);
                        if (!string.IsNullOrEmpty(statsPreview))
                        {
                            _font.DrawText(spriteBatch, $"  {statsPreview}", new Vector2(leftArea.X + 10, yOffset), Color.Gray);
                            yOffset += lineHeight;
                        }

                        yOffset += 5;
                    }
                    else
                    {
                        yOffset += itemDisplayHeight; // Still advance offset even if not drawn
                    }
                }
            }

            // Draw scrollbar for left panel if needed
            if (_inventoryMaxScrollOffset > 0)
            {
                DrawInventoryScrollbar(spriteBatch, leftArea);
            }

            // End scissor test
            spriteBatch.End();
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // === RIGHT SIDE: EQUIPPED ITEMS (NO SCROLL) ===
            int equipYOffset = rightArea.Y;
            _font.DrawText(spriteBatch, "EQUIPPED", new Vector2(rightArea.X, equipYOffset), Color.Yellow);
            equipYOffset += lineHeight * 2;

            // Draw equipment slots
            DrawEquipmentSlot(spriteBatch, rightArea.X, equipYOffset, EquipmentSlot.Helmet, "HELMET", rightArea.Width);
            equipYOffset += lineHeight * 3;

            DrawEquipmentSlot(spriteBatch, rightArea.X, equipYOffset, EquipmentSlot.Armor, "ARMOR", rightArea.Width);
            equipYOffset += lineHeight * 3;

            DrawEquipmentSlot(spriteBatch, rightArea.X, equipYOffset, EquipmentSlot.Weapon, "WEAPON", rightArea.Width);
            equipYOffset += lineHeight * 3;

            DrawEquipmentSlot(spriteBatch, rightArea.X, equipYOffset, EquipmentSlot.Boots, "BOOTS", rightArea.Width);
            equipYOffset += lineHeight * 3;

            DrawEquipmentSlot(spriteBatch, rightArea.X, equipYOffset, EquipmentSlot.Accessory1, "RING 1", rightArea.Width);
            equipYOffset += lineHeight * 3;

            DrawEquipmentSlot(spriteBatch, rightArea.X, equipYOffset, EquipmentSlot.Accessory2, "RING 2", rightArea.Width);
        }

        private void DrawInventoryScrollbar(SpriteBatch spriteBatch, Rectangle scrollArea)
        {
            // Scrollbar background (track)
            Rectangle scrollbarTrack = new Rectangle(
                scrollArea.Right - SCROLLBAR_WIDTH,
                scrollArea.Y,
                SCROLLBAR_WIDTH,
                scrollArea.Height
            );
            spriteBatch.Draw(_pixelTexture, scrollbarTrack, Color.DarkGray * 0.5f);

            // Calculate thumb size and position
            float contentRatio = scrollArea.Height / _inventoryContentHeight;
            int thumbHeight = Math.Max(20, (int)(scrollArea.Height * contentRatio));

            float scrollPercentage = _inventoryMaxScrollOffset > 0 ? _inventoryScrollOffset / _inventoryMaxScrollOffset : 0;
            int thumbY = scrollArea.Y + (int)((scrollArea.Height - thumbHeight) * scrollPercentage);

            // Scrollbar thumb
            Rectangle scrollbarThumb = new Rectangle(
                scrollArea.Right - SCROLLBAR_WIDTH,
                thumbY,
                SCROLLBAR_WIDTH,
                thumbHeight
            );
            spriteBatch.Draw(_pixelTexture, scrollbarThumb, Color.LightGray * 0.8f);
        }

        private void DrawItemHighlight(SpriteBatch spriteBatch, Rectangle bounds, Color color)
        {
            spriteBatch.Draw(_pixelTexture, bounds, color);
        }

        private void DrawEquipmentSlot(SpriteBatch spriteBatch, int x, int y, EquipmentSlot slot, string slotName, int width)
        {
            int lineHeight = 20;
            int slotHeight = lineHeight * 2;

            // Store bounds for click detection
            Rectangle slotBounds = new Rectangle(x, y, width, slotHeight);
            _equipmentSlotBounds[slot] = slotBounds;

            // Get equipped item
            var equippedItem = _player.Inventory.GetEquippedItem(slot);

            // Highlight if hovered
            bool isHovered = _hoveredItem == equippedItem && equippedItem != null;
            if (isHovered)
            {
                DrawItemHighlight(spriteBatch, slotBounds, Color.White * 0.2f);
            }

            // Slot label
            _font.DrawText(spriteBatch, slotName, new Vector2(x, y), Color.Cyan);
            y += lineHeight;

            if (equippedItem == null)
            {
                _font.DrawText(spriteBatch, "  [Empty]", new Vector2(x, y), Color.DarkGray);
            }
            else
            {
                Color itemColor = isHovered ? Color.White : equippedItem.GetRarityColor();
                _font.DrawText(spriteBatch, $"  {equippedItem.Name}", new Vector2(x, y), itemColor);
            }
        }

        private string GetItemStatsPreview(Equipment item)
        {
            // Show the most relevant stat for quick scanning
            List<string> stats = new List<string>();

            if (item.AttackDamageBonus > 0)
                stats.Add($"+{item.AttackDamageBonus} ATK");
            if (item.DefenseBonus > 0)
                stats.Add($"+{item.DefenseBonus} DEF");
            if (item.MaxHealthBonus > 0)
                stats.Add($"+{item.MaxHealthBonus} HP");
            if (item.SpeedBonus > 0)
                stats.Add($"+{item.SpeedBonus:F0} SPD");
            if (item.CritChanceBonus > 0)
                stats.Add($"+{(item.CritChanceBonus * 100):F0}% CRIT");

            if (stats.Count == 0)
                return "";

            // Return first 2 stats
            return string.Join(", ", stats.GetRange(0, System.Math.Min(2, stats.Count)));
        }

        private void DrawOptionsTab(SpriteBatch spriteBatch, Rectangle area)
        {
            int yOffset = area.Y;

            _font.DrawText(spriteBatch, "OPTIONS", new Vector2(area.X, yOffset), Color.Yellow);
            yOffset += 50;

            _font.DrawText(spriteBatch, "Controls:", new Vector2(area.X, yOffset), Color.LightGray);
            yOffset += 30;

            _font.DrawText(spriteBatch, "WASD / Arrows - Move", new Vector2(area.X + 20, yOffset), Color.White);
            yOffset += 25;
            _font.DrawText(spriteBatch, "Space - Attack", new Vector2(area.X + 20, yOffset), Color.White);
            yOffset += 25;
            _font.DrawText(spriteBatch, "Tab - Menu", new Vector2(area.X + 20, yOffset), Color.White);
            yOffset += 25;
            _font.DrawText(spriteBatch, "Esc - Quit", new Vector2(area.X + 20, yOffset), Color.White);
        }

        private void DrawStatLine(SpriteBatch spriteBatch, string label, string value, int x, int y)
        {
            _font.DrawText(spriteBatch, label + ":", new Vector2(x, y), Color.LightGray);
            _font.DrawText(spriteBatch, value, new Vector2(x + 200, y), Color.White);
        }

        private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color, int thickness)
        {
            // Top
            spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), color);
            // Bottom
            spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), color);
            // Left
            spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), color);
            // Right
            spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), color);
        }
    }
}