using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Command;
using EldmeresTale.ECS.Components.Tag;
using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Systems;

public class PickupCollectionSystem : AEntitySetSystem<float> {

	private readonly EntitySet _collectors;

	public PickupCollectionSystem(World world)
		: base(world.GetEntities()
			.With<RoomActive>()
			.With<Position>()
			.With<Collider>()
			.With<Pickup>()
			.AsSet()) {
		_collectors = world.GetEntities()
			.With<RoomActive>()
			.With<Position>()
			.With<Collider>()
			.With<CanCollectPickups>().AsSet();
	}

	protected override void Update(float deltaTime, in Entity entity) {
		Position pos = entity.Get<Position>();
		Collider collider = entity.Get<Collider>();

		// Get pickup bounds
		Rectangle pickupBounds = collider.GetBounds(pos);
		Rectangle collectorBounds;
		foreach (Entity collector in _collectors.GetEntities()) {
			Position collectorPos = collector.Get<Position>();
			Collider collectorCollider = collector.Get<Collider>();
			collectorBounds = collectorCollider.GetBounds(collectorPos);

			// Check collision
			if (pickupBounds.Intersects(collectorBounds)) {
				Pickup pickup = entity.Get<Pickup>();
				World.CreateEntity().Set(new ECSEvent(new Events.PickupCollectedEvent {
					Collector = collector,
					Type = pickup.Type,
					Value = pickup.Value,
					Position = pos.Value
				}, true));

				// Destroy pickup
				entity.Set<ToDispose>();
				return;
			}
		}
	}


	public override void Dispose() {
		_collectors.Dispose();
		base.Dispose();
	}
}
