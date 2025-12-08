using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Candyland.Entities;

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

        // Menu dimensions
        private Rectangle _menuBounds;
        private Rectangle[] _tabButtons;
        private const int MENU_WIDTH = 600;
        private const int MENU_HEIGHT = 400;
        private const int TAB_HEIGHT = 30;

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

            _previousKeyState = Keyboard.GetState();
        }

        public void Update(GameTime gameTime)
        {
            if (!IsOpen) return;

            KeyboardState currentKeyState = Keyboard.GetState();

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

            _previousKeyState = currentKeyState;
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

            // Draw content based on current tab
            Rectangle contentArea = new Rectangle(
                _menuBounds.X + 20,
                _menuBounds.Y + TAB_HEIGHT + 20,
                _menuBounds.Width - 40,
                _menuBounds.Height - TAB_HEIGHT - 40
            );

            switch (CurrentTab)
            {
                case MenuTab.Stats:
                    DrawStatsTab(spriteBatch, contentArea);
                    break;
                case MenuTab.Inventory:
                    DrawInventoryTab(spriteBatch, contentArea);
                    break;
                case MenuTab.Options:
                    DrawOptionsTab(spriteBatch, contentArea);
                    break;
            }

            // Draw instructions at bottom
            string instructions = "TAB: Close   1/2/3 or Arrows: Switch Tabs";
            int textWidth = _font.MeasureString(instructions);
            Vector2 instructPos = new Vector2(
                _menuBounds.X + (_menuBounds.Width - textWidth) / 2,
                _menuBounds.Bottom - 25
            );
            _font.DrawText(spriteBatch, instructions, instructPos, Color.Gray);
        }

        private void DrawTabs(SpriteBatch spriteBatch)
        {
            string[] tabNames = { "STATS", "INVENTORY", "OPTIONS" };

            for (int i = 0; i < 3; i++)
            {
                bool isActive = (int)CurrentTab == i;
                Color tabColor = isActive ? Color.SlateGray : Color.DarkGray;
                Color textColor = isActive ? Color.Yellow : Color.LightGray;

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
            int lineHeight = 25;

            _font.DrawText(spriteBatch, "PLAYER STATISTICS", new Vector2(area.X, yOffset), Color.Yellow);
            yOffset += lineHeight * 2;

            // Player stats
            DrawStatLine(spriteBatch, "Level", _player.Level.ToString(), area.X, yOffset);
            yOffset += lineHeight;

            DrawStatLine(spriteBatch, "Health", $"{_player.Health} / {_player.MaxHealth}", area.X, yOffset);
            yOffset += lineHeight;

            DrawStatLine(spriteBatch, "Attack Damage", _player.AttackDamage.ToString(), area.X, yOffset);
            yOffset += lineHeight;

            DrawStatLine(spriteBatch, "Speed", _player.Speed.ToString("F0"), area.X, yOffset);
            yOffset += lineHeight;

            DrawStatLine(spriteBatch, "Coins", _player.Coins.ToString(), area.X, yOffset);
            yOffset += lineHeight;

            DrawStatLine(spriteBatch, "XP", $"{_player.XP} / {_player.XPToNextLevel}", area.X, yOffset);
            yOffset += lineHeight * 2;

            _font.DrawText(spriteBatch, "Next level in " + (_player.XPToNextLevel - _player.XP) + " XP",
                new Vector2(area.X, yOffset), Color.Cyan);
        }

        private void DrawInventoryTab(SpriteBatch spriteBatch, Rectangle area)
        {
            int yOffset = area.Y;

            _font.DrawText(spriteBatch, "INVENTORY", new Vector2(area.X, yOffset), Color.Yellow);
            yOffset += 50;

            _font.DrawText(spriteBatch, "No items yet", new Vector2(area.X + 20, yOffset), Color.Gray);
            yOffset += 30;

            _font.DrawText(spriteBatch, "Coming soon:", new Vector2(area.X + 20, yOffset), Color.LightGray);
            yOffset += 25;
            _font.DrawText(spriteBatch, "- Health Potions", new Vector2(area.X + 40, yOffset), Color.Gray);
            yOffset += 25;
            _font.DrawText(spriteBatch, "- Keys", new Vector2(area.X + 40, yOffset), Color.Gray);
            yOffset += 25;
            _font.DrawText(spriteBatch, "- Weapons", new Vector2(area.X + 40, yOffset), Color.Gray);
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