using Candyland.Dialog;
using Candyland.Entities;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Candyland.Quests;

/// <summary>
/// Manages all quests - loading, tracking progress, checking completion
/// Integrates with dialog system for conditions and effects
/// </summary>
public class QuestManager {
	private readonly Player _player;
	private readonly LocalizationManager _localization;
	private readonly GameStateManager _gameState;
	private readonly ConditionEvaluator _conditionEvaluator;
	private readonly EffectExecutor _effectExecutor;

	// Quest data
	private Dictionary<string, Quest> _questDefinitions;
	private Dictionary<string, QuestInstance> _activeQuests;
	private HashSet<string> _completedQuests;

	// Events
	public event System.Action<Quest> OnQuestStarted;
	public event System.Action<Quest> OnQuestCompleted;
	public event System.Action<Quest, QuestObjective> OnObjectiveUpdated;
	public event System.Action<Quest> OnNodeAdvanced;

	public QuestManager(Player player,
					   LocalizationManager localization,
					   GameStateManager gameState,
					   ConditionEvaluator conditionEvaluator,
					   EffectExecutor effectExecutor) {
		_player = player;
		_localization = localization;
		_gameState = gameState;
		_conditionEvaluator = conditionEvaluator;
		_effectExecutor = effectExecutor;

		_questDefinitions = new Dictionary<string, Quest>();
		_activeQuests = new Dictionary<string, QuestInstance>();
		_completedQuests = new HashSet<string>();
	}
	public void SetDialogManager(DialogManager dialogManager) {
		dialogManager.OnResponseChosen += OnDialogResponseChosen;
	}

	// ================================================================
	// LOADING
	// ================================================================

	/// <summary>
	/// Load quest definitions from JSON file
	/// </summary>
	public void loadQuests(string filepath) {
		if(!File.Exists(filepath)) {
			System.Diagnostics.Debug.WriteLine($"Quest file not found: {filepath}");
			return;
		}

		try {
			string json = File.ReadAllText(filepath);
			JsonDocument doc = JsonDocument.Parse(json);
			JsonElement root = doc.RootElement;

			if(root.TryGetProperty("quests", out JsonElement questsElement)) {
				foreach(JsonProperty questProperty in questsElement.EnumerateObject()) {
					Quest quest = parseQuest(questProperty.Value);
					if(quest != null) {
						_questDefinitions[quest.id] = quest;
					}
				}
			}

			System.Diagnostics.Debug.WriteLine($"Loaded {_questDefinitions.Count} quests from {filepath}");
		} catch(System.Exception ex) {
			System.Diagnostics.Debug.WriteLine($"Error loading quests: {ex.Message}");
		}
	}

	private Quest parseQuest(JsonElement questElement) {
		var quest = new Quest();

		if(questElement.TryGetProperty("id", out JsonElement idProp))
			quest.id = idProp.GetString();

		if(questElement.TryGetProperty("nameKey", out JsonElement nameProp))
			quest.nameKey = nameProp.GetString();

		if(questElement.TryGetProperty("descriptionKey", out JsonElement descProp))
			quest.descriptionKey = descProp.GetString();

		if(questElement.TryGetProperty("startNode", out JsonElement startProp))
			quest.startNodeId = startProp.GetString();

		if(questElement.TryGetProperty("questGiver", out JsonElement giverProp))
			quest.questGiver = giverProp.GetString();

		// Parse requirements (conditions to accept quest)
		if(questElement.TryGetProperty("requirements", out JsonElement reqElement)) {
			foreach(JsonElement req in reqElement.EnumerateArray()) {
				quest.requirements.Add(req.GetString());
			}
		}

		// Parse nodes
		if(questElement.TryGetProperty("nodes", out JsonElement nodesElement)) {
			foreach(JsonProperty nodeProperty in nodesElement.EnumerateObject()) {
				QuestNode node = parseQuestNode(nodeProperty.Value, nodeProperty.Name);
				quest.nodes[nodeProperty.Name] = node;
			}
		}

		return quest;
	}

