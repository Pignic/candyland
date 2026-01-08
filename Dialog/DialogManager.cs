using EldmeresTale.Entities.Definitions;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EldmeresTale.Dialog;

public class DialogManager {
	private readonly ConditionEvaluator _conditionEvaluator;
	private readonly EffectExecutor _effectExecutor;

	// Expose for external access
	public LocalizationManager Localization { get; }

	public GameStateManager GameState { get; }

	// Dialog data
	private readonly Dictionary<string, DialogTree> dialogTrees;
	private readonly Dictionary<string, NPCDefinition> npcDefinitions;

	// Current state
	public DialogTree CurrentDialog { get; private set; }
	private NPCDefinition currentNPC;

	public bool IsDialogActive => CurrentDialog?.IsFinished() == false;

	//Events
	public event System.Action<string> OnResponseChosen;

	public DialogManager(LocalizationManager localization,
						GameStateManager gameState,
						ConditionEvaluator conditionEvaluator,
						EffectExecutor effectExecutor) {
		Localization = localization;
		GameState = gameState;
		_conditionEvaluator = conditionEvaluator;
		_effectExecutor = effectExecutor;

		dialogTrees = [];
		npcDefinitions = [];
	}

	public void LoadDialogTrees(string filepath) {
		if (!File.Exists(filepath)) {
			System.Diagnostics.Debug.WriteLine($"Dialog tree file not found: {filepath}");
			return;
		}

		try {
			string json = File.ReadAllText(filepath);
			JsonDocument doc = JsonDocument.Parse(json);
			JsonElement root = doc.RootElement;
			if (root.TryGetProperty("dialogTrees", out JsonElement treesElement)) {
				foreach (JsonProperty treeProperty in treesElement.EnumerateObject()) {
					DialogTree tree = ParseDialogTree(treeProperty.Value);
					if (tree != null) {
						dialogTrees[tree.Id] = tree;
					}
				}
			}
			System.Diagnostics.Debug.WriteLine($"Loaded {dialogTrees.Count} dialog trees from {filepath}");
		} catch (System.Exception ex) {
			System.Diagnostics.Debug.WriteLine($"Error loading dialog trees: {ex.Message}");
		}
	}

	private static DialogTree ParseDialogTree(JsonElement treeElement) {
		DialogTree tree = new DialogTree();
		if (treeElement.TryGetProperty("id", out JsonElement idProp)) {
			tree.Id = idProp.GetString();
		}
		if (treeElement.TryGetProperty("npcId", out JsonElement npcProp)) {
			tree.NpcId = npcProp.GetString();
		}
		if (treeElement.TryGetProperty("startNode", out JsonElement startProp)) {
			tree.StartNodeId = startProp.GetString();
		}
		if (treeElement.TryGetProperty("nodes", out JsonElement nodesElement)) {
			foreach (JsonProperty nodeProperty in nodesElement.EnumerateObject()) {
				tree.Nodes[nodeProperty.Name] = ParseDialogNode(nodeProperty.Value, nodeProperty.Name);
			}
		}
		return tree;
	}

	private static DialogNode ParseDialogNode(JsonElement nodeElement, string nodeId) {
		DialogNode node = new DialogNode { Id = nodeId };

		// Check node type
		if (nodeElement.TryGetProperty("type", out JsonElement typeProp)) {
			node.NodeType = typeProp.GetString();
		}

		// Parse command nodes
		if (node.NodeType == "command") {
			if (nodeElement.TryGetProperty("command", out JsonElement commandProp)) {
				node.Command = CutsceneCommandParser.ParseCommand(commandProp);
				node.Command.Id = nodeId;
			}
			node.IsEndNode = false; // Commands are never end nodes by themselves
			return node;
		}

		// Parse dialog nodes (existing code)
		if (nodeElement.TryGetProperty("text", out JsonElement textProp)) {
			node.TextKey = textProp.GetString();
		}

		if (nodeElement.TryGetProperty("portrait", out JsonElement portraitProp)) {
			node.PortraitKey = portraitProp.GetString();
		}

		if (nodeElement.TryGetProperty("effects", out JsonElement effectsElement)) {
			foreach (JsonElement effect in effectsElement.EnumerateArray()) {
				node.Effects.Add(effect.GetString());
			}
		}

		if (nodeElement.TryGetProperty("responses", out JsonElement responsesElement)) {
			foreach (JsonElement responseElement in responsesElement.EnumerateArray()) {
				node.Responses.Add(ParseDialogResponse(responseElement));
			}
		}

		// Check if this is an end node (no responses or nextNode is "end")
		node.IsEndNode = node.Responses.Count == 0;

		return node;
	}

