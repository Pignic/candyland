using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EldmeresTale.Dialog;

public class DialogTree {

	[JsonPropertyName("id")]
	public string Id { get; set; }

	[JsonPropertyName("npcId")]
	public string NpcId { get; set; }

	[JsonPropertyName("startNodeId")]
	public string StartNodeId { get; set; }

	[JsonPropertyName("nodes")]
	public Dictionary<string, DialogNode> Nodes { get; set; }

	private string currentNodeId;

	public DialogTree() {
		Nodes = [];
	}

	public void Start() {
		currentNodeId = StartNodeId;
	}

	public DialogNode GetCurrentNode() {
		if (currentNodeId != null && Nodes.ContainsKey(currentNodeId)) {
			return Nodes[currentNodeId];
		}
		return null;
	}

	public void GoToNode(string nodeId) {
		if (nodeId == "end" || nodeId == null) {
			currentNodeId = null;
		} else if (Nodes.ContainsKey(nodeId)) {
			currentNodeId = nodeId;
		}
	}

	public bool IsFinished() {
		if (currentNodeId == null) {
			return true;
		}
		return GetCurrentNode()?.IsEndNode != false;
	}

	public List<DialogResponse> GetAvailableResponses(ConditionEvaluator evaluator) {
		DialogNode currentNode = GetCurrentNode();
		if (currentNode == null) {
			return [];
		}

		List<DialogResponse> availableResponses = [];
		foreach (DialogResponse response in currentNode.Responses) {
			if (evaluator.EvaluateAll(response.Conditions)) {
				availableResponses.Add(response);
			}
		}

		return availableResponses;
	}

	public void ChooseResponse(DialogResponse response, EffectExecutor executor) {
		// Execute response effects
		if (response.Effects != null) {
			foreach (string effect in response.Effects) {
				executor.Execute(effect);
			}
		}

		// Move to next node
		GoToNode(response.NextNodeId);
	}
}