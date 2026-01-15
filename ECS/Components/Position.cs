using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Components;

public struct Position {

	public Vector2 Value;

	public Position(float x, float y)
		: this(new Vector2(x, y)) {
	}

	public Position(Vector2 value) {
		Value = value;
	}
}