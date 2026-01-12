using EldmeresTale.Entities;
using Microsoft.Xna.Framework;

namespace EldmeresTale.Events;

// ===== ENEMY EVENTS =====

public class EnemyHitEvent : GameEvent {
	public Enemy Enemy { get; set; }
	public int Damage { get; set; }
	public bool WasCritical { get; set; }
	public Vector2 DamagePosition { get; set; }
}

public class EnemyKilledEvent : GameEvent {
	public Enemy Enemy { get; set; }
	public Vector2 DeathPosition { get; set; }
}

// ===== PROP EVENTS =====

public class PropHitEvent : GameEvent {
	//public Prop Prop { get; set; }
	public int Damage { get; set; }
	public bool WasCritical { get; set; }
	public Vector2 DamagePosition { get; set; }
}

public class PropDestroyedEvent : GameEvent {
	//public Prop Prop { get; set; }
	public Vector2 DestructionPosition { get; set; }
}

// ===== PLAYER EVENTS =====

public class PlayerHitEvent : GameEvent {
	public Enemy AttackingEnemy { get; set; }
	public int Damage { get; set; }
	public Vector2 DamagePosition { get; set; }
}

public class PlayerDeathEvent : GameEvent {
	public Vector2 DeathPosition { get; set; }
}

public class PlayerAttackEvent : GameEvent {
	public ActorEntity Actor { get; set; }
}

public class PlayerDodgeEvent : GameEvent {
	public Vector2 DodgeDirection { get; set; }
}

public class PlayerLevelUpEvent : GameEvent {
	public int NewLevel { get; set; }
	public int XpGained { get; set; }
}
