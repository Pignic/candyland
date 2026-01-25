using DefaultEcs;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Tag;
using Microsoft.Xna.Framework;
using System;

namespace EldmeresTale.ECS.Systems;

public class CollisionSystem : IDisposable {

	private readonly World _world;

	private readonly EntitySet _propsWithColliders;

	public CollisionSystem(World world) {
		_world = world;
		_propsWithColliders = _world.GetEntities()
			.With<Collider>()
			.With((in Faction f) => f.Name == FactionName.Prop)
			.AsSet();
	}

	public bool WouldCollideWithProps(Entity testEntity, Rectangle bounds) {
		if (testEntity.Has<RoomId>()) {
			string entityRoom = testEntity.Get<RoomId>().Name;
			foreach (Entity entity in _propsWithColliders.GetEntities()) {
				if (entity.Has<RoomId>()) {
					if (entity == testEntity || entityRoom != entity.Get<RoomId>().Name) {
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
		}
		return false;
	}


	public void Dispose() {
		_propsWithColliders?.Dispose();
	}
}