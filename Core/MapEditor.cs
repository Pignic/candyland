using Candyland.Core.UI;
using Candyland.Entities;
using Candyland.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;

namespace Candyland.Core {
	public class MapEditor {
		private TileMap _currentMap;
		private Room _currentRoom; // Store reference to the room for doors/enemies
		private Camera _camera;
		private BitmapFont _font;
		private int _scale;

		private TileType _selectedTileType = TileType.Grass;
		private KeyboardState _previousKeyState;
		private MouseState _previousMouseState;
		private enum EditorMode {
			Tiles,
			Props
		}

		private EditorMode _currentMode = EditorMode.Tiles;
		private string _selectedPropId = "crate";
		private List<string> _propCatalog;
		private int _selectedPropIndex = 0;
		private string _selectedCategory = "Breakable";
		private GraphicsDevice _graphicsDevice;
		private AssetManager _assetManager;

		private const int TILE_SIZE = 16;

		public MapEditor(BitmapFont font, Camera camera, int scale, AssetManager assetManager, GraphicsDevice graphicsDevice) {
			_font = font;
			_camera = camera;
			_graphicsDevice = graphicsDevice;
			_scale = scale;
			_assetManager = assetManager;

			// Initialize prop catalog
			UpdatePropCatalog();
		}


		private void UpdatePropCatalog() {
			_propCatalog = PropFactory.GetPropsByCategory(_selectedCategory);
			if(_propCatalog.Count > 0)
				_selectedPropId = _propCatalog[0];
			_selectedPropIndex = 0;
		}

		public void SetMap(TileMap map) {
			_currentMap = map;
		}

		public void SetRoom(Room room) {
			_currentRoom = room;
			_currentMap = room?.map;
		}

		private Point ScaleMousePosition(Point displayMousePos) {
			return new Point(
				displayMousePos.X / _scale,
				displayMousePos.Y / _scale
			);
		}

		public void Update(GameTime gameTime) {
			if(_currentMap == null) return;

			var keyState = Keyboard.GetState();
			var mouseState = Mouse.GetState();

			if(keyState.IsKeyDown(Keys.P) && _previousKeyState.IsKeyUp(Keys.P)) {
				_currentMode = _currentMode == EditorMode.Tiles ? EditorMode.Props : EditorMode.Tiles;
				System.Diagnostics.Debug.WriteLine($"Editor mode: {_currentMode}");
			}

			// === TILE MODE ===
			if(_currentMode == EditorMode.Tiles) {
				// Cycle through tile types with number keys
				if(keyState.IsKeyDown(Keys.D1) && _previousKeyState.IsKeyUp(Keys.D1))
					_selectedTileType = TileType.Grass;
				if(keyState.IsKeyDown(Keys.D2) && _previousKeyState.IsKeyUp(Keys.D2))
					_selectedTileType = TileType.Water;
				if(keyState.IsKeyDown(Keys.D3) && _previousKeyState.IsKeyUp(Keys.D3))
					_selectedTileType = TileType.Stone;
				if(keyState.IsKeyDown(Keys.D4) && _previousKeyState.IsKeyUp(Keys.D4))
					_selectedTileType = TileType.Tree;

				// Paint tiles with mouse
				if(mouseState.LeftButton == ButtonState.Pressed) {
					PaintTile(ScaleMousePosition(mouseState.Position));
				}
			}

			// === PROP MODE ===
			else if(_currentMode == EditorMode.Props) {
				// Cycle through categories with Q/E
				if(keyState.IsKeyDown(Keys.Q) && _previousKeyState.IsKeyUp(Keys.Q)) {
					CyclePropCategory(-1);
				}
				if(keyState.IsKeyDown(Keys.E) && _previousKeyState.IsKeyUp(Keys.E)) {
					CyclePropCategory(1);
				}

				// Cycle through props with mouse wheel or arrow keys
				int scrollDelta = mouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
				if(scrollDelta > 0 || (keyState.IsKeyDown(Keys.Up) && _previousKeyState.IsKeyUp(Keys.Up))) {
					CycleProp(-1);
				} else if(scrollDelta < 0 || (keyState.IsKeyDown(Keys.Down) && _previousKeyState.IsKeyUp(Keys.Down))) {
					CycleProp(1);
				}

				// Place prop with left click
				if(mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released) {
					PlaceProp(ScaleMousePosition(mouseState.Position));
				}

				// Delete prop with right click
				if(mouseState.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released) {
					DeleteProp(ScaleMousePosition(mouseState.Position));
				}
			}

			// Save map
			if(keyState.IsKeyDown(Keys.F5) && _previousKeyState.IsKeyUp(Keys.F5)) {
				SaveCurrentMap();
			}

			_previousKeyState = keyState;
			_previousMouseState = mouseState;
		}

