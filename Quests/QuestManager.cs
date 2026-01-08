using EldmeresTale.Dialog;
using EldmeresTale.Entities;
using EldmeresTale.Events;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EldmeresTale.Quests;

public class QuestManager {
	private readonly Player _player;
	private readonly LocalizationManager _localization;
	private readonly ConditionEvaluator _conditionEvaluator;
	private readonly EffectExecutor _effectExecutor;

	// Quest data
	private Dictionary<string, Quest> _questDefinitions;
	private Dictionary<string, QuestInstance> _activeQuests;
	private HashSet<string> _completedQuests;

	// Events
	private GameEventBus _eventBus;
	public event System.Action<Quest> OnQuestStarted;
	public event System.Action<Quest, QuestNode> OnQuestCompleted;
	public event System.Action<Quest, QuestObjective> OnObjectiveUpdated;
	public event System.Action<Quest> OnNodeAdvanced;

	public QuestManager(Player player,
					   LocalizationManager localization,
					   GameStateManager gameState,
					   ConditionEvaluator conditionEvaluator,
					   EffectExecutor effectExecutor) {
		_player = player;
		_localization = localization;
		_conditionEvaluator = conditionEvaluator;
		_effectExecutor = effectExecutor;

		_questDefinitions = new Dictionary<string, Quest>();
		_activeQuests = new Dictionary<string, QuestInstance>();
		_completedQuests = new HashSet<string>();
	}
	public void SetEventBus(GameEventBus eventBus) {
		_eventBus = eventBus;
	}

	public void SetDialogManager(DialogManager dialogManager) {
		dialogManager.OnResponseChosen += OnDialogResponseChosen;
	}

	// TODO: use deserialize for all that
	public void LoadQuests(string filepath) {
		if (!File.Exists(filepath)) {
			System.Diagnostics.Debug.WriteLine($"Quest file not found: {filepath}");
			return;
		}
		try {
			string json = File.ReadAllText(filepath);
			JsonDocument doc = JsonDocument.Parse(json);
			JsonElement root = doc.RootElement;

			if (root.TryGetProperty("quests", out JsonElement questsElement)) {
				foreach (JsonProperty questProperty in questsElement.EnumerateObject()) {
					Quest quest = ParseQuest(questProperty.Value);
					if (quest != null) {
						_questDefinitions[quest.Id] = quest;
					}
				}
			}

			System.Diagnostics.Debug.WriteLine($"Loaded {_questDefinitions.Count} quests from {filepath}");
		} catch (System.Exception ex) {
			System.Diagnostics.Debug.WriteLine($"Error loading quests: {ex.Message}");
		}
	}

	private Quest ParseQuest(JsonElement questElement) {
		Quest quest = new Quest();
		if (questElement.TryGetProperty("id", out JsonElement idProp)) {
			quest.Id = idProp.GetString();
		}
		if (questElement.TryGetProperty("nameKey", out JsonElement nameProp)) {
			quest.NameKey = nameProp.GetString();
		}
		if (questElement.TryGetProperty("descriptionKey", out JsonElement descProp)) {
			quest.DescriptionKey = descProp.GetString();
		}
		if (questElement.TryGetProperty("startNode", out JsonElement startProp)) {
			quest.StartNodeId = startProp.GetString();
		}
		if (questElement.TryGetProperty("questGiver", out JsonElement giverProp)) {
			quest.QuestGiver = giverProp.GetString();
		}
		// Parse requirements (conditions to accept quest)
		if (questElement.TryGetProperty("requirements", out JsonElement reqElement)) {
			foreach (JsonElement req in reqElement.EnumerateArray()) {
				quest.Requirements.Add(req.GetString());
			}
		}
		// Parse nodes
		if (questElement.TryGetProperty("nodes", out JsonElement nodesElement)) {
			foreach (JsonProperty nodeProperty in nodesElement.EnumerateObject()) {
				QuestNode node = ParseQuestNode(nodeProperty.Value, nodeProperty.Name);
				quest.Nodes[nodeProperty.Name] = node;
			}
		}
		return quest;
	}

