using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.Audio;
using EldmeresTale.Core;
using EldmeresTale.Core.Coordination;
using EldmeresTale.Core.UI;
using EldmeresTale.Dialog;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Command;
using EldmeresTale.ECS.Factories;
using EldmeresTale.ECS.Systems;
using EldmeresTale.Entities;
using EldmeresTale.Entities.Factories;
using EldmeresTale.Quests;
using EldmeresTale.Systems;
using EldmeresTale.Worlds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace EldmeresTale.Scenes;

internal class GameScene : Scene {

	// ECS
	private readonly World _world;
	private readonly SequentialSystem<float> _updateSystems;
	private readonly SequentialSystem<SpriteBatch> _renderSystems;

	// Factories
	private readonly PickupFactory _pickupFactory;
	private readonly PropFactory _propFactory;
	private readonly EnemyFactory _enemyFactory;
	private readonly ParticleEmitter _particleEmitter;
	private readonly NPCsFactory _npcsFactory;

	// Logic systems
	private readonly RoomActivationSystem _roomActivationSystem;
	private readonly PickupCollectionSystem _pickupCollectionSystem;
	private readonly InteractionSystem _interactionSystem;
	private readonly CollisionSystem _collisionSystem;

	// Entity sets
	private readonly EntitySet _interactionRequests;

	private readonly GameServices _gameServices;

	private readonly SystemManager _systemManager;
	private VFXSystem _vfxSystem;
	private readonly MovementSystem _movementSystem;
	private readonly InputSystem _inputSystem;
	private NotificationSystem _notificationSystem;

	private EventCoordinator _eventCoordinator;
	private RoomTransitionManager _roomTransition;

	private RenderTarget2D _gameRenderTarget;

	// Texture
	private Texture2D _doorTexture;

	private UIBar _healthBar;
	private UIBar _xpBar;
	private UICounter _coinCounter;
	private UICounter _lvlCounter;

	private readonly Player _player;
	private QuestManager _questManager;
	private RoomManager _roomManager;
	private readonly BitmapFont _font;

	private readonly bool _loadFromSave;
	private readonly string _saveName;

	private int currentMood = 0;

	public GameScene(ApplicationContext appContext, GameServices gameServices, bool loadFromSave = false, string saveName = "test_save", bool exclusive = true) : base(appContext, exclusive) {
		_font = appContext.Font;

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

		_world = new World();
		_player = _gameServices.Player;
		_player.Entity = _world.CreateEntity();
		_player.Entity.Set(new Faction(FactionName.Player));
		_player.Entity.Set(new Position(_player.Position));
		_player.Entity.Set(new Velocity());
		_player.Entity.Set(new Collider(_player.Width / 2, _player.Height / 2));
		_player.Entity.Set(new CombatStats {
			AttackDamage = _player.AttackDamage,
			AttackAngle = (float)(Math.PI / 2),
			AttackRange = 50,
			AttackCooldown = 0.5f,
			MovementSpeed = 100
		});
		_player.Entity.Set(new Health(100));
		_player.Entity.Set(new RoomId());
		_player.Entity.Set(new Sprite(appContext.AssetManager.LoadTexture("Assets/Sprites/player.png")));
		_player.Entity.Set(new ECS.Components.Animation(3, 32, 32, 0.1f, true, true));

		// Create factory
		_pickupFactory = new PickupFactory(_world, appContext.AssetManager);
		_propFactory = new PropFactory(_world, appContext.AssetManager);
		_particleEmitter = new ParticleEmitter(_world);
		_enemyFactory = new EnemyFactory(_world, appContext.AssetManager);
		_npcsFactory = new NPCsFactory(_world, appContext.AssetManager);

		// TODO: initialize this properly
		gameServices.DialogManager.NPCsFactory = _npcsFactory;

		// Create ECS systems
		_roomActivationSystem = new RoomActivationSystem(_world);
		_interactionSystem = new InteractionSystem(_world, _player);
		_collisionSystem = new CollisionSystem(_world);

		// Todo: remove reference, refactor collisionSystem
		_movementSystem = new MovementSystem(_world, _collisionSystem);
		_pickupCollectionSystem = new PickupCollectionSystem(_world, _player);
		_pickupCollectionSystem.OnPickupCollected += OnPickupCollected;

		_interactionRequests = _world.GetEntities().With<InteractionRequest>().AsSet();

		_updateSystems = new SequentialSystem<float>(
			_roomActivationSystem,
			new BobAnimationSystem(_world),
			_pickupCollectionSystem,
			new AISystem(_world, _player),
			new EnemyCombatSystem(_world, _player),
			new AttackSystem(_world, camera),
			new DamageSystem(_world),
			_movementSystem,
			new HealthSystem(_world),
			new DeathSystem(_world, _particleEmitter, _pickupFactory),
			new DeathAnimationSystem(_world),
			new SoundSystem(_world, appContext.SoundEffects),
			new AnimationSystem(_world),
			new ParticlePhysicsSystem(_world),

			new LifetimeSystem(_world)
		);

		_renderSystems = new SequentialSystem<SpriteBatch>(
			new SpriteRenderSystem(_world, appContext.AssetManager.DefaultTexture),
			new ParticleRenderSystem(_world, appContext.AssetManager.DefaultTexture),
			new AttackVisualizationSystem(_world, appContext.AssetManager.DefaultTexture),
			new IndicatorSystem(_world, _font, gameServices.QuestManager)
		);

		_gameServices.PickupFactory = _pickupFactory;
		_gameServices.PropFactory = _propFactory;
		_gameServices.EnemyFactory = _enemyFactory;
		_gameServices.NPCsFactory = _npcsFactory;

		// Subscribe to enemy death events (spawn loot)
		_world.Subscribe<EnemyDeathEvent>(OnEnemyDeath);
	}