	private QuestNode parseQuestNode(JsonElement nodeElement, string nodeId) {
		var node = new QuestNode { id = nodeId };

		if(nodeElement.TryGetProperty("descriptionKey", out JsonElement descProp))
			node.descriptionKey = descProp.GetString();

		// Parse objectives
		if(nodeElement.TryGetProperty("objectives", out JsonElement objsElement)) {
			foreach(JsonElement objElement in objsElement.EnumerateArray()) {
				node.objectives.Add(parseObjective(objElement));
			}
		}

		// Parse onComplete (what happens when all objectives done)
		if(nodeElement.TryGetProperty("onComplete", out JsonElement completeElement)) {
			// Effects to execute
			if(completeElement.TryGetProperty("effects", out JsonElement effectsElement)) {
				foreach(JsonElement effect in effectsElement.EnumerateArray()) {
					node.onCompleteEffects.Add(effect.GetString());
				}
			}

			// Rewards
			if(completeElement.TryGetProperty("rewards", out JsonElement rewardsElement)) {
				node.rewards = parseRewards(rewardsElement);
			}

			// Branching (conditional next nodes)
			if(completeElement.TryGetProperty("branches", out JsonElement branchesElement)) {
				foreach(JsonElement branch in branchesElement.EnumerateArray()) {
					var branchData = new QuestBranch();

					if(branch.TryGetProperty("conditions", out JsonElement condElement)) {
						foreach(JsonElement cond in condElement.EnumerateArray()) {
							branchData.conditions.Add(cond.GetString());
						}
					}

					if(branch.TryGetProperty("nextNode", out JsonElement nextProp)) {
						branchData.nextNodeId = nextProp.GetString();
					}

					node.branches.Add(branchData);
				}
			}

			// Simple next node (no conditions)
			if(completeElement.TryGetProperty("nextNode", out JsonElement nextNodeProp)) {
				node.nextNodeId = nextNodeProp.GetString();
			}
		}

		return node;
	}

	private QuestObjective parseObjective(JsonElement objElement) {
		var objective = new QuestObjective();

		if(objElement.TryGetProperty("type", out JsonElement typeProp))
			objective.type = typeProp.GetString();

		if(objElement.TryGetProperty("target", out JsonElement targetProp))
			objective.target = targetProp.GetString();

		if(objElement.TryGetProperty("count", out JsonElement countProp))
			objective.requiredCount = countProp.GetInt32();

		if(objElement.TryGetProperty("descriptionKey", out JsonElement descProp))
			objective.descriptionKey = descProp.GetString();

		return objective;
	}

	private QuestReward parseRewards(JsonElement rewardsElement) {
		var rewards = new QuestReward();

		if(rewardsElement.TryGetProperty("xp", out JsonElement xpProp))
			rewards.xp = xpProp.GetInt32();

		if(rewardsElement.TryGetProperty("gold", out JsonElement goldProp))
			rewards.gold = goldProp.GetInt32();

		if(rewardsElement.TryGetProperty("items", out JsonElement itemsElement)) {
			foreach(JsonElement item in itemsElement.EnumerateArray()) {
				rewards.items.Add(item.GetString());
			}
		}

		return rewards;
	}

	// ================================================================
	// QUEST MANAGEMENT
	// ================================================================

	/// <summary>
	/// Check if player can accept a quest
	/// </summary>
	public bool canAcceptQuest(string questId) {
		if(!_questDefinitions.ContainsKey(questId)){
			System.Diagnostics.Debug.WriteLine($"canAcceptQuest? _questDefinitions doesn't contains key");
			return false;
		}

		if(_activeQuests.ContainsKey(questId)) {
			System.Diagnostics.Debug.WriteLine($"canAcceptQuest? Already active");
			return false; // Already active
		}

		if(_completedQuests.Contains(questId)) {
			System.Diagnostics.Debug.WriteLine($"canAcceptQuest? Already completed");
			return false;
		}

		var quest = _questDefinitions[questId];
		return _conditionEvaluator.EvaluateAll(quest.requirements);
	}

	/// <summary>
	/// Start a quest
	/// </summary>
	public bool startQuest(string questId) {
		if(!canAcceptQuest(questId)){
			return false;
		}

		var quest = _questDefinitions[questId];
		var instance = new QuestInstance(quest);

		_activeQuests[questId] = instance;

		System.Diagnostics.Debug.WriteLine($"Started quest: {questId}");
		OnQuestStarted?.Invoke(quest);

		return true;
	}

	public List<Quest> getAllQuests() {
		return new List<Quest>(_questDefinitions.Values);
	}

	/// <summary>
	/// Update objective progress (called from game events)
	/// </summary>
	public void updateObjectiveProgress(string objectiveType, string target, int amount = 1) {
		foreach(var kvp in _activeQuests) {
			var instance = kvp.Value;
			var currentNode = instance.getCurrentNode();

			if(currentNode == null) continue;

			foreach(var objective in currentNode.objectives) {
				// Check if this objective matches
				if(objective.type == objectiveType && objective.target == target) {
					if(!instance.objectiveProgress.ContainsKey(objective)) {
						instance.objectiveProgress[objective] = 0;
					}

					instance.objectiveProgress[objective] += amount;

					System.Diagnostics.Debug.WriteLine(
						$"Quest '{kvp.Key}' objective updated: {objective.descriptionKey} " +
						$"({instance.objectiveProgress[objective]}/{objective.requiredCount})"
					);

					OnObjectiveUpdated?.Invoke(instance.quest, objective);

					// Check if node is complete
					if(isNodeComplete(instance, currentNode)) {
						completeNode(instance, currentNode);
					}
				}
			}
		}
	}

	/// <summary>
	/// Check if all objectives in a node are complete
	/// </summary>
	private bool isNodeComplete(QuestInstance instance, QuestNode node) {
		foreach(var objective in node.objectives) {
			if(!instance.objectiveProgress.ContainsKey(objective))
				return false;

			if(instance.objectiveProgress[objective] < objective.requiredCount)
				return false;
		}
		return true;
	}