	private QuestNode ParseQuestNode(JsonElement nodeElement, string nodeId) {
		QuestNode node = new QuestNode { Id = nodeId };
		if (nodeElement.TryGetProperty("descriptionKey", out JsonElement descProp)) {
			node.DescriptionKey = descProp.GetString();
		}
		// Parse objectives
		if (nodeElement.TryGetProperty("objectives", out JsonElement objsElement)) {
			foreach (JsonElement objElement in objsElement.EnumerateArray()) {
				node.Objectives.Add(ParseObjective(objElement));
			}
		}
		// Parse onComplete (what happens when all objectives done)
		if (nodeElement.TryGetProperty("onComplete", out JsonElement completeElement)) {
			// Effects to execute
			if (completeElement.TryGetProperty("effects", out JsonElement effectsElement)) {
				foreach (JsonElement effect in effectsElement.EnumerateArray()) {
					node.OnCompleteEffects.Add(effect.GetString());
				}
			}
			// Rewards
			if (completeElement.TryGetProperty("rewards", out JsonElement rewardsElement)) {
				node.Rewards = ParseRewards(rewardsElement);
			}
			// Branching (conditional next nodes)
			if (completeElement.TryGetProperty("branches", out JsonElement branchesElement)) {
				foreach (JsonElement branch in branchesElement.EnumerateArray()) {
					QuestBranch branchData = new QuestBranch();
					if (branch.TryGetProperty("conditions", out JsonElement condElement)) {
						foreach (JsonElement cond in condElement.EnumerateArray()) {
							branchData.Conditions.Add(cond.GetString());
						}
					}
					if (branch.TryGetProperty("nextNode", out JsonElement nextProp)) {
						branchData.NextNodeId = nextProp.GetString();
					}
					node.Branches.Add(branchData);
				}
			}

			// Simple next node (no conditions)
			if (completeElement.TryGetProperty("nextNode", out JsonElement nextNodeProp)) {
				node.NextNodeId = nextNodeProp.GetString();
			}
		}

		return node;
	}

	private QuestObjective ParseObjective(JsonElement objElement) {
		QuestObjective objective = new QuestObjective();

		if (objElement.TryGetProperty("type", out JsonElement typeProp)) {
			objective.Type = typeProp.GetString();
		}
		if (objElement.TryGetProperty("target", out JsonElement targetProp)) {
			objective.Target = targetProp.GetString();
		}
		if (objElement.TryGetProperty("count", out JsonElement countProp)) {
			objective.RequiredCount = countProp.GetInt32();
		}
		if (objElement.TryGetProperty("descriptionKey", out JsonElement descProp)) {
			objective.DescriptionKey = descProp.GetString();
		}
		return objective;
	}

	private QuestReward ParseRewards(JsonElement rewardsElement) {
		QuestReward rewards = new QuestReward();
		if (rewardsElement.TryGetProperty("xp", out JsonElement xpProp)) {
			rewards.Xp = xpProp.GetInt32();
		}
		if (rewardsElement.TryGetProperty("gold", out JsonElement goldProp)) {
			rewards.Gold = goldProp.GetInt32();
		}
		if (rewardsElement.TryGetProperty("items", out JsonElement itemsElement)) {
			foreach (JsonElement item in itemsElement.EnumerateArray()) {
				rewards.Items.Add(item.GetString());
			}
		}
		return rewards;
	}

	public bool CanAcceptQuest(string questId) {
		if (!_questDefinitions.ContainsKey(questId)) {
			System.Diagnostics.Debug.WriteLine($"canAcceptQuest? _questDefinitions doesn't contains key");
			return false;
		}

		if (_activeQuests.ContainsKey(questId)) {
			System.Diagnostics.Debug.WriteLine($"canAcceptQuest? Already active");
			return false; // Already active
		}

		if (_completedQuests.Contains(questId)) {
			System.Diagnostics.Debug.WriteLine($"canAcceptQuest? Already completed");
			return false;
		}

		Quest quest = _questDefinitions[questId];
		return _conditionEvaluator.EvaluateAll(quest.Requirements);
	}

