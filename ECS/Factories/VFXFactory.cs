using DefaultEcs;
using EldmeresTale.Core.UI;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Command;
using EldmeresTale.ECS.Components.Tag;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.ECS.Factories;

public class VFXFactory {

	private readonly BitmapFont _font;
	private readonly World _world;

	public VFXFactory(World world, BitmapFont font) {
		_font = font;
		_world = world;
	}

	public Entity CreateDamageNumber(Vector2 position, string text, Color color, float scale) {
		return CreateBaseVFX(position, text, color, scale, 2, 200);
	}

	public Entity CreateLevelUp(Vector2 position) {
		return CreateBaseVFX(position, "LEVEL UP!", Color.Gold, 2, 2, 50);
	}

	private Entity CreateBaseVFX(Vector2 position, string text, Color color, float scale, float lifeTime, float sidewayOffestRange) {
		Entity entity = _world.CreateEntity();
		entity.Set(new Position(position));
		entity.Set(new Collider(0, 0));
		entity.Set(new Lifetime(lifeTime, true));
		entity.Set(new RoomActive());
		entity.Set(new ZPosition(100, 1));
		entity.Set(new Gravity(-1));
		entity.Set(new BobAnimation(5, 5, true, false));
		entity.Set(new Velocity(new Vector2(0, -1), new Vector2((new Random().NextSingle() - 0.5f) * sidewayOffestRange, 0)));
		entity.Set(new RequestSpriteBatch {
			action = (SpriteBatch spriteBatch) => {
				Texture2D texture = _font.GetTexture2D(spriteBatch, text, color, null, null, scale);
				entity.Set(new Sprite(texture, null, false));
			}
		});
		return entity;
	}
}
