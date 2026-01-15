using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Components;

public struct Collider {
	public int Width;
	public int Height;
	public Vector2 Offset;  // Offset from position

	public Collider(int width, int height, Vector2 offset) {
		Width = width;
		Height = height;
		Offset = offset;
	}

	public Collider(int width, int height) : this(width, height, new Vector2(width / 2, height / 2)) { }

	public Rectangle GetBounds(Position position) {
		return new Rectangle(
			(int)(position.Value.X + Offset.X),
			(int)(position.Value.Y + Offset.Y),
			Width,
			Height
		);
	}
}