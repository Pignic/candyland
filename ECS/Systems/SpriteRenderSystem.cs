using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.Core;
using EldmeresTale.Core.UI;
using EldmeresTale.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;

namespace EldmeresTale.ECS.Systems;

public sealed class SpriteRenderSystem : AEntitySetSystem<SpriteBatch> {
	private readonly Camera _camera;
	private readonly BitmapFont _font;

	public SpriteRenderSystem(DefaultEcs.World world, Camera camera, BitmapFont font)
		: base(world.GetEntities()
			.With<Position>()
			.With<Sprite>()
			.AsSet()) {
		_camera = camera;
		_font = font;
	}

	protected override void Update(SpriteBatch spriteBatch, ReadOnlySpan<Entity> entities) {
		// Sort by Y for depth
		var sorted = entities.ToArray()
			.OrderBy(e => e.Get<Position>().Value.Y);

		foreach (var entity in sorted) {
			DrawProp(spriteBatch, entity);
		}

		// Draw interaction prompts on top
		foreach (var entity in entities) {
			if (entity.Has<InteractionZone>()) {
				DrawInteractionPrompt(spriteBatch, entity);
			}
		}
	}

	private void DrawProp(SpriteBatch spriteBatch, Entity entity) {
		var pos = entity.Get<Position>();
		var sprite = entity.Get<Sprite>();

		if (sprite.Texture == null) {
			return;
		}

		spriteBatch.Draw(
			sprite.Texture,
			pos.Value,
			sprite.SourceRect,
			sprite.Tint,
			sprite.Rotation,
			sprite.Origin,
			sprite.Scale,
			sprite.Effects,
			0f
		);
	}

	private void DrawInteractionPrompt(SpriteBatch spriteBatch, Entity entity) {
		var zone = entity.Get<InteractionZone>();

		if (!zone.IsPlayerNearby) {
			return;
		}

		var pos = entity.Get<Position>();

		// Draw "E" indicator above prop
		string text = "E";
		int textWidth = _font.MeasureString(text);
		int textHeight = _font.GetHeight();

		Vector2 textPos = new Vector2(
			pos.Value.X + 16 - (textWidth / 2),  // Center over 32x32 prop
			pos.Value.Y - 20  // Above prop
		);

		// Draw with slight bob effect
		float bobOffset = MathF.Sin((float)DateTime.Now.TimeOfDay.TotalSeconds * 3f) * 2f;
		textPos.Y += bobOffset;

		_font.DrawText(spriteBatch, text, textPos, Color.Yellow, scale: 2);
	}
}