		private void CyclePropCategory(int direction) {
			var categories = PropFactory.GetCategories();
			int currentIndex = categories.IndexOf(_selectedCategory);
			currentIndex = (currentIndex + direction + categories.Count) % categories.Count;
			_selectedCategory = categories[currentIndex];
			UpdatePropCatalog();
			System.Diagnostics.Debug.WriteLine($"Category: {_selectedCategory}");
		}

		private void CycleProp(int direction) {
			if(_propCatalog.Count == 0) return;

			_selectedPropIndex = (_selectedPropIndex + direction + _propCatalog.Count) % _propCatalog.Count;
			_selectedPropId = _propCatalog[_selectedPropIndex];
			System.Diagnostics.Debug.WriteLine($"Selected: {_selectedPropId}");
		}

		private void PlaceProp(Point mousePosition) {
			if(_currentRoom == null) return;

			// Convert screen position to world position
			Vector2 screenPos = new Vector2(mousePosition.X, mousePosition.Y);
			Vector2 worldPos = _camera.ScreenToWorld(screenPos);

			// Snap to grid (optional)
			worldPos.X = (int)(worldPos.X / TILE_SIZE) * TILE_SIZE;
			worldPos.Y = (int)(worldPos.Y / TILE_SIZE) * TILE_SIZE;

			string spritePath = $"Assets/Sprites/Props/{_selectedPropId}.png";
			var sprite = _assetManager.LoadTexture(spritePath);

			// Create prop
			var prop = PropFactory.Create(_selectedPropId, sprite, worldPos, _graphicsDevice);
			if(prop != null) {
				_currentRoom.props.Add(prop);
				System.Diagnostics.Debug.WriteLine($"Placed {_selectedPropId} at {worldPos}");
			}
		}

		private void DeleteProp(Point mousePosition) {
			if(_currentRoom == null) return;

			// Convert screen position to world position
			Vector2 screenPos = new Vector2(mousePosition.X, mousePosition.Y);
			Vector2 worldPos = _camera.ScreenToWorld(screenPos);

			// Find prop at position
			for(int i = _currentRoom.props.Count - 1; i >= 0; i--) {
				var prop = _currentRoom.props[i];
				if(prop.Bounds.Contains((int)worldPos.X, (int)worldPos.Y)) {
					_currentRoom.props.RemoveAt(i);
					System.Diagnostics.Debug.WriteLine($"Deleted prop at {prop.Position}");
					break;
				}
			}
		}

		private void PaintTile(Point mousePosition) {
			// Convert screen position to world position
			Vector2 screenPos = new Vector2(mousePosition.X, mousePosition.Y);
			Vector2 worldPos = _camera.ScreenToWorld(screenPos);

			// Convert world position to tile coordinates
			int tileX = (int)(worldPos.X / TILE_SIZE);
			int tileY = (int)(worldPos.Y / TILE_SIZE);

			// Set the tile
			if(tileX >= 0 && tileX < _currentMap.width && tileY >= 0 && tileY < _currentMap.height) {
				_currentMap.setTile(tileX, tileY, _selectedTileType);
			}
		}

		private void SaveCurrentMap() {
			if(_currentMap == null) return;

			// Create MapData from current TileMap
			var mapData = new MapData(_currentMap.width, _currentMap.height, _currentMap.tileSize);

			// Save tiles
			for(int x = 0; x < _currentMap.width; x++) {
				for(int y = 0; y < _currentMap.height; y++) {
					var tile = _currentMap.getTile(x, y);
					if(tile.HasValue) {
						mapData.tiles[x, y] = tile.Value;
					}
				}
			}

			// Save room data if we have a room reference
			if(_currentRoom != null) {
				// Save player spawn
				mapData.playerSpawnX = _currentRoom.playerSpawnPosition.X;
				mapData.playerSpawnY = _currentRoom.playerSpawnPosition.Y;

				// Save doors
				foreach(var door in _currentRoom.doors) {
					var doorData = new DoorData {
						direction = (int)door.direction,
						targetRoomId = door.targetRoomId,
						targetDirection = (int)door.targetDoorDirection
					};
					mapData.doors.Add(doorData);
				}

				// Save enemies
				foreach(var enemy in _currentRoom.enemies) {
					var enemyData = new EnemyData {
						behavior = (int)enemy.Behavior,
						x = enemy.Position.X,
						y = enemy.Position.Y,
						speed = enemy.Speed,
						detectionRange = enemy.DetectionRange,
						patrolStartX = 0, // Will need to be set if patrol behavior
						patrolStartY = 0,
						patrolEndX = 0,
						patrolEndY = 0
					};
					mapData.enemies.Add(enemyData);
				}

				foreach(var prop in _currentRoom.props) {
					var propDef = PropFactory.Catalog.Values.FirstOrDefault(d =>
						d.Type == prop.type && d.Width == prop.Width && d.Height == prop.Height);

					var propData = new PropData {
						propId = propDef?.Id ?? "unknown",
						x = prop.Position.X,
						y = prop.Position.Y
					};
					mapData.props.Add(propData);
				}
			}

			// Save to file
			string filename = _currentRoom != null
				? $"{_currentRoom.id}.json"
				: $"map_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";

			string filepath = System.IO.Path.Combine("Assets", "Maps", filename);
			mapData.SaveToFile(filepath);

			System.Diagnostics.Debug.WriteLine($"Map saved to: {filepath}");
		}

