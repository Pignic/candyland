using Candyland.Dialog;
using Candyland.Entities;
using Candyland.Quests;
using Candyland.World;
using Microsoft.Xna.Framework.Graphics;

namespace Candyland.Core;

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
	private static GameServices _instance;
	public static GameServices Instance => _instance;

	private GameServices() { }

	public static GameServices Initialize(ApplicationContext appContext) {
		if(_instance != null) {
			throw new System.Exception("GameServices already initialized!");
		}
		_instance = new GameServices();
		_instance.Localization = appContext.Localization;
		_instance.assetManager = appContext.assetManager;
		_instance.graphicDevice = appContext.graphicsDevice;
		return _instance;
	}

	public GameServices setPlayer(Player player) {
		// Phase 1: Create core services
		_instance.Player = player;
		_instance.GameState = new GameStateManager();

		// Phase 2: Create evaluator/executor (depend on core services)
		_instance.ConditionEvaluator = new ConditionEvaluator(
			_instance.Player,
			_instance.GameState
		);
		_instance.EffectExecutor = new EffectExecutor(
			_instance.Player,
			_instance.GameState
		);

		// Phase 3: Create managers (depend on evaluator/executor)
		_instance.QuestManager = new QuestManager(
			_instance.Player,
			_instance.Localization,
			_instance.GameState,
			_instance.ConditionEvaluator,
			_instance.EffectExecutor
		);

		_instance.RoomManager = new RoomManager(
			_instance.graphicDevice,
			_instance.assetManager,
			_instance.QuestManager,
			_instance.Player);

		_instance.DialogManager = new DialogManager(
			_instance.Localization,
			_instance.GameState,
			_instance.ConditionEvaluator,
			_instance.EffectExecutor
		);

		// Phase 4: Wire up cross-references
		_instance.ConditionEvaluator.SetQuestManager(_instance.QuestManager);
		_instance.EffectExecutor.SetQuestManager(_instance.QuestManager);

		return _instance;
	}

	public static void Reset() {
		_instance = null;
	}
}