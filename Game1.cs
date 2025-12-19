using Candyland.Core;
using Candyland.Core.UI;
using Candyland.Dialog;
using Candyland.Entities;
using Candyland.Quests;
using Candyland.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

namespace Candyland {
	public class Game1 : Game {

		private GameState _gameState = GameState.MainMenu;
		private MainMenu _mainMenu;
		private CreditsScreen _creditsScreen;
		private bool _gameInitialized = false;

		private GameServices Services => GameServices.Instance;
		private GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;

		private DialogManager _dialogManager;
		private UIDialog _dialogUI;

		// Player, Camera, and World
		private Player _player;
		private Camera _camera;
		private RoomManager _roomManager;
		private AssetManager _assetManager;
		private RoomLoader _roomLoader;

		// Current room entities (references to current room's lists)
		private List<Enemy> _currentEnemies;
		private List<Pickup> _currentPickups;

		// Damage numbers
		private List<DamageNumber> _damageNumbers;
		private List<LevelUpEffect> _levelUpEffects;

		// Textures
		private Texture2D _healthPotionTexture;
		private Texture2D _coinTexture;
		private Texture2D _doorTexture;

		// Quests
		private QuestManager _questManager;

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
		private static int SCALE = 2;  // 3x for 1920x1080, 2x for 1280x720
		private int DISPLAY_WIDTH = NATIVE_WIDTH * SCALE;
		private int DISPLAY_HEIGHT = NATIVE_HEIGHT * SCALE;

		// Tile settings
		private const int TILE_SIZE = 16;  // Native tile size

		private RenderTarget2D _gameRenderTarget;

		public Game1() {
			_graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			IsMouseVisible = true;

			// Set window size
			_graphics.PreferredBackBufferWidth = DISPLAY_WIDTH;
			_graphics.PreferredBackBufferHeight = DISPLAY_HEIGHT;
			//_graphics.ToggleFullScreen();
		}

		protected override void Initialize() {
			base.Initialize();
		}

		protected override void LoadContent() {
			_spriteBatch = new SpriteBatch(GraphicsDevice);

			// Initialize font
			_font = new BitmapFont(GraphicsDevice);

			GameServices services = GameServices.Initialize();
			// load UI
			services.Localization.loadLanguage("en", "Assets/UI/Localization/en.json");

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



			// Create main menu
			_mainMenu = new MainMenu(GraphicsDevice, _font, NATIVE_WIDTH, NATIVE_HEIGHT, SCALE);
			_mainMenu.HasSaveFile = CheckForSaveFile(); // We'll implement this later
			_mainMenu.OnNewGame = StartNewGame;
			_mainMenu.OnContinue = ContinueGame;
			_mainMenu.OnOptions = OpenOptions;
			_mainMenu.OnCredits = OpenCredits;
			_mainMenu.OnQuit = () => Exit();

			// Create credits screen
			_creditsScreen = new CreditsScreen(GraphicsDevice, _font, NATIVE_WIDTH, NATIVE_HEIGHT, SCALE);
			_creditsScreen.OnBack = () => _gameState = GameState.MainMenu;

		}

