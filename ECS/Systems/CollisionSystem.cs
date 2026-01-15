using DefaultEcs;
using EldmeresTale.ECS.Components;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace EldmeresTale.ECS.Systems;

public class CollisionSystem {
	private readonly World _world;
	private readonly EntitySet _collidables;

	public CollisionSystem(World world) {
		_world = world;
		_collidables = world.GetEntities()
			.With<Position>()
			.With<Collider>()
			.AsSet();
	}

	public bool WouldCollideWithProps(Entity testEntity, Rectangle bounds) {
		foreach (Entity entity in _collidables.GetEntities()) {
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
		return false;
	}

	public Rectangle[] GetCollidableBounds() {
		List<Rectangle> bounds = [];
		foreach (Entity entity in _collidables.GetEntities()) {
			Collider collider = entity.Get<Collider>();
			bounds.Add(collider.GetBounds(entity.Get<Position>()));
		}
		return bounds.ToArray();
	}

	public void Dispose() {
		_collidables?.Dispose();
	}
}