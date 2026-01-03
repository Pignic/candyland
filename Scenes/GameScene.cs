using EldmeresTale.Audio;
using EldmeresTale.Core;
using EldmeresTale.Core.UI;
using EldmeresTale.Dialog;
using EldmeresTale.Entities;
using EldmeresTale.Quests;
using EldmeresTale.Systems;
using EldmeresTale.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Scenes;

internal class GameScene : Scene {
	private SystemManager _systemManager;
	private VFXSystem _vfxSystem;
	private CombatSystem _combatSystem;
	private PhysicsSystem _physicsSystem;
	private LootSystem _lootSystem;
	private InputSystem _inputSystem;
	private NotificationSystem _notificationSystem;

	private bool _playerIsDead = false;
	private RenderTarget2D _gameRenderTarget;

	// Tile settings
	private const int TILE_SIZE = 16;  // Native tile size

	// Current room entities (references to current room's lists)
	private List<Enemy> _currentEnemies;

	// Texture
	private Texture2D _doorTexture;

	private UIBar _healthBar;
	private UIBar _xpBar;
	private UICounter _coinCounter;
	private UICounter _lvlCounter;

	private Player _player;
	private QuestManager _questManager;
	private DialogManager _dialogManager;
	private RoomManager _roomManager;
	private BitmapFont _font;

	private bool _loadFromSave;
	private string _saveName;

	private int currentMood = 0;

	public GameScene(ApplicationContext appContext, bool loadFromSave = false, string saveName = "test_save", bool exclusive = true) : base(appContext, exclusive) {

		// Create camera for this scene
		camera = new Camera(
			appContext.Display.VirtualWidth,
			appContext.Display.VirtualHeight
		);
		_systemManager = new SystemManager();
		_inputSystem = appContext.Input;
		_loadFromSave = loadFromSave;
		_saveName = saveName;
	}

	public override void Load() {
		base.Load();

		Song dungeonTheme = appContext.assetManager.LoadMusic("Assets/Music/overworld_theme.music");
		if(dungeonTheme != null) {
			appContext.MusicPlayer.LoadSong(dungeonTheme);
			appContext.MusicPlayer.Play();
		}

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
		player.OnAttack += this.player_OnAttack;
		player.OnDodge += () => {
			appContext.SoundEffects.Play("dodge_whoosh", 0.6f);
		};
		if(_loadFromSave) {
			appContext.SaveManager.Load(appContext.gameState, _saveName);
		} else {
			GiveStartingEquipment(player);
		}

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
		_physicsSystem = new PhysicsSystem(_player);
		_systemManager.AddSystem(_physicsSystem);
		_lootSystem = new LootSystem(_player, appContext.assetManager, appContext.graphicsDevice);
		_systemManager.AddSystem(_lootSystem);
		_notificationSystem = new NotificationSystem(_font,
			appContext.Display.VirtualWidth,
			appContext.Display.VirtualHeight);
		_systemManager.AddSystem(_notificationSystem);

		// Subscribe to combat events
		_combatSystem.OnEnemyHit += OnEnemyHit;
		_combatSystem.OnEnemyKilled += OnEnemyKilled;
		_combatSystem.OnPropHit += OnPropHit;
		_combatSystem.OnPropDestroyed += OnPropDestroyed;
		_combatSystem.OnPlayerHit += OnPlayerHit;

		_physicsSystem.OnPropCollected += OnPropCollected;
		_physicsSystem.OnPropPushed += OnPropPushed;

		_lootSystem.OnPickupCollected += OnPickupCollected;
		_lootSystem.OnPickupSpawned += OnPickupSpawned;

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
		if(!_loadFromSave) {
			appContext.gameState.RoomManager.setCurrentRoom("room1");
		}
		_currentEnemies = appContext.gameState.RoomManager.currentRoom.enemies;

		_combatSystem.SetEnemies(_currentEnemies);
		_combatSystem.SetProps(appContext.gameState.RoomManager.currentRoom.props);

		_physicsSystem.SetMap(appContext.gameState.RoomManager.currentRoom.map);
		_physicsSystem.SetProps(appContext.gameState.RoomManager.currentRoom.props);
		_physicsSystem.SetEnemies(_currentEnemies);

		// Position player at spawn
		if(!_loadFromSave) {
			player.Position = appContext.gameState.RoomManager.currentRoom.playerSpawnPosition;
		}

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
		_player.OnPlayerDeath += OnPlayerDeath;

		// Create render target for death effect
		_gameRenderTarget = new RenderTarget2D(
			appContext.graphicsDevice,
			appContext.Display.VirtualWidth,
			appContext.Display.VirtualHeight
		);
	}
	private void OnPlayerDeath() {
		_playerIsDead = true;

		appContext.GameOver();

		// Start game over music
		Song gameOverTheme = appContext.assetManager.LoadMusic("Assets/Music/game_over.music");
		appContext.MusicPlayer.LoadSong(gameOverTheme);
		appContext.MusicPlayer.Play();

		System.Diagnostics.Debug.WriteLine("[DEATH] Player died - starting death sequence");
	}

