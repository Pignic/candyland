using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.Worlds;
using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Systems;

public sealed class MovementSystem : AEntitySetSystem<float> {
	private TileMap _currentMap;
	private readonly CollisionSystem _propCollisionSystem;

	public MovementSystem(World world, CollisionSystem propCollisionSystem)
		: base(world.GetEntities()
			.With<Position>()
			.With<Velocity>()
			.With<Collider>()
			.With((in Health h) => !h.IsDead)
			.AsSet()) {
		_propCollisionSystem = propCollisionSystem;
	}

	public void SetCurrentMap(TileMap tileMap) {
		_currentMap = tileMap;
	}

	protected override void Update(float deltaTime, in Entity entity) {
		ref Position pos = ref entity.Get<Position>();
		ref Velocity vel = ref entity.Get<Velocity>();
		Collider collider = entity.Get<Collider>();

		// Calculate desired position
		Vector2 movement = (vel.Value + vel.Impulse) * deltaTime;
		Vector2 desiredPosition = pos.Value + movement;
		// Calculate drag
		vel.Impulse -= vel.Impulse * vel.Drag * deltaTime;

		// Calculate desired bounds
		Rectangle desiredBounds = collider.GetBounds(desiredPosition);

		// Check tilemap collision
		bool tilemapBlocked = _currentMap?.IsRectangleWalkable(desiredBounds) == false;

		// Check prop collision
		bool propsBlocked = _propCollisionSystem?.WouldCollideWithProps(entity, desiredBounds) ?? false;

		// Apply movement if not blocked
		if (!tilemapBlocked && !propsBlocked) {
			pos.Value = desiredPosition;
		} else {
			// Try X only
			desiredBounds = collider.GetBounds(pos.Value + new Vector2(movement.X, 0));
			bool xBlocked = (_currentMap?.IsRectangleWalkable(desiredBounds) == false) ||
							(_propCollisionSystem?.WouldCollideWithProps(entity, desiredBounds) ?? false);
			if (!xBlocked) {
				pos.Value.X += movement.X;
			}

			// Try Y only
			desiredBounds = collider.GetBounds(pos.Value + new Vector2(0, movement.Y));
			bool yBlocked = (_currentMap?.IsRectangleWalkable(desiredBounds) == false) ||
							(_propCollisionSystem?.WouldCollideWithProps(entity, desiredBounds) ?? false);

			if (!yBlocked) {
				pos.Value.Y += movement.Y;
			}
		}
	}
}