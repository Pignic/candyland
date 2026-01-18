using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Systems;

public class PickupCollectionSystem : AEntitySetSystem<float> {
	private Entities.Player _player;
	private readonly DefaultEcs.World _world;

	public void SetPlayer(Entities.Player player) {
		_player = player;
	}

	// Events
	public event System.Action<PickupType, int> OnPickupCollected;

	public PickupCollectionSystem(World world, Entities.Player player)
		: base(world.GetEntities()
			.With<Position>()
			.With<Collider>()
			.With<Pickup>()
			.AsSet()) {
		_player = player;
		_world = world;
	}

	protected override void Update(float deltaTime, in Entity entity) {
		Position pos = entity.Get<Position>();
		Collider collider = entity.Get<Collider>();
		Pickup pickup = entity.Get<Pickup>();

		// Get pickup bounds
		Rectangle pickupBounds = collider.GetBounds(pos);

		// Get player bounds
		Rectangle playerBounds = _player.Bounds;

		// Check collision
		if (pickupBounds.Intersects(playerBounds)) {
			CollectPickup(entity, pickup);
		}
	}

	private void CollectPickup(Entity entity, Pickup pickup) {
		// Apply effect to player
		switch (pickup.Type) {
			case PickupType.Health:
				_player.Health = System.Math.Min(
					_player.Health + pickup.Value,
					_player.Stats.MaxHealth
				);
				break;

			case PickupType.Coin:
				_player.Coins += pickup.Value;
				break;

			case PickupType.XP:
				_player.XP += pickup.Value;
				break;
		}

		// Fire event
		OnPickupCollected?.Invoke(pickup.Type, pickup.Value);

		// Publish ECS event (other systems can react)
		_world.Publish(new PickupCollectedEvent {
			PickupEntity = entity,
			Type = pickup.Type,
			Value = pickup.Value,
			Position = entity.Get<Position>().Value
		});

		// Destroy pickup entity
		entity.Dispose();
	}
}

// Event struct
public struct PickupCollectedEvent {
	public Entity PickupEntity;
	public PickupType Type;
	public int Value;
	public Vector2 Position;
}