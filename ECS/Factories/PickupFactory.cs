using DefaultEcs;
using EldmeresTale.Core;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Tag;
using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Factories;

public class PickupFactory {
	private readonly World _world;
	private readonly AssetManager _assetManager;

	public PickupFactory(World world, AssetManager assetManager) {
		_world = world;
		_assetManager = assetManager;
	}


	public Entity CreatePickup(PickupType type, Vector2 position, string roomId, int value = 1, Vector2? impulse = null, float zImpulse = 0) {
		Entity entity = _world.CreateEntity();
		entity.Set(new RoomId(roomId));
		entity.Set(new Position(position));
		entity.Set(new Sprite(_assetManager.LoadTexture($"Assets/Sprites/Pickups/{type}.png")));
		entity.Set(new Pickup(type, value));
		entity.Set(new Collider(16, 16));
		entity.Set(new BobAnimation());
		if (impulse.HasValue) {
			entity.Set(new Velocity(Vector2.Zero, impulse.Value));
		}
		if (zImpulse != 0) {
			entity.Set(new Gravity());
			entity.Set(new ZPosition(0, zImpulse));
		}
		return entity;
	}
}