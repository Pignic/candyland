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

	private AssetManager assetManager;

	private GraphicsDevice graphicDevice;

	// Singleton instance
	public static GameServices Instance;

	private GameServices() { }

	public static GameServices Initialize(ApplicationContext appContext) {
		if (Instance != null) {
			throw new System.Exception("GameServices already initialized!");
		}
		Instance = new GameServices {
			Localization = appContext.Localization,
			assetManager = appContext.assetManager,
			graphicDevice = appContext.graphicsDevice
		};
		return Instance;
	}

	public GameServices setPlayer(Player player) {
		// Phase 1: Create core services
		Instance.Player = player;
		Instance.GameState = new GameStateManager();

		// Phase 2: Create evaluator/executor (depend on core services)
		Instance.ConditionEvaluator = new ConditionEvaluator(
			Instance.Player,
			Instance.GameState
		);
		Instance.EffectExecutor = new EffectExecutor(
			Instance.Player,
			Instance.GameState
		);

		// Phase 3: Create managers (depend on evaluator/executor)
		Instance.QuestManager = new QuestManager(
			Instance.Player,
			Instance.Localization,
			Instance.GameState,
			Instance.ConditionEvaluator,
			Instance.EffectExecutor
		);

		Instance.RoomManager = new RoomManager(
			Instance.graphicDevice,
			Instance.assetManager,
			Instance.QuestManager);

		Instance.RoomManager.Load();

		Instance.DialogManager = new DialogManager(
			Instance.Localization,
			Instance.GameState,
			Instance.ConditionEvaluator,
			Instance.EffectExecutor
		);

		// Phase 4: Wire up cross-references
		Instance.ConditionEvaluator.SetQuestManager(Instance.QuestManager);
		Instance.EffectExecutor.SetQuestManager(Instance.QuestManager);

		return Instance;
	}

	public static void Reset() {
		Instance = null;
	}
}