	private void player_OnAttack(ActorEntity obj) {
		appContext.SoundEffects.Play("sword_woosh");
	}

	private void LoadDialogSystem() {
		var dialogManager = appContext.gameState.DialogManager;

		// Load dialog trees and NPCs
		dialogManager.loadDialogTrees("Assets/Dialogs/Trees/dialogs.json");
		dialogManager.loadDialogTrees("Assets/Dialogs/Cutscene/cutscene.json");
		dialogManager.loadNPCDefinitions("Assets/Dialogs/NPCs/npcs.json");
		appContext.Localization.loadLanguage("en", "Assets/Dialogs/Localization/en.json");

		appContext.gameState.QuestManager.loadQuests("Assets/Quests/quests.json");
		appContext.Localization.loadLanguage("en", "Assets/Quests/Localization/en.json");

		// Wire up quest manager to dialog manager
		appContext.gameState.QuestManager.SetDialogManager(dialogManager);

	}
	private void OnEnemyHit(Enemy enemy, int damage, bool wasCrit, Vector2 damagePos) {
		// Show damage number
		Color damageColor = wasCrit ? Color.Orange : Color.White;
		_vfxSystem.ShowDamage(damage, damagePos, wasCrit, damageColor);
		if(wasCrit) {
			appContext.SoundEffects.Play("crit_attack", 0.5f);
			camera.Shake(2f, 0.15f);
			_combatSystem.Pause(0.08f);
		}
		appContext.SoundEffects.Play("monster_hurt_mid", 0.5f);
	}

	private void OnEnemyKilled(Enemy enemy, Vector2 position) {
		// Spawn loot
		_lootSystem.SpawnLootFromEnemy(enemy);
		enemy.HasDroppedLoot = true;
		camera.Shake(2f, 0.15f); 
		_combatSystem.Pause(0.06f);
		// Update quest
		_questManager.updateObjectiveProgress("kill_enemy", enemy.EnemyType, 1);

		// Grant XP
		bool leveledUp = _player.GainXP(enemy.XPValue);
		if(leveledUp) {
			_vfxSystem.ShowLevelUp(_player.Position);
			appContext.SoundEffects.Play("level_up", 1.0f);
		}
		appContext.SoundEffects.Play("monster_growl_mid", 0.8f);
	}

	private void OnPropHit(Prop prop, int damage, bool wasCrit, Vector2 damagePos) {
		// Show damage number
		_vfxSystem.ShowDamage(damage, damagePos, wasCrit, Color.Gray);
		appContext.SoundEffects.Play("material_hit", 0.5f);
	}

	private void OnPropDestroyed(Prop prop, Vector2 position) {
		appContext.SoundEffects.Play("equip_armor", 0.6f);
		if(prop.type == PropType.Breakable) {
			var random = new Random();
			if(random.NextDouble() < 0.7) {
				_lootSystem.SpawnPickup(PickupType.Coin, position);
			}
			if(random.NextDouble() < 0.3) {
				_lootSystem.SpawnPickup(PickupType.HealthPotion, position);
			}
		}

		_questManager.updateObjectiveProgress("destroy_prop", prop.type.ToString(), 1);
	}
	private void OnPropCollected(Prop prop) {
		System.Diagnostics.Debug.WriteLine($"Collected prop: {prop.type}");
		appContext.SoundEffects.Play("buy_item", 0.7f);
		_questManager.updateObjectiveProgress("collect_item", prop.type.ToString(), 1);
	}

