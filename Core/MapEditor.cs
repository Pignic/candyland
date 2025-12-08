using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Candyland.World;

namespace Candyland.Core
{
    public class MapEditor
    {
        public bool IsActive { get; set; }

        private TileMap _currentMap;
        private Camera _camera;
        private BitmapFont _font;
        private Texture2D _pixelTexture;

        private TileType _selectedTileType = TileType.Grass;
        private KeyboardState _previousKeyState;
        private MouseState _previousMouseState;

        private const int TILE_SIZE = 32;

        public MapEditor(BitmapFont font, Texture2D pixelTexture, Camera camera)
        {
            _font = font;
            _pixelTexture = pixelTexture;
            _camera = camera;
            IsActive = false;
        }

        public void SetMap(TileMap map)
        {
            _currentMap = map;
        }

        public void Update(GameTime gameTime)
        {
            if (!IsActive || _currentMap == null) return;

            var keyState = Keyboard.GetState();
            var mouseState = Mouse.GetState();

            // Cycle through tile types with number keys
            if (keyState.IsKeyDown(Keys.D1) && _previousKeyState.IsKeyUp(Keys.D1))
                _selectedTileType = TileType.Grass;
            if (keyState.IsKeyDown(Keys.D2) && _previousKeyState.IsKeyUp(Keys.D2))
                _selectedTileType = TileType.Water;
            if (keyState.IsKeyDown(Keys.D3) && _previousKeyState.IsKeyUp(Keys.D3))
                _selectedTileType = TileType.Stone;
            if (keyState.IsKeyDown(Keys.D4) && _previousKeyState.IsKeyUp(Keys.D4))
                _selectedTileType = TileType.Tree;

            // Paint tiles with mouse
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                PaintTile(mouseState.Position);
            }

            // Save map
            if (keyState.IsKeyDown(Keys.F5) && _previousKeyState.IsKeyUp(Keys.F5))
            {
                SaveCurrentMap();
            }

            _previousKeyState = keyState;
            _previousMouseState = mouseState;
        }

        private void PaintTile(Point mousePosition)
        {
            // Convert screen position to world position
            Vector2 screenPos = new Vector2(mousePosition.X, mousePosition.Y);
            Vector2 worldPos = _camera.ScreenToWorld(screenPos);

            // Convert world position to tile coordinates
            int tileX = (int)(worldPos.X / TILE_SIZE);
            int tileY = (int)(worldPos.Y / TILE_SIZE);

            // Set the tile
            if (tileX >= 0 && tileX < _currentMap.Width && tileY >= 0 && tileY < _currentMap.Height)
            {
                _currentMap.SetTile(tileX, tileY, new Tile(_selectedTileType));
            }
        }

        private void SaveCurrentMap()
        {
            if (_currentMap == null) return;

            // Create MapData from current TileMap
            var mapData = new MapData(_currentMap.Width, _currentMap.Height, _currentMap.TileSize);

            for (int x = 0; x < _currentMap.Width; x++)
            {
                for (int y = 0; y < _currentMap.Height; y++)
                {
                    var tile = _currentMap.GetTile(x, y);
                    if (tile != null)
                    {
                        mapData.Tiles[x, y] = tile.Type;
                    }
                }
            }

            // Save to file
            string filename = $"map_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
            mapData.SaveToFile(filename);

            System.Diagnostics.Debug.WriteLine($"Map saved to: {filename}");
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsActive) return;

            // Draw UI (no camera transform)
            string instructions = "MAP EDITOR - 1:Grass 2:Water 3:Stone 4:Tree | Click:Paint | F5:Save | E:Exit";
            _font.DrawText(spriteBatch, instructions, new Vector2(10, 10), Color.Yellow);

            string selectedTile = $"Selected: {_selectedTileType}";
            _font.DrawText(spriteBatch, selectedTile, new Vector2(10, 30), Color.White);

            // Draw cursor tile preview (with camera transform)
            var mouseState = Mouse.GetState();
            Vector2 screenPos = new Vector2(mouseState.X, mouseState.Y);
            Vector2 worldPos = _camera.ScreenToWorld(screenPos);

            int tileX = (int)(worldPos.X / TILE_SIZE);
            int tileY = (int)(worldPos.Y / TILE_SIZE);

            Rectangle tileRect = new Rectangle(tileX * TILE_SIZE, tileY * TILE_SIZE, TILE_SIZE, TILE_SIZE);

            // Get color for selected tile type
            Color previewColor = GetTileColor(_selectedTileType);

            // This needs to be drawn with camera transform, so we'll do it in the main draw
        }

        public Rectangle GetCursorTileRect()
        {
            if (!IsActive) return Rectangle.Empty;

            var mouseState = Mouse.GetState();
            Vector2 screenPos = new Vector2(mouseState.X, mouseState.Y);
            Vector2 worldPos = _camera.ScreenToWorld(screenPos);

            int tileX = (int)(worldPos.X / TILE_SIZE);
            int tileY = (int)(worldPos.Y / TILE_SIZE);

            return new Rectangle(tileX * TILE_SIZE, tileY * TILE_SIZE, TILE_SIZE, TILE_SIZE);
        }

        public Color GetSelectedTileColor()
        {
            return GetTileColor(_selectedTileType);
        }

        private Color GetTileColor(TileType type)
        {
            return type switch
            {
                TileType.Grass => new Color(34, 139, 34),
                TileType.Water => new Color(30, 144, 255),
                TileType.Stone => new Color(128, 128, 128),
                TileType.Tree => new Color(0, 100, 0),
                _ => Color.White
            };
        }
    }
}