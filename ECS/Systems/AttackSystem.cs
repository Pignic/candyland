using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.Core;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Command;
using EldmeresTale.ECS.Components.Result;
using EldmeresTale.ECS.Components.Tag;
using Microsoft.Xna.Framework;
using System;

namespace EldmeresTale.ECS.Systems;

public sealed class AttackSystem : AEntitySetSystem<float> {

	private readonly Random _critRandom;
	private readonly Camera _camera;
	private readonly EntitySet _damagedEntities;

	public AttackSystem(World world, Camera camera)
		: base(world.GetEntities()
			  .With<Attacking>()
			  .AsSet()) {
		_critRandom = new Random();
		_camera = camera;

		_damagedEntities = world.GetEntities().With<Damaged>().AsSet();
	}

	protected override void PreUpdate(float state) {
		foreach (Entity e in _damagedEntities.GetEntities()) {
			e.Remove<Damaged>();
		}
	}

	protected override void Update(float deltaTime, in Entity entity) {
		Attacking attacking = entity.Get<Attacking>();
		if (!entity.Has<RoomId>()) {
			System.Diagnostics.Debug.WriteLine("[ATTACK] Attacker has no RoomId!");
			return;
		}
		RoomId roomId = entity.Get<RoomId>();

		float halfAngle = attacking.Angle * 0.5f;
		float cosThreshold = MathF.Cos(halfAngle);

		Vector2 selfKnockback = attacking.Direction * -50f;
		bool hit = false;

		float rangeSq = attacking.AttackRange * attacking.AttackRange;
		foreach (Entity target in World.GetEntities()
			.With<Position>()
			.With<Collider>()
			.With((in Faction f) => f.Name != attacking.AttackerFaction)
			.With((in RoomId r) => r.Name == roomId.Name)
			.AsEnumerable()) {
			ref readonly Position pos = ref target.Get<Position>();
			ref readonly Collider col = ref target.Get<Collider>();

			// Get the actual bounding box of the collider
			Rectangle bounds = col.GetBounds(pos);

			// ---- Find closest point on AABB to attack origin ----
			Vector2 closestPoint = new Vector2(
				MathF.Max(bounds.Left, MathF.Min(attacking.Origin.X, bounds.Right)),
				MathF.Max(bounds.Top, MathF.Min(attacking.Origin.Y, bounds.Bottom))
			);

			// ---- Range check: is closest point within attack range? ----
			Vector2 toTarget = closestPoint - attacking.Origin;
			// If the points overlapped
			if (toTarget.Length() <= 0) {
				// away from the position
				toTarget = pos.Value - attacking.Origin;
				if (toTarget.Length() <= 0) {
					// fallback
					toTarget = Vector2.UnitX;
				}
			}

			// ---- Angle check: is closest point within attack cone? ----
			Vector2 toTargetDir = Vector2.Normalize(toTarget);

			if (!AABBIntersectsCone(bounds, attacking.Origin, attacking.Direction, halfAngle, attacking.AttackRange)) {
				continue;  // No intersection
			}

			// ---- HIT! Apply damage ----
			Vector2 knockbackDir = toTargetDir;  // Knockback away from closest point
			bool isCrit = _critRandom.NextSingle() <= attacking.CritChance;
			float damage = attacking.AttackDamage * (isCrit ? attacking.CritMultiplier : 1f);

			if (target.Has<Damaged>()) {
				ref Damaged d = ref target.Get<Damaged>();
				d.DamageAmount += damage;
			} else {
				_camera.Shake(isCrit ? 2 : 1, 0.3f);
				target.Set(new Damaged {
					DamageAmount = damage,
					Direction = knockbackDir,
					KnockbackStrength = 500,
					WasCrit = isCrit
				});
			}

			hit = true;

			Vector2 soundLocation = closestPoint;
			target.Set(new PlaySound("monster_hurt_mid", soundLocation));
		}
		if (hit) {
			selfKnockback *= 5f;
		}

		if (entity.Has<Velocity>()) {
			ref Velocity vel = ref entity.Get<Velocity>();
			vel.Impulse += selfKnockback;
		}

		// ---- Consume the attack ----
		entity.Remove<Attacking>();
	}

	public static bool AABBIntersectsCone(Rectangle aabb, Vector2 coneOrigin, Vector2 coneDirection, float coneHalfAngle, float coneRange) {
		float cosHalfAngle = MathF.Cos(coneHalfAngle);
		float rangeSq = coneRange * coneRange;

		// 1. Check if cone origin is inside AABB (overlapping case)
		if (aabb.Contains(coneOrigin)) {
			return true;
		}

		// 2. Check all 4 corners
		Vector2[] corners = new Vector2[4] {
		new Vector2(aabb.Left, aabb.Top),
		new Vector2(aabb.Right, aabb.Top),
		new Vector2(aabb.Right, aabb.Bottom),
		new Vector2(aabb.Left, aabb.Bottom)
	};

		for (int i = 0; i < 4; i++) {
			if (IsPointInCone(corners[i], coneOrigin, coneDirection, cosHalfAngle, rangeSq)) {
				return true;
			}
		}

		// 3. Check center (catches wide enemies where corners are outside cone)
		Vector2 center = new Vector2(aabb.Center.X, aabb.Center.Y);
		if (IsPointInCone(center, coneOrigin, coneDirection, cosHalfAngle, rangeSq)) {
			return true;
		}

		// 4. Check closest point on AABB (catches remaining edge cases)
		Vector2 closestPoint = new Vector2(
			MathF.Max(aabb.Left, MathF.Min(coneOrigin.X, aabb.Right)),
			MathF.Max(aabb.Top, MathF.Min(coneOrigin.Y, aabb.Bottom))
		);

		return IsPointInCone(closestPoint, coneOrigin, coneDirection, cosHalfAngle, rangeSq);
	}

	/// <summary>
	/// Check if a point is inside a cone
	/// </summary>
	private static bool IsPointInCone(Vector2 point, Vector2 coneOrigin, Vector2 coneDirection, float cosHalfAngle, float rangeSq) {
		Vector2 toPoint = point - coneOrigin;
		float distSq = toPoint.LengthSquared();

		// Out of range
		if (distSq > rangeSq || distSq < 0.0001f) {
			return false;
		}

		// Check angle
		Vector2 dirToPoint = toPoint / MathF.Sqrt(distSq);  // Normalize
		float dot = Vector2.Dot(coneDirection, dirToPoint);

		return dot >= cosHalfAngle;
	}

	public override void Dispose() {
		_damagedEntities.Dispose();
		base.Dispose();
	}
}