	private void OnPropPushed(Prop prop, Vector2 direction) {
		appContext.SoundEffects.Play("equip_armor", 0.3f);
		System.Diagnostics.Debug.WriteLine($"Pushed prop: {prop.type}");
	}

	private void OnPlayerHit(Enemy enemy, int damage, Vector2 damagePos) {
		_vfxSystem.ShowDamage(damage, damagePos, false, Color.Red);
		_combatSystem.Pause(0.12f);
		camera.Shake(5f, 0.2f);
		appContext.SoundEffects.Play("player_hurt", 1.0f);
	}

	private void OnPickupCollected(Pickup pickup) {
		// Apply pickup effect
		_player.CollectPickup(pickup);

		string sound = pickup.Type switch {
			PickupType.HealthPotion => "use_potion",
			_ => "buy_item"  // Coins
		};
		appContext.SoundEffects.Play(sound, 0.8f);
		// Update quest
		_questManager.updateObjectiveProgress("collect_item", pickup.ItemId, 1);

		System.Diagnostics.Debug.WriteLine($"[LOOT] Collected {pickup.Type}");
	}
	private void OnPickupSpawned(Pickup pickup) {
		appContext.SoundEffects.Play("buy_item", 0.2f);
		System.Diagnostics.Debug.WriteLine($"[LOOT] Spawned {pickup.Type}");
	}


	public override void OnDisplayChanged() {
		base.OnDisplayChanged();  // Updates camera viewport
	}

	// Event handlers for notifications
	private void OnQuestStarted(Quest quest) {
		string name = appContext.gameState.QuestManager.getQuestName(quest);
		System.Diagnostics.Debug.WriteLine($"[QUEST STARTED] {name}");
		appContext.SoundEffects.Play("menu_accept", 0.9f);
		_notificationSystem.ShowQuestStarted(name);
	}

	private void OnQuestCompleted(Quest quest, QuestNode lastNode) {
		string name = appContext.gameState.QuestManager.getQuestName(quest);
		System.Diagnostics.Debug.WriteLine($"[QUEST COMPLETED] {name}");
		appContext.SoundEffects.Play("level_up", 1.0f);
		_notificationSystem.ShowQuestCompleted(name, lastNode.rewards.xp, lastNode.rewards.gold);
	}

	private void OnObjectiveUpdated(Quest quest, QuestObjective objective) {
		// Optional: Show progress update
		string questName = appContext.gameState.QuestManager.getQuestName(quest);
		appContext.SoundEffects.Play("menu_move", 0.5f);
		System.Diagnostics.Debug.WriteLine($"[QUEST] {questName} - Objective updated");
	}
	private void OnNodeAdvanced(Quest quest) {
		appContext.SoundEffects.Play("menu_move", 0.4f);
		System.Diagnostics.Debug.WriteLine($"[QUEST] Node advanced: {quest.id}");
	}

