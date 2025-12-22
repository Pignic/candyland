using Candyland.Core;
using Candyland.Core.UI;
using Candyland.Dialog;
using Candyland.Entities;
using Candyland.Quests;
using Candyland.Systems;
using Candyland.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;

namespace Candyland.Scenes;

internal class GameScene : Scene {
	private SystemManager _systemManager;
	private VFXSystem _vfxSystem;
	private CombatSystem _combatSystem;

	// Tile settings
	private const int TILE_SIZE = 16;  // Native tile size

	// Current room entities (references to current room's lists)
	private List<Enemy> _currentEnemies;
	private List<Pickup> _currentPickups;

	// Textures
	private Texture2D _healthPotionTexture;
	private Texture2D _coinTexture;
	private Texture2D _doorTexture;

	private UIBar _healthBar;
	private UIBar _xpBar;
	private UICounter _coinCounter;
	private UICounter _lvlCounter;

	private KeyboardState _previousKeyState;


	private Player _player;
	private QuestManager _questManager;
	private DialogManager _dialogManager;
	private RoomManager _roomManager;
	private BitmapFont _font;

	public GameScene(ApplicationContext appContext, bool exclusive = true) : base(appContext, exclusive) {

		// Create camera for this scene
		camera = new Camera(
			appContext.Display.VirtualWidth,
			appContext.Display.VirtualHeight
		);
		_systemManager = new SystemManager();
	}

	public override void Load() {
		base.Load();


		// Create simple textures (these are cheap)
		_healthPotionTexture = Graphics.CreateColoredTexture(
			appContext.graphicsDevice, 16, 16, Color.LimeGreen);
		_coinTexture = Graphics.CreateColoredTexture(
			appContext.graphicsDevice, 6, 6, Color.Gold);
		_doorTexture = Graphics.CreateColoredTexture(
			appContext.graphicsDevice, 1, 1, Color.White);

		// Load player texture
		Texture2D playerTexture = appContext.assetManager.LoadTextureOrFallback(
			"Assets/Sprites/player.png",
			() => Graphics.CreateColoredTexture(
				appContext.graphicsDevice, TILE_SIZE, TILE_SIZE, Color.Yellow)
		);

		// Create player
		Vector2 tempPosition = Vector2.Zero;
		Player player;

		if(playerTexture != null && playerTexture.Width == 96) {
			// Animated sprite sheet
			int frameCount = 3;
			int frameWidth = 32;
			int frameHeight = 32;
			float frameTime = 0.1f;

			player = new Player(
				playerTexture, tempPosition,
				frameCount, frameWidth, frameHeight, frameTime,
				width: TILE_SIZE, height: TILE_SIZE
			);
		} else {
			// Static sprite
			player = new Player(
				playerTexture, tempPosition,
				width: TILE_SIZE, height: TILE_SIZE
			);
		}

		// Set player in game state
		appContext.gameState.setPlayer(player);

		_player = appContext.gameState.Player;
		_questManager = appContext.gameState.QuestManager;
		_dialogManager = appContext.gameState.DialogManager;
		_roomManager = appContext.gameState.RoomManager;
		_font = appContext.Font;

		// Initialize systems
		_vfxSystem = new VFXSystem(_font);
		_systemManager.AddSystem(_vfxSystem);
		_combatSystem = new CombatSystem(_player);
		_systemManager.AddSystem(_combatSystem);

		// Subscribe to combat events
		_combatSystem.OnEnemyHit += OnEnemyHit;
		_combatSystem.OnEnemyKilled += OnEnemyKilled;
		_combatSystem.OnPropHit += OnPropHit;
		_combatSystem.OnPropDestroyed += OnPropDestroyed;
		_combatSystem.OnPlayerHit += OnPlayerHit;



		_systemManager.Initialize();

		// Initialize attack effect
		player.InitializeAttackEffect(appContext.graphicsDevice);

		// Give player starting equipment
		player.Inventory.AddItem(EquipmentFactory.CreateIronSword());
		player.Inventory.AddItem(EquipmentFactory.CreateLeatherArmor());
		player.Inventory.AddItem(EquipmentFactory.CreateSpeedBoots());
		player.Inventory.AddItem(EquipmentFactory.CreateVampireBlade());
		player.Inventory.AddItem(EquipmentFactory.CreateCriticalRing());
		player.Inventory.AddItem(EquipmentFactory.CreateRegenerationAmulet());


		// Set starting room
		appContext.gameState.RoomManager.setCurrentRoom("room1");
		_currentEnemies = appContext.gameState.RoomManager.currentRoom.enemies;
		_currentPickups = appContext.gameState.RoomManager.currentRoom.pickups;

		// Position player at spawn
		player.Position = appContext.gameState.RoomManager.currentRoom.playerSpawnPosition;

		// Set camera bounds to match current room
		camera.WorldBounds = new Rectangle(
			0, 0,
			appContext.gameState.RoomManager.currentRoom.map.pixelWidth,
			appContext.gameState.RoomManager.currentRoom.map.pixelHeight
		);

		// Create UI elements
		_healthBar = new UIBar(
			appContext.graphicsDevice, appContext.Font,
			10, 10, 200, 2,
			Color.DarkRed, Color.Red, Color.White, Color.White,
			() => $"{player.health} / {player.Stats.MaxHealth}",
			() => player.health / (float)player.Stats.MaxHealth
		);

		_xpBar = new UIBar(
			appContext.graphicsDevice, appContext.Font,
			10, 30, 200, 2,
			Color.DarkGray, Color.Gray, Color.White, Color.White,
			() => $"{player.XP} / {player.XPToNextLevel}",
			() => player.XP / (float)player.XPToNextLevel
		);

		_coinCounter = new UICounter(
			appContext.Font,
			_healthBar.width + _healthBar.x + 4,
			_healthBar.y, 2, Color.Gold, "$",
			() => $"x {player.Coins}"
		);

		_lvlCounter = new UICounter(
			appContext.Font,
			_xpBar.width + _xpBar.x + 4,
			_xpBar.y, 2, Color.White, "LV",
			() => $"{player.Level}"
		);

		// Load dialog system
		LoadDialogSystem();

		// Set up NPCs with quest manager
		foreach(var npc in appContext.gameState.RoomManager.currentRoom.NPCs) {
			npc.SetQuestManager(appContext.gameState.QuestManager);
			npc.SetFont(appContext.Font);
		}

		// Subscribe to quest events
		appContext.gameState.QuestManager.OnQuestStarted += OnQuestStarted;
		appContext.gameState.QuestManager.OnQuestCompleted += OnQuestCompleted;
		appContext.gameState.QuestManager.OnObjectiveUpdated += OnObjectiveUpdated;
		appContext.gameState.QuestManager.OnNodeAdvanced += OnNodeAdvanced;

		_previousKeyState = Keyboard.GetState();
	}

