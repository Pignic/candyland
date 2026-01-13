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
		Velocity vel = entity.Get<Velocity>();
		Collider collider = entity.Get<Collider>();

		// Calculate desired position
		Vector2 movement = vel.Value * deltaTime;
		Vector2 desiredPosition = pos.Value + movement;

		// Calculate desired bounds
		Rectangle desiredBounds = new Rectangle(
			(int)desiredPosition.X,
			(int)desiredPosition.Y,
			collider.Width,
			collider.Height
		);

		// Check tilemap collision
		bool tilemapBlocked = _currentMap?.IsRectangleWalkable(desiredBounds) == false;

		// Check prop collision
		bool propsBlocked = _propCollisionSystem?.WouldCollideWithProps(desiredBounds) ?? false;

		// Apply movement if not blocked
		if (!tilemapBlocked && !propsBlocked) {
			pos.Value = desiredPosition;
		} else {
			// Try X only
			Rectangle xOnlyBounds = new Rectangle(
				(int)(pos.Value.X + movement.X),
				(int)pos.Value.Y,
				collider.Width,
				collider.Height
			);

			bool xBlocked = (_currentMap?.IsRectangleWalkable(xOnlyBounds) == false) ||
							(_propCollisionSystem?.WouldCollideWithProps(xOnlyBounds) ?? false);

			if (!xBlocked) {
				pos.Value.X += movement.X;
			}

			// Try Y only
			Rectangle yOnlyBounds = new Rectangle(
				(int)pos.Value.X,
				(int)(pos.Value.Y + movement.Y),
				collider.Width,
				collider.Height
			);

			bool yBlocked = (_currentMap?.IsRectangleWalkable(yOnlyBounds) == false) ||
							(_propCollisionSystem?.WouldCollideWithProps(yOnlyBounds) ?? false);

			if (!yBlocked) {
				pos.Value.Y += movement.Y;
			}
		}
	}

	// Update map reference when room changes
	public void SetMap(TileMap map) {
		// Store in field if needed
	}
}