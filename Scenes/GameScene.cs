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

namespace Candyland.Scenes;

internal class GameScene : Scene {
	// Custom renderer
	private RenderTarget2D _gameRenderTarget;

	// Tile settings
	private const int TILE_SIZE = 16;  // Native tile size

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

	private MapEditor _mapEditor;
	private UIBar _healthBar;
	private UIBar _xpBar;
	private UICounter _coinCounter;
	private UICounter _lvlCounter;

	// TODO: create a scene for that
	private UIDialog _dialogUI;


	private KeyboardState _previousKeyState;


	public GameScene(ApplicationContext appContext, bool exclusive = true) : base(appContext, exclusive) {

		_assetManager = appContext.assetManager;
		_roomManager = new RoomManager();


		Effect variationEffect = null;
		try {
			variationEffect = appContext.game.Content.Load<Effect>("VariationMask");
			System.Diagnostics.Debug.WriteLine($"Shader loaded: {variationEffect != null}");
		} catch(Exception ex) {
			System.Diagnostics.Debug.WriteLine($"Shader load error: {ex.Message}");
		}
		_roomLoader = new RoomLoader(appContext.graphicsDevice, _assetManager, appContext.gameState.QuestManager, null, variationEffect);

		_damageNumbers = new List<DamageNumber>();
		_levelUpEffects = new List<LevelUpEffect>();

		// Create pickup and door textures
		_healthPotionTexture = Graphics.CreateColoredTexture(appContext.graphicsDevice, 16, 16, Color.LimeGreen);
		_coinTexture = Graphics.CreateColoredTexture(appContext.graphicsDevice, 6, 6, Color.Gold);
		_doorTexture = Graphics.CreateColoredTexture(appContext.graphicsDevice, 1, 1, Color.White);

		// Load player texture/spritesheet
		Texture2D playerTexture = _assetManager.LoadTextureOrFallback(
			"Assets/Sprites/player.png",
			() => Graphics.CreateColoredTexture(appContext.graphicsDevice, TILE_SIZE, TILE_SIZE, Color.Yellow)
		);

		// Create placeholder player first (will be repositioned when rooms are created)
		Vector2 tempPosition = new Vector2(0, 0);

		Player player;

		// Check if we're using an animated sprite sheet or static sprite
		if(playerTexture != null) {
			int frameCount = 3;
			int frameWidth = 32;
			int frameHeight = 32;
			float frameTime = 0.1f;

			player = new Player(playerTexture, tempPosition, frameCount, frameWidth, frameHeight, frameTime, width: TILE_SIZE, height: TILE_SIZE);
		} else {
			playerTexture = Graphics.CreateColoredTexture(appContext.graphicsDevice, TILE_SIZE, TILE_SIZE, Color.Yellow);
			player = new Player(playerTexture, tempPosition, width: TILE_SIZE, height: TILE_SIZE);
		}

		appContext.gameState.setPlayer(player);

		// Initialize attack effect
		player.InitializeAttackEffect(appContext.graphicsDevice);

		// Create rooms (now that player exists)
		CreateRooms();

		// Set starting room
		_roomManager.setCurrentRoom("room1");
		_currentEnemies = _roomManager.currentRoom.enemies;
		_currentPickups = _roomManager.currentRoom.pickups;

		// Position player at spawn
		player.Position = _roomManager.currentRoom.playerSpawnPosition;

		// Set world bounds for native resolution
		appContext.Scenes.Camera.WorldBounds = new Rectangle(0, 0, _roomManager.currentRoom.map.pixelWidth, _roomManager.currentRoom.map.pixelHeight);

		var pixelTexture = Graphics.CreateColoredTexture(appContext.graphicsDevice, 1, 1, Color.White);

		// Create map editor
		_mapEditor = new MapEditor(appContext.Font, pixelTexture, appContext.Scenes.Camera, appContext.Scale, _assetManager, appContext.graphicsDevice);
		_mapEditor.SetRoom(_roomManager.currentRoom);

		_previousKeyState = Keyboard.GetState();

		player.Inventory.AddItem(EquipmentFactory.CreateIronSword());
		player.Inventory.AddItem(EquipmentFactory.CreateLeatherArmor());
		player.Inventory.AddItem(EquipmentFactory.CreateSpeedBoots());
		player.Inventory.AddItem(EquipmentFactory.CreateVampireBlade());
		player.Inventory.AddItem(EquipmentFactory.CreateCriticalRing());
		player.Inventory.AddItem(EquipmentFactory.CreateRegenerationAmulet());

		_healthBar = new UIBar(appContext.graphicsDevice, appContext.Font, 10, 10, 200, 2, Color.DarkRed, Color.Red, Color.White, Color.White,
			() => { return $"{player.health} / {player.Stats.MaxHealth}"; },
			() => { return player.health / (float)player.Stats.MaxHealth; }
		);
		_xpBar = new UIBar(appContext.graphicsDevice, appContext.Font, 10, 30, 200, 2, Color.DarkGray, Color.Gray, Color.White, Color.White,
			() => { return $"{player.XP} / {player.XPToNextLevel}"; },
			() => { return player.XP / (float)player.XPToNextLevel; }
		);
		_coinCounter = new UICounter(appContext.Font, _healthBar.width + _healthBar.x + 4, _healthBar.y, 2, Color.Gold, "$",
			() => { return $"x {player.Coins}"; }
		);
		_lvlCounter = new UICounter(appContext.Font, _xpBar.width + _xpBar.x + 4, _xpBar.y, 2, Color.White, "LV",
			() => { return $"{player.Level}"; }
		);

		DialogManager dialogManager = appContext.gameState.DialogManager;
		QuestManager questManager = appContext.gameState.QuestManager;
		LocalizationManager localizationManager = appContext.Localization;

		// === Load data ===
		dialogManager.loadDialogTrees("Assets/Dialogs/Trees/dialogs.json");
		dialogManager.loadNPCDefinitions("Assets/Dialogs/NPCs/npcs.json");
		localizationManager.loadLanguage("en", "Assets/Dialogs/Localization/en.json");

		questManager.loadQuests("Assets/Quests/quests.json");
		localizationManager.loadLanguage("en", "Assets/Quests/Localization/en.json");


		// === Subscribe to events ===
		questManager.OnQuestStarted += OnQuestStarted;
		questManager.OnQuestCompleted += OnQuestCompleted;
		questManager.OnObjectiveUpdated += OnObjectiveUpdated;
		questManager.OnNodeAdvanced += OnNodeAdvanced;
		dialogManager.OnResponseChosen += questManager.OnDialogResponseChosen;

		// === Create UI ===
		_dialogUI = new UIDialog(
			dialogManager,
			appContext.Font,
			appContext.graphicsDevice,
			appContext.Display.VirtualWidth,
			appContext.Display.VirtualHeight,
			appContext.Scale
		);

		_dialogUI.loadPortrait("npc_villager_concerned",
		appContext.assetManager.LoadTexture("Assets/Portrait/npc_villager_concerned.png"));


		foreach(var npc in _roomManager.currentRoom.NPCs) {
			npc.SetQuestManager(appContext.gameState.QuestManager);
			npc.SetFont(appContext.Font);
		}

		_gameRenderTarget = new RenderTarget2D(
				appContext.graphicsDevice,
				appContext.Display.VirtualWidth,   // 640
				appContext.Display.VirtualHeight,  // 360
				false,
				appContext.graphicsDevice.PresentationParameters.BackBufferFormat,
				DepthFormat.None,
				0,
				RenderTargetUsage.DiscardContents
			);

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
				"shepherd", appContext.gameState.QuestManager,
				3, 32, 32, 0.1f,
				width: 24, height: 24
			);
			room1.NPCs.Add(questGiver);
		}
	}


	// Event handlers for notifications
	private void OnQuestStarted(Quest quest) {
		string name = appContext.gameState.QuestManager.getQuestName(quest);
		System.Diagnostics.Debug.WriteLine($"[QUEST STARTED] {name}");
		// TODO: Show notification on screen
	}

	private void OnQuestCompleted(Quest quest) {
		string name = appContext.gameState.QuestManager.getQuestName(quest);
		System.Diagnostics.Debug.WriteLine($"[QUEST COMPLETED] {name}");
		// TODO: Show completion notification with rewards
	}

	private void OnObjectiveUpdated(Quest quest, QuestObjective objective) {
		// Optional: Show progress update
		string questName = appContext.gameState.QuestManager.getQuestName(quest);
		System.Diagnostics.Debug.WriteLine($"[QUEST] {questName} - Objective updated");
	}
	private void OnNodeAdvanced(Quest quest) {
		System.Diagnostics.Debug.WriteLine($"[QUEST] Node advanced: {quest.id}");
	}

	public override void Update(GameTime time) {
		Player _player = appContext.gameState.Player;
		QuestManager _questManager = appContext.gameState.QuestManager;
		DialogManager _dialogManager = appContext.gameState.DialogManager;
		BitmapFont _font = appContext.Font;
		Camera _camera = appContext.Scenes.Camera;

		KeyboardState currentKeyState = Keyboard.GetState();

		if(currentKeyState.IsKeyDown(Keys.Escape) && _previousKeyState.IsKeyUp(Keys.Escape)) {
			// Todo: open main menu
			//_gameState = GameState.MainMenu;
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
			// Todo: open game menu
			//_gameMenu.IsOpen = !_gameMenu.IsOpen;
		}

		// Toggle map editor with E
		if(currentKeyState.IsKeyDown(Keys.M) && _previousKeyState.IsKeyUp(Keys.M)) {
			_mapEditor.IsActive = !_mapEditor.IsActive;
			if(_mapEditor.IsActive) {
				_mapEditor.SetRoom(_roomManager.currentRoom);
			}
		}
		if(_dialogUI.isActive) {
			_dialogUI.update(time);
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
			_mapEditor.Update(time);
			_camera.Update(); // Still update camera for panning
			_previousKeyState = currentKeyState;
			return; // Don't update game when editor is active
		}


		var currentMap = _roomManager.currentRoom.map;

		// === UPDATE PROPS ===
		foreach(var prop in _roomManager.currentRoom.props) {
			prop.Update(time);

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
		_player.Update(time, currentMap);

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
			enemy.Update(time);

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
			pickup.Update(time);

			// Check if player collects it
			if(pickup.CheckCollision(_player)) {
				_player.CollectPickup(pickup);
				_questManager.updateObjectiveProgress("collect_item", pickup.ItemId, 1);
			}
		}

		foreach(var npc in _roomManager.currentRoom.NPCs) {
			npc.Update(time);
			npc.IsPlayerInRange(_player.Position);
		}

		// Remove collected pickups
		_currentPickups.RemoveAll(p => p.IsCollected);

		// Update damage numbers
		foreach(var damageNumber in _damageNumbers) {
			damageNumber.Update(time);
		}

		// Remove expired damage numbers
		_damageNumbers.RemoveAll(d => d.IsExpired);

		// Update level up effects
		foreach(var effect in _levelUpEffects) {
			effect.Update(time);
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
		float deltaTime = (float)time.ElapsedGameTime.TotalSeconds;
		_camera.FollowSmooth(_player.Position + new Vector2(_player.Width / 2f, _player.Height / 2f), deltaTime);


		_camera.Update();

		_previousKeyState = currentKeyState;

		base.Update(time);
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

	public override void Draw(SpriteBatch spriteBatch) {
		GraphicsDevice GraphicsDevice = appContext.graphicsDevice;
		GraphicsDevice.SetRenderTarget(_gameRenderTarget);
		GraphicsDevice.Clear(Color.Black);

		spriteBatch.End();
		// Draw world with camera transform
		spriteBatch.Begin(
			samplerState: SamplerState.PointClamp,
			transformMatrix: appContext.Scenes.Camera.Transform
		);

		// Draw the tilemap
		_roomManager.currentRoom.map.draw(spriteBatch, appContext.Scenes.Camera.GetVisibleArea(), appContext.Scenes.Camera.Transform);

		spriteBatch.End();
		spriteBatch.Begin(
			samplerState: SamplerState.PointClamp,
			transformMatrix: appContext.Scenes.Camera.Transform
		);

		// Draw doors
		_roomManager.currentRoom.drawDoors(spriteBatch, _doorTexture);

		// Draw pickups
		foreach(var pickup in _currentPickups) {
			pickup.Draw(spriteBatch);
		}

		List<Entity> entities = new List<Entity>();
		entities.AddRange(_roomManager.currentRoom.props);
		entities.AddRange(_currentEnemies);
		entities.AddRange(_roomManager.currentRoom.NPCs);
		entities.Add(appContext.gameState.Player);

		entities.Sort((a, b) =>
			(a.Position.Y + a.Bounds.Height)
				.CompareTo(b.Position.Y + b.Bounds.Height));

		foreach(var entity in entities) {
			entity.Draw(spriteBatch);
		}

		// Draw attack effect
		appContext.gameState.Player.DrawAttackEffect(spriteBatch);

		// Draw map editor cursor
		if(_mapEditor.IsActive) {
			var cursorRect = _mapEditor.GetCursorTileRect();
			if(cursorRect != Rectangle.Empty) {
				var editorTexture = Graphics.CreateColoredTexture(GraphicsDevice, 1, 1, Color.White);
				spriteBatch.Draw(editorTexture, cursorRect, _mapEditor.GetSelectedTileColor() * 0.5f);
			}

			var propRect = _mapEditor.GetCursorPropRect();
			if(propRect != Rectangle.Empty) {
				var editorTexture = Graphics.CreateColoredTexture(GraphicsDevice, 1, 1, Color.White);
				spriteBatch.Draw(editorTexture, propRect, _mapEditor.GetSelectedPropColor() * 0.6f);
			}
		}

		// Draw damage numbers
		foreach(var damageNumber in _damageNumbers) {
			damageNumber.Draw(spriteBatch);
		}

		// Draw level up effects
		foreach(var effect in _levelUpEffects) {
			effect.Draw(spriteBatch);
		}

		spriteBatch.End();

		// Draw UI (no camera transform)

		if(!_mapEditor.IsActive) {
			spriteBatch.Begin(samplerState: SamplerState.PointClamp);
			// Draw health and xp bar
			_healthBar.draw(spriteBatch);
			_xpBar.draw(spriteBatch);
			_coinCounter.draw(spriteBatch);
			_lvlCounter.draw(spriteBatch);
			spriteBatch.End();
		}


		// Draw menu on top of everything
		spriteBatch.Begin(samplerState: SamplerState.PointClamp);

		// Game menu (only if not in map editor)
		if(!_mapEditor.IsActive) {
			//_gameMenu.Draw(spriteBatch);
			_dialogUI.draw(spriteBatch);
		}

		// Draw map editor UI
		if(_mapEditor.IsActive) {
			_mapEditor.Draw(spriteBatch);
		}

		spriteBatch.End();

		GraphicsDevice.SetRenderTarget(null);
		GraphicsDevice.Clear(Color.Black);

		spriteBatch.Begin(samplerState: SamplerState.PointClamp);

		// Draw the 640x360 game scaled to 1920x1080
		Rectangle destinationRect = new Rectangle(0, 0, appContext.Display.VirtualWidth * appContext.Scale, appContext.Display.VirtualWidth * appContext.Scale);
		spriteBatch.Draw(_gameRenderTarget, destinationRect, Color.White);

		base.Draw(spriteBatch);
	}
}
