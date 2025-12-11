using Candyland.Core;
using Candyland.Core.UI;
using Candyland.Dialog;
using Candyland.Entities;
using Candyland.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;

namespace Candyland {
	public class Game1 : Game {
		private GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;

		private DialogManager _dialogManager;
		private DialogUI _dialogUI;

		// Player, Camera, and World
		private Player _player;
		private Camera _camera;
		private RoomManager _roomManager;

		// Current room entities (references to current room's lists)
		private System.Collections.Generic.List<Enemy> _currentEnemies;
		private System.Collections.Generic.List<Pickup> _currentPickups;

		// Damage numbers
		private System.Collections.Generic.List<DamageNumber> _damageNumbers;
		private System.Collections.Generic.List<LevelUpEffect> _levelUpEffects;

		// Textures
		private Texture2D _healthPotionTexture;
		private Texture2D _coinTexture;
		private Texture2D _doorTexture;

		// UI
		private BitmapFont _font;
		private GameMenu _gameMenu;
		private MapEditor _mapEditor;
		private KeyboardState _previousKeyState;
		private UIBar _healthBar;
		private UIBar _xpBar;
		private UICounter _coinCounter;
		private UICounter _lvlCounter;

		// === RESOLUTION CONSTANTS ===
		private const int NATIVE_WIDTH = 640;
		private const int NATIVE_HEIGHT = 360;
		private const int SCALE = 3;  // 3x for 1920x1080, 2x for 1280x720
		private const int DISPLAY_WIDTH = NATIVE_WIDTH * SCALE;
		private const int DISPLAY_HEIGHT = NATIVE_HEIGHT * SCALE;

		// Tile settings
		private const int TILE_SIZE = 16;  // Native tile size
		private const int MAP_WIDTH = 50;  // tiles
		private const int MAP_HEIGHT = 40; // tiles

		private RenderTarget2D _gameRenderTarget;

		public Game1() {
			_graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			IsMouseVisible = true;

			// Set window size
			_graphics.PreferredBackBufferWidth = DISPLAY_WIDTH;
			_graphics.PreferredBackBufferHeight = DISPLAY_HEIGHT;
			_graphics.ToggleFullScreen();
		}

		protected override void Initialize() {
			base.Initialize();
		}

