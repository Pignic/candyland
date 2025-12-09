using System.Collections.Generic;

namespace Candyland.Dialog
{
    /// <summary>
    /// Represents a complete dialog tree (conversation)
    /// </summary>
    public class DialogTree
    {
        public string Id { get; set; }
        public string NpcId { get; set; }
        public string StartNodeId { get; set; }
        public Dictionary<string, DialogNode> Nodes { get; set; }

        private string _currentNodeId;

        public DialogTree()
        {
            Nodes = new Dictionary<string, DialogNode>();
        }

        /// <summary>
        /// Start or restart the dialog tree
        /// </summary>
        public void Start()
        {
            _currentNodeId = StartNodeId;
        }

        /// <summary>
        /// Get the current dialog node
        /// </summary>
        public DialogNode GetCurrentNode()
        {
            if (_currentNodeId != null && Nodes.ContainsKey(_currentNodeId))
            {
                return Nodes[_currentNodeId];
            }
            return null;
        }

        /// <summary>
        /// Move to a specific node
        /// </summary>
        public void GoToNode(string nodeId)
        {
            if (nodeId == "end" || nodeId == null)
            {
                _currentNodeId = null;
            }
            else if (Nodes.ContainsKey(nodeId))
            {
                _currentNodeId = nodeId;
            }
        }

        /// <summary>
        /// Check if dialog is finished
        /// </summary>
        public bool IsFinished()
        {
            if (_currentNodeId == null)
                return true;

            var currentNode = GetCurrentNode();
            return currentNode == null || currentNode.IsEndNode;
        }

        /// <summary>
        /// Get available responses for current node that meet conditions
        /// </summary>
        public List<DialogResponse> GetAvailableResponses(ConditionEvaluator evaluator)
        {
            var currentNode = GetCurrentNode();
            if (currentNode == null)
                return new List<DialogResponse>();

            var availableResponses = new List<DialogResponse>();
            foreach (var response in currentNode.Responses)
            {
                if (evaluator.EvaluateAll(response.Conditions))
                {
                    availableResponses.Add(response);
                }
            }

            return availableResponses;
        }

        /// <summary>
        /// Choose a response and move to next node
        /// </summary>
        public void ChooseResponse(DialogResponse response, EffectExecutor executor)
        {
            // Execute response effects
            if (response.Effects != null)
            {
                foreach (var effect in response.Effects)
                {
                    executor.Execute(effect);
                }
            }

            // Move to next node
            GoToNode(response.NextNodeId);
        }
    }
}