using DefaultEcs;
using EldmeresTale.Dialog;
using EldmeresTale.ECS.Factories;
using EldmeresTale.Entities;
using EldmeresTale.Quests;

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
	public RoomTransitionFactory RoomTransitionFactory { get; set; }
	public PickupFactory PickupFactory { get; set; }
	public PropFactory PropFactory { get; set; }
	public EnemyFactory EnemyFactory { get; set; }
	public NPCsFactory NPCsFactory { get; set; }

	public World World { get; set; }

	public GameServices(
			Player player,
			LocalizationManager localization,
			AssetManager assetManager) {

		// Phase 1: Store core services
		Player = player;
		Localization = localization;

		// Phase 2: Create state manager
		GameState = new GameStateManager(Player);

		// Phase 3: Create evaluator/executor (depend on state)
		ConditionEvaluator = new ConditionEvaluator(Player, GameState);
		EffectExecutor = new EffectExecutor(Player, GameState);

		// Phase 4: Create managers (depend on evaluator/executor)
		QuestManager = new QuestManager(
			Localization,
			ConditionEvaluator,
			EffectExecutor
		);

		RoomManager = new RoomManager(
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
}