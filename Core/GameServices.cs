using EldmeresTale.Dialog;
using EldmeresTale.ECS.Factories;
using EldmeresTale.Entities;
using EldmeresTale.Quests;
using EldmeresTale.World;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Core;

public class GameServices {
	public Player Player { get; }
	public LocalizationManager Localization { get; }
	public GameStateManager GameState { get; }
	public ConditionEvaluator ConditionEvaluator { get; }
	public EffectExecutor EffectExecutor { get; }
	public QuestManager QuestManager { get; }
	public DialogManager DialogManager { get; }
	public RoomManager RoomManager { get; }

	// Factories
	public PickupFactory PickupFactory { get; set; }
	public PropFactory PropFactory { get; set; }

	public GameServices(
			Player player,
			LocalizationManager localization,
			AssetManager assetManager,
			GraphicsDevice graphicsDevice) {

		// Phase 1: Store core services
		Player = player;
		Localization = localization;

		// Phase 2: Create state manager
		GameState = new GameStateManager(player);

		// Phase 3: Create evaluator/executor (depend on state)
		ConditionEvaluator = new ConditionEvaluator(Player, GameState);
		EffectExecutor = new EffectExecutor(Player, GameState);

		// Phase 4: Create managers (depend on evaluator/executor)
		QuestManager = new QuestManager(
			Player,
			Localization,
			ConditionEvaluator,
			EffectExecutor
		);

		RoomManager = new RoomManager(
			graphicsDevice,
			assetManager,
			this
		);

		DialogManager = new DialogManager(
			Localization,
			GameState,
			ConditionEvaluator,
			EffectExecutor
		);

		// Phase 5: Wire up cross-references
		ConditionEvaluator.SetQuestManager(QuestManager);
		EffectExecutor.SetQuestManager(QuestManager);

		System.Diagnostics.Debug.WriteLine("[GAME SERVICES] Initialized for new game session");
	}
	public void LoadRooms() {
		RoomManager.Load();
		System.Diagnostics.Debug.WriteLine($"[GAME SERVICES] Loaded {RoomManager.Rooms.Count} rooms");
	}

	public void SetCurrentRoom(string roomId) {
		RoomManager.SetCurrentRoom(roomId);
	}
}