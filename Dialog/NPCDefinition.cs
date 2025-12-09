using System.Collections.Generic;

namespace Candyland.Dialog
{
    /// <summary>
    /// Defines an NPC's properties and dialog options
    /// </summary>
    public class NPCDefinition
    {
        public string Id { get; set; }
        public string NameKey { get; set; }  // Localization key for NPC name
        public string DefaultPortrait { get; set; }  // Default portrait sprite
        public string RequiresItem { get; set; }  // Item required to talk to this NPC
        public string RefuseDialogKey { get; set; }  // What NPC says if requirement not met
        public List<NPCDialogEntry> Dialogs { get; set; }

        public NPCDefinition()
        {
            Dialogs = new List<NPCDialogEntry>();
        }
    }

    /// <summary>
    /// Represents a conditional dialog option for an NPC
    /// </summary>
    public class NPCDialogEntry
    {
        public string TreeId { get; set; }  // Which dialog tree to use
        public int Priority { get; set; }  // Lower number = higher priority
        public List<string> Conditions { get; set; }  // Conditions that must be met

        public NPCDialogEntry()
        {
            Conditions = new List<string>();
            Priority = 999;  // Default low priority
        }
    }
}