	public override void Load() {
		base.Load();
		Song dungeonTheme = appContext.AssetManager.LoadMusic("Assets/Music/overworld_theme.music");
		if (dungeonTheme != null) {
			appContext.MusicPlayer.LoadSong(dungeonTheme);
			appContext.MusicPlayer.Play();
		}

		_doorTexture = Graphics.CreateColoredTexture(
			appContext.GraphicsDevice, 1, 1, Color.White);

		_pickupCollectionSystem.SetPlayer(_player);

		_questManager = _gameServices.QuestManager;
		_roomManager = _gameServices.RoomManager;
		_roomManager.SetWorld(_world);

		_player.OnAttack += Player_OnAttack;
		_player.OnDodge += (Vector2 direction) => {
			_particleEmitter.SpawnDustCloud(_roomManager.CurrentRoom.Id, _player.Position, direction * -1, 15);
		};
		_player.OnPlayerDeath += OnPlayerDeath;

		// Initialize systems
		_vfxSystem = new VFXSystem(_font);
		_systemManager.AddSystem(_vfxSystem);
		_notificationSystem = new NotificationSystem(_font, appContext.Display);
		_systemManager.AddSystem(_notificationSystem);

		_systemManager.Initialize();
		_player.SetEventBus(appContext.EventBus);
		_questManager.SetEventBus(appContext.EventBus);

		_eventCoordinator = new EventCoordinator(
			appContext.EventBus,
			_vfxSystem,
			_questManager,
			_player,
			camera,
			appContext.SoundEffects,
			_notificationSystem,
			_movementSystem
		);

		_roomTransition = new RoomTransitionManager(
			_roomManager,
			appContext.EventBus,
			camera,
			_roomActivationSystem
		);

		_eventCoordinator.Initialize();


		// Initialize attack effect
		_player.InitializeAttackEffect(appContext.GraphicsDevice);

		// Set starting room
		if (!_loadFromSave) {
			_roomTransition.SetRoom("room1", _player);
		} else {
			appContext.SaveManager.Load(_gameServices, _saveName);
		}

		if (!_loadFromSave) {
			GiveStartingEquipment(_player);
		}

		// Create UI elements
		_healthBar = new UIBar(
			10, 10, 200, 2,
			Color.DarkRed, Color.Red, Color.White, Color.White,
			() => $"{_player.Health} / {_player.Stats.MaxHealth}",
			() => _player.Health / (float)_player.Stats.MaxHealth
		);

		_xpBar = new UIBar(
			10, 30, 200, 2,
			Color.DarkGray, Color.Gray, Color.White, Color.White,
			() => $"{_player.XP} / {_player.XPToNextLevel}",
			() => _player.XP / (float)_player.XPToNextLevel
		);

		_coinCounter = new UICounter(
			_healthBar.Width + _healthBar.X + 4,
			_healthBar.Y + 2, 2, Color.Gold, "$",
			() => $"x {_player.Coins}"
		);

		_lvlCounter = new UICounter(
			_xpBar.Width + _xpBar.X + 4,
			_xpBar.Y + 2, 2, Color.White, "LV",
			() => $"{_player.Level}"
		);

		// Load dialog system
		LoadDialogSystem();

		// Create render target for death effect
		_gameRenderTarget = new RenderTarget2D(
			appContext.GraphicsDevice,
			appContext.Display.VirtualWidth,
			appContext.Display.VirtualHeight
		);
	}
	private void OnPlayerDeath() {
		appContext.GameOver(_gameRenderTarget);

		// Start game over music
		Song gameOverTheme = appContext.AssetManager.LoadMusic("Assets/Music/game_over.music");
		appContext.MusicPlayer.LoadSong(gameOverTheme);
		appContext.MusicPlayer.Play();

		System.Diagnostics.Debug.WriteLine("[DEATH] Player died - starting death sequence");
	}

	private void Player_OnAttack(ActorEntity obj) {
		appContext.SoundEffects.Play("sword_woosh");
	}