	private static DialogResponse ParseDialogResponse(JsonElement responseElement) {
		DialogResponse response = new DialogResponse();

		if (responseElement.TryGetProperty("id", out JsonElement idProp)) {
			response.Id = idProp.GetString();
		}
		if (responseElement.TryGetProperty("text", out JsonElement textProp)) {
			response.TextKey = textProp.GetString();
		}
		if (responseElement.TryGetProperty("nextNode", out JsonElement nextProp)) {
			response.NextNodeId = nextProp.GetString();
		}
		if (responseElement.TryGetProperty("conditions", out JsonElement conditionsElement)) {
			foreach (JsonElement condition in conditionsElement.EnumerateArray()) {
				response.Conditions.Add(condition.GetString());
			}
		}

		if (responseElement.TryGetProperty("effects", out JsonElement effectsElement)) {
			foreach (JsonElement effect in effectsElement.EnumerateArray()) {
				response.Effects.Add(effect.GetString());
			}
		}

		return response;
	}

	public void LoadNPCDefinitions(string filepath) {
		if (!File.Exists(filepath)) {
			System.Diagnostics.Debug.WriteLine($"NPC definitions file not found: {filepath}");
			return;
		}

		try {
			string json = File.ReadAllText(filepath);
			JsonDocument doc = JsonDocument.Parse(json);
			JsonElement root = doc.RootElement;

			if (root.TryGetProperty("npcs", out JsonElement npcsElement)) {
				foreach (JsonProperty npcProperty in npcsElement.EnumerateObject()) {
					string npcId = npcProperty.Name;
					npcDefinitions[npcId] = ParseNPCDefinition(npcProperty.Value, npcId);
				}
			}

			System.Diagnostics.Debug.WriteLine($"Loaded {npcDefinitions.Count} NPC definitions from {filepath}");
		} catch (System.Exception ex) {
			System.Diagnostics.Debug.WriteLine($"Error loading NPC definitions: {ex.Message}");
		}
	}

	public NPCDefinition GetNPCDefinition(string npcId) {
		return npcDefinitions[npcId];
	}

	private static NPCDefinition ParseNPCDefinition(JsonElement npcElement, string npcId) {
		NPCDefinition npc = new NPCDefinition { Id = npcId };

		if (npcElement.TryGetProperty("name", out JsonElement nameProp)) {
			npc.NameKey = nameProp.GetString();
		}

		if (npcElement.TryGetProperty("defaultPortrait", out JsonElement portraitProp)) {
			npc.DefaultPortrait = portraitProp.GetString();
		}

		if (npcElement.TryGetProperty("requiresItem", out JsonElement itemProp)) {
			npc.RequiresItem = itemProp.GetString();
		}

		if (npcElement.TryGetProperty("refuseDialog", out JsonElement refuseProp)) {
			npc.RefuseDialogKey = refuseProp.GetString();
		}

		if (npcElement.TryGetProperty("dialogs", out JsonElement dialogsElement)) {
			foreach (JsonElement dialogElement in dialogsElement.EnumerateArray()) {
				NPCDialogEntry dialogEntry = new NPCDialogEntry();

				if (dialogElement.TryGetProperty("treeId", out JsonElement treeProp)) {
					dialogEntry.TreeId = treeProp.GetString();
				}

				if (dialogElement.TryGetProperty("priority", out JsonElement priorityProp)) {
					dialogEntry.Priority = priorityProp.GetInt32();
				}

				if (dialogElement.TryGetProperty("conditions", out JsonElement conditionsElement)) {
					foreach (JsonElement condition in conditionsElement.EnumerateArray()) {
						dialogEntry.Conditions.Add(condition.GetString());
					}
				}

				npc.Dialogs.Add(dialogEntry);
			}
		}

		return npc;
	}

