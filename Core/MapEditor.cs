using EldmeresTale.Core.UI;
using EldmeresTale.ECS.Factories;
using EldmeresTale.Entities.Definitions;
using EldmeresTale.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;

namespace EldmeresTale.Core {

	public class MapEditor {
		private TileMap _currentMap;
		private Room _currentRoom; // Store reference to the room for doors/enemies
		private readonly Camera _camera;
		private readonly BitmapFont _font;
		private readonly int _scale;

		private string _selectedTileType = "grass";
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
		private readonly AssetManager _assetManager;
		private readonly PropFactory _propFactory;

		private const int TILE_SIZE = 16;


		public MapEditor(BitmapFont font, Camera camera, int scale, AssetManager assetManager, GameServices gameServices) {
			_font = font;
			_camera = camera;
			_scale = scale;
			_assetManager = assetManager;
			_propFactory = gameServices.PropFactory;

			// Initialize prop catalog
			UpdatePropCatalog();
		}


		private void UpdatePropCatalog() {
			_propCatalog = PropFactory.GetPropsByCategory(_selectedCategory);
			if (_propCatalog.Count > 0) {
				_selectedPropId = _propCatalog[0];
			}

			_selectedPropIndex = 0;
		}

		public void SetMap(TileMap map) {
			_currentMap = map;
		}

		public void SetRoom(Room room) {
			_currentRoom = room;
			_currentMap = room?.Map;
		}

		private Point ScaleMousePosition(Point displayMousePos) {
			return new Point(
				displayMousePos.X / _scale,
				displayMousePos.Y / _scale
			);
		}

		public void Update(GameTime gameTime) {
			if (_currentMap == null) {
				return;
			}

			KeyboardState keyState = Keyboard.GetState();
			MouseState mouseState = Mouse.GetState();

			if (keyState.IsKeyDown(Keys.P) && _previousKeyState.IsKeyUp(Keys.P)) {
				_currentMode = _currentMode == EditorMode.Tiles ? EditorMode.Props : EditorMode.Tiles;
				System.Diagnostics.Debug.WriteLine($"Editor mode: {_currentMode}");
			}

			// === TILE MODE ===
			if (_currentMode == EditorMode.Tiles) {
				// Cycle through tile types with number keys
				if (keyState.IsKeyDown(Keys.D1) && _previousKeyState.IsKeyUp(Keys.D1)) {
					_selectedTileType = "grass";
				}

				if (keyState.IsKeyDown(Keys.D2) && _previousKeyState.IsKeyUp(Keys.D2)) {
					_selectedTileType = "water";
				}

				if (keyState.IsKeyDown(Keys.D3) && _previousKeyState.IsKeyUp(Keys.D3)) {
					_selectedTileType = "dirt";
				}

				if (keyState.IsKeyDown(Keys.D4) && _previousKeyState.IsKeyUp(Keys.D4)) {
					_selectedTileType = "rock";
				}

				// Paint tiles with mouse
				if (mouseState.LeftButton == ButtonState.Pressed) {
					PaintTile(ScaleMousePosition(mouseState.Position));
				}
			}

			// === PROP MODE ===
			else if (_currentMode == EditorMode.Props) {
				// Cycle through categories with Q/E
				if (keyState.IsKeyDown(Keys.Q) && _previousKeyState.IsKeyUp(Keys.Q)) {
					CyclePropCategory(-1);
				}
				if (keyState.IsKeyDown(Keys.E) && _previousKeyState.IsKeyUp(Keys.E)) {
					CyclePropCategory(1);
				}

				// Cycle through props with mouse wheel or arrow keys
				int scrollDelta = mouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
				if (scrollDelta > 0 || (keyState.IsKeyDown(Keys.Up) && _previousKeyState.IsKeyUp(Keys.Up))) {
					CycleProp(-1);
				} else if (scrollDelta < 0 || (keyState.IsKeyDown(Keys.Down) && _previousKeyState.IsKeyUp(Keys.Down))) {
					CycleProp(1);
				}

				// Place prop with left click
				if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released) {
					PlaceProp(ScaleMousePosition(mouseState.Position));
				}

				// Delete prop with right click
				if (mouseState.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released) {
					DeleteProp(ScaleMousePosition(mouseState.Position));
				}
			}