	public override void Update(GameTime time) {
		InputCommands input = _inputSystem.GetCommands(camera);

		// Menu toggle
		if(input.MenuPressed) {
			appContext.OpenGameMenu();
			return;
		}

		//debug toggle
		if(input.ToggleDebugMode) {
			GameSettings.Instance.DebugMode = !GameSettings.Instance.DebugMode;
			GameSettings.Instance.Save();
			System.Diagnostics.Debug.WriteLine($"[DEBUG] Debug mode: {GameSettings.Instance.DebugMode}");
		}

		// Map editor toggle
		if(GameSettings.Instance.DebugMode) {
			// F5 = Save
			if(_inputSystem.GetKeyboardStateState().IsKeyDown(Keys.F5) && !_inputSystem.GetPreviousKeyboardStateState().IsKeyDown(Keys.F5)) {
				bool success = appContext.SaveManager.Save(appContext.gameState, "test_save");
				System.Diagnostics.Debug.WriteLine(success
					? "✅ Game saved to test_save.json!"
					: "❌ Save failed!");
			}

			// F9 = Load
			if(_inputSystem.GetKeyboardStateState().IsKeyDown(Keys.F9) && !_inputSystem.GetPreviousKeyboardStateState().IsKeyDown(Keys.F9)) {
				bool success = appContext.SaveManager.Load(appContext.gameState, "test_save");
				System.Diagnostics.Debug.WriteLine(success
					? "✅ Game loaded from test_save.json!"
					: "❌ Load failed!");
			}

			// F6 = Play
			if(Keyboard.GetState().IsKeyDown(Keys.F6) && _inputSystem.GetPreviousKeyboardStateState().IsKeyUp(Keys.F6)) {
				appContext.MusicPlayer.Play();
				System.Diagnostics.Debug.WriteLine("[F6] Music Play");
			}

			// F7 = Pause/Resume
			if(Keyboard.GetState().IsKeyDown(Keys.F7) && _inputSystem.GetPreviousKeyboardStateState().IsKeyUp(Keys.F7)) {
				if(appContext.MusicPlayer.IsPlaying) {
					appContext.MusicPlayer.Pause();
					System.Diagnostics.Debug.WriteLine("[F7] Music Paused");
				} else {
					appContext.MusicPlayer.Resume();
					System.Diagnostics.Debug.WriteLine("[F7] Music Resumed");
				}
			}

			// F8 = Stop
			if(Keyboard.GetState().IsKeyDown(Keys.F8) && _inputSystem.GetPreviousKeyboardStateState().IsKeyUp(Keys.F8)) {
				appContext.MusicPlayer.Stop();
				System.Diagnostics.Debug.WriteLine("[F8] Music Stopped");
			}

			// change the mood with [ and ]
			if(Keyboard.GetState().IsKeyDown(Keys.OemOpenBrackets) && _inputSystem.GetPreviousKeyboardStateState().IsKeyUp(Keys.OemOpenBrackets)) {
				currentMood++;
				currentMood %= Enum.GetNames(typeof(MoodType)).Length;
				appContext.MusicPlayer.SetMood((MoodType)currentMood);
				System.Diagnostics.Debug.WriteLine($"Mood changed to {Enum.GetNames(typeof(MoodType))[currentMood]}");
			}
			if(Keyboard.GetState().IsKeyDown(Keys.OemCloseBrackets) && _inputSystem.GetPreviousKeyboardStateState().IsKeyUp(Keys.OemCloseBrackets)) {
				currentMood--;
				currentMood %= Enum.GetNames(typeof(MoodType)).Length;
				appContext.MusicPlayer.SetMood((MoodType)currentMood);
				System.Diagnostics.Debug.WriteLine($"Mood changed to {Enum.GetNames(typeof(MoodType))[currentMood]}");
			}

			// Debug quest commands
			if(Keyboard.GetState().IsKeyDown(Keys.F1) && _inputSystem.GetPreviousKeyboardStateState().IsKeyUp(Keys.F1)) {
				_questManager.startQuest("wolf_hunt");
			}

			if(Keyboard.GetState().IsKeyDown(Keys.F2) && _inputSystem.GetPreviousKeyboardStateState().IsKeyUp(Keys.F2)) {
				_questManager.updateObjectiveProgress("kill_enemy", "wolf", 1);
			}

			if(Keyboard.GetState().IsKeyDown(Keys.F3) && _inputSystem.GetPreviousKeyboardStateState().IsKeyUp(Keys.F3)) {
				_questManager.startQuest("meet_the_elder");
			}

			if(Keyboard.GetState().IsKeyDown(Keys.F10)) {
				appContext.StartDialog("test_cutscene_simple");
			}

			if(input.MapEditor) {
				appContext.OpenMapEditor(camera);
			}
		}
		if(!_combatSystem.IsPaused) {

			// Interact with NPCs
			if(input.InteractPressed) {
				TryInteractWithNPC();
			}

			// Interact with props
			if(input.InteractPressed) {
				TryInteractWithProp();
			}

			var currentMap = _roomManager.currentRoom.map;

			// Update player with collision detection
			_player.Update(time, currentMap, input);


			CheckDoorTransitions();

			// Update all enemies
			foreach(var enemy in _currentEnemies) {
				enemy.Update(time);
			}

			foreach(var npc in _roomManager.currentRoom.NPCs) {
				npc.Update(time);
				npc.IsPlayerInRange(_player.Position);
			}

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
						OnPlayerHit(enemy, enemy.AttackDamage, damagePos);
					}
				}
			}
		}

		_systemManager.Update(time);

		// Make camera follow player smoothly
		float deltaTime = (float)time.ElapsedGameTime.TotalSeconds;

		camera.FollowSmooth(_player.Position + new Vector2(_player.Width / 2f, _player.Height / 2f), deltaTime);

		camera.Update();

		base.Update(time);
	}

	private void TryInteractWithNPC() {
		foreach(var npc in _roomManager.currentRoom.NPCs) {
			float distance = Vector2.Distance(_player.Position, npc.Position);
			if(distance < 50f) {
				appContext.SoundEffects.Play("npc_blip", 1.0f);
				appContext.StartDialog(npc.DialogId);
				return;
			}
		}
	}

	private void TryInteractWithProp() {
		Vector2 playerCenter = _player.Position + new Vector2(_player.Width / 2, _player.Height / 2);

		foreach(var prop in _roomManager.currentRoom.props) {
			if(prop.type == PropType.Interactive && prop.IsPlayerInRange(playerCenter)) {
				prop.Interact();
				return;
			}
		}
	}

	private void CheckDoorTransitions() {
		var door = _roomManager.currentRoom.checkDoorCollision(_player.Bounds);
		if(door == null) return;

		System.Diagnostics.Debug.WriteLine($"Transitioning from {_roomManager.currentRoom.id} to {door.targetRoomId}");

		_roomManager.transitionToRoom(door.targetRoomId, _player, door.targetDoorDirection);
		_currentEnemies = _roomManager.currentRoom.enemies;

		// Update all systems with new room
		_combatSystem.SetEnemies(_currentEnemies);
		_combatSystem.SetProps(_roomManager.currentRoom.props);

		_physicsSystem.SetMap(_roomManager.currentRoom.map);
		_physicsSystem.SetProps(_roomManager.currentRoom.props);
		_physicsSystem.SetEnemies(_currentEnemies);

		_lootSystem.Clear();

		camera.WorldBounds = new Rectangle(
			0, 0,
			_roomManager.currentRoom.map.pixelWidth,
			_roomManager.currentRoom.map.pixelHeight
		);

		System.Diagnostics.Debug.WriteLine($"Now in room: {_roomManager.currentRoom.id}, Player pos: {_player.Position}");
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
		foreach(var pickup in _lootSystem.Pickups) {
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

		_notificationSystem.Draw(spriteBatch);

		base.Draw(spriteBatch);
	}

	public override void Dispose() {

		// Unsubscribe from events
		if(_combatSystem != null) {
			_combatSystem.OnEnemyHit -= OnEnemyHit;
			_combatSystem.OnEnemyKilled -= OnEnemyKilled;
			_combatSystem.OnPropHit -= OnPropHit;
			_combatSystem.OnPropDestroyed -= OnPropDestroyed;
			_combatSystem.OnPlayerHit -= OnPlayerHit;
		}
		if(_physicsSystem != null) {
			_physicsSystem.OnPropCollected -= OnPropCollected;
			_physicsSystem.OnPropPushed -= OnPropPushed;
		}
		if(_lootSystem != null) {
			_lootSystem.OnPickupCollected -= OnPickupCollected;
			_lootSystem.OnPickupSpawned -= OnPickupSpawned;
		}
		if(appContext.gameState?.QuestManager != null) {
			appContext.gameState.QuestManager.OnQuestStarted -= OnQuestStarted;
			appContext.gameState.QuestManager.OnQuestCompleted -= OnQuestCompleted;
			appContext.gameState.QuestManager.OnObjectiveUpdated -= OnObjectiveUpdated;
			appContext.gameState.QuestManager.OnNodeAdvanced -= OnNodeAdvanced;
		}
		_systemManager?.Dispose();
		base.Dispose();
	}

	// Debug: Remove
	private void GiveStartingEquipment(Player player) {
		player.Inventory.AddItem(EquipmentFactory.CreateIronSword());
		player.Inventory.AddItem(EquipmentFactory.CreateLeatherArmor());
		player.Inventory.AddItem(EquipmentFactory.CreateSpeedBoots());
		player.Inventory.AddItem(EquipmentFactory.CreateVampireBlade());
		player.Inventory.AddItem(EquipmentFactory.CreateCriticalRing());
		player.Inventory.AddItem(EquipmentFactory.CreateRegenerationAmulet());
	}
}