		protected override void LoadContent() {
			_spriteBatch = new SpriteBatch(GraphicsDevice);

			// === CREATE NATIVE RESOLUTION RENDER TARGET ===
			_gameRenderTarget = new RenderTarget2D(
				GraphicsDevice,
				NATIVE_WIDTH,   // 640
				NATIVE_HEIGHT,  // 360
				false,
				GraphicsDevice.PresentationParameters.BackBufferFormat,
				DepthFormat.None,
				0,
				RenderTargetUsage.DiscardContents
			);

			// Initialize font
			_font = new BitmapFont(GraphicsDevice);

			// Initialize room manager
			_roomManager = new RoomManager();
			_damageNumbers = new System.Collections.Generic.List<DamageNumber>();
			_levelUpEffects = new System.Collections.Generic.List<LevelUpEffect>();

			// Create pickup and door textures
			_healthPotionTexture = Graphics.CreateColoredTexture(GraphicsDevice, 16, 16, Color.LimeGreen);
			_coinTexture = Graphics.CreateColoredTexture(GraphicsDevice, 6, 6, Color.Gold);
			_doorTexture = Graphics.CreateColoredTexture(GraphicsDevice, 1, 1, Color.White);

			// Load player texture/spritesheet
			Texture2D playerTexture = LoadTextureFromFile("Assets/Sprites/player.png");

			// Create placeholder player first (will be repositioned when rooms are created)
			Vector2 tempPosition = new Vector2(NATIVE_WIDTH / 2, NATIVE_HEIGHT / 2);

			// Check if we're using an animated sprite sheet or static sprite
			if(playerTexture != null) {
				int frameCount = 4;
				int frameWidth = playerTexture.Width / frameCount;
				int frameHeight = playerTexture.Height / frameCount;
				float frameTime = 0.1f;

				_player = new Player(playerTexture, tempPosition, frameCount, frameWidth, frameHeight, frameTime, width: TILE_SIZE, height: TILE_SIZE);
			} else {
				playerTexture = Graphics.CreateColoredTexture(GraphicsDevice, TILE_SIZE, TILE_SIZE, Color.Yellow);
				_player = new Player(playerTexture, tempPosition, width: TILE_SIZE, height: TILE_SIZE);
			}

			// Initialize attack effect
			_player.InitializeAttackEffect(GraphicsDevice);

			// Create rooms (now that player exists)
			CreateRooms();
			//CreateDualGridRooms();

			// Set starting room
			_roomManager.SetCurrentRoom("room1");
			_currentEnemies = _roomManager.CurrentRoom.Enemies;
			_currentPickups = _roomManager.CurrentRoom.Pickups;

			// Position player at spawn
			_player.Position = _roomManager.CurrentRoom.PlayerSpawnPosition;

			// Create camera
			_camera = new Camera(NATIVE_WIDTH, NATIVE_HEIGHT);

			// Set world bounds to match current room map size
			//_camera.WorldBounds = new Rectangle(0, 0, _roomManager.CurrentRoom.Map.PixelWidth, _roomManager.CurrentRoom.Map.PixelHeight);

			// Set world bounds for native resolution
			_camera.WorldBounds = new Rectangle(
				0,
				0,
				_roomManager.CurrentRoom.Map.PixelWidth,  // 40 * 16 = 640
				_roomManager.CurrentRoom.Map.PixelHeight  // 40 * 16 = 640
			);


			// Create game menu
			var pixelTexture = Graphics.CreateColoredTexture(GraphicsDevice, 1, 1, Color.White);
			_gameMenu = new GameMenu(_font, pixelTexture, _player, NATIVE_WIDTH, NATIVE_HEIGHT, SCALE);

			// Create map editor
			_mapEditor = new MapEditor(_font, pixelTexture, _camera, SCALE);
			_mapEditor.SetRoom(_roomManager.CurrentRoom);

			_previousKeyState = Keyboard.GetState();

			_player.Inventory.AddItem(EquipmentFactory.CreateIronSword());
			_player.Inventory.AddItem(EquipmentFactory.CreateLeatherArmor());
			_player.Inventory.AddItem(EquipmentFactory.CreateSpeedBoots());
			_player.Inventory.AddItem(EquipmentFactory.CreateVampireBlade());
			_player.Inventory.AddItem(EquipmentFactory.CreateCriticalRing());
			_player.Inventory.AddItem(EquipmentFactory.CreateRegenerationAmulet());

			var questGiverSprite = Graphics.CreateColoredTexture(GraphicsDevice, 28, 28, Color.Pink);
			var questGiver = new NPC(
				questGiverSprite,
				new Vector2(400, 300),    // Position
				"quest_giver_forest",     // DialogId
				width: 24, height: 24
			);
			_roomManager.CurrentRoom.NPCs.Add(questGiver);

			_healthBar = new UIBar(GraphicsDevice, _font, 10, 10, 200, 2, Color.DarkRed, Color.Red, Color.White, Color.White,
				() => { return $"{_player.Health} / {_player.Stats.MaxHealth}"; },
				() => { return _player.Health / (float)_player.Stats.MaxHealth; }
			);
			_xpBar = new UIBar(GraphicsDevice, _font, 10, 30, 200, 2, Color.DarkGray, Color.Gray, Color.White, Color.White,
				() => { return $"{_player.XP} / {_player.XPToNextLevel}"; },
				() => { return _player.XP / (float)_player.XPToNextLevel; }
			);
			_coinCounter = new UICounter(_font, _healthBar.width + _healthBar.x + 4, _healthBar.y, 2, Color.Gold, "$",
				() => { return $"x {_player.Coins}"; }
			);
			_lvlCounter = new UICounter(_font, _xpBar.width + _xpBar.x + 4, _xpBar.y, 2, Color.White, "LV",
				() => { return $"{_player.Level}"; }
			);

			LoadContent_DialogSystem();
		}

		private void LoadContent_DialogSystem() {
			// 1. Initialize Dialog Manager
			_dialogManager = new DialogManager(_player);

			// 2. Load dialog data
			_dialogManager.LoadDialogTrees("Assets/Dialogs/Trees/example_dialogs.json");
			_dialogManager.LoadNPCDefinitions("Assets/Dialogs/NPCs/npcs.json");

			// 3. Load localization (language files)
			_dialogManager.Localization.LoadLanguage("en", "Assets/Dialogs/Localization/en.json");

			// 4. Create Dialog UI
			_dialogUI = new DialogUI(
				_dialogManager,
				_font,
				Graphics.CreateColoredTexture(GraphicsDevice, 1, 1, Color.White), // pixel texture
				NATIVE_WIDTH,
				NATIVE_HEIGHT,
				SCALE
			);

			// 5. (Optional) Load portrait images
			// _dialogUI.LoadPortrait("quest_giver", questGiverTexture);
			// _dialogUI.LoadPortrait("merchant", merchantTexture);
			// _dialogUI.LoadPortrait("elder", elderTexture);
		}

