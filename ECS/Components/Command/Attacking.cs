using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Components.Command;

public struct Attacking {
	public FactionName AttackerFaction;
	public Vector2 Origin;
	public float Angle;
	public Vector2 Direction;

	public int AttackDamage;
	public float AttackRange;
	public float CritChance;
	public float CritMultiplier;
}
