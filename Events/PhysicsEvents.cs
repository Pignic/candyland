using Microsoft.Xna.Framework;

namespace EldmeresTale.Events;

public class PropCollectedEvent : GameEvent {
	//public Prop Prop { get; set; }
}

public class PropPushedEvent : GameEvent {
	//public Prop Prop { get; set; }
	public Vector2 PushDirection { get; set; }
}
