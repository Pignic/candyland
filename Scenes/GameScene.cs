using EldmeresTale.Audio;
using EldmeresTale.Core;
using EldmeresTale.Core.Coordination;
using EldmeresTale.Core.UI;
using EldmeresTale.Dialog;
using EldmeresTale.Entities;
using EldmeresTale.Events;
using EldmeresTale.Quests;
using EldmeresTale.Systems;
using EldmeresTale.Systems.Particles;
using EldmeresTale.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Scenes;

internal class GameScene : Scene {

	private readonly GameServices _gameServices;

	private SystemManager _systemManager;
	private VFXSystem _vfxSystem;
	private ParticleSystem _particleSystem;
	private CombatSystem _combatSystem;
	private PhysicsSystem _physicsSystem;
	private LootSystem _lootSystem;
	private InputSystem _inputSystem;
	private NotificationSystem _notificationSystem;

	private EventCoordinator _eventCoordinator;
	private RoomTransitionManager _roomTransition;

	private bool _playerIsDead = false;
	private RenderTarget2D _gameRenderTarget;

	// Tile settings
	private const int TILE_SIZE = 16;  // Native tile size

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

	public GameScene(ApplicationContext appContext, GameServices gameServices, bool loadFromSave = false, string saveName = "test_save", bool exclusive = true) : base(appContext, exclusive) {

		// Create camera for this scene
		camera = new Camera(
			appContext.Display.VirtualWidth,
			appContext.Display.VirtualHeight
		);
		_gameServices = gameServices;
		_systemManager = new SystemManager();
		_inputSystem = appContext.Input;
		_loadFromSave = loadFromSave;
		_saveName = saveName;
	}

