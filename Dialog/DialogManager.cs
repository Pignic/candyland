using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EldmeresTale.Entities;
using EldmeresTale.Quests;

namespace EldmeresTale.Dialog;

public class DialogManager {
	private readonly LocalizationManager _localization;
	private readonly GameStateManager _gameState;
	private readonly ConditionEvaluator _conditionEvaluator;
	private readonly EffectExecutor _effectExecutor;

	// Expose for external access
	public LocalizationManager Localization => _localization;
	public GameStateManager GameState => _gameState;

	// Dialog data
	private readonly Dictionary<string, DialogTree> dialogTrees;
	private readonly Dictionary<string, NPCDefinition> npcDefinitions;

	// Current state
	private DialogTree currentDialog;
	private NPCDefinition currentNPC;

	public bool isDialogActive => currentDialog?.isFinished() == false;

	//Events
	public event System.Action<string> OnResponseChosen;

	public DialogManager(LocalizationManager localization,
						GameStateManager gameState,
						ConditionEvaluator conditionEvaluator,
						EffectExecutor effectExecutor) {
		_localization = localization;
		_gameState = gameState;
		_conditionEvaluator = conditionEvaluator;
		_effectExecutor = effectExecutor;

		dialogTrees = new Dictionary<string, DialogTree>();
		npcDefinitions = new Dictionary<string, NPCDefinition>();
	}

	public void loadDialogTrees(string filepath) {
		if(!File.Exists(filepath)) {
			System.Diagnostics.Debug.WriteLine($"Dialog tree file not found: {filepath}");
			return;
		}

		try {
			string json = File.ReadAllText(filepath);
			JsonDocument doc = JsonDocument.Parse(json);
			JsonElement root = doc.RootElement;
			if(root.TryGetProperty("dialogTrees", out var treesElement)) {
				foreach(var treeProperty in treesElement.EnumerateObject()) {
					var tree = parseDialogTree(treeProperty.Value);
					if(tree != null) {
						dialogTrees[tree.id] = tree;
					}
				}
			}
			System.Diagnostics.Debug.WriteLine($"Loaded {dialogTrees.Count} dialog trees from {filepath}");
		} catch(System.Exception ex) {
			System.Diagnostics.Debug.WriteLine($"Error loading dialog trees: {ex.Message}");
		}
	}

	private DialogTree parseDialogTree(JsonElement treeElement) {
		var tree = new DialogTree();
		if(treeElement.TryGetProperty("id", out JsonElement idProp)) {
			tree.id = idProp.GetString();
		}
		if(treeElement.TryGetProperty("npcId", out JsonElement npcProp)) {
			tree.npcId = npcProp.GetString();
		}
		if(treeElement.TryGetProperty("startNode", out JsonElement startProp)) {
			tree.startNodeId = startProp.GetString();
		}
		if(treeElement.TryGetProperty("nodes", out JsonElement nodesElement)) {
			foreach(JsonProperty nodeProperty in nodesElement.EnumerateObject()) {
				tree.nodes[nodeProperty.Name] = parseDialogNode(nodeProperty.Value, nodeProperty.Name);
			}
		}
		return tree;
	}

	private DialogNode parseDialogNode(JsonElement nodeElement, string nodeId) {
		var node = new DialogNode { id = nodeId };

		if(nodeElement.TryGetProperty("text", out JsonElement textProp)) {
			node.textKey = textProp.GetString();
		}

		if(nodeElement.TryGetProperty("portrait", out JsonElement portraitProp)) {
			node.portraitKey = portraitProp.GetString();
		}

		if(nodeElement.TryGetProperty("effects", out JsonElement effectsElement)) {
			foreach(JsonElement effect in effectsElement.EnumerateArray()) {
				node.effects.Add(effect.GetString());
			}
		}

		if(nodeElement.TryGetProperty("responses", out JsonElement responsesElement)) {
			foreach(JsonElement responseElement in responsesElement.EnumerateArray()) {
				node.responses.Add(parseDialogResponse(responseElement));
			}
		}

		// Check if this is an end node (no responses or nextNode is "end")
		node.isEndNode = node.responses.Count == 0;

		return node;
	}

	private DialogResponse parseDialogResponse(JsonElement responseElement) {
		var response = new DialogResponse();

		if(responseElement.TryGetProperty("id", out JsonElement idProp)) {
			response.id = idProp.GetString();
		}
		if(responseElement.TryGetProperty("text", out JsonElement textProp)) {
			response.textKey = textProp.GetString();
		}
		if(responseElement.TryGetProperty("nextNode", out JsonElement nextProp)) {
			response.nextNodeId = nextProp.GetString();
		}
		if(responseElement.TryGetProperty("conditions", out JsonElement conditionsElement)) {
			foreach(JsonElement condition in conditionsElement.EnumerateArray()) {
				response.conditions.Add(condition.GetString());
			}
		}

		if(responseElement.TryGetProperty("effects", out JsonElement effectsElement)) {
			foreach(JsonElement effect in effectsElement.EnumerateArray()) {
				response.effects.Add(effect.GetString());
			}
		}

		return response;
	}

	public void loadNPCDefinitions(string filepath) {
		if(!File.Exists(filepath)) {
			System.Diagnostics.Debug.WriteLine($"NPC definitions file not found: {filepath}");
			return;
		}

		try {
			string json = File.ReadAllText(filepath);
			var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;

			if(root.TryGetProperty("npcs", out JsonElement npcsElement)) {
				foreach(JsonProperty npcProperty in npcsElement.EnumerateObject()) {
					string npcId = npcProperty.Name;
					npcDefinitions[npcId] = parseNPCDefinition(npcProperty.Value, npcId);
				}
			}

			System.Diagnostics.Debug.WriteLine($"Loaded {npcDefinitions.Count} NPC definitions from {filepath}");
		} catch(System.Exception ex) {
			System.Diagnostics.Debug.WriteLine($"Error loading NPC definitions: {ex.Message}");
		}
	}