	public bool StartQuest(string questId) {
		if (!CanAcceptQuest(questId)) {
			return false;
		}

		Quest quest = _questDefinitions[questId];

		_activeQuests[questId] = new QuestInstance(quest);

		System.Diagnostics.Debug.WriteLine($"Started quest: {questId}");
		OnQuestStarted?.Invoke(quest);
		_eventBus?.Publish(new QuestStartedEvent {
			Quest = quest,
			QuestName = GetQuestName(quest)
		});

		return true;
	}

	public List<Quest> GetAllQuests() {
		return new List<Quest>(_questDefinitions.Values);
	}

	public void UpdateObjectiveProgress(string objectiveType, string target, int amount = 1) {
		foreach (KeyValuePair<string, QuestInstance> kvp in _activeQuests) {
			QuestInstance instance = kvp.Value;
			QuestNode currentNode = instance.GetCurrentNode();

			if (currentNode == null) {
				continue;
			}

			foreach (QuestObjective objective in currentNode.Objectives) {
				// Check if this objective matches
				if (objective.Type == objectiveType && objective.Target == target) {
					if (!instance.ObjectiveProgress.ContainsKey(objective)) {
						instance.ObjectiveProgress[objective] = 0;
					}

					instance.ObjectiveProgress[objective] += amount;

					System.Diagnostics.Debug.WriteLine(
						$"Quest '{kvp.Key}' objective updated: {objective.DescriptionKey} " +
						$"({instance.ObjectiveProgress[objective]}/{objective.RequiredCount})"
					);

					OnObjectiveUpdated?.Invoke(instance.Quest, objective);
					_eventBus?.Publish(new QuestObjectiveUpdatedEvent {
						Quest = instance.Quest,
						Objective = objective
					});

					// Check if node is complete
					if (IsNodeComplete(instance, currentNode)) {
						CompleteNode(instance, currentNode);
					}
				}
			}
		}
	}

	private bool IsNodeComplete(QuestInstance instance, QuestNode node) {
		foreach (QuestObjective objective in node.Objectives) {
			if (!instance.ObjectiveProgress.ContainsKey(objective)) {
				return false;
			}

			if (instance.ObjectiveProgress[objective] < objective.RequiredCount) {
				return false;
			}
		}
		return true;
	}

	public void OnDialogResponseChosen(string responseId) {
		System.Diagnostics.Debug.WriteLine($"[QUEST] Dialog response chosen: {responseId}");
		UpdateObjectiveProgress("choose_dialog_response", responseId, 1);
	}

	private void CompleteNode(QuestInstance instance, QuestNode node) {
		System.Diagnostics.Debug.WriteLine($"Quest node '{node.Id}' completed!");

		// Execute effects
		foreach (string effect in node.OnCompleteEffects) {
			_effectExecutor.execute(effect);
		}

		// Give rewards
		if (node.Rewards != null) {
			GiveRewards(node.Rewards);
		}

		// Determine next node
		string nextNodeId = null;

		// Check branches first (conditional)
		foreach (QuestBranch branch in node.Branches) {
			if (_conditionEvaluator.EvaluateAll(branch.Conditions)) {
				nextNodeId = branch.NextNodeId;
				break;
			}
		}

		// Fallback to simple next node
		if (nextNodeId == null) {
			nextNodeId = node.NextNodeId;
		}

		// Advance quest
		if (nextNodeId == null || nextNodeId == "end") {
			CompleteQuest(instance.Quest.Id, node);
		} else {
			instance.GoToNode(nextNodeId);
			System.Diagnostics.Debug.WriteLine($"Quest advanced to node: {nextNodeId}");
			OnNodeAdvanced?.Invoke(instance.Quest);
			_eventBus?.Publish(new QuestNodeAdvancedEvent {
				Quest = instance.Quest,
				OldNodeId = node.Id,
				NewNodeId = nextNodeId
			});
		}
	}
	public bool IsQuestOnNode(string questId, string nodeId) {
		if (!_activeQuests.ContainsKey(questId)) {
			return false;
		}
		QuestInstance instance = _activeQuests[questId];
		return instance.CurrentNodeId == nodeId;
	}

