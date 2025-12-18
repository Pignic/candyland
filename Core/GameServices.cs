using Candyland.Dialog;
using Candyland.Entities;
using Candyland.Quests;

namespace Candyland.Core;

/// <summary>
/// Central container for all game-wide singleton services
/// Manages initialization order and dependencies
/// </summary>
public class GameServices {
	// Singletons
	public Player Player { get; private set; }
	public LocalizationManager Localization { get; private set; }
	public GameStateManager GameState { get; private set; }
	public ConditionEvaluator ConditionEvaluator { get; private set; }
	public EffectExecutor EffectExecutor { get; private set; }
	public QuestManager QuestManager { get; private set; }
	public DialogManager DialogManager { get; private set; }

	// Singleton instance
	private static GameServices _instance;
	public static GameServices Instance => _instance;

	private GameServices() { }

	/// <summary>
	/// Initialize all services in correct order
	/// </summary>
	public static GameServices Initialize(Player player) {
		if(_instance != null) {
			throw new System.Exception("GameServices already initialized!");
		}

		_instance = new GameServices();

		// Phase 1: Create core services
		_instance.Player = player;
		_instance.Localization = new LocalizationManager();
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

	/// <summary>
	/// Reset for new game (optional)
	/// </summary>
	public static void Reset() {
		_instance = null;
	}
}