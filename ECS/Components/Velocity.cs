using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Components;

public struct Velocity {
	public Vector2 Value;
	public Vector2 Impulse;
	public float Drag;

	public Velocity() {
		Value = Vector2.Zero;
		Impulse = Vector2.Zero;
		Drag = 10;
	}

	public Velocity(Vector2 value)
		: this() {
		Value = value;
	}

	public Velocity(float x, float y)
		: this(new Vector2(x, y)) {
	}
}
