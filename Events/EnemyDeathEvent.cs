using Microsoft.Xna.Framework;

namespace EldmeresTale.Events;

public class EnemyDeathEvent : GameEvent {
	public string EnemyType;
	public Vector2 DeathPosition;
}
