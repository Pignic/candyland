using EldmeresTale.Entities;
using EldmeresTale.Events;
using EldmeresTale.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace EldmeresTale.Systems;

public class PhysicsSystem : GameSystem {
	private readonly Player _player;
	private TileMap _map;
	private List<Prop> _props;
	private List<Enemy> _enemies;
	private readonly GameEventBus _eventBus;

	public PhysicsSystem(Player player, GameEventBus eventBus) {
		_eventBus = eventBus;
		_player = player;
		_props = [];
		_enemies = [];
		Enabled = true;
		Visible = false; // Physics doesn't draw anything
	}

	public override void Initialize() {
		System.Diagnostics.Debug.WriteLine("[PHYSICS SYSTEM] Initialized");
	}

	public override void Update(GameTime gameTime) {
		if (!Enabled || _map == null) {
			return;
		}

		// Update prop physics first (pushable movement)
		UpdatePropPhysics(gameTime);

		// Apply player collisions
		ApplyPlayerCollisions();

		// Apply enemy collisions
		ApplyEnemyCollisions();

		// Clamp everything to world bounds
		ClampToWorldBounds();

		// Handle collectible props
		ProcessCollectibles();
	}

	private void UpdatePropPhysics(GameTime gameTime) {
		Rectangle worldBounds = new Rectangle(0, 0, _map.PixelWidth, _map.PixelHeight);

		foreach (Prop prop in _props) {
			if (!prop.IsActive) {
				continue;
			}

			// Update prop (handles pushable movement internally)
			prop.Update(gameTime);

			// Apply world bounds for pushable props
			if (prop.IsPushable) {
				prop.ApplyWorldBounds(worldBounds);
			}
		}
	}

	private void ApplyPlayerCollisions() {
		// Check tile collision
		if (!_map.IsRectangleWalkable(_player.Bounds)) {
			_player.Position = _player.PreviousPosition;
			return; // Don't check props if we're stuck in a tile
		}

		// Check prop collisions
		bool pushedAProp = false;

		foreach (Prop prop in _props) {
			if (!prop.IsCollidable || !prop.IsActive) {
				continue;
			}

			if (!prop.Bounds.Intersects(_player.Bounds)) {
				continue;
			}

			// Pushable props
			if (prop.IsPushable) {
				PushProp(prop);
				pushedAProp = true;
			} else {
				// Solid prop - stop player movement
				_player.Position = _player.PreviousPosition;
				return;
			}
		}

		// If we pushed a prop, check if the prop is now colliding with anything
		if (pushedAProp) {
			// Validate prop positions don't overlap with tiles or other props
			foreach (Prop prop in _props) {
				if (!prop.IsPushable || !prop.IsActive) {
					continue;
				}

				// Check tile collision
				if (!_map.IsRectangleWalkable(prop.Bounds)) {
					// Prop is stuck in wall, undo the push
					_player.Position = _player.PreviousPosition;
					prop.PushVelocity = Vector2.Zero;
				}

				// Check prop-to-prop collision (can't push into another prop)
				foreach (Prop otherProp in _props) {
					if (otherProp == prop) {
						continue;
					}

					if (!otherProp.IsCollidable || !otherProp.IsActive) {
						continue;
					}

					if (prop.Bounds.Intersects(otherProp.Bounds)) {
						// Props are colliding, undo the push
						_player.Position = _player.PreviousPosition;
						prop.PushVelocity = Vector2.Zero;
						return;
					}
				}
			}
		}
	}

	private void PushProp(Prop prop) {
		Vector2 playerCenter = _player.Position + new Vector2(_player.Width / 2f, _player.Height / 2f);
		Vector2 propCenter = prop.Position + new Vector2(prop.Width / 2f, prop.Height / 2f);
		Vector2 pushDirection = propCenter - playerCenter;

		if (pushDirection != Vector2.Zero) {
			pushDirection.Normalize();
			prop.Push(pushDirection, 120f);

			// Fire event
			_eventBus.Publish(new PropPushedEvent {
				Prop = prop,
				PushDirection = pushDirection,
				Position = prop.Position
			});
		}
	}

	private void ApplyEnemyCollisions() {
		foreach (Enemy enemy in _enemies) {
			if (!enemy.IsAlive) {
				continue;
			}

			// Check tile collision
			enemy.ApplyCollisionConstraints(_map);
		}
	}

	private void ClampToWorldBounds() {
		// Clamp player
		_player.Position = new Vector2(
			MathHelper.Clamp(_player.Position.X, 0, _map.PixelWidth - _player.Width),
			MathHelper.Clamp(_player.Position.Y, 0, _map.PixelHeight - _player.Height)
		);

		// Clamp enemies
		foreach (Enemy enemy in _enemies) {
			if (!enemy.IsAlive) {
				continue;
			}

			enemy.Position = new Vector2(
				MathHelper.Clamp(enemy.Position.X, 0, _map.PixelWidth - enemy.Width),
				MathHelper.Clamp(enemy.Position.Y, 0, _map.PixelHeight - enemy.Height)
			);
		}
	}

	private void ProcessCollectibles() {
		// Process in reverse so we can safely remove items
		for (int i = _props.Count - 1; i >= 0; i--) {
			Prop prop = _props[i];

			if (prop.Type != PropType.Collectible) {
				continue;
			}

			if (!prop.IsActive) {
				continue;
			}

			if (!prop.Bounds.Intersects(_player.Bounds)) {
				continue;
			}

			// Collect the item
			prop.IsActive = false;

			// Fire event
			_eventBus.Publish(new PropCollectedEvent {
				Prop = prop,
				Position = prop.Position
			});

			// Remove from list
			_props.RemoveAt(i);
		}
	}

	public override void Draw(SpriteBatch spriteBatch) {
		// Physics doesn't draw anything
	}

	public override void OnRoomChanged(Room newRoom) {
		// Update all room references
		_map = newRoom.Map;
		_props = newRoom.Props;
		_enemies = newRoom.Enemies;

		System.Diagnostics.Debug.WriteLine($"[PHYSICS SYSTEM] Room changed - {_props.Count} props, {_enemies.Count} enemies");
	}

	public override void Dispose() {
		System.Diagnostics.Debug.WriteLine("[PHYSICS SYSTEM] Disposed");
	}
}