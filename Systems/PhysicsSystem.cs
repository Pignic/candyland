using Candyland.Entities;
using Candyland.World;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Candyland.Systems;

public class PhysicsSystem : GameSystem {
	private TileMap _map;
	private List<Prop> _props;

	public void ApplyCollisions(Player player) {
		// Tile collision
		if(_map.checkCollision(player.Bounds)) {
			player.Position = player.PreviousPosition;
		}

		// Prop collision
		foreach(var prop in _props.Where(p => p.isCollidable && p.isActive)) {
			if(prop.Bounds.Intersects(player.Bounds)) {
				if(prop.isPushable) {
					//PushProp(player, prop);
				} else {
					player.Position = player.PreviousPosition;
				}
			}
		}

		// Clamp to world bounds
		//player.Position = ClampToWorldBounds(player.Position, _map.pixelWidth, _map.pixelHeight);
	}

	public void UpdateProps(GameTime time, Rectangle worldBounds) {
		foreach(var prop in _props) {
			prop.Update(time);
			if(prop.isPushable) {
				prop.ApplyWorldBounds(worldBounds);
			}
		}
	}
}