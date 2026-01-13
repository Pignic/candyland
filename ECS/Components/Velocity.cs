using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Components;

public struct Velocity {
	public Vector2 Value;

	public Velocity(Vector2 value) {
		Value = value;
	}

	public Velocity(float x, float y) {
		Value = new Vector2(x, y);
	}

	public static implicit operator Vector2(Velocity vel) => vel.Value;
	public static implicit operator Velocity(Vector2 vec) => new Velocity(vec);
}
