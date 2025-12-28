using System.Collections.Generic;

namespace EldmeresTale.Dialog;

public class DialogTree {
	public string id { get; set; }
	public string npcId { get; set; }
	public string startNodeId { get; set; }
	public Dictionary<string, DialogNode> nodes { get; set; }

	private string currentNodeId;

	public DialogTree() {
		this.nodes = new Dictionary<string, DialogNode>();
	}

	public void start() {
		currentNodeId = startNodeId;
	}

	public DialogNode getCurrentNode() {
		if(this.currentNodeId != null && this.nodes.ContainsKey(currentNodeId)) {
			return nodes[currentNodeId];
		}
		return null;
	}

	public void goToNode(string nodeId) {
		if(nodeId == "end" || nodeId == null) {
			currentNodeId = null;
		} else if(nodes.ContainsKey(nodeId)) {
			currentNodeId = nodeId;
		}
	}

	public bool isFinished() {
		if(currentNodeId == null){
			return true;
		}
		return getCurrentNode()?.isEndNode != false;
	}

	public List<DialogResponse> getAvailableResponses(ConditionEvaluator evaluator) {
		DialogNode currentNode = getCurrentNode();
		if(currentNode == null){
			return new List<DialogResponse>();
		}

		List<DialogResponse> availableResponses = new List<DialogResponse>();
		foreach(DialogResponse response in currentNode.responses) {
			if(evaluator.EvaluateAll(response.conditions)) {
				availableResponses.Add(response);
			}
		}

		return availableResponses;
	}

	public void chooseResponse(DialogResponse response, EffectExecutor executor) {
		// Execute response effects
		if(response.effects != null) {
			foreach(string effect in response.effects) {
				executor.execute(effect);
			}
		}

		// Move to next node
		goToNode(response.nextNodeId);
	}
}