		private void InitializeGame() {
			
			// Initialize room manager
			_roomManager = new RoomManager();

			_assetManager = new AssetManager(GraphicsDevice);

			Effect variationEffect = null;
			try {
				variationEffect = Content.Load<Effect>("VariationMask");
				System.Diagnostics.Debug.WriteLine($"Shader loaded: {variationEffect != null}");
			} catch(Exception ex) {
				System.Diagnostics.Debug.WriteLine($"Shader load error: {ex.Message}");
			}
			_roomLoader = new RoomLoader(GraphicsDevice, _assetManager, _questManager, _player, variationEffect);

			_damageNumbers = new List<DamageNumber>();
			_levelUpEffects = new List<LevelUpEffect>();

			// Create pickup and door textures
			_healthPotionTexture = Graphics.CreateColoredTexture(GraphicsDevice, 16, 16, Color.LimeGreen);
			_coinTexture = Graphics.CreateColoredTexture(GraphicsDevice, 6, 6, Color.Gold);
			_doorTexture = Graphics.CreateColoredTexture(GraphicsDevice, 1, 1, Color.White);

			// Load player texture/spritesheet
			Texture2D playerTexture = _assetManager.LoadTextureOrFallback(
				"Assets/Sprites/player.png",
				() => Graphics.CreateColoredTexture(GraphicsDevice, TILE_SIZE, TILE_SIZE, Color.Yellow)
			);

			// Create placeholder player first (will be repositioned when rooms are created)
			Vector2 tempPosition = new Vector2(NATIVE_WIDTH / 2, NATIVE_HEIGHT / 2);

			// Check if we're using an animated sprite sheet or static sprite
			if(playerTexture != null) {
				int frameCount = 3;
				int frameWidth = 32;
				int frameHeight = 32;
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

			// Set starting room
			_roomManager.setCurrentRoom("room1");
			_currentEnemies = _roomManager.currentRoom.enemies;
			_currentPickups = _roomManager.currentRoom.pickups;

			// Position player at spawn
			_player.Position = _roomManager.currentRoom.playerSpawnPosition;

			// Create camera
			_camera = new Camera(NATIVE_WIDTH, NATIVE_HEIGHT);
			// Set world bounds for native resolution
			_camera.WorldBounds = new Rectangle(0, 0, _roomManager.currentRoom.map.pixelWidth, _roomManager.currentRoom.map.pixelHeight);

			var pixelTexture = Graphics.CreateColoredTexture(GraphicsDevice, 1, 1, Color.White);

			// Create map editor
			_mapEditor = new MapEditor(_font, pixelTexture, _camera, SCALE, _assetManager, GraphicsDevice);
			_mapEditor.SetRoom(_roomManager.currentRoom);

			_previousKeyState = Keyboard.GetState();

			_player.Inventory.AddItem(EquipmentFactory.CreateIronSword());
			_player.Inventory.AddItem(EquipmentFactory.CreateLeatherArmor());
			_player.Inventory.AddItem(EquipmentFactory.CreateSpeedBoots());
			_player.Inventory.AddItem(EquipmentFactory.CreateVampireBlade());
			_player.Inventory.AddItem(EquipmentFactory.CreateCriticalRing());
			_player.Inventory.AddItem(EquipmentFactory.CreateRegenerationAmulet());

			_healthBar = new UIBar(GraphicsDevice, _font, 10, 10, 200, 2, Color.DarkRed, Color.Red, Color.White, Color.White,
				() => { return $"{_player.health} / {_player.Stats.MaxHealth}"; },
				() => { return _player.health / (float)_player.Stats.MaxHealth; }
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

			foreach(var npc in _roomManager.currentRoom.NPCs) {
				npc.SetQuestManager(Services.QuestManager);
				npc.SetFont(_font);
			}

			// Create game menu
			_gameMenu = new GameMenu(GraphicsDevice, _font, _player, NATIVE_WIDTH, NATIVE_HEIGHT, SCALE, _questManager);

			_gameMenu.OnScaleChanged += OnScaleChanged;
			_gameMenu.OnFullscreenChanged += OnFullscreenChanged;

		}

		private void OnScaleChanged(int newScale) {
			System.Diagnostics.Debug.WriteLine($"[GAME] Changing scale from {SCALE} to {newScale}");

			// Update scale constant (you'll need to make SCALE non-const)
			SCALE = newScale;

			// Resize window
			int newWidth = NATIVE_WIDTH * SCALE;
			int newHeight = NATIVE_HEIGHT * SCALE;

			DISPLAY_WIDTH = newWidth;
			DISPLAY_HEIGHT = newHeight;

			_gameMenu.SetScale(newScale);
			_dialogUI.SetScale(newScale);

			_graphics.PreferredBackBufferWidth = newWidth;
			_graphics.PreferredBackBufferHeight = newHeight;
			_graphics.ApplyChanges();

			// Recreate render target
			_gameRenderTarget?.Dispose();
			_gameRenderTarget = new RenderTarget2D(
				GraphicsDevice,
				NATIVE_WIDTH,
				NATIVE_HEIGHT,
				false,
				SurfaceFormat.Color,
				DepthFormat.None,
				0,
				RenderTargetUsage.PreserveContents
			);

			_mainMenu.SetScale(newScale);
			_creditsScreen.SetScale(newScale);
			_gameMenu.SetScale(newScale);
			_dialogUI.SetScale(newScale);

			System.Diagnostics.Debug.WriteLine($"[GAME] Window resized to {newWidth}x{newHeight}");
		}

		private void LoadContent_DialogSystem() {
			// === Initialize all services in one go ===
			var services = GameServices.Instance.setPlayer(_player);

			// Store references for convenience
			_questManager = services.QuestManager;
			_dialogManager = services.DialogManager;

			// === Load data ===
			_dialogManager.loadDialogTrees("Assets/Dialogs/Trees/dialogs.json");
			_dialogManager.loadNPCDefinitions("Assets/Dialogs/NPCs/npcs.json");
			services.Localization.loadLanguage("en", "Assets/Dialogs/Localization/en.json");

			_questManager.loadQuests("Assets/Quests/quests.json");
			services.Localization.loadLanguage("en", "Assets/Quests/Localization/en.json");


			// === Subscribe to events ===
			_questManager.OnQuestStarted += OnQuestStarted;
			_questManager.OnQuestCompleted += OnQuestCompleted;
			_questManager.OnObjectiveUpdated += OnObjectiveUpdated;
			_questManager.OnNodeAdvanced += OnNodeAdvanced;
			_dialogManager.OnResponseChosen += _questManager.OnDialogResponseChosen;

			// === Create UI ===
			_dialogUI = new UIDialog(
				_dialogManager,
				_font,
				GraphicsDevice,
				NATIVE_WIDTH,
				NATIVE_HEIGHT,
				SCALE
			);

			_dialogUI.loadPortrait("npc_villager_concerned",
			LoadTextureFromFile("Assets/Portrait/npc_villager_concerned.png"));
		}

		private void OnFullscreenChanged(bool isFullscreen) {
			System.Diagnostics.Debug.WriteLine($"[GAME] Changing fullscreen to: {isFullscreen}");

			_graphics.IsFullScreen = isFullscreen;
			_graphics.ApplyChanges();

			if(isFullscreen) {
				// Center the native resolution in fullscreen
				var displayMode = GraphicsDevice.DisplayMode;
				_graphics.PreferredBackBufferWidth = displayMode.Width;
				_graphics.PreferredBackBufferHeight = displayMode.Height;
				_graphics.ApplyChanges();
			} else {
				// Return to windowed mode with current scale
				int newWidth = NATIVE_WIDTH * SCALE;
				int newHeight = NATIVE_HEIGHT * SCALE;
				_graphics.PreferredBackBufferWidth = newWidth;
				_graphics.PreferredBackBufferHeight = newHeight;
				_graphics.ApplyChanges();
			}
		}

		// Event handlers for notifications
		private void OnQuestStarted(Quest quest) {
			string name = _questManager.getQuestName(quest);
			System.Diagnostics.Debug.WriteLine($"[QUEST STARTED] {name}");
			// TODO: Show notification on screen
		}

		private void OnQuestCompleted(Quest quest) {
			string name = _questManager.getQuestName(quest);
			System.Diagnostics.Debug.WriteLine($"[QUEST COMPLETED] {name}");
			// TODO: Show completion notification with rewards
		}

		private void OnObjectiveUpdated(Quest quest, QuestObjective objective) {
			// Optional: Show progress update
			string questName = _questManager.getQuestName(quest);
			System.Diagnostics.Debug.WriteLine($"[QUEST] {questName} - Objective updated");
		}
		private void OnNodeAdvanced(Quest quest) {
			System.Diagnostics.Debug.WriteLine($"[QUEST] Node advanced: {quest.id}");
		}

		private bool CheckForSaveFile() {
			// TODO: Check if save file exists
			return false;
		}

		private void StartNewGame() {
			if(!_gameInitialized) {
				// Initialize game for first time
				InitializeGame();
				_gameInitialized = true;
			} else {
				// Reset game state
				ResetGame();
			}
			_gameState = GameState.Playing;
		}

		private void ContinueGame() {
			// TODO: Load save file
			_gameState = GameState.Playing;
		}
		private void ResetGame() {
			// Reset player stats
			_player.reset();
			_player.Position = _roomManager.currentRoom.playerSpawnPosition;

			// Reset quests
			// TODO: Implement quest reset

			// Reset room
			_roomManager.setCurrentRoom("room1");
			_currentEnemies = _roomManager.currentRoom.enemies;
			_currentPickups = _roomManager.currentRoom.pickups;
		}

		private void OpenOptions() {
			// Open game menu to options tab
			_gameMenu.IsOpen = true;
			_gameState = GameState.Playing; // Or create separate Options state
		}

		private void OpenCredits() {
			_gameState = GameState.GameOver; // Reuse GameOver for Credits temporarily
		}

		protected override void Update(GameTime gameTime) {
			switch(_gameState) {
				case GameState.MainMenu:
					_mainMenu.Update(gameTime);
					break;

				case GameState.Playing:
					UpdateGame(gameTime);
					break;

				case GameState.Paused:
					// Handle pause menu
					UpdateGame(gameTime); // Still update for pause menu
					break;

				case GameState.GameOver:
					_creditsScreen.Update(gameTime);
					break;
			}

			base.Update(gameTime);
		}

		private void UpdateGame(GameTime gameTime) {
			KeyboardState currentKeyState = Keyboard.GetState();

			if(currentKeyState.IsKeyDown(Keys.Escape) && _previousKeyState.IsKeyUp(Keys.Escape)) {
				_gameState = GameState.MainMenu;
				_previousKeyState = currentKeyState;
				return;
			}

			// Test: Start wolf hunt quest
			if(currentKeyState.IsKeyDown(Keys.F1) && _previousKeyState.IsKeyUp(Keys.F1)) {
				_questManager.startQuest("wolf_hunt");
			}

			// Test: Simulate killing a wolf
			if(currentKeyState.IsKeyDown(Keys.F2) && _previousKeyState.IsKeyUp(Keys.F2)) {
				_questManager.updateObjectiveProgress("kill_enemy", "wolf", 1);
			}

			// Test: Start pirate quest
			if(currentKeyState.IsKeyDown(Keys.F3) && _previousKeyState.IsKeyUp(Keys.F3)) {
				_questManager.startQuest("meet_the_elder");
			}

			// Toggle menu with Tab
			if(currentKeyState.IsKeyDown(Keys.Tab) && _previousKeyState.IsKeyUp(Keys.Tab)) {
				_gameMenu.IsOpen = !_gameMenu.IsOpen;
			}

			// Toggle map editor with E
			if(currentKeyState.IsKeyDown(Keys.M) && _previousKeyState.IsKeyUp(Keys.M)) {
				_mapEditor.IsActive = !_mapEditor.IsActive;
				if(_mapEditor.IsActive) {
					_mapEditor.SetRoom(_roomManager.currentRoom);
				}
			}
			if(_dialogUI.isActive) {
				_dialogUI.update(gameTime);
				_previousKeyState = currentKeyState;
				return; // Don't update game when dialog is active
			}

			if(currentKeyState.IsKeyDown(Keys.E) && _previousKeyState.IsKeyUp(Keys.E)) {
				foreach(var npc in _roomManager.currentRoom.NPCs) {
					float distance = Vector2.Distance(_player.Position, npc.Position);
					if(distance < 50f) {
						_dialogManager.startDialog(npc.DialogId);
						_questManager.updateObjectiveProgress("talk_to_npc", npc.DialogId, 1);
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
			if(_gameMenu.IsOpen) {
				_gameMenu.Update(gameTime);
				_previousKeyState = currentKeyState;
				return; // Don't update game when menu is open
			}

			var currentMap = _roomManager.currentRoom.map;

			// === UPDATE PROPS ===
			foreach(var prop in _roomManager.currentRoom.props) {
				prop.Update(gameTime);

				// Apply world bounds for pushable props
				if(prop.isPushable) {
					prop.ApplyWorldBounds(new Rectangle(0, 0, currentMap.pixelWidth, currentMap.pixelHeight));
				}
			}


			// Press E to interact
			if(currentKeyState.IsKeyDown(Keys.E) && _previousKeyState.IsKeyUp(Keys.E)) {
				Vector2 playerCenter = _player.Position + new Vector2(_player.Width / 2, _player.Height / 2);

				// Check props
				foreach(var prop in _roomManager.currentRoom.props) {
					if(prop.type == PropType.Interactive && prop.IsPlayerInRange(playerCenter)) {
						prop.Interact();
						break;
					}
				}
			}

			// === PLAYER ATTACK HITTING PROPS ===
			if(_player.AttackBounds != Rectangle.Empty) {
				foreach(var prop in _roomManager.currentRoom.props) {
					if(prop.type == PropType.Breakable && prop.isActive) {
						if(_player.AttackBounds.Intersects(prop.Bounds)) {
							var (damage, wasCrit) = _player.CalculateDamage();
							prop.TakeDamage(damage);

							// Show damage number
							Vector2 damagePos = prop.Position + new Vector2(prop.Width / 2f, 0);
							_damageNumbers.Add(new DamageNumber(damage, damagePos, _font, wasCrit));
						}
					}
				}
			}

			// === PLAYER PUSHING PROPS ===
			foreach(var prop in _roomManager.currentRoom.props) {
				if(prop.isPushable && prop.isActive && prop.Bounds.Intersects(_player.Bounds)) {
					// Calculate push direction
					Vector2 playerCenter = _player.Position + new Vector2(_player.Width / 2, _player.Height / 2);
					Vector2 propCenter = prop.Position + new Vector2(prop.Width / 2, prop.Height / 2);
					Vector2 pushDirection = propCenter - playerCenter;

					if(pushDirection != Vector2.Zero) {
						prop.Push(pushDirection, 120f);
					}
				}
			}

			// === PLAYER COLLISION WITH PROPS ===
			bool collidingWithProps = false;
			foreach(var prop in _roomManager.currentRoom.props) {
				if(prop.isCollidable && prop.isActive && prop.Bounds.Intersects(_player.Bounds)) {
					collidingWithProps = true;
					break;
				}
			}

			if(collidingWithProps) {
				_player.Position = _player.PreviousPosition;  // Undo movement
			}

			// === COLLECTIBLE PROPS (AUTO-PICKUP) ===
			for(int i = _roomManager.currentRoom.props.Count - 1; i >= 0; i--) {
				var prop = _roomManager.currentRoom.props[i];

				if(prop.type == PropType.Collectible && prop.isActive && prop.Bounds.Intersects(_player.Bounds)) {
					// Collect the item
					// TODO: Add to inventory or apply effect
					prop.isActive = false;
					_roomManager.currentRoom.props.RemoveAt(i);
				}
			}

			// Update player with collision detection
			_player.Update(gameTime, currentMap);

			// Clamp player to world bounds
			_player.Position = new Vector2(
				MathHelper.Clamp(_player.Position.X, 0, currentMap.pixelWidth - _player.Width),
				MathHelper.Clamp(_player.Position.Y, 0, currentMap.pixelHeight - _player.Height)
			);

			// Check door collisions
			var door = _roomManager.currentRoom.checkDoorCollision(_player.Bounds);
			if(door != null) {
				System.Diagnostics.Debug.WriteLine($"Transitioning from {_roomManager.currentRoom.id} to {door.targetRoomId}");

				_roomManager.transitionToRoom(door.targetRoomId, _player, door.targetDoorDirection);
				_currentEnemies = _roomManager.currentRoom.enemies;
				_currentPickups = _roomManager.currentRoom.pickups;
				_camera.WorldBounds = new Rectangle(0, 0, _roomManager.currentRoom.map.pixelWidth, _roomManager.currentRoom.map.pixelHeight);

				System.Diagnostics.Debug.WriteLine($"Now in room: {_roomManager.currentRoom.id}, Player pos: {_player.Position}");
			}

			// Update all enemies
			foreach(var enemy in _currentEnemies) {
				enemy.Update(gameTime);

				// Apply collision constraints for enemies that hit walls
				enemy.ApplyCollisionConstraints(currentMap);

				// Clamp enemies to world bounds
				enemy.Position = new Vector2(
					MathHelper.Clamp(enemy.Position.X, 0, currentMap.pixelWidth - enemy.Width),
					MathHelper.Clamp(enemy.Position.Y, 0, currentMap.pixelHeight - enemy.Height)
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
							_questManager.updateObjectiveProgress("kill_enemy", enemy.EnemyType, 1);
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
					_questManager.updateObjectiveProgress("collect_item", pickup.ItemId, 1);
				}
			}

			foreach(var npc in _roomManager.currentRoom.NPCs) {
				npc.Update(gameTime);
				npc.IsPlayerInRange(_player.Position);
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
			float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			_camera.FollowSmooth(_player.Position + new Vector2(_player.Width / 2f, _player.Height / 2f), deltaTime);
			

			_camera.Update();

			_previousKeyState = currentKeyState;

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime) {
			GraphicsDevice.SetRenderTarget(_gameRenderTarget);
			GraphicsDevice.Clear(Color.Black);

			switch(_gameState) {
				case GameState.MainMenu:
					_spriteBatch.Begin(samplerState: SamplerState.PointClamp);
					_mainMenu.Draw(_spriteBatch);
					_spriteBatch.End();
					break;

				case GameState.Playing:
				case GameState.Paused:
					DrawGame(gameTime);
					break;

				case GameState.GameOver:
					_spriteBatch.Begin(samplerState: SamplerState.PointClamp);
					_creditsScreen.Draw(_spriteBatch);
					_spriteBatch.End();
					break;
			}

			// Composite to screen
			GraphicsDevice.SetRenderTarget(null);
			_spriteBatch.Begin(samplerState: SamplerState.PointClamp);
			_spriteBatch.Draw(_gameRenderTarget, new Rectangle(0, 0, DISPLAY_WIDTH, DISPLAY_HEIGHT), Color.White);
			_spriteBatch.End();

			base.Draw(gameTime);
		}

		private void DrawGame(GameTime gameTime) {
			GraphicsDevice.SetRenderTarget(_gameRenderTarget);
			GraphicsDevice.Clear(Color.Black);

			// Draw world with camera transform
			_spriteBatch.Begin(
				samplerState: SamplerState.PointClamp,
				transformMatrix: _camera.Transform
			);

			// Draw the tilemap
			_roomManager.currentRoom.map.draw(_spriteBatch, _camera.GetVisibleArea(), _camera.Transform);

			_spriteBatch.End();
			_spriteBatch.Begin(
				samplerState: SamplerState.PointClamp,
				transformMatrix: _camera.Transform
			);

			// Draw doors
			_roomManager.currentRoom.drawDoors(_spriteBatch, _doorTexture);

			// Draw pickups
			foreach(var pickup in _currentPickups) {
				pickup.Draw(_spriteBatch);
			}

			List<Entity> entities = new List<Entity>();
			entities.AddRange(_roomManager.currentRoom.props);
			entities.AddRange(_currentEnemies);
			entities.AddRange(_roomManager.currentRoom.NPCs);
			entities.Add(_player);

			entities.Sort((a, b) =>
				(a.Position.Y + a.Bounds.Height)
					.CompareTo(b.Position.Y + b.Bounds.Height));

			foreach(var entity in entities) {
				entity.Draw(_spriteBatch);
			}

			// Draw attack effect
			_player.DrawAttackEffect(_spriteBatch);

			// Draw map editor cursor
			if(_mapEditor.IsActive) {
				var cursorRect = _mapEditor.GetCursorTileRect();
				if(cursorRect != Rectangle.Empty) {
					var editorTexture = Graphics.CreateColoredTexture(GraphicsDevice, 1, 1, Color.White);
					_spriteBatch.Draw(editorTexture, cursorRect, _mapEditor.GetSelectedTileColor() * 0.5f);
				}

				// === NEW: DRAW PROP PREVIEW ===
				var propRect = _mapEditor.GetCursorPropRect();
				if(propRect != Rectangle.Empty) {
					var editorTexture = Graphics.CreateColoredTexture(GraphicsDevice, 1, 1, Color.White);
					_spriteBatch.Draw(editorTexture, propRect, _mapEditor.GetSelectedPropColor() * 0.6f);
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

			if(!_mapEditor.IsActive) {
				_spriteBatch.Begin(samplerState: SamplerState.PointClamp);
				// Draw health and xp bar
				_healthBar.draw(_spriteBatch);
				_xpBar.draw(_spriteBatch);
				_coinCounter.draw(_spriteBatch);
				_lvlCounter.draw(_spriteBatch);

				DrawStatDisplay(_spriteBatch, 10, 70);
				_spriteBatch.End();
			}


			// Draw menu on top of everything
			_spriteBatch.Begin(samplerState: SamplerState.PointClamp);

			// Game menu (only if not in map editor)
			if(!_mapEditor.IsActive) {
				_gameMenu.Draw(_spriteBatch);
				_dialogUI.draw(_spriteBatch);
			}

			// Draw map editor UI
			if(_mapEditor.IsActive) {
				_mapEditor.Draw(_spriteBatch);
			}

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
			// Define which rooms to load
			var roomDefinitions = new Dictionary<string, string> {
				{ "room1", "Assets/Maps/room1.json" },
				{ "room2", "Assets/Maps/room2.json" },
				{ "room3", "Assets/Maps/room3.json" }
			};

			// Load all rooms
			foreach(var (roomId, mapPath) in roomDefinitions) {
				var room = _roomLoader.LoadRoom(roomId, mapPath);

				if(room != null) {
					_roomManager.addRoom(room);
				} else {
					// Fallback to procedural generation
					System.Diagnostics.Debug.WriteLine($"Failed to load {roomId}, generating procedural room");
					var proceduralRoom = _roomLoader.CreateProceduralRoom(roomId, roomId.GetHashCode());
					_roomManager.addRoom(proceduralRoom);
				}
			}

			// Optionally add the NPC to room1 (or move this to MapData)
			var room1 = _roomManager.rooms["room1"]; 
			var questGiverSprite = _assetManager.LoadTexture("Assets/Sprites/quest_giver_forest.png");
			if(questGiverSprite != null && room1 != null) {
				var questGiver = new NPC(
					questGiverSprite,
					new Vector2(400, 300),
					"shepherd", _questManager,
					3, 32, 32, 0.1f,
					width: 24, height: 24
				);
				room1.NPCs.Add(questGiver);
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
	}
}
