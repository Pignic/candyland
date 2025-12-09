using System.Collections.Generic;

namespace Candyland.Dialog
{
    /// <summary>
    /// Represents a single node in a dialog tree
    /// </summary>
    public class DialogNode
    {
        public string Id { get; set; }
        public string TextKey { get; set; }  // Localization key
        public string PortraitKey { get; set; }  // Which portrait to show
        public List<DialogResponse> Responses { get; set; }
        public List<string> Effects { get; set; }  // Effects to execute when this node is shown
        public bool IsEndNode { get; set; }  // True if this ends the conversation

        public DialogNode()
        {
            Responses = new List<DialogResponse>();
            Effects = new List<string>();
            IsEndNode = false;
        }
    }

    /// <summary>
    /// Represents a player response option
    /// </summary>
    public class DialogResponse
    {
        public string TextKey { get; set; }  // Localization key for response text
        public string NextNodeId { get; set; }  // Which node to go to
        public List<string> Conditions { get; set; }  // Conditions that must be met to show this option
        public List<string> Effects { get; set; }  // Effects to execute when this response is chosen

        public DialogResponse()
        {
            Conditions = new List<string>();
            Effects = new List<string>();
        }
    }
}