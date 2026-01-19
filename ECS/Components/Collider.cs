using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Components;

public struct Collider {
	public int Width;
	public int Height;
	public Vector2 Offset;

	public Collider(int width, int height, Vector2 offset) {
		Width = width;
		Height = height;
		Offset = offset;
	}

	public Collider(int width, int height) : this(width, height, new Vector2(0, 0)) { }

	public Vector2 GetCenter(Position pos) {
		return new Vector2(
			pos.Value.X + Offset.X,
			pos.Value.Y + Offset.Y - (Height / 2f)
		);
	}

	public Rectangle GetBounds(Position position) {
		return GetBounds(position.Value);
	}

	public Rectangle GetBounds(Vector2 position) {
		return new Rectangle(
			(int)(position.X + Offset.X - (Width / 2f)),
			(int)(position.Y + Offset.Y - Height),
			Width,
			Height
		);
	}
}