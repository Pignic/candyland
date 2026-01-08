using EldmeresTale.Dialog;
using EldmeresTale.Entities;
using EldmeresTale.Quests;
using EldmeresTale.World;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.Core;

public class GameServices {
	// Singletons
	public Player Player { get; private set; }
	public LocalizationManager Localization { get; private set; }
	public GameStateManager GameState { get; private set; }
	public ConditionEvaluator ConditionEvaluator { get; private set; }
	public EffectExecutor EffectExecutor { get; private set; }
	public QuestManager QuestManager { get; private set; }
	public DialogManager DialogManager { get; private set; }
	public RoomManager RoomManager { get; private set; }

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
			GameState,
			ConditionEvaluator,
			EffectExecutor
		);

		RoomManager = new RoomManager(
			graphicsDevice,
			assetManager,
			QuestManager
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