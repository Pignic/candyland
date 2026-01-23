using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.Worlds;
using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Systems;

public sealed class MovementSystem : AEntitySetSystem<float> {
	private TileMap _currentMap;
	private readonly CollisionSystem _propCollisionSystem;
	private readonly EntitySet _velocityEntities;

	public MovementSystem(World world, CollisionSystem propCollisionSystem)
		: base(world.GetEntities()
			.With<Position>()
			.With<Velocity>()
			.With<Collider>()
			.AsSet()) {
		_propCollisionSystem = propCollisionSystem;
		_velocityEntities = world.GetEntities().With<Velocity>().AsSet();
	}

	public void SetCurrentMap(TileMap tileMap) {
		_currentMap = tileMap;
	}

	protected override void PostUpdate(float deltaTime) {
		foreach (Entity entity in _velocityEntities.GetEntities()) {
			ref Velocity vel = ref entity.Get<Velocity>();
			// Calculate drag
			vel.Impulse -= vel.Impulse * vel.Drag * deltaTime;
			// Update last direction
			vel.UpdateVelocity(vel.Value);
		}
		base.PostUpdate(deltaTime);
	}

	protected override void Update(float deltaTime, in Entity entity) {
		ref Position pos = ref entity.Get<Position>();
		ref Velocity vel = ref entity.Get<Velocity>();
		Collider collider = entity.Get<Collider>();

		// Calculate desired position
		Vector2 movement = (vel.Value + vel.Impulse) * deltaTime;

		// Check tilemap collision
		TileMap.MovementResult tileMapMovement = _currentMap.ResolveMovement(collider.GetBounds(pos), movement);
		Rectangle desiredBounds = collider.GetBounds(pos.Value + tileMapMovement.Movement);
		vel.Impulse *= new Vector2(
			tileMapMovement.BlockedVelocity.X != 0 ? -0.5f : 1f,  // Bounce with 50% energy loss
			tileMapMovement.BlockedVelocity.Y != 0 ? -0.5f : 1f
		);

		// Check prop collision
		bool propsBlocked = _propCollisionSystem?.WouldCollideWithProps(entity, desiredBounds) ?? false;

		// Apply movement if not blocked
		if (!propsBlocked) {
			pos.Value += tileMapMovement.Movement;
		} else {
			// Bounce off props too
			Vector2 propBounce = Vector2.Zero;

			// Try X only
			Vector2 xOnlyPos = pos.Value + new Vector2(tileMapMovement.Movement.X, 0);
			Rectangle xBounds = collider.GetBounds(new Position(xOnlyPos));
			bool xBlocked = _propCollisionSystem?.WouldCollideWithProps(entity, xBounds) ?? false;

			if (!xBlocked) {
				pos.Value.X = xOnlyPos.X;
			} else {
				propBounce.X = -0.5f;  // Mark X for bouncing
			}

			// Try Y only
			Vector2 yOnlyPos = pos.Value + new Vector2(0, tileMapMovement.Movement.Y);
			Rectangle yBounds = collider.GetBounds(new Position(yOnlyPos));
			bool yBlocked = _propCollisionSystem?.WouldCollideWithProps(entity, yBounds) ?? false;

			if (!yBlocked) {
				pos.Value.Y = yOnlyPos.Y;
			} else {
				propBounce.Y = -0.5f;  // Mark Y for bouncing
			}

			// Apply bounce to impulse
			if (propBounce != Vector2.Zero) {
				vel.Impulse = new Vector2(
					propBounce.X != 0 ? vel.Impulse.X * propBounce.X : vel.Impulse.X,
					propBounce.Y != 0 ? vel.Impulse.Y * propBounce.Y : vel.Impulse.Y
				);
			}
		}
	}
}