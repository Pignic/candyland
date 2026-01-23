using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Command;
using EldmeresTale.ECS.Components.Tag;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.ECS.Systems;

public sealed class AttackVisualizationSystem : AEntitySetSystem<SpriteBatch> {
	private readonly Texture2D _pixel;
	private readonly EntitySet _potentialTargets;

	public AttackVisualizationSystem(World world, Texture2D pixel)
		: base(world.GetEntities()
			.With<Attacking>()
			.AsSet()) {
		_pixel = pixel;

		_potentialTargets = world.GetEntities()
			.With<RoomActive>()
			.With<Position>()
			.With<Collider>()
			.With<Faction>()
			.AsSet();
	}

	protected override void Update(SpriteBatch spriteBatch, in Entity entity) {

		Attacking attack = entity.Get<Attacking>();

		// Draw attack range circle
		DrawCircle(spriteBatch, attack.Origin, attack.AttackRange, Color.Yellow * 0.3f, 32);

		// Draw attack cone
		DrawCone(spriteBatch, attack.Origin, attack.Direction, attack.Angle, attack.AttackRange, Color.Red * 0.4f, 16);

		// Draw origin point
		DrawCircle(spriteBatch, attack.Origin, 3f, Color.White, 8);

		// Draw direction line
		Vector2 directionEnd = attack.Origin + (attack.Direction * attack.AttackRange);
		DrawLine(spriteBatch, attack.Origin, directionEnd, Color.Cyan * 0.6f, 2f);

		foreach (ref readonly Entity target in _potentialTargets.GetEntities()) {
			Faction faction = target.Get<Faction>();
			if (faction.Name == attack.AttackerFaction) {
				continue;  // Skip same faction
			}

			Position pos = target.Get<Position>();
			Collider col = target.Get<Collider>();

			Rectangle bounds = col.GetBounds(pos);
			Color hitboxColor = Color.Green * 0.5f;

			// Check if this target would be hit by the attack
			if (WouldBeHit(attack, pos, col)) {
				hitboxColor = Color.Red * 0.7f;  // Red if in attack range/cone
			}

			DrawRectangle(spriteBatch, bounds, hitboxColor);
		}
	}

	private void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, int segments) {
		float angleStep = MathHelper.TwoPi / segments;

		for (int i = 0; i < segments; i++) {
			float angle1 = i * angleStep;
			float angle2 = (i + 1) * angleStep;

			Vector2 p1 = center + new Vector2(
				MathF.Cos(angle1) * radius,
				MathF.Sin(angle1) * radius
			);
			Vector2 p2 = center + new Vector2(
				MathF.Cos(angle2) * radius,
				MathF.Sin(angle2) * radius
			);

			DrawLine(spriteBatch, p1, p2, color, 1f);
		}
	}

	private void DrawCone(SpriteBatch spriteBatch, Vector2 origin, Vector2 direction, float coneAngle, float range, Color color, int segments) {
		float baseAngle = MathF.Atan2(direction.Y, direction.X);
		float halfAngle = coneAngle * 0.5f;
		float startAngle = baseAngle - halfAngle;
		float endAngle = baseAngle + halfAngle;

		float angleStep = (endAngle - startAngle) / segments;

		// Draw cone edges
		Vector2 edgeStart1 = origin + new Vector2(
			MathF.Cos(startAngle) * range,
			MathF.Sin(startAngle) * range
		);
		Vector2 edgeStart2 = origin + new Vector2(
			MathF.Cos(endAngle) * range,
			MathF.Sin(endAngle) * range
		);

		DrawLine(spriteBatch, origin, edgeStart1, color, 2f);
		DrawLine(spriteBatch, origin, edgeStart2, color, 2f);

		// Draw cone arc
		for (int i = 0; i < segments; i++) {
			float angle1 = startAngle + (i * angleStep);
			float angle2 = startAngle + ((i + 1) * angleStep);

			Vector2 p1 = origin + new Vector2(
				MathF.Cos(angle1) * range,
				MathF.Sin(angle1) * range
			);
			Vector2 p2 = origin + new Vector2(
				MathF.Cos(angle2) * range,
				MathF.Sin(angle2) * range
			);

			DrawLine(spriteBatch, p1, p2, color, 1f);
		}

		// Fill cone with transparent triangles (optional)
		for (int i = 0; i <= segments; i++) {
			float angle = startAngle + (i * angleStep);
			Vector2 point = origin + new Vector2(
				MathF.Cos(angle) * range,
				MathF.Sin(angle) * range
			);

			// Draw line from origin to arc
			DrawLine(spriteBatch, origin, point, color * 0.3f, 1f);
		}
	}

	private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness) {
		Vector2 diff = end - start;
		float length = diff.Length();
		float angle = MathF.Atan2(diff.Y, diff.X);

		spriteBatch.Draw(
			_pixel,
			start,
			null,
			color,
			angle,
			Vector2.Zero,
			new Vector2(length, thickness),
			SpriteEffects.None,
			0f
		);
	}
	private void DrawRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color) {
		// Draw 4 edges
		DrawLine(spriteBatch, new Vector2(rect.Left, rect.Top), new Vector2(rect.Right, rect.Top), color, 1f);
		DrawLine(spriteBatch, new Vector2(rect.Right, rect.Top), new Vector2(rect.Right, rect.Bottom), color, 1f);
		DrawLine(spriteBatch, new Vector2(rect.Right, rect.Bottom), new Vector2(rect.Left, rect.Bottom), color, 1f);
		DrawLine(spriteBatch, new Vector2(rect.Left, rect.Bottom), new Vector2(rect.Left, rect.Top), color, 1f);
	}

	private bool WouldBeHit(Attacking attack, Position pos, Collider col) {
		Rectangle bounds = col.GetBounds(pos);
		return AttackSystem.AABBIntersectsCone(bounds, attack.Origin, attack.Direction, attack.Angle * 0.5f, attack.AttackRange);
	}
}