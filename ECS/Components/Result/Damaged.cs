using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Components.Result;

public struct Damaged {
	public float DamageAmount;
	public bool WasCrit;
	public Vector2 Direction;
	public float KnockbackStrength;
}