	public NPCDefinition getNPCDefinition(string npcId) {
		return npcDefinitions[npcId];
	}

	private NPCDefinition parseNPCDefinition(JsonElement npcElement, string npcId) {
		var npc = new NPCDefinition { id = npcId };

		if(npcElement.TryGetProperty("name", out JsonElement nameProp)) {
			npc.nameKey = nameProp.GetString();
		}

		if(npcElement.TryGetProperty("defaultPortrait", out JsonElement portraitProp)) {
			npc.defaultPortrait = portraitProp.GetString();
		}

		if(npcElement.TryGetProperty("requiresItem", out JsonElement itemProp)) {
			npc.requiresItem = itemProp.GetString();
		}

		if(npcElement.TryGetProperty("refuseDialog", out JsonElement refuseProp)) {
			npc.refuseDialogKey = refuseProp.GetString();
		}

		if(npcElement.TryGetProperty("dialogs", out JsonElement dialogsElement)) {
			foreach(JsonElement dialogElement in dialogsElement.EnumerateArray()) {
				NPCDialogEntry dialogEntry = new NPCDialogEntry();

				if(dialogElement.TryGetProperty("treeId", out JsonElement treeProp)) {
					dialogEntry.treeId = treeProp.GetString();
				}

				if(dialogElement.TryGetProperty("priority", out JsonElement priorityProp)) {
					dialogEntry.priority = priorityProp.GetInt32();
				}

				if(dialogElement.TryGetProperty("conditions", out JsonElement conditionsElement)) {
					foreach(JsonElement condition in conditionsElement.EnumerateArray()) {
						dialogEntry.conditions.Add(condition.GetString());
					}
				}

				npc.dialogs.Add(dialogEntry);
			}
		}

		return npc;
	}

	public bool startDialog(string npcId) {
		if(!this.npcDefinitions.ContainsKey(npcId)) {
			System.Diagnostics.Debug.WriteLine($"NPC not found: {npcId}");
			return false;
		}

		currentNPC = npcDefinitions[npcId];

		// Check if NPC requires an item
		if(!string.IsNullOrEmpty(currentNPC.requiresItem)) {
			if(!_gameState.hasItem(currentNPC.requiresItem)) {
				// Show refuse dialog
				System.Diagnostics.Debug.WriteLine($"NPC refuses: missing item {currentNPC.requiresItem}");
				return false;
			}
		}

		// Find the appropriate dialog tree based on conditions and priority
		string treeId = getNPCDialogTree(npcId);

		if(string.IsNullOrEmpty(treeId) || !this.dialogTrees.ContainsKey(treeId)) {
			System.Diagnostics.Debug.WriteLine($"No valid dialog tree found for NPC: {npcId}");
			return false;
		}

		currentDialog = dialogTrees[treeId];
		currentDialog.start();

		// Execute effects for the starting node
		var startNode = currentDialog.getCurrentNode();
		if(startNode?.effects != null) {
			foreach(var effect in startNode.effects) {
				_effectExecutor.execute(effect);
			}
		}

		System.Diagnostics.Debug.WriteLine($"Started dialog: {treeId} with NPC: {npcId}");
		return true;
	}

	private string getNPCDialogTree(string npcId) {
		// Check for overridden dialog tree
		string overrideTree = _gameState.getNPCDialogTree(npcId);
		if(!string.IsNullOrEmpty(overrideTree)) {
			return overrideTree;
		}

		// Find dialog based on conditions and priority
		var npc = npcDefinitions[npcId];
		var sortedDialogs = new List<NPCDialogEntry>(npc.dialogs);
		sortedDialogs.Sort((a, b) => a.priority.CompareTo(b.priority));

		foreach(var dialogEntry in sortedDialogs) {
			if(this._conditionEvaluator.EvaluateAll(dialogEntry.conditions)) {
				return dialogEntry.treeId;
			}
		}

		return null;
	}

	public void chooseResponse(int responseIndex) {
		if(currentDialog == null) {
			return;
		}

		var availableResponses = currentDialog.getAvailableResponses(_conditionEvaluator);
		if(responseIndex < 0 || responseIndex >= availableResponses.Count) {
			return;
		}

		DialogResponse chosenResponse = availableResponses[responseIndex];

		if(!string.IsNullOrEmpty(chosenResponse.id)) {
			OnResponseChosen?.Invoke(chosenResponse.id);  // ← Notify quests
		}

		currentDialog.chooseResponse(chosenResponse, _effectExecutor);

		// Execute effects for the new node
		var currentNode = currentDialog.getCurrentNode();
		if(currentNode != null && currentNode.effects != null) {
			foreach(var effect in currentNode.effects) {
				_effectExecutor.execute(effect);
			}
		}

		// Check if dialog ended
		if(currentDialog.isFinished()) {
			endDialog();
		}
	}

	public void endDialog() {
		currentDialog = null;
		currentNPC = null;
	}

	public DialogNode getCurrentNode() {
		return currentDialog?.getCurrentNode();
	}

	public List<DialogResponse> getAvailableResponses() {
		if(this.currentDialog == null) {
			return [];
		}

		return this.currentDialog.getAvailableResponses(this._conditionEvaluator);
	}

	public NPCDefinition getCurrentNPC() {
		return currentNPC;
	}
}