	public bool StartDialog(string npcId) {
		if (!npcDefinitions.TryGetValue(npcId, out NPCDefinition value)) {
			System.Diagnostics.Debug.WriteLine($"NPC not found: {npcId}");
			return false;
		}

		currentNPC = value;

		// Check if NPC requires an item
		if (!string.IsNullOrEmpty(currentNPC.RequiresItem)) {
			if (!GameState.HasItem(currentNPC.RequiresItem)) {
				// Show refuse dialog
				System.Diagnostics.Debug.WriteLine($"NPC refuses: missing item {currentNPC.RequiresItem}");
				return false;
			}
		}

		// Find the appropriate dialog tree based on conditions and priority
		string treeId = GetNPCDialogTree(npcId);

		if (string.IsNullOrEmpty(treeId) || !dialogTrees.TryGetValue(treeId, out DialogTree value1)) {
			System.Diagnostics.Debug.WriteLine($"No valid dialog tree found for NPC: {npcId}");
			return false;
		}

		CurrentDialog = value1;
		CurrentDialog.Start();

		// Execute effects for the starting node
		DialogNode startNode = CurrentDialog.GetCurrentNode();
		if (startNode?.Effects != null) {
			foreach (string effect in startNode.Effects) {
				_effectExecutor.Execute(effect);
			}
		}

		System.Diagnostics.Debug.WriteLine($"Started dialog: {treeId} with NPC: {npcId}");
		return true;
	}

	public bool StartCutscene(string treeId) {
		if (!dialogTrees.TryGetValue(treeId, out DialogTree value)) {
			System.Diagnostics.Debug.WriteLine($"Dialog tree not found: {treeId}");
			return false;
		}

		CurrentDialog = value;
		currentNPC = null; // No NPC for cutscenes
		CurrentDialog.Start();

		// Execute effects for the starting node
		DialogNode startNode = CurrentDialog.GetCurrentNode();
		if (startNode?.Effects != null) {
			foreach (string effect in startNode.Effects) {
				_effectExecutor.Execute(effect);
			}
		}

		System.Diagnostics.Debug.WriteLine($"Started cutscene: {treeId}");
		return true;
	}

	private string GetNPCDialogTree(string npcId) {
		// Check for overridden dialog tree
		string overrideTree = GameState.GetNPCDialogTree(npcId);
		if (!string.IsNullOrEmpty(overrideTree)) {
			return overrideTree;
		}

		// Find dialog based on conditions and priority
		NPCDefinition npc = npcDefinitions[npcId];
		List<NPCDialogEntry> sortedDialogs = [.. npc.Dialogs];
		sortedDialogs.Sort((a, b) => a.Priority.CompareTo(b.Priority));

		foreach (NPCDialogEntry dialogEntry in sortedDialogs) {
			if (_conditionEvaluator.EvaluateAll(dialogEntry.Conditions)) {
				return dialogEntry.TreeId;
			}
		}

		return null;
	}

	public void ChooseResponse(int responseIndex) {
		if (CurrentDialog == null) {
			return;
		}

		List<DialogResponse> availableResponses = CurrentDialog.GetAvailableResponses(_conditionEvaluator);
		if (responseIndex < 0 || responseIndex >= availableResponses.Count) {
			return;
		}

		DialogResponse chosenResponse = availableResponses[responseIndex];

		if (!string.IsNullOrEmpty(chosenResponse.Id)) {
			OnResponseChosen?.Invoke(chosenResponse.Id);  // ← Notify quests
		}

		CurrentDialog.ChooseResponse(chosenResponse, _effectExecutor);

		// Execute effects for the new node
		DialogNode currentNode = CurrentDialog.GetCurrentNode();
		if (currentNode?.Effects != null) {
			foreach (string effect in currentNode.Effects) {
				_effectExecutor.Execute(effect);
			}
		}

		// Check if dialog ended
		if (CurrentDialog.IsFinished()) {
			EndDialog();
		}
	}

	public void EndDialog() {
		CurrentDialog = null;
		currentNPC = null;
	}

	public DialogNode GetCurrentNode() {
		return CurrentDialog?.GetCurrentNode();
	}

	public List<DialogResponse> GetAvailableResponses() {
		if (CurrentDialog == null) {
			return [];
		}
		return CurrentDialog.GetAvailableResponses(_conditionEvaluator);
	}

	public NPCDefinition GetCurrentNPC() {
		return currentNPC;
	}
}