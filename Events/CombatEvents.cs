using EldmeresTale.Entities;
using Microsoft.Xna.Framework;

namespace EldmeresTale.Events;


public class PlayerDeathEvent : GameEvent {
	public Vector2 DeathPosition { get; set; }
}

public class PlayerAttackEvent : GameEvent {
	public Player Player { get; set; }
}

public class PlayerDodgeEvent : GameEvent {
	public Vector2 DodgeDirection { get; set; }
}

public class PlayerLevelUpEvent : GameEvent {
	public int NewLevel { get; set; }
	public int XpGained { get; set; }
}