	private void CompleteQuest(string questId, QuestNode lastNode) {
		if (!_activeQuests.ContainsKey(questId)) {
			return;
		}

		QuestInstance instance = _activeQuests[questId];
		_activeQuests.Remove(questId);
		_completedQuests.Add(questId);

		System.Diagnostics.Debug.WriteLine($"Quest completed: {questId}");
		_eventBus?.Publish(new QuestCompletedEvent {
			Quest = instance.Quest,
			LastNode = lastNode,
			QuestName = GetQuestName(instance.Quest)
		});
		OnQuestCompleted?.Invoke(instance.Quest, lastNode);
	}

	private void GiveRewards(QuestReward rewards) {
		if (rewards.Xp > 0) {
			_player.GainXP(rewards.Xp);
			System.Diagnostics.Debug.WriteLine($"Rewarded {rewards.Xp} XP");
		}

		if (rewards.Gold > 0) {
			_player.Coins += rewards.Gold;
			System.Diagnostics.Debug.WriteLine($"Rewarded {rewards.Gold} gold");
		}

		foreach (string itemId in rewards.Items) {
			// TODO: Give item to player
			System.Diagnostics.Debug.WriteLine($"Rewarded item: {itemId}");
		}
	}

	public List<QuestInstance> GetActiveQuests() {
		return new List<QuestInstance>(_activeQuests.Values);
	}

	public QuestInstance GetActiveQuest(string questId) {
		return _activeQuests.ContainsKey(questId) ? _activeQuests[questId] : null;
	}

	public bool IsQuestCompleted(string questId) {
		return _completedQuests.Contains(questId);
	}

	public bool IsQuestActive(string questId) {
		return _activeQuests.ContainsKey(questId);
	}

	public string GetQuestName(Quest quest) {
		return _localization.getString(quest.NameKey);
	}

	public string GetQuestDescription(Quest quest) {
		return _localization.getString(quest.DescriptionKey);
	}

	public string GetObjectiveDescription(QuestInstance instance, QuestObjective objective) {
		string baseText = _localization.getString(objective.DescriptionKey);

		if (objective.RequiredCount > 1) {
			int current = instance.ObjectiveProgress.ContainsKey(objective)
				? instance.ObjectiveProgress[objective]
				: 0;
			return $"{baseText} ({current}/{objective.RequiredCount})";
		}

		return baseText;
	}

	public HashSet<string> GetCompletedQuests() {
		return new HashSet<string>(_completedQuests);
	}

	public QuestInstance GetQuestInstance(string questId) {
		return _activeQuests.ContainsKey(questId) ? _activeQuests[questId] : null;
	}

	public void ClearAll() {
		_activeQuests.Clear();
		_completedQuests.Clear();
		System.Diagnostics.Debug.WriteLine("[QUEST] Cleared all quest state");
	}

	public void MarkAsCompleted(string questId) {
		_completedQuests.Add(questId);
		System.Diagnostics.Debug.WriteLine($"[QUEST] Marked quest as completed: {questId}");
	}

	public void LoadQuest(string questId, string currentNodeId, Dictionary<string, int> objectiveProgress) {
		if (!_questDefinitions.ContainsKey(questId)) {
			System.Diagnostics.Debug.WriteLine($"[QUEST] WARNING: Cannot load unknown quest: {questId}");
			return;
		}

		Quest quest = _questDefinitions[questId];
		QuestInstance instance = new QuestInstance(quest) {
			// Set current node
			CurrentNodeId = currentNodeId
		};

		// Restore objective progress
		QuestNode currentNode = instance.GetCurrentNode();
		if (currentNode != null) {
			foreach (QuestObjective objective in currentNode.Objectives) {
				// Create a unique key for this objective (type:target)
				string key = $"{objective.Type}:{objective.Target}";

				if (objectiveProgress.ContainsKey(key)) {
					instance.ObjectiveProgress[objective] = objectiveProgress[key];
					System.Diagnostics.Debug.WriteLine(
						$"[QUEST] Restored progress for {questId}: {key} = {objectiveProgress[key]}/{objective.RequiredCount}"
					);
				}
			}
		}

		_activeQuests[questId] = instance;
		System.Diagnostics.Debug.WriteLine($"[QUEST] Loaded quest: {questId} at node {currentNodeId}");
	}
}