			// Save map
			if (keyState.IsKeyDown(Keys.F5) && _previousKeyState.IsKeyUp(Keys.F5)) {
				SaveCurrentMap();
			}

			_previousKeyState = keyState;
			_previousMouseState = mouseState;
		}

		private void CyclePropCategory(int direction) {
			List<string> categories = PropFactory.GetCategories();
			int currentIndex = categories.IndexOf(_selectedCategory);
			currentIndex = (currentIndex + direction + categories.Count) % categories.Count;
			_selectedCategory = categories[currentIndex];
			UpdatePropCatalog();
			System.Diagnostics.Debug.WriteLine($"Category: {_selectedCategory}");
		}

		private void CycleProp(int direction) {
			if (_propCatalog.Count == 0) {
				return;
			}

			_selectedPropIndex = (_selectedPropIndex + direction + _propCatalog.Count) % _propCatalog.Count;
			_selectedPropId = _propCatalog[_selectedPropIndex];
			System.Diagnostics.Debug.WriteLine($"Selected: {_selectedPropId}");
		}

		private void PlaceProp(Point mousePosition) {
			if (_currentRoom == null) {
				return;
			}

			// Convert screen position to world position
			Vector2 screenPos = new Vector2(mousePosition.X, mousePosition.Y);
			Vector2 worldPos = _camera.ScreenToWorld(screenPos);

			// Snap to grid (optional)
			worldPos.X = (int)(worldPos.X / TILE_SIZE) * TILE_SIZE;
			worldPos.Y = (int)(worldPos.Y / TILE_SIZE) * TILE_SIZE;

			string spritePath = $"Assets/Sprites/Props/{_selectedPropId}.png";
			Texture2D sprite = _assetManager.LoadTexture(spritePath);

			// Create prop
			_currentRoom.Props.Add(_propFactory.Create(_selectedPropId, worldPos));
			System.Diagnostics.Debug.WriteLine($"Placed {_selectedPropId} at {worldPos}");
		}