	/// <summary>
	/// Handle dialog response chosen for quest objectives
	/// </summary>
	public void OnDialogResponseChosen(string responseId) {
		System.Diagnostics.Debug.WriteLine($"[QUEST] Dialog response chosen: {responseId}");
		updateObjectiveProgress("choose_dialog_response", responseId, 1);
	}

	/// <summary>
	/// Complete a quest node and advance to next
	/// </summary>
	private void completeNode(QuestInstance instance, QuestNode node) {
		System.Diagnostics.Debug.WriteLine($"Quest node '{node.id}' completed!");

		// Execute effects
		foreach(var effect in node.onCompleteEffects) {
			_effectExecutor.execute(effect);
		}

		// Give rewards
		if(node.rewards != null) {
			giveRewards(node.rewards);
		}

		// Determine next node
		string nextNodeId = null;

		// Check branches first (conditional)
		foreach(var branch in node.branches) {
			if(_conditionEvaluator.EvaluateAll(branch.conditions)) {
				nextNodeId = branch.nextNodeId;
				break;
			}
		}

		// Fallback to simple next node
		if(nextNodeId == null) {
			nextNodeId = node.nextNodeId;
		}

		// Advance quest
		if(nextNodeId == null || nextNodeId == "end") {
			completeQuest(instance.quest.id);
		} else {
			instance.goToNode(nextNodeId);
			System.Diagnostics.Debug.WriteLine($"Quest advanced to node: {nextNodeId}");
			OnNodeAdvanced?.Invoke(instance.quest);
		}
	}
	public bool isQuestOnNode(string questId, string nodeId) {
		if(!_activeQuests.ContainsKey(questId)) {
			return false;
		}
		var instance = _activeQuests[questId];
		return instance.currentNodeId == nodeId;
	}

	/// <summary>
	/// Complete a quest
	/// </summary>
	private void completeQuest(string questId) {
		if(!_activeQuests.ContainsKey(questId))
			return;

		var instance = _activeQuests[questId];
		_activeQuests.Remove(questId);
		_completedQuests.Add(questId);

		System.Diagnostics.Debug.WriteLine($"Quest completed: {questId}");
		OnQuestCompleted?.Invoke(instance.quest);
	}

	/// <summary>
	/// Give rewards to player
	/// </summary>
	private void giveRewards(QuestReward rewards) {
		if(rewards.xp > 0) {
			_player.GainXP(rewards.xp);
			System.Diagnostics.Debug.WriteLine($"Rewarded {rewards.xp} XP");
		}

		if(rewards.gold > 0) {
			_player.Coins += rewards.gold;
			System.Diagnostics.Debug.WriteLine($"Rewarded {rewards.gold} gold");
		}

		foreach(var itemId in rewards.items) {
			// TODO: Give item to player
			System.Diagnostics.Debug.WriteLine($"Rewarded item: {itemId}");
		}
	}

	// ================================================================
	// QUERIES
	// ================================================================

	/// <summary>
	/// Get all active quests
	/// </summary>
	public List<QuestInstance> getActiveQuests() {
		return new List<QuestInstance>(_activeQuests.Values);
	}

	/// <summary>
	/// Get a specific active quest
	/// </summary>
	public QuestInstance getActiveQuest(string questId) {
		return _activeQuests.ContainsKey(questId) ? _activeQuests[questId] : null;
	}

	/// <summary>
	/// Check if quest is completed
	/// </summary>
	public bool isQuestCompleted(string questId) {
		return _completedQuests.Contains(questId);
	}

	/// <summary>
	/// Check if quest is active
	/// </summary>
	public bool isQuestActive(string questId) {
		return _activeQuests.ContainsKey(questId);
	}

	/// <summary>
	/// Get localized quest name
	/// </summary>
	public string getQuestName(Quest quest) {
		return _localization.getString(quest.nameKey);
	}

	/// <summary>
	/// Get localized quest description
	/// </summary>
	public string getQuestDescription(Quest quest) {
		return _localization.getString(quest.descriptionKey);
	}

	/// <summary>
	/// Get localized objective description with progress
	/// </summary>
	public string getObjectiveDescription(QuestInstance instance, QuestObjective objective) {
		string baseText = _localization.getString(objective.descriptionKey);

		if(objective.requiredCount > 1) {
			int current = instance.objectiveProgress.ContainsKey(objective)
				? instance.objectiveProgress[objective]
				: 0;
			return $"{baseText} ({current}/{objective.requiredCount})";
		}

		return baseText;
	}

	// ================================================================
	// SAVE / LOAD (TODO: Implement when you add save system)
	// ================================================================

	public void save() {
		// TODO: Save active quests and completed quests to file
	}

	public void load() {
		// TODO: Load active quests and completed quests from file
	}
}