	private void LoadDialogSystem() {
		var dialogManager = appContext.gameState.DialogManager;

		// Load dialog trees and NPCs
		dialogManager.loadDialogTrees("Assets/Dialogs/Trees/dialogs.json");
		dialogManager.loadNPCDefinitions("Assets/Dialogs/NPCs/npcs.json");
		appContext.Localization.loadLanguage("en", "Assets/Dialogs/Localization/en.json");

		appContext.gameState.QuestManager.loadQuests("Assets/Quests/quests.json");
		appContext.Localization.loadLanguage("en", "Assets/Quests/Localization/en.json");

		// Wire up quest manager to dialog manager
		appContext.gameState.QuestManager.SetDialogManager(dialogManager);

	}
	private void OnEnemyHit(Enemy enemy, int damage, bool wasCrit, Vector2 damagePos) {
		// Show damage number
		_vfxSystem.ShowDamage(damage, damagePos, wasCrit);
	}

	private void OnEnemyKilled(Enemy enemy, Vector2 position) {
		// Spawn loot
		SpawnLoot(enemy);
		enemy.HasDroppedLoot = true;

		// Update quest
		_questManager.updateObjectiveProgress("kill_enemy", enemy.EnemyType, 1);

		// Grant XP
		bool leveledUp = _player.GainXP(enemy.XPValue);
		if(leveledUp) {
			_vfxSystem.ShowLevelUp(_player.Position);
		}
	}

	private void OnPropHit(Prop prop, int damage, bool wasCrit, Vector2 damagePos) {
		// Show damage number
		_vfxSystem.ShowDamage(damage, damagePos, wasCrit);
	}