		private void DeleteProp(Point mousePosition) {
			if (_currentRoom == null) {
				return;
			}

			// Convert screen position to world position
			Vector2 screenPos = new Vector2(mousePosition.X, mousePosition.Y);
			Vector2 worldPos = _camera.ScreenToWorld(screenPos);

			// Find prop at position
			for (int i = _currentRoom.Props.Count - 1; i >= 0; i--) {
				//	Prop prop = _currentRoom.Props[i];
				//	if (prop.Bounds.Contains((int)worldPos.X, (int)worldPos.Y)) {
				//		_currentRoom.Props.RemoveAt(i);
				//		System.Diagnostics.Debug.WriteLine($"Deleted prop at {prop.Position}");
				//		break;
				//	}
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
			if (tileX >= 0 && tileX < _currentMap.Width && tileY >= 0 && tileY < _currentMap.Height) {
				_currentMap.SetTile(tileX, tileY, _selectedTileType);
			}
		}

		private void SaveCurrentMap() {
			List<string> tileIndex = TileRegistry.Instance.GetTileIds().ToList();
			if (_currentMap == null) {
				return;
			}

			// Create MapData from current TileMap
			MapData mapData = new MapData(_currentMap.Width, _currentMap.Height, _currentMap.TileSize);

			// Save tiles
			for (int x = 0; x < _currentMap.Width; x++) {
				for (int y = 0; y < _currentMap.Height; y++) {
					mapData.Tiles[x, y] = tileIndex.IndexOf(_currentMap.GetTile(x, y));
				}
			}

			// Save room data if we have a room reference
			if (_currentRoom != null) {
				// Save player spawn
				mapData.PlayerSpawnX = _currentRoom.PlayerSpawnPosition.X;
				mapData.PlayerSpawnY = _currentRoom.PlayerSpawnPosition.Y;

				// Save doors
				foreach (Door door in _currentRoom.Doors) {
					DoorData doorData = new DoorData {
						Direction = (int)door.Direction,
						TargetRoomId = door.TargetRoomId,
						TargetDirection = (int)door.TargetDoorDirection
					};
					mapData.Doors.Add(doorData);
				}

				//foreach (Prop prop in _currentRoom.Props) {
				//	PropDefinition propDef = PropFactory.Catalog.Values.FirstOrDefault(d =>
				//		d.Type == prop.Type && d.Width == prop.Width && d.Height == prop.Height);

				//	PropData propData = new PropData {
				//		PropId = propDef?.Id ?? "unknown",
				//		X = prop.Position.X,
				//		Y = prop.Position.Y
				//	};
				//	mapData.Props.Add(propData);
				//}
			}

			// Save to file
			string filename = _currentRoom != null
				? $"{_currentRoom.Id}.json"
				: $"map_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";

			string filepath = System.IO.Path.Combine("Assets", "Maps", filename);
			mapData.SaveToFile(filepath);

			System.Diagnostics.Debug.WriteLine($"Map saved to: {filepath}");
		}

		public void Draw(SpriteBatch spriteBatch) {
			if (_currentMode == EditorMode.Tiles) {
				const string instructions = "MAP EDITOR [TILES] - 1:Grass 2:Water 3:Stone 4:Tree | Click:Paint | P:Props | F5:Save | M:Exit";
				_font.DrawText(spriteBatch, instructions, new Vector2(10, 10), Color.Yellow);

				string selectedTile = $"Selected: {_selectedTileType}";
				_font.DrawText(spriteBatch, selectedTile, new Vector2(10, 30), Color.White);
			} else if (_currentMode == EditorMode.Props) {
				const string instructions = "MAP EDITOR [PROPS] - Scroll/Arrows:Select | Q/E:Category | Click:Place | Right-Click:Delete | P:Tiles | F5:Save | M:Exit";
				_font.DrawText(spriteBatch, instructions, new Vector2(10, 10), Color.Yellow);

				string category = $"Category: {_selectedCategory} ({_selectedPropIndex + 1}/{_propCatalog.Count})";
				_font.DrawText(spriteBatch, category, new Vector2(10, 30), Color.White);

				//PropDefinition definition = PropFactory.Catalog[_selectedPropId];
				//string selected = $"Selected: {definition.DisplayName} ({definition.Type})";
				//_font.DrawText(spriteBatch, selected, new Vector2(10, 50), Color.Cyan);
			}

			if (_currentRoom != null) {
				string roomInfo = $"Editing: {_currentRoom.Id}";
				_font.DrawText(spriteBatch, roomInfo, new Vector2(10, 320), Color.Cyan);
			}
		}

		public Rectangle GetCursorTileRect() {
			if (_currentMode != EditorMode.Tiles) {
				return Rectangle.Empty;
			}

			Point mousePos = ScaleMousePosition(Mouse.GetState().Position);
			Vector2 screenPos = new Vector2(mousePos.X, mousePos.Y);

			int tileX = (int)(screenPos.X / TILE_SIZE);
			int tileY = (int)(screenPos.Y / TILE_SIZE);

			return new Rectangle(tileX * TILE_SIZE, tileY * TILE_SIZE, TILE_SIZE, TILE_SIZE);
		}


		public Rectangle GetCursorPropRect() {
			if (_currentMode != EditorMode.Props) {
				return Rectangle.Empty;
			}

			Point mousePos = ScaleMousePosition(Mouse.GetState().Position);
			Vector2 screenPos = new Vector2(mousePos.X, mousePos.Y);

			int tileX = (int)(screenPos.X / TILE_SIZE);
			int tileY = (int)(screenPos.Y / TILE_SIZE);

			PropDefinition definition = PropFactory.Catalog[_selectedPropId];
			return new Rectangle(tileX * TILE_SIZE, tileY * TILE_SIZE, definition.Width, definition.Height);
		}

		public Color GetSelectedTileColor() {
			return GetTileColor(_selectedTileType);
		}


		public Color GetSelectedPropColor() {
			if (_currentMode != EditorMode.Props) {
				return Color.White;
			}
			PropDefinition definition = PropFactory.Catalog[_selectedPropId];
			return definition.DefaultColor * 0.7f;
		}

		private static Color GetTileColor(string type) {
			return TileRegistry.Instance.GetTile(type).MainColor;
		}
	}
}