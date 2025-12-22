using Candyland.Entities;
using Candyland.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Candyland.Systems;

public class PhysicsSystem : GameSystem {
	private readonly Player _player;
	private TileMap _map;
	private List<Prop> _props;
	private List<Enemy> _enemies;

	// Physics events
	public event Action<Prop> OnPropCollected;
	public event Action<Prop, Vector2> OnPropPushed;

	public PhysicsSystem(Player player) {
		_player = player;
		_props = new List<Prop>();
		_enemies = new List<Enemy>();
		Enabled = true;
		Visible = false; // Physics doesn't draw anything
	}

	public override void Initialize() {
		System.Diagnostics.Debug.WriteLine("[PHYSICS SYSTEM] Initialized");
	}

	public override void Update(GameTime gameTime) {
		if(!Enabled || _map == null) return;

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
		Rectangle worldBounds = new Rectangle(0, 0, _map.pixelWidth, _map.pixelHeight);

		foreach(var prop in _props) {
			if(!prop.isActive) continue;

			// Update prop (handles pushable movement internally)
			prop.Update(gameTime);

			// Apply world bounds for pushable props
			if(prop.isPushable) {
				prop.ApplyWorldBounds(worldBounds);
			}
		}
	}

	private void ApplyPlayerCollisions() {
		// Check tile collision
		if(_map.checkCollision(_player.Bounds)) {
			_player.Position = _player.PreviousPosition;
			return; // Don't check props if we're stuck in a tile
		}

		// Check prop collisions
		bool pushedAProp = false;

		foreach(var prop in _props) {
			if(!prop.isCollidable || !prop.isActive) continue;
			if(!prop.Bounds.Intersects(_player.Bounds)) continue;

			// Pushable props
			if(prop.isPushable) {
				PushProp(prop);
				pushedAProp = true;
			} else {
				// Solid prop - stop player movement
				_player.Position = _player.PreviousPosition;
				return;
			}
		}

		// If we pushed a prop, check if the prop is now colliding with anything
		if(pushedAProp) {
			// Validate prop positions don't overlap with tiles or other props
			foreach(Prop prop in _props) {
				if(!prop.isPushable || !prop.isActive) continue;

				// Check tile collision
				if(_map.checkCollision(prop.Bounds)) {
					// Prop hit a wall, undo the push
					_player.Position = _player.PreviousPosition;
					prop.pushVelocity = Vector2.Zero; // Stop the prop
					return;
				}

				// Check prop-to-prop collision (can't push into another prop)
				foreach(var otherProp in _props) {
					if(otherProp == prop) continue;
					if(!otherProp.isCollidable || !otherProp.isActive) continue;

					if(prop.Bounds.Intersects(otherProp.Bounds)) {
						// Props are colliding, undo the push
						_player.Position = _player.PreviousPosition;
						prop.pushVelocity = Vector2.Zero;
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

		if(pushDirection != Vector2.Zero) {
			pushDirection.Normalize();
			prop.Push(pushDirection, 120f);

			// Fire event
			OnPropPushed?.Invoke(prop, pushDirection);
		}
	}

	private void ApplyEnemyCollisions() {
		foreach(var enemy in _enemies) {
			if(!enemy.IsAlive) continue;

			// Check tile collision
			enemy.ApplyCollisionConstraints(_map);
		}
	}

	private void ClampToWorldBounds() {
		// Clamp player
		_player.Position = new Vector2(
			MathHelper.Clamp(_player.Position.X, 0, _map.pixelWidth - _player.Width),
			MathHelper.Clamp(_player.Position.Y, 0, _map.pixelHeight - _player.Height)
		);

		// Clamp enemies
		foreach(var enemy in _enemies) {
			if(!enemy.IsAlive) continue;

			enemy.Position = new Vector2(
				MathHelper.Clamp(enemy.Position.X, 0, _map.pixelWidth - enemy.Width),
				MathHelper.Clamp(enemy.Position.Y, 0, _map.pixelHeight - enemy.Height)
			);
		}
	}

	private void ProcessCollectibles() {
		// Process in reverse so we can safely remove items
		for(int i = _props.Count - 1; i >= 0; i--) {
			var prop = _props[i];

			if(prop.type != PropType.Collectible) continue;
			if(!prop.isActive) continue;
			if(!prop.Bounds.Intersects(_player.Bounds)) continue;

			// Collect the item
			prop.isActive = false;

			// Fire event
			OnPropCollected?.Invoke(prop);

			// Remove from list
			_props.RemoveAt(i);
		}
	}

	public void SetMap(TileMap map) {
		_map = map;
	}

	public void SetProps(List<Prop> props) {
		_props = props;
	}

	public void SetEnemies(List<Enemy> enemies) {
		_enemies = enemies;
	}

	public override void Draw(SpriteBatch spriteBatch) {
		// Physics doesn't draw anything
	}

	public override void Dispose() {
		OnPropCollected = null;
		OnPropPushed = null;
		System.Diagnostics.Debug.WriteLine("[PHYSICS SYSTEM] Disposed");
	}
}