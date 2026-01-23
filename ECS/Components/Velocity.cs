using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Components;

public struct Velocity {
	public Vector2 Value;
	public Vector2 Impulse;
	public float Drag;
	public Vector2 Direction;

	public Velocity() {
		Value = Vector2.Zero;
		Impulse = Vector2.Zero;
		Drag = 10;
		Direction = Vector2.UnitY;
	}

	public Velocity(Vector2 value)
		: this() {
		Value = value;
		Direction = Value.LengthSquared() > 0 ? Vector2.Normalize(Value) : Vector2.UnitY;
	}

	public Velocity(float x, float y)
		: this(new Vector2(x, y)) {
	}

	public Velocity(Vector2 value, Vector2 impulse) : this(value) {
		Impulse = impulse;
	}

	public void UpdateVelocity(Vector2 value) {
		Value = value;
		if (Value.LengthSquared() > 0) {
			Direction = Vector2.Normalize(Value);
		}
	}
}