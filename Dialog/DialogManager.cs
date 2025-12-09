using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using Candyland.Entities;

namespace Candyland.Dialog
{
    /// <summary>
    /// Main manager for the dialog system
    /// </summary>
    public class DialogManager
    {
        // Core systems
        public LocalizationManager Localization { get; private set; }
        public GameStateManager GameState { get; private set; }
        public ConditionEvaluator ConditionEvaluator { get; private set; }
        public EffectExecutor EffectExecutor { get; private set; }

        // Dialog data
        private Dictionary<string, DialogTree> _dialogTrees;
        private Dictionary<string, NPCDefinition> _npcDefinitions;

        // Current state
        private DialogTree _currentDialog;
        private NPCDefinition _currentNPC;
        public bool IsDialogActive => _currentDialog != null && !_currentDialog.IsFinished();

        public DialogManager(Player player)
        {
            Localization = new LocalizationManager();
            GameState = new GameStateManager();
            ConditionEvaluator = new ConditionEvaluator(player, GameState);
            EffectExecutor = new EffectExecutor(player, GameState);

            _dialogTrees = new Dictionary<string, DialogTree>();
            _npcDefinitions = new Dictionary<string, NPCDefinition>();
        }

        #region Loading

        /// <summary>
        /// Load dialog trees from a JSON file
        /// </summary>
        public void LoadDialogTrees(string filepath)
        {
            if (!File.Exists(filepath))
            {
                System.Diagnostics.Debug.WriteLine($"Dialog tree file not found: {filepath}");
                return;
            }

            try
            {
                string json = File.ReadAllText(filepath);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("dialogTrees", out var treesElement))
                {
                    foreach (var treeProperty in treesElement.EnumerateObject())
                    {
                        var tree = ParseDialogTree(treeProperty.Value);
                        if (tree != null)
                        {
                            _dialogTrees[tree.Id] = tree;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {_dialogTrees.Count} dialog trees from {filepath}");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dialog trees: {ex.Message}");
            }
        }

        private DialogTree ParseDialogTree(JsonElement treeElement)
        {
            var tree = new DialogTree();

            if (treeElement.TryGetProperty("id", out var idProp))
                tree.Id = idProp.GetString();

            if (treeElement.TryGetProperty("npcId", out var npcProp))
                tree.NpcId = npcProp.GetString();

            if (treeElement.TryGetProperty("startNode", out var startProp))
                tree.StartNodeId = startProp.GetString();

            if (treeElement.TryGetProperty("nodes", out var nodesElement))
            {
                foreach (var nodeProperty in nodesElement.EnumerateObject())
                {
                    string nodeId = nodeProperty.Name;
                    var node = ParseDialogNode(nodeProperty.Value, nodeId);
                    tree.Nodes[nodeId] = node;
                }
            }

            return tree;
        }

        private DialogNode ParseDialogNode(JsonElement nodeElement, string nodeId)
        {
            var node = new DialogNode { Id = nodeId };

            if (nodeElement.TryGetProperty("text", out var textProp))
                node.TextKey = textProp.GetString();

            if (nodeElement.TryGetProperty("portrait", out var portraitProp))
                node.PortraitKey = portraitProp.GetString();

            if (nodeElement.TryGetProperty("effects", out var effectsElement))
            {
                foreach (var effect in effectsElement.EnumerateArray())
                {
                    node.Effects.Add(effect.GetString());
                }
            }

            if (nodeElement.TryGetProperty("responses", out var responsesElement))
            {
                foreach (var responseElement in responsesElement.EnumerateArray())
                {
                    var response = ParseDialogResponse(responseElement);
                    node.Responses.Add(response);
                }
            }

            // Check if this is an end node (no responses or nextNode is "end")
            node.IsEndNode = node.Responses.Count == 0;

            return node;
        }

        private DialogResponse ParseDialogResponse(JsonElement responseElement)
        {
            var response = new DialogResponse();

            if (responseElement.TryGetProperty("text", out var textProp))
                response.TextKey = textProp.GetString();

            if (responseElement.TryGetProperty("nextNode", out var nextProp))
                response.NextNodeId = nextProp.GetString();

            if (responseElement.TryGetProperty("conditions", out var conditionsElement))
            {
                foreach (var condition in conditionsElement.EnumerateArray())
                {
                    response.Conditions.Add(condition.GetString());
                }
            }

            if (responseElement.TryGetProperty("effects", out var effectsElement))
            {
                foreach (var effect in effectsElement.EnumerateArray())
                {
                    response.Effects.Add(effect.GetString());
                }
            }

            return response;
        }

        /// <summary>
        /// Load NPC definitions from a JSON file
        /// </summary>
        public void LoadNPCDefinitions(string filepath)
        {
            if (!File.Exists(filepath))
            {
                System.Diagnostics.Debug.WriteLine($"NPC definitions file not found: {filepath}");
                return;
            }

            try
            {
                string json = File.ReadAllText(filepath);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("npcs", out var npcsElement))
                {
                    foreach (var npcProperty in npcsElement.EnumerateObject())
                    {
                        string npcId = npcProperty.Name;
                        var npc = ParseNPCDefinition(npcProperty.Value, npcId);
                        _npcDefinitions[npcId] = npc;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {_npcDefinitions.Count} NPC definitions from {filepath}");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading NPC definitions: {ex.Message}");
            }
        }

        public NPCDefinition GetNPCDefinition(string npcId)
        {
            return _npcDefinitions[npcId];
        }

        private NPCDefinition ParseNPCDefinition(JsonElement npcElement, string npcId)
        {
            var npc = new NPCDefinition { Id = npcId };

            if (npcElement.TryGetProperty("name", out var nameProp))
                npc.NameKey = nameProp.GetString();

            if (npcElement.TryGetProperty("defaultPortrait", out var portraitProp))
                npc.DefaultPortrait = portraitProp.GetString();

            if (npcElement.TryGetProperty("requiresItem", out var itemProp))
                npc.RequiresItem = itemProp.GetString();

            if (npcElement.TryGetProperty("refuseDialog", out var refuseProp))
                npc.RefuseDialogKey = refuseProp.GetString();

            if (npcElement.TryGetProperty("dialogs", out var dialogsElement))
            {
                foreach (var dialogElement in dialogsElement.EnumerateArray())
                {
                    var dialogEntry = new NPCDialogEntry();

                    if (dialogElement.TryGetProperty("treeId", out var treeProp))
                        dialogEntry.TreeId = treeProp.GetString();

                    if (dialogElement.TryGetProperty("priority", out var priorityProp))
                        dialogEntry.Priority = priorityProp.GetInt32();

                    if (dialogElement.TryGetProperty("conditions", out var conditionsElement))
                    {
                        foreach (var condition in conditionsElement.EnumerateArray())
                        {
                            dialogEntry.Conditions.Add(condition.GetString());
                        }
                    }

                    npc.Dialogs.Add(dialogEntry);
                }
            }

            return npc;
        }

        #endregion

        #region Dialog Control

        /// <summary>
        /// Start a dialog with an NPC
        /// </summary>
        public bool StartDialog(string npcId)
        {
            if (!_npcDefinitions.ContainsKey(npcId))
            {
                System.Diagnostics.Debug.WriteLine($"NPC not found: {npcId}");
                return false;
            }

            _currentNPC = _npcDefinitions[npcId];

            // Check if NPC requires an item
            if (!string.IsNullOrEmpty(_currentNPC.RequiresItem))
            {
                if (!GameState.HasItem(_currentNPC.RequiresItem))
                {
                    // Show refuse dialog
                    System.Diagnostics.Debug.WriteLine($"NPC refuses: missing item {_currentNPC.RequiresItem}");
                    return false;
                }
            }

            // Find the appropriate dialog tree based on conditions and priority
            string treeId = GetNPCDialogTree(npcId);

            if (string.IsNullOrEmpty(treeId) || !_dialogTrees.ContainsKey(treeId))
            {
                System.Diagnostics.Debug.WriteLine($"No valid dialog tree found for NPC: {npcId}");
                return false;
            }

            _currentDialog = _dialogTrees[treeId];
            _currentDialog.Start();

            // Execute effects for the starting node
            var startNode = _currentDialog.GetCurrentNode();
            if (startNode != null && startNode.Effects != null)
            {
                foreach (var effect in startNode.Effects)
                {
                    EffectExecutor.Execute(effect);
                }
            }

            System.Diagnostics.Debug.WriteLine($"Started dialog: {treeId} with NPC: {npcId}");
            return true;
        }

        /// <summary>
        /// Get the appropriate dialog tree for an NPC based on conditions
        /// </summary>
        private string GetNPCDialogTree(string npcId)
        {
            // Check for overridden dialog tree
            string overrideTree = GameState.GetNPCDialogTree(npcId);
            if (!string.IsNullOrEmpty(overrideTree))
                return overrideTree;

            // Find dialog based on conditions and priority
            var npc = _npcDefinitions[npcId];
            var sortedDialogs = new List<NPCDialogEntry>(npc.Dialogs);
            sortedDialogs.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            foreach (var dialogEntry in sortedDialogs)
            {
                if (ConditionEvaluator.EvaluateAll(dialogEntry.Conditions))
                {
                    return dialogEntry.TreeId;
                }
            }

            return null;
        }

        /// <summary>
        /// Choose a response and advance dialog
        /// </summary>
        public void ChooseResponse(int responseIndex)
        {
            if (_currentDialog == null)
                return;

            var availableResponses = _currentDialog.GetAvailableResponses(ConditionEvaluator);
            if (responseIndex < 0 || responseIndex >= availableResponses.Count)
                return;

            var chosenResponse = availableResponses[responseIndex];
            _currentDialog.ChooseResponse(chosenResponse, EffectExecutor);

            // Execute effects for the new node
            var currentNode = _currentDialog.GetCurrentNode();
            if (currentNode != null && currentNode.Effects != null)
            {
                foreach (var effect in currentNode.Effects)
                {
                    EffectExecutor.Execute(effect);
                }
            }

            // Check if dialog ended
            if (_currentDialog.IsFinished())
            {
                EndDialog();
            }
        }

        /// <summary>
        /// End the current dialog
        /// </summary>
        public void EndDialog()
        {
            _currentDialog = null;
            _currentNPC = null;
            System.Diagnostics.Debug.WriteLine("Dialog ended");
        }

        /// <summary>
        /// Get current dialog node
        /// </summary>
        public DialogNode GetCurrentNode()
        {
            return _currentDialog?.GetCurrentNode();
        }

        /// <summary>
        /// Get available responses for current node
        /// </summary>
        public List<DialogResponse> GetAvailableResponses()
        {
            if (_currentDialog == null)
                return new List<DialogResponse>();

            return _currentDialog.GetAvailableResponses(ConditionEvaluator);
        }

        /// <summary>
        /// Get current NPC definition
        /// </summary>
        public NPCDefinition GetCurrentNPC()
        {
            return _currentNPC;
        }

        #endregion
    }
}