		public void Draw(SpriteBatch spriteBatch) {
			if(_currentMode == EditorMode.Tiles) {
				string instructions = "MAP EDITOR [TILES] - 1:Grass 2:Water 3:Stone 4:Tree | Click:Paint | P:Props | F5:Save | M:Exit";
				_font.drawText(spriteBatch, instructions, new Vector2(10, 10), Color.Yellow);

				string selectedTile = $"Selected: {_selectedTileType}";
				_font.drawText(spriteBatch, selectedTile, new Vector2(10, 30), Color.White);
			} else if(_currentMode == EditorMode.Props) {
				string instructions = "MAP EDITOR [PROPS] - Scroll/Arrows:Select | Q/E:Category | Click:Place | Right-Click:Delete | P:Tiles | F5:Save | M:Exit";
				_font.drawText(spriteBatch, instructions, new Vector2(10, 10), Color.Yellow);

				string category = $"Category: {_selectedCategory} ({_selectedPropIndex + 1}/{_propCatalog.Count})";
				_font.drawText(spriteBatch, category, new Vector2(10, 30), Color.White);

				var definition = PropFactory.Catalog[_selectedPropId];
				string selected = $"Selected: {definition.DisplayName} ({definition.Type})";
				_font.drawText(spriteBatch, selected, new Vector2(10, 50), Color.Cyan);
			}

			if(_currentRoom != null) {
				string roomInfo = $"Editing: {_currentRoom.id}";
				_font.drawText(spriteBatch, roomInfo, new Vector2(10, 320), Color.Cyan);
			}
		}

		public Rectangle GetCursorTileRect() {
			if(_currentMode != EditorMode.Tiles) return Rectangle.Empty;

			Point mousePos = ScaleMousePosition(Mouse.GetState().Position);
			Vector2 screenPos = new Vector2(mousePos.X, mousePos.Y);

			int tileX = (int)(screenPos.X / TILE_SIZE);
			int tileY = (int)(screenPos.Y / TILE_SIZE);

			return new Rectangle(tileX * TILE_SIZE, tileY * TILE_SIZE, TILE_SIZE, TILE_SIZE);
		}


		public Rectangle GetCursorPropRect() {
			if(_currentMode != EditorMode.Props) return Rectangle.Empty;

			Point mousePos = ScaleMousePosition(Mouse.GetState().Position);
			Vector2 screenPos = new Vector2(mousePos.X, mousePos.Y);

			int tileX = (int)(screenPos.X / TILE_SIZE);
			int tileY = (int)(screenPos.Y / TILE_SIZE);

			var definition = PropFactory.Catalog[_selectedPropId];
			return new Rectangle(tileX * TILE_SIZE, tileY * TILE_SIZE, definition.Width, definition.Height);
		}

		public Color GetSelectedTileColor() {
			return GetTileColor(_selectedTileType);
		}


		public Color GetSelectedPropColor() {
			if(_currentMode != EditorMode.Props) return Color.White;
			var definition = PropFactory.Catalog[_selectedPropId];
			return definition.DefaultColor * 0.7f;
		}

		private Color GetTileColor(TileType type) {
			return type switch {
				TileType.Grass => new Color(34, 139, 34),
				TileType.Water => new Color(30, 144, 255),
				TileType.Stone => new Color(128, 128, 128),
				TileType.Tree => new Color(0, 100, 0),
				_ => Color.White
			};
		}
	}
}