	public override void Load() {
		base.Load();

		Song dungeonTheme = appContext.assetManager.LoadMusic("Assets/Music/overworld_theme.music");
		if (dungeonTheme != null) {
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

		_player = _gameServices.Player;
		GiveStartingEquipment(_player);
		_questManager = _gameServices.QuestManager;
		_dialogManager = _gameServices.DialogManager;
		_roomManager = _gameServices.RoomManager;
		_font = appContext.Font;

		_player.OnAttack += player_OnAttack;
		_player.OnDodge += (Vector2 direction) => {
			_particleSystem.Emit(ParticleType.Dust, _player.Position, 20, direction * -1);
			appContext.SoundEffects.Play("dodge_whoosh", 0.6f);
		};
		_player.OnPlayerDeath += OnPlayerDeath;

		// Initialize systems
		_vfxSystem = new VFXSystem(_font);
		_systemManager.AddSystem(_vfxSystem);
		_particleSystem = new ParticleSystem(appContext.graphicsDevice);
		_systemManager.AddSystem(_particleSystem);
		_combatSystem = new CombatSystem(_player, appContext.EventBus);
		_systemManager.AddSystem(_combatSystem);
		_physicsSystem = new PhysicsSystem(_player, appContext.EventBus);
		_systemManager.AddSystem(_physicsSystem);
		_lootSystem = new LootSystem(_player, appContext.assetManager, appContext.graphicsDevice, appContext.EventBus);
		_systemManager.AddSystem(_lootSystem);
		_notificationSystem = new NotificationSystem(_font, appContext.Display);
		_systemManager.AddSystem(_notificationSystem);

		_systemManager.Initialize();
		_player.SetEventBus(appContext.EventBus);
		_questManager.SetEventBus(appContext.EventBus);

		_eventCoordinator = new EventCoordinator(
			appContext.EventBus,
			_particleSystem,
			_vfxSystem,
			_lootSystem,
			_questManager,
			_player,
			camera,
			appContext.SoundEffects,
			_combatSystem,
			_notificationSystem
		);
		_eventCoordinator.Initialize();

		_roomTransition = new RoomTransitionManager(
			_roomManager,
			appContext.EventBus,
			camera
		);

		_roomTransition.RegisterSystem(_combatSystem);
		_roomTransition.RegisterSystem(_physicsSystem);
		_roomTransition.RegisterSystem(_lootSystem);

		// Initialize attack effect
		_player.InitializeAttackEffect(appContext.graphicsDevice);

		// Set starting room
		if (!_loadFromSave) {
			_gameServices.RoomManager.SetCurrentRoom("room1");
		}

		Room currentRoom = _gameServices.RoomManager.CurrentRoom;
		_combatSystem.OnRoomChanged(currentRoom);
		_physicsSystem.OnRoomChanged(currentRoom);
		_lootSystem.OnRoomChanged(currentRoom);


		// Position player at spawn
		if (!_loadFromSave) {
			_player.Position = _gameServices.RoomManager.CurrentRoom.PlayerSpawnPosition;
		}

		// Set camera bounds to match current room
		camera.WorldBounds = new Rectangle(
			0, 0,
			_gameServices.RoomManager.CurrentRoom.Map.PixelWidth,
			_gameServices.RoomManager.CurrentRoom.Map.PixelHeight
		);

		// Create UI elements
		_healthBar = new UIBar(
			appContext.graphicsDevice, appContext.Font,
			10, 10, 200, 2,
			Color.DarkRed, Color.Red, Color.White, Color.White,
			() => $"{_player.health} / {_player.Stats.MaxHealth}",
			() => _player.health / (float)_player.Stats.MaxHealth
		);

		_xpBar = new UIBar(
			appContext.graphicsDevice, appContext.Font,
			10, 30, 200, 2,
			Color.DarkGray, Color.Gray, Color.White, Color.White,
			() => $"{_player.XP} / {_player.XPToNextLevel}",
			() => _player.XP / (float)_player.XPToNextLevel
		);

		_coinCounter = new UICounter(
			appContext.Font,
			_healthBar.width + _healthBar.x + 4,
			_healthBar.y, 2, Color.Gold, "$",
			() => $"x {_player.Coins}"
		);

		_lvlCounter = new UICounter(
			appContext.Font,
			_xpBar.width + _xpBar.x + 4,
			_xpBar.y, 2, Color.White, "LV",
			() => $"{_player.Level}"
		);

		// Load dialog system
		LoadDialogSystem();

		// Set up NPCs with quest manager
		foreach (NPC npc in _gameServices.RoomManager.CurrentRoom.NPCs) {
			npc.SetQuestManager(_gameServices.QuestManager);
			npc.SetFont(appContext.Font);
		}

		// Create render target for death effect
		_gameRenderTarget = new RenderTarget2D(
			appContext.graphicsDevice,
			appContext.Display.VirtualWidth,
			appContext.Display.VirtualHeight
		);
	}
	private void OnPlayerDeath() {
		_playerIsDead = true;

		appContext.GameOver(_gameRenderTarget);

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
		DialogManager dialogManager = _gameServices.DialogManager;

		// Load dialog trees and NPCs
		dialogManager.loadDialogTrees("Assets/Dialogs/Trees/dialogs.json");
		dialogManager.loadDialogTrees("Assets/Dialogs/Cutscene/cutscene.json");
		dialogManager.loadNPCDefinitions("Assets/Dialogs/NPCs/npcs.json");
		appContext.Localization.loadLanguage("en", "Assets/Dialogs/Localization/en.json");

		_gameServices.QuestManager.LoadQuests("Assets/Quests/quests.json");
		appContext.Localization.loadLanguage("en", "Assets/Quests/Localization/en.json");

		// Wire up quest manager to dialog manager
		_gameServices.QuestManager.SetDialogManager(dialogManager);

	}

	public override void Update(GameTime time) {
		InputCommands input = _inputSystem.GetCommands(camera);

		// Menu toggle
		if (input.MenuPressed) {
			appContext.OpenGameMenu(_gameServices);
			return;
		}

		//debug toggle
		if (input.ToggleDebugMode) {
			GameSettings.Instance.DebugMode = !GameSettings.Instance.DebugMode;
			GameSettings.Instance.Save();
			System.Diagnostics.Debug.WriteLine($"[DEBUG] Debug mode: {GameSettings.Instance.DebugMode}");
		}

		// Map editor toggle
		if (GameSettings.Instance.DebugMode) {
			// F5 = Save
			if (_inputSystem.GetKeyboardStateState().IsKeyDown(Keys.F5) && !_inputSystem.GetPreviousKeyboardStateState().IsKeyDown(Keys.F5)) {
				bool success = appContext.SaveManager.Save(_gameServices, "test_save");
				System.Diagnostics.Debug.WriteLine(success
					? "✅ Game saved to test_save.json!"
					: "❌ Save failed!");
			}

			// F9 = Load
			if (_inputSystem.GetKeyboardStateState().IsKeyDown(Keys.F9) && !_inputSystem.GetPreviousKeyboardStateState().IsKeyDown(Keys.F9)) {
				bool success = appContext.SaveManager.Load(_gameServices, "test_save");
				System.Diagnostics.Debug.WriteLine(success
					? "✅ Game loaded from test_save.json!"
					: "❌ Load failed!");
			}

			// F6 = Play
			if (Keyboard.GetState().IsKeyDown(Keys.F6) && _inputSystem.GetPreviousKeyboardStateState().IsKeyUp(Keys.F6)) {
				appContext.MusicPlayer.Play();
				System.Diagnostics.Debug.WriteLine("[F6] Music Play");
			}

			// F7 = Pause/Resume
			if (Keyboard.GetState().IsKeyDown(Keys.F7) && _inputSystem.GetPreviousKeyboardStateState().IsKeyUp(Keys.F7)) {
				if (appContext.MusicPlayer.IsPlaying) {
					appContext.MusicPlayer.Pause();
					System.Diagnostics.Debug.WriteLine("[F7] Music Paused");
				} else {
					appContext.MusicPlayer.Resume();
					System.Diagnostics.Debug.WriteLine("[F7] Music Resumed");
				}
			}

			// F8 = Stop
			if (Keyboard.GetState().IsKeyDown(Keys.F8) && _inputSystem.GetPreviousKeyboardStateState().IsKeyUp(Keys.F8)) {
				appContext.MusicPlayer.Stop();
				System.Diagnostics.Debug.WriteLine("[F8] Music Stopped");
			}

			// change the mood with [ and ]
			if (Keyboard.GetState().IsKeyDown(Keys.OemOpenBrackets) && _inputSystem.GetPreviousKeyboardStateState().IsKeyUp(Keys.OemOpenBrackets)) {
				currentMood++;
				currentMood %= Enum.GetNames(typeof(MoodType)).Length;
				appContext.MusicPlayer.SetMood((MoodType)currentMood);
				System.Diagnostics.Debug.WriteLine($"Mood changed to {Enum.GetNames(typeof(MoodType))[currentMood]}");
			}
			if (Keyboard.GetState().IsKeyDown(Keys.OemCloseBrackets) && _inputSystem.GetPreviousKeyboardStateState().IsKeyUp(Keys.OemCloseBrackets)) {
				currentMood--;
				currentMood %= Enum.GetNames(typeof(MoodType)).Length;
				appContext.MusicPlayer.SetMood((MoodType)currentMood);
				System.Diagnostics.Debug.WriteLine($"Mood changed to {Enum.GetNames(typeof(MoodType))[currentMood]}");
			}

			// Debug quest commands
			if (Keyboard.GetState().IsKeyDown(Keys.F1) && _inputSystem.GetPreviousKeyboardStateState().IsKeyUp(Keys.F1)) {
				_questManager.StartQuest("wolf_hunt");
			}

			if (Keyboard.GetState().IsKeyDown(Keys.F2) && _inputSystem.GetPreviousKeyboardStateState().IsKeyUp(Keys.F2)) {
				_questManager.UpdateObjectiveProgress("kill_enemy", "wolf", 1);
			}

			if (Keyboard.GetState().IsKeyDown(Keys.F3) && _inputSystem.GetPreviousKeyboardStateState().IsKeyUp(Keys.F3)) {
				_questManager.StartQuest("meet_the_elder");
			}

			if (Keyboard.GetState().IsKeyDown(Keys.F10)) {
				appContext.StartDialog("test_cutscene_simple", _gameServices);
			}

			if (input.MapEditor) {
				appContext.OpenMapEditor(camera, _gameServices);
			}
		}
		if (!_combatSystem.IsPaused) {

			// Interact with NPCs
			if (input.InteractPressed) {
				TryInteractWithNPC();
			}

			// Interact with props
			if (input.InteractPressed) {
				TryInteractWithProp();
			}

			TileMap currentMap = _roomManager.CurrentRoom.Map;

			// Update player with collision detection
			_player.Update(time, currentMap, input);

			_roomTransition.CheckAndTransition(_player);

			// Update all enemies
			foreach (Enemy enemy in _roomManager.CurrentRoom.Enemies) {
				enemy.Update(time);
			}

			foreach (NPC npc in _roomManager.CurrentRoom.NPCs) {
				npc.Update(time);
				npc.IsPlayerInRange(_player.Position);
			}

			// Remove dead enemies
			_roomManager.CurrentRoom.Enemies.RemoveAll(e => !e.IsAlive && !e.IsDying);

			// Check enemies hitting player
			foreach (Enemy enemy in _roomManager.CurrentRoom.Enemies) {
				if (enemy.IsAlive && enemy.CollidesWith(_player)) {
					Vector2 enemyCenter = enemy.Position + new Vector2(enemy.Width / 2f, enemy.Height / 2f);

					// Check if player wasn't already invincible to avoid duplicate damage numbers
					bool wasInvincible = _player.IsInvincible;
					_player.TakeDamage(enemy.AttackDamage, enemyCenter);

					// Show damage number only if damage was actually taken
					if (!wasInvincible && _player.IsInvincible) {
						Vector2 damagePos = enemy.Position + new Vector2(enemy.Width / 2f, 0);
						appContext.EventBus.Publish(new PlayerHitEvent {
							AttackingEnemy = enemy,
							Damage = enemy.AttackDamage,
							DamagePosition = damagePos,
							Position = damagePos
						});
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
		foreach (NPC npc in _roomManager.CurrentRoom.NPCs) {
			float distance = Vector2.Distance(_player.Position, npc.Position);
			if (distance < 50f) {
				appContext.SoundEffects.Play("npc_blip", 1.0f);
				appContext.StartDialog(npc.DialogId, _gameServices);
				return;
			}
		}
	}

	private void TryInteractWithProp() {
		Vector2 playerCenter = _player.Position + new Vector2(_player.Width / 2, _player.Height / 2);

		foreach (Prop prop in _roomManager.CurrentRoom.Props) {
			if (prop.type == PropType.Interactive && prop.IsPlayerInRange(playerCenter)) {
				prop.Interact();
				return;
			}
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
		_roomManager.CurrentRoom.Map.Draw(spriteBatch, camera.GetVisibleArea(), camera.Transform);

		spriteBatch.End();
		spriteBatch.Begin(
			samplerState: SamplerState.PointClamp,
			transformMatrix: camera.Transform
		);

		// Draw doors
		_roomManager.CurrentRoom.DrawDoors(spriteBatch, _doorTexture);

		// Draw pickups
		foreach (Pickup pickup in _lootSystem.Pickups) {
			pickup.Draw(spriteBatch);
		}

		List<Entity> entities = new List<Entity>();
		entities.AddRange(_roomManager.CurrentRoom.Props);
		entities.AddRange(_roomManager.CurrentRoom.Enemies);
		entities.AddRange(_roomManager.CurrentRoom.NPCs);
		entities.Add(_gameServices.Player);

		entities.Sort((a, b) =>
			(a.Position.Y + a.Bounds.Height)
				.CompareTo(b.Position.Y + b.Bounds.Height));

		foreach (Entity entity in entities) {
			entity.Draw(spriteBatch);
		}

		_particleSystem.Draw(spriteBatch);

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
		_eventCoordinator.Dispose();
		_systemManager?.Dispose();
		base.Dispose();
	}

	// Debug: Remove
	private void GiveStartingEquipment(Player player) {
		player.Inventory.AddItem(EquipmentFactory.CreateFromId("iron_sword"));
		player.Inventory.AddItem(EquipmentFactory.CreateFromId("leather_armor"));
		player.Inventory.AddItem(EquipmentFactory.CreateFromId("speed_boots"));
		player.Inventory.AddItem(EquipmentFactory.CreateFromId("vampire_blade"));
		player.Inventory.AddItem(EquipmentFactory.CreateFromId("critical_ring"));
		player.Inventory.AddItem(EquipmentFactory.CreateFromId("regeneration_amulet"));
	}
}
