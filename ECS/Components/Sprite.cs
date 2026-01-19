using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EldmeresTale.ECS.Components;

public struct Sprite {
	public Texture2D Texture;
	public Rectangle? SourceRect;
	public Color Tint;
	public float Rotation;
	public Vector2 TextureSize => SourceRect.HasValue ? new Vector2(SourceRect.Value.Width, SourceRect.Value.Height) : new Vector2(Texture.Width, Texture.Height);
	public Vector2 Origin;
	public Vector2 Scale;
	public SpriteEffects Effects;


	public Sprite(Texture2D texture, SpriteEffects? effects = null) {
		Texture = texture;
		SourceRect = null;
		Tint = Color.White;
		Rotation = 0f;
		Origin = Vector2.Zero;
		Scale = Vector2.One;
		Effects = effects ?? SpriteEffects.None;
	}
}