	private void LoadDialogSystem() {
		DialogManager dialogManager = _gameServices.DialogManager;

		// Load dialog trees and NPCs
		dialogManager.LoadDialogTrees("Assets/Dialogs/Trees/dialogs.json");
		dialogManager.LoadDialogTrees("Assets/Dialogs/Cutscene/cutscene.json");
		appContext.Localization.LoadLanguage("en", "Assets/Dialogs/Localization/en.json");

		_gameServices.QuestManager.LoadQuests("Assets/Quests/quests.json");
		appContext.Localization.LoadLanguage("en", "Assets/Quests/Localization/en.json");

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

		TileMap currentMap = _roomManager.CurrentRoom.Map;

		// Update player with collision detection
		_player.Update(time, currentMap, input);

		_roomTransition.CheckAndTransition(_player);

		//foreach (NPC npc in _roomManager.CurrentRoom.NPCs) {
		//	npc.Update(time);
		//	npc.IsPlayerInRange(_player.Position);
		//}

		_systemManager.Update(time);

		// Make camera follow player smoothly
		float deltaTime = (float)time.ElapsedGameTime.TotalSeconds;

		camera.FollowSmooth(_player.Position + new Vector2(_player.Width / 2f, _player.Height / 2f), deltaTime);

		camera.Update();

		_updateSystems.Update(deltaTime);
		_interactionSystem.Update(input);


		// Interact with NPCs
		if (input.InteractPressed) {
			TryInteractWithNPC();
		}

		base.Update(time);
	}

	private void TryInteractWithNPC() {
		foreach (Entity entity in _interactionRequests.GetEntities()) {
			InteractionRequest interactionRequest = entity.Get<InteractionRequest>();
			if (entity.Has<Faction>()) {
				Faction faction = entity.Get<Faction>();
				if (faction.Name == FactionName.NPC) {
					appContext.SoundEffects.Play("npc_blip", 1.0f);
					appContext.StartDialog(interactionRequest.InteractionId, _gameServices);
				}
			}
			entity.Remove<InteractionRequest>();
		}
	}

	public override void Draw(SpriteBatch spriteBatch) {
		GraphicsDevice GraphicsDevice = appContext.GraphicsDevice;
		spriteBatch.End();

		Matrix fake3D = Matrix.CreateScale(1f, 1f, 1f) *
			new Matrix(
				1, 0, 0, 0,
				0, 1, 0, 0,
				0, 0, 1, 0,
				0, 0, 0, 1
			);

		// Draw world with camera transform
		spriteBatch.Begin(
			samplerState: SamplerState.PointClamp,
			transformMatrix: camera.Transform * fake3D
		);

		// Draw the tilemap
		_roomManager.CurrentRoom.Map.Draw(spriteBatch, camera.GetVisibleArea(), camera.Transform * fake3D);

		spriteBatch.End();

		spriteBatch.Begin(
			samplerState: SamplerState.PointClamp,
			transformMatrix: camera.Transform * fake3D,
			blendState: BlendState.AlphaBlend
		);

		// Draw doors
		_roomManager.CurrentRoom.DrawDoors(spriteBatch, _doorTexture);

		_renderSystems.Update(spriteBatch);

		_gameServices.Player.Draw(spriteBatch);

		// Draw damage numbers
		_vfxSystem.Draw(spriteBatch);

		spriteBatch.End();

		// Draw UI (no camera transform)

		spriteBatch.Begin(samplerState: SamplerState.PointClamp);
		// Draw health and xp bar
		_healthBar.Draw(spriteBatch);
		_xpBar.Draw(spriteBatch);
		_coinCounter.Draw(spriteBatch);
		_lvlCounter.Draw(spriteBatch);

		_notificationSystem.Draw(spriteBatch);

		base.Draw(spriteBatch);
	}

	public override void Dispose() {
		_updateSystems?.Dispose();
		_renderSystems?.Dispose();
		_world?.Dispose();
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
	private void OnPickupCollected(PickupType type, int value) {
		// Play sound
		appContext.SoundEffects.Play(type == PickupType.Coin ? "coin" : "pickup", 1.0f);

		// Show notification
		string message = type switch {
			PickupType.Health => $"+{value} Health",
			PickupType.Coin => $"+{value} Coins",
			PickupType.XP => $"+{value} XP",
			_ => ""
		};
	}

	private void OnEnemyDeath(in EnemyDeathEvent evt) {
		// Spawn loot at enemy position
		Vector2 dropPos = evt.Position;

		// Random coin drop
		if (Random.Shared.NextDouble() < evt.CoinDropChance) {
			int coins = Random.Shared.Next(evt.CoinMin, evt.CoinMax + 1);
			_pickupFactory.CreatePickup(PickupType.Coin, dropPos, _roomManager.CurrentRoom.Id, coins);
		}

		// Random health drop
		if (Random.Shared.NextDouble() < evt.HealthDropChance) {
			_pickupFactory.CreatePickup(PickupType.Health, dropPos + new Vector2(10, 0), _roomManager.CurrentRoom.Id, 20);
		}

		// Always drop XP
		_pickupFactory.CreatePickup(PickupType.XP, dropPos + new Vector2(-10, 0), _roomManager.CurrentRoom.Id, evt.XPValue);
	}
}
public struct EnemyDeathEvent {
	public Vector2 Position;
	public int XPValue;
	public int CoinMin;
	public int CoinMax;
	public float CoinDropChance;
	public float HealthDropChance;
}
