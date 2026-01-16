using DefaultEcs;
using EldmeresTale.ECS.Components;
using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Systems;

public class CollisionSystem {
	private readonly World _world;

	public CollisionSystem(World world) {
		_world = world;
	}

	public bool WouldCollideWithProps(Entity testEntity, Rectangle bounds) {
		if (testEntity.Has<RoomId>()) {
			string entityRoom = testEntity.Get<RoomId>().Name;
			EntitySet collidables = _world.GetEntities()
				.With<Collider>()
				.With((in Faction f) => f.Name == FactionName.Prop)
				.With((in RoomId r) => r.Name == entityRoom).AsSet();
			foreach (Entity entity in collidables.GetEntities()) {
				if (entity == testEntity) {
					continue;
				}
				Position pos = entity.Get<Position>();
				Collider collider = entity.Get<Collider>();
				Rectangle propBounds = collider.GetBounds(pos);

				if (bounds.Intersects(propBounds)) {
					return true;
				}
			}
		}
		return false;
	}
}