		protected override void Update(GameTime gameTime) {
			KeyboardState currentKeyState = Keyboard.GetState();

			if(GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
				Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			// Toggle menu with Tab
			if(currentKeyState.IsKeyDown(Keys.Tab) && _previousKeyState.IsKeyUp(Keys.Tab)) {
				_gameMenu.isOpen = !_gameMenu.isOpen;
			}

			// Toggle map editor with E
			if(currentKeyState.IsKeyDown(Keys.M) && _previousKeyState.IsKeyUp(Keys.M)) {
				_mapEditor.IsActive = !_mapEditor.IsActive;
				if(_mapEditor.IsActive) {
					_mapEditor.SetRoom(_roomManager.CurrentRoom);
				}
			}
			if(_dialogUI.IsActive) {
				_dialogUI.Update(gameTime);
				_previousKeyState = currentKeyState;
				return; // Don't update game when dialog is active
			}

			if(currentKeyState.IsKeyDown(Keys.E) && _previousKeyState.IsKeyUp(Keys.E)) {
				foreach(var npc in _roomManager.CurrentRoom.NPCs) {
					float distance = Vector2.Distance(_player.Position, npc.Position);
					if(distance < 50f) {
						_dialogManager.StartDialog(npc.DialogId);
						break;
					}
				}
			}

			// Update map editor if active
			if(_mapEditor.IsActive) {
				_mapEditor.Update(gameTime);
				_camera.Update(); // Still update camera for panning
				_previousKeyState = currentKeyState;
				return; // Don't update game when editor is active
			}

			// Update menu if open
			if(_gameMenu.isOpen) {
				_gameMenu.Update(gameTime);
				_previousKeyState = currentKeyState;
				return; // Don't update game when menu is open
			}

			var currentMap = _roomManager.CurrentRoom.Map;

			// Update player with collision detection
			_player.Update(gameTime);

			// Clamp player to world bounds
			_player.Position = new Vector2(
				MathHelper.Clamp(_player.Position.X, 0, currentMap.PixelWidth - _player.Width),
				MathHelper.Clamp(_player.Position.Y, 0, currentMap.PixelHeight - _player.Height)
			);

			// Check door collisions
			var door = _roomManager.CurrentRoom.CheckDoorCollision(_player.Bounds);
			if(door != null) {
				System.Diagnostics.Debug.WriteLine($"Transitioning from {_roomManager.CurrentRoom.Id} to {door.TargetRoomId}");

				_roomManager.TransitionToRoom(door.TargetRoomId, _player, door.TargetDoorDirection);
				_currentEnemies = _roomManager.CurrentRoom.Enemies;
				_currentPickups = _roomManager.CurrentRoom.Pickups;
				_camera.WorldBounds = new Rectangle(0, 0, _roomManager.CurrentRoom.Map.PixelWidth, _roomManager.CurrentRoom.Map.PixelHeight);

				System.Diagnostics.Debug.WriteLine($"Now in room: {_roomManager.CurrentRoom.Id}, Player pos: {_player.Position}");
			}

			// Update all enemies
			foreach(var enemy in _currentEnemies) {
				enemy.Update(gameTime);

				// Apply collision constraints for enemies that hit walls
				enemy.ApplyCollisionConstraints(currentMap);

				// Clamp enemies to world bounds
				enemy.Position = new Vector2(
					MathHelper.Clamp(enemy.Position.X, 0, currentMap.PixelWidth - enemy.Width),
					MathHelper.Clamp(enemy.Position.Y, 0, currentMap.PixelHeight - enemy.Height)
				);
			}

			// Check player attack hitting enemies
			if(_player.AttackBounds != Rectangle.Empty) {
				foreach(var enemy in _currentEnemies) {
					// Only hit each enemy once per attack
					if(enemy.IsAlive && !_player.HasHitEntity(enemy) && _player.AttackBounds.Intersects(enemy.Bounds)) {
						Vector2 playerCenter = _player.Position + new Vector2(_player.Width / 2f, _player.Height / 2f);
						bool wasAlive = enemy.IsAlive;

						// Calculate damage with crit
						var (damage, wasCrit) = _player.CalculateDamage();

						enemy.TakeDamage(damage, playerCenter);

						// Mark this enemy as hit during this attack
						_player.MarkEntityAsHit(enemy);

						// Apply lifesteal
						_player.OnDamageDealt(damage);

						// Show damage number (yellow for crit, white for normal)
						Vector2 damagePos = enemy.Position + new Vector2(enemy.Width / 2f, 0);
						Color damageColor = wasCrit ? Color.Yellow : Color.White;
						_damageNumbers.Add(new DamageNumber(damage, damagePos, _font, false, damageColor));

						// Check if this attack killed the enemy
						if(wasAlive && !enemy.IsAlive && !enemy.HasDroppedLoot) {
							// Award XP
							bool leveledUp = _player.GainXP(enemy.XPValue);
							if(leveledUp) {
								// Show level up effect
								_levelUpEffects.Add(new LevelUpEffect(_player.Position, _font));
							}

							SpawnLoot(enemy);
							enemy.HasDroppedLoot = true;
						}
					}
				}
			}

			// Update pickups
			foreach(var pickup in _currentPickups) {
				pickup.Update(gameTime);

				// Check if player collects it
				if(pickup.CheckCollision(_player)) {
					_player.CollectPickup(pickup);
				}
			}

			foreach(var npc in _roomManager.CurrentRoom.NPCs) {
				npc.Update(gameTime);
			}

			// Remove collected pickups
			_currentPickups.RemoveAll(p => p.IsCollected);

			// Update damage numbers
			foreach(var damageNumber in _damageNumbers) {
				damageNumber.Update(gameTime);
			}

			// Remove expired damage numbers
			_damageNumbers.RemoveAll(d => d.IsExpired);

			// Update level up effects
			foreach(var effect in _levelUpEffects) {
				effect.Update(gameTime);
			}

			// Remove expired effects
			_levelUpEffects.RemoveAll(e => e.IsExpired);

			// Remove dead enemies
			_currentEnemies.RemoveAll(e => !e.IsAlive);

			// Check enemies hitting player
			foreach(var enemy in _currentEnemies) {
				if(enemy.IsAlive && enemy.CollidesWith(_player)) {
					Vector2 enemyCenter = enemy.Position + new Vector2(enemy.Width / 2f, enemy.Height / 2f);

					// Check if player wasn't already invincible to avoid duplicate damage numbers
					bool wasInvincible = _player.IsInvincible;
					_player.TakeDamage(enemy.AttackDamage, enemyCenter);

					// Show damage number only if damage was actually taken
					if(!wasInvincible && _player.IsInvincible) {
						Vector2 damagePos = _player.Position + new Vector2(_player.Width / 2f, 0);
						_damageNumbers.Add(new DamageNumber(enemy.AttackDamage, damagePos, _font, true));
					}
				}
			}

			// Make camera follow player smoothly
			_camera.FollowSmooth(_player.Position + new Vector2(_player.Width / 2f, _player.Height / 2f), 0.1f);
			_camera.Update();

			_previousKeyState = currentKeyState;

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime) {
			GraphicsDevice.SetRenderTarget(_gameRenderTarget);
			GraphicsDevice.Clear(Color.Black);

			// Draw world with camera transform
			_spriteBatch.Begin(
				samplerState: SamplerState.PointClamp,
				transformMatrix: _camera.Transform
			);

			// Draw the tilemap
			_roomManager.CurrentRoom.Map.Draw(_spriteBatch, _camera.GetVisibleArea());

			// Draw doors
			_roomManager.CurrentRoom.DrawDoors(_spriteBatch, _doorTexture);

			// Draw pickups
			foreach(var pickup in _currentPickups) {
				pickup.Draw(_spriteBatch);
			}

			// Draw enemies
			foreach(var enemy in _currentEnemies) {
				enemy.Draw(_spriteBatch);
			}

			foreach(var npc in _roomManager.CurrentRoom.NPCs) {
				npc.Draw(_spriteBatch);
			}

			// Draw player (on top of enemies)
			_player.Draw(_spriteBatch);

			// Draw attack effect
			_player.DrawAttackEffect(_spriteBatch);

			// Draw map editor cursor
			if(_mapEditor.IsActive) {
				var cursorRect = _mapEditor.GetCursorTileRect();
				if(cursorRect != Rectangle.Empty) {
					var editorTexture = Graphics.CreateColoredTexture(GraphicsDevice, 1, 1, Color.White);
					_spriteBatch.Draw(editorTexture, cursorRect, _mapEditor.GetSelectedTileColor() * 0.5f);
				}
			}

			// Draw damage numbers
			foreach(var damageNumber in _damageNumbers) {
				damageNumber.Draw(_spriteBatch);
			}

			// Draw level up effects
			foreach(var effect in _levelUpEffects) {
				effect.Draw(_spriteBatch);
			}

			_spriteBatch.End();

			// Draw UI (no camera transform)
			_spriteBatch.Begin(samplerState: SamplerState.PointClamp);

			// Draw health and xp bar
			_healthBar.draw(_spriteBatch);
			_xpBar.draw(_spriteBatch);
			_coinCounter.draw(_spriteBatch);
			_lvlCounter.draw(_spriteBatch);

			DrawStatDisplay(_spriteBatch, 10, 70);

			_spriteBatch.End();

			// Draw menu on top of everything
			_spriteBatch.Begin(samplerState: SamplerState.PointClamp);
			_gameMenu.draw(_spriteBatch);

			// Draw map editor UI
			if(_mapEditor.IsActive) {
				_mapEditor.Draw(_spriteBatch);
			}

			_dialogUI.Draw(_spriteBatch);

			_spriteBatch.End();

			GraphicsDevice.SetRenderTarget(null);
			GraphicsDevice.Clear(Color.Black);

			_spriteBatch.Begin(samplerState: SamplerState.PointClamp);

			// Draw the 640x360 game scaled to 1920x1080
			Rectangle destinationRect = new Rectangle(0, 0, DISPLAY_WIDTH, DISPLAY_HEIGHT);
			_spriteBatch.Draw(_gameRenderTarget, destinationRect, Color.White);

			_spriteBatch.End();

			base.Draw(gameTime);
		}

		// Helper: Load texture directly from file (bypasses Content Pipeline)
		private Texture2D LoadTextureFromFile(string path) {
			if(!File.Exists(path))
				return null;

			using var fileStream = new FileStream(path, FileMode.Open);
			return Texture2D.FromStream(GraphicsDevice, fileStream);
		}

		private void SpawnLoot(Enemy enemy) {
			var random = new System.Random();
			Vector2 dropPosition = enemy.Position + new Vector2(enemy.Width / 2f - 8, enemy.Height / 2f - 8);

			// Check if health potion drops
			if(random.NextDouble() < enemy.HealthDropChance) {
				_currentPickups.Add(new Pickup(PickupType.HealthPotion, dropPosition, _healthPotionTexture));
			}

			// Check if coins drop
			if(random.NextDouble() < enemy.CoinDropChance) {
				// Random chance for big coin
				PickupType coinType = random.NextDouble() < 0.2 ? PickupType.BigCoin : PickupType.Coin;
				Vector2 coinPos = dropPosition + new Vector2(random.Next(-10, 10), random.Next(-10, 10));
				_currentPickups.Add(new Pickup(coinType, coinPos, _coinTexture));
			}
		}


		private void CreateRooms() {
			// Load enemy sprites (with fallback to colored blobs)
			Texture2D idleEnemySprite = LoadTextureFromFile("Assets/Sprites/enemy_idle.png")
				?? CreateEnemySprite(Color.Red, Color.DarkRed);
			Texture2D patrolEnemySprite = LoadTextureFromFile("Assets/Sprites/enemy_patrol.png")
				?? CreateEnemySprite(Color.Blue, Color.DarkBlue);
			Texture2D wanderEnemySprite = LoadTextureFromFile("Assets/Sprites/enemy_wander.png")
				?? CreateEnemySprite(Color.Orange, Color.DarkOrange);
			Texture2D chaseEnemySprite = LoadTextureFromFile("Assets/Sprites/enemy_chase.png")
				?? CreateEnemySprite(Color.Purple, Color.DarkMagenta);

			// Room 1 - Load from file or fallback
			var room1MapData = MapData.LoadFromFile("Assets/Maps/room1.json");

			Room room1;
			if(room1MapData != null && room1MapData.Doors.Count > 0) {
				// Load complete room from MapData (includes doors, enemies, spawn)
				room1 = Room.FromMapData("room1", room1MapData, GraphicsDevice);

				room1.Map.LoadTileset(TileType.Grass, DualGridTilesetGenerator.GenerateTileset(GraphicsDevice, TileType.Grass, TILE_SIZE));
				room1.Map.LoadTileset(TileType.Water, DualGridTilesetGenerator.GenerateTileset(GraphicsDevice, TileType.Water, TILE_SIZE));
				room1.Map.LoadTileset(TileType.Stone, DualGridTilesetGenerator.GenerateTileset(GraphicsDevice, TileType.Stone, TILE_SIZE));
				room1.Map.LoadTileset(TileType.Tree, DualGridTilesetGenerator.GenerateTileset(GraphicsDevice, TileType.Tree, TILE_SIZE));

				// Create enemies from saved data
				foreach(var enemyData in room1MapData.Enemies) {
					Texture2D enemySprite = GetEnemySpriteForBehavior((EnemyBehavior)enemyData.Behavior,
						idleEnemySprite, patrolEnemySprite, wanderEnemySprite, chaseEnemySprite);

					bool isAnimated = enemySprite.Width == 128 && enemySprite.Height == 128;
					Enemy enemy;

					if(isAnimated) {
						enemy = new Enemy(enemySprite, new Vector2(enemyData.X, enemyData.Y),
							(EnemyBehavior)enemyData.Behavior, 4, 32, 32, 0.15f, speed: enemyData.Speed);
					} else {
						enemy = new Enemy(enemySprite, new Vector2(enemyData.X, enemyData.Y),
							(EnemyBehavior)enemyData.Behavior, speed: enemyData.Speed);
					}

					// Set detection range for chase enemies
					if(enemyData.Behavior == (int)EnemyBehavior.Chase) {
						enemy.DetectionRange = enemyData.DetectionRange;
						enemy.SetChaseTarget(_player, room1.Map);
					}

					// Set patrol points if patrol enemy
					if(enemyData.Behavior == (int)EnemyBehavior.Patrol) {
						enemy.SetPatrolPoints(
							new Vector2(enemyData.PatrolStartX, enemyData.PatrolStartY),
							new Vector2(enemyData.PatrolEndX, enemyData.PatrolEndY)
						);
					}

					room1.Enemies.Add(enemy);
				}
			} else {
				// Fallback to procedural generation
				var room1Map = room1MapData != null
					? room1MapData.ToTileMap(GraphicsDevice)
					: new DualGridTileMap(MAP_WIDTH, MAP_HEIGHT, TILE_SIZE, GraphicsDevice, seed: 100);

				room1 = new Room("room1", room1Map, 100);

				// Add default enemies and doors as before
				bool idleAnimated = idleEnemySprite.Width == 128 && idleEnemySprite.Height == 128;
				bool patrolAnimated = patrolEnemySprite.Width == 128 && patrolEnemySprite.Height == 128;

				if(idleAnimated) {
					room1.Enemies.Add(new Enemy(idleEnemySprite, new Vector2(400, 300), EnemyBehavior.Idle, 4, 32, 32, 0.15f));
				} else {
					room1.Enemies.Add(new Enemy(idleEnemySprite, new Vector2(400, 300), EnemyBehavior.Idle));
				}

				if(patrolAnimated) {
					var patrolEnemy = new Enemy(patrolEnemySprite, new Vector2(600, 400), EnemyBehavior.Patrol, 4, 32, 32, 0.15f, speed: 80f);
					patrolEnemy.SetPatrolPoints(new Vector2(600, 400), new Vector2(800, 400));
					room1.Enemies.Add(patrolEnemy);
				} else {
					var patrolEnemy = new Enemy(patrolEnemySprite, new Vector2(600, 400), EnemyBehavior.Patrol, speed: 80f);
					patrolEnemy.SetPatrolPoints(new Vector2(600, 400), new Vector2(800, 400));
					room1.Enemies.Add(patrolEnemy);
				}

				// Add default doors
				room1.AddDoor(DoorDirection.North, "room2", DoorDirection.South);
				room1.AddDoor(DoorDirection.East, "room3", DoorDirection.West);
			}

			_roomManager.AddRoom(room1);

			// Room 2 - Similar pattern
			var room2MapData = MapData.LoadFromFile("Assets/Maps/room2.json");
			Room room2;

			if(room2MapData != null && room2MapData.Doors.Count > 0) {
				room2 = Room.FromMapData("room2", room2MapData, GraphicsDevice);
				LoadEnemiesFromMapData(room2, room2MapData, idleEnemySprite, patrolEnemySprite, wanderEnemySprite, chaseEnemySprite);
			} else {
				var room2Map = room2MapData != null
					? room2MapData.ToTileMap(GraphicsDevice)
					: new DualGridTileMap(MAP_WIDTH, MAP_HEIGHT, TILE_SIZE, GraphicsDevice, seed: 200);
				room2 = new Room("room2", room2Map, 200);

				// Add default enemies and doors
				bool wanderAnimated = wanderEnemySprite.Width == 128 && wanderEnemySprite.Height == 128;
				if(wanderAnimated) {
					room2.Enemies.Add(new Enemy(wanderEnemySprite, new Vector2(300, 600), EnemyBehavior.Wander, 4, 32, 32, 0.15f, speed: 60f));
					room2.Enemies.Add(new Enemy(wanderEnemySprite, new Vector2(700, 400), EnemyBehavior.Wander, 4, 32, 32, 0.15f, speed: 60f));
				} else {
					room2.Enemies.Add(new Enemy(wanderEnemySprite, new Vector2(300, 600), EnemyBehavior.Wander, speed: 60f));
					room2.Enemies.Add(new Enemy(wanderEnemySprite, new Vector2(700, 400), EnemyBehavior.Wander, speed: 60f));
				}

				room2.AddDoor(DoorDirection.South, "room1", DoorDirection.North);
			}

			_roomManager.AddRoom(room2);

			// Room 3 - Similar pattern
			var room3MapData = MapData.LoadFromFile("Assets/Maps/room3.json");
			Room room3;

			if(room3MapData != null && room3MapData.Doors.Count > 0) {
				room3 = Room.FromMapData("room3", room3MapData, GraphicsDevice);
				LoadEnemiesFromMapData(room3, room3MapData, idleEnemySprite, patrolEnemySprite, wanderEnemySprite, chaseEnemySprite);
			} else {
				var room3Map = room3MapData != null
					? room3MapData.ToTileMap(GraphicsDevice)
					: new DualGridTileMap(MAP_WIDTH, MAP_HEIGHT, TILE_SIZE, GraphicsDevice, seed: 300);
				room3 = new Room("room3", room3Map, 300);

				// Add default chase enemy
				bool chaseAnimated = chaseEnemySprite.Width == 128 && chaseEnemySprite.Height == 128;
				if(chaseAnimated) {
					var chaseEnemy = new Enemy(chaseEnemySprite, new Vector2(900, 450), EnemyBehavior.Chase, 4, 32, 32, 0.15f, speed: 120f);
					chaseEnemy.DetectionRange = 200f;
					chaseEnemy.SetChaseTarget(_player, room3Map);
					room3.Enemies.Add(chaseEnemy);
				} else {
					var chaseEnemy = new Enemy(chaseEnemySprite, new Vector2(900, 450), EnemyBehavior.Chase, speed: 120f);
					chaseEnemy.DetectionRange = 200f;
					chaseEnemy.SetChaseTarget(_player, room3Map);
					room3.Enemies.Add(chaseEnemy);
				}

				room3.AddDoor(DoorDirection.West, "room1", DoorDirection.East);
			}

			_roomManager.AddRoom(room3);
		}

		private Texture2D CreateEnemySprite(Color primaryColor, Color secondaryColor) {
			int size = TILE_SIZE;
			Texture2D texture = new Texture2D(GraphicsDevice, size, size);
			Color[] data = new Color[size * size];

			// Simple enemy design - blob with eyes
			for(int y = 0; y < size; y++) {
				for(int x = 0; x < size; x++) {
					int index = y * size + x;
					int centerX = size / 2;
					int centerY = size / 2;

					// Calculate distance from center
					int dx = x - centerX;
					int dy = y - centerY;
					int distSq = dx * dx + dy * dy;

					// Create circular body
					if(distSq < (size / 2 - 2) * (size / 2 - 2)) {
						// Bottom half darker
						if(y > centerY) {
							data[index] = secondaryColor;
						} else {
							data[index] = primaryColor;
						}

						// Eyes (white with black pupils)
						if(y >= centerY - 4 && y <= centerY - 2) {
							// Left eye
							if(x >= centerX - 6 && x <= centerX - 4) {
								if(x == centerX - 5 && y == centerY - 3)
									data[index] = Color.Black; // Pupil
								else
									data[index] = Color.White; // Eye white
							}
							// Right eye
							if(x >= centerX + 4 && x <= centerX + 6) {
								if(x == centerX + 5 && y == centerY - 3)
									data[index] = Color.Black; // Pupil
								else
									data[index] = Color.White; // Eye white
							}
						}

						// Simple mouth
						if(y == centerY + 3) {
							if(x >= centerX - 4 && x <= centerX + 4) {
								data[index] = Color.Black;
							}
						}
					} else {
						data[index] = Color.Transparent;
					}
				}
			}

			texture.SetData(data);
			return texture;
		}
		private Texture2D GetEnemySpriteForBehavior(EnemyBehavior behavior,
	Texture2D idle, Texture2D patrol, Texture2D wander, Texture2D chase) {
			return behavior switch {
				EnemyBehavior.Idle => idle,
				EnemyBehavior.Patrol => patrol,
				EnemyBehavior.Wander => wander,
				EnemyBehavior.Chase => chase,
				_ => idle
			};
		}

		private void LoadEnemiesFromMapData(Room room, MapData mapData,
			Texture2D idle, Texture2D patrol, Texture2D wander, Texture2D chase) {
			foreach(var enemyData in mapData.Enemies) {
				Texture2D enemySprite = GetEnemySpriteForBehavior((EnemyBehavior)enemyData.Behavior,
					idle, patrol, wander, chase);

				bool isAnimated = enemySprite.Width == 128 && enemySprite.Height == 128;
				Enemy enemy;

				if(isAnimated) {
					enemy = new Enemy(enemySprite, new Vector2(enemyData.X, enemyData.Y),
						(EnemyBehavior)enemyData.Behavior, 4, 32, 32, 0.15f, speed: enemyData.Speed);
				} else {
					enemy = new Enemy(enemySprite, new Vector2(enemyData.X, enemyData.Y),
						(EnemyBehavior)enemyData.Behavior, speed: enemyData.Speed);
				}

				if(enemyData.Behavior == (int)EnemyBehavior.Chase) {
					enemy.DetectionRange = enemyData.DetectionRange;
					enemy.SetChaseTarget(_player, room.Map);
				}

				if(enemyData.Behavior == (int)EnemyBehavior.Patrol) {
					enemy.SetPatrolPoints(
						new Vector2(enemyData.PatrolStartX, enemyData.PatrolStartY),
						new Vector2(enemyData.PatrolEndX, enemyData.PatrolEndY)
					);
				}

				room.Enemies.Add(enemy);
			}
		}
		private void DrawStatDisplay(SpriteBatch spriteBatch, int x, int y) {
			int lineHeight = 16;
			int currentY = y;

			// Attack stats
			string atkText = $"ATK: {_player.Stats.AttackDamage}";
			if(_player.Stats.CritChance > 0) {
				atkText += $" ({(_player.Stats.CritChance * 100):F0}% crit)";
			}
			_font.drawText(spriteBatch, atkText, new Vector2(x, currentY), Color.White);
			currentY += lineHeight;

			// Defense
			if(_player.Stats.Defense > 0) {
				string defText = $"DEF: {_player.Stats.Defense}";
				_font.drawText(spriteBatch, defText, new Vector2(x, currentY), Color.LightBlue);
				currentY += lineHeight;
			}

			// Speed
			string spdText = $"SPD: {_player.Stats.Speed:F0}";
			_font.drawText(spriteBatch, spdText, new Vector2(x, currentY), Color.LightGreen);
			currentY += lineHeight;

			// Regen (if any)
			if(_player.Stats.HealthRegen > 0) {
				string regenText = $"REGEN: {_player.Stats.HealthRegen:F1}/s";
				_font.drawText(spriteBatch, regenText, new Vector2(x, currentY), Color.LimeGreen);
				currentY += lineHeight;
			}

			// Attack Speed
			string atkSpdText = $"ATK SPD: {_player.Stats.AttackSpeed:F1}/s";
			_font.drawText(spriteBatch, atkSpdText, new Vector2(x, currentY), Color.Orange);
		}

		private string GetNearbyNPCId() {
			// Example implementation - replace with your actual NPC detection
			float interactionRange = 50f;

			// Check all NPCs in current room
			foreach(var npc in _roomManager.CurrentRoom.NPCs) {
				float distance = Vector2.Distance(_player.Position, npc.Position);
				if(distance < interactionRange) {
					return npc.DialogId;
				}
			}

			return null;
		}

		/// <summary>
		/// Example: How to trigger dialog from code (e.g., cutscene)
		/// </summary>
		private void TriggerCutsceneDialog() {
			_dialogManager.StartDialog("village_elder");
		}

		/// <summary>
		/// Example: How to check if player can talk to NPC
		/// </summary>
		private bool CanTalkToNPC(string npcId) {
			// This checks if NPC requires an item
			var npc = _dialogManager.GetNPCDefinition(npcId);
			if(npc != null && !string.IsNullOrEmpty(npc.RequiresItem)) {
				return _dialogManager.GameState.HasItem(npc.RequiresItem);
			}
			return true;
		}
		private Point ScaleMousePosition(Point displayMousePos) {
			return new Point(
				displayMousePos.X / SCALE,
				displayMousePos.Y / SCALE
			);
		}
	}
}