	private void OnPropDestroyed(Prop prop, Vector2 position) {
		// TODO: Spawn loot from props (if needed)
		// TODO: Update quests (if there are "destroy prop" objectives)
	}

	private void OnPlayerHit(Enemy enemy, int damage, Vector2 damagePos) {
		// Show damage number in red
		_vfxSystem.ShowDamage(damage, damagePos, false, Color.Red);
	}



	public override void OnDisplayChanged() {
		base.OnDisplayChanged();  // Updates camera viewport
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
			appContext.OpenGameMenu();
			_previousKeyState = currentKeyState;
			return;
		}

		// Toggle map editor with M
		if(currentKeyState.IsKeyDown(Keys.M) && _previousKeyState.IsKeyUp(Keys.M)) {
			appContext.OpenMapEditor(camera);
		}

		if(currentKeyState.IsKeyDown(Keys.E) && _previousKeyState.IsKeyUp(Keys.E)) {
			foreach(var npc in _roomManager.currentRoom.NPCs) {
				float distance = Vector2.Distance(_player.Position, npc.Position);
				if(distance < 50f) {
					appContext.StartDialog(npc.DialogId);
					_previousKeyState = currentKeyState;
					break;
				}
			}
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
						Vector2 damagePos = prop.Position + new Vector2(prop.Width / 2f, 0);
						_vfxSystem.ShowDamage(damage, damagePos, wasCrit);
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

			// Update combat system with new room entities
			_combatSystem.SetEnemies(_currentEnemies);
			_combatSystem.SetProps(_roomManager.currentRoom.props);

			camera.WorldBounds = new Rectangle(
				0, 0,
				_roomManager.currentRoom.map.pixelWidth,
				_roomManager.currentRoom.map.pixelHeight
			);

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
					Vector2 damagePos = enemy.Position + new Vector2(enemy.Width / 2f, 0);
					_vfxSystem.ShowDamage(enemy.AttackDamage, damagePos, false, Color.Red);
				}
			}
		}

		_previousKeyState = currentKeyState;

		_systemManager.Update(time);

		// Make camera follow player smoothly
		float deltaTime = (float)time.ElapsedGameTime.TotalSeconds;

		camera.FollowSmooth(
			appContext.gameState.Player.Position +
			new Vector2(
				appContext.gameState.Player.Width / 2f,
				appContext.gameState.Player.Height / 2f
			),
			deltaTime
		);

		camera.Update();

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
		spriteBatch.End();
		// Draw world with camera transform
		spriteBatch.Begin(
			samplerState: SamplerState.PointClamp,
			transformMatrix: camera.Transform
		);

		// Draw the tilemap
		_roomManager.currentRoom.map.draw(spriteBatch, camera.GetVisibleArea(), camera.Transform);

		spriteBatch.End();
		spriteBatch.Begin(
			samplerState: SamplerState.PointClamp,
			transformMatrix: camera.Transform
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

		// Draw damage numbers
		_vfxSystem.Draw(spriteBatch);

		spriteBatch.End();

		// Draw UI (no camera transform)

		spriteBatch.Begin(samplerState: SamplerState.PointClamp);
		// Draw health and xp bar
		_healthBar.draw(spriteBatch);
		_xpBar.draw(spriteBatch);
		_coinCounter.draw(spriteBatch);
		_lvlCounter.draw(spriteBatch);
		//spriteBatch.End();

		base.Draw(spriteBatch);
	}

	public override void Dispose() {
		if(_combatSystem != null) {
			_combatSystem.OnEnemyHit -= OnEnemyHit;
			_combatSystem.OnEnemyKilled -= OnEnemyKilled;
			_combatSystem.OnPropHit -= OnPropHit;
			_combatSystem.OnPropDestroyed -= OnPropDestroyed;
			_combatSystem.OnPlayerHit -= OnPlayerHit;
		}
		// Unsubscribe from events
		if(appContext.gameState?.QuestManager != null) {
			appContext.gameState.QuestManager.OnQuestStarted -= OnQuestStarted;
			appContext.gameState.QuestManager.OnQuestCompleted -= OnQuestCompleted;
			appContext.gameState.QuestManager.OnObjectiveUpdated -= OnObjectiveUpdated;
			appContext.gameState.QuestManager.OnNodeAdvanced -= OnNodeAdvanced;
		}
		_systemManager?.Dispose();
		base.Dispose();
	}
}
