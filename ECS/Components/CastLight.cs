using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Components;

internal class CastLight {

	public Color Tint;
	public float Radius;
	public Vector2 Direction;

	public CastLight() {
		Tint = new Color();
		Radius = 10;
	}

	public CastLight(Color tint) {
		Tint = tint;
		Radius = 10;
	}

	public CastLight(Color tint, float radius) : this(tint) {
		Radius = radius;
	}

	public CastLight(Color tint, float radius, Vector2 direction) : this(tint, radius) {
		Direction = direction;
	}
}
