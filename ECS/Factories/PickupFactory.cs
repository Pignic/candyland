using DefaultEcs;
using EldmeresTale.Core;
using EldmeresTale.ECS.Components;
using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Factories;

public class PickupFactory {
	private readonly DefaultEcs.World _world;
	private readonly AssetManager _assetManager;

	public PickupFactory(DefaultEcs.World world, AssetManager assetManager) {
		_world = world;
		_assetManager = assetManager;
	}


	public Entity CreatePickup(PickupType type, int value, Vector2 position) {
		Entity entity = _world.CreateEntity();

		entity.Set(new Position(position));
		entity.Set(new Sprite(_assetManager.LoadTexture($"Assets/Sprites/Pickups/{type}.png")));
		entity.Set(new Pickup(type, value));
		entity.Set(new Collider(16, 16));
		entity.Set(new BobAnimation(position.Y));

		// Optional: Auto-destroy after 30 seconds
		entity.Set(new Lifetime(30f));

		return entity;
	}

	// Convenience methods
	public Entity CreateHealthPickup(Vector2 position, int healAmount = 20) {
		return CreatePickup(PickupType.Health, healAmount, position);
	}

	public Entity CreateCoinPickup(Vector2 position, int coinAmount = 1) {
		return CreatePickup(PickupType.Coin, coinAmount, position);
	}

	public Entity CreateXPPickup(Vector2 position, int xpAmount = 10) {
		return CreatePickup(PickupType.XP, xpAmount, position);
	}
}