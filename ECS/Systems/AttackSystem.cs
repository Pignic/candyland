using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.Core;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Command;
using EldmeresTale.ECS.Components.Result;
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
			float distSq = toTarget.LengthSquared();

			if (distSq > rangeSq || distSq == 0f) {
				continue;  // Too far or origin inside collider
			}

			// ---- Angle check: is closest point within attack cone? ----
			Vector2 toTargetDir = Vector2.Normalize(toTarget);
			float dot = Vector2.Dot(attacking.Direction, toTargetDir);

			if (dot < cosThreshold) {
				continue;  // Outside cone angle
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
					KnockbackStrength = 1000,
					WasCrit = isCrit
				});
			}

			Vector2 soundLocation = closestPoint;
			target.Set(new PlaySound("monster_hurt_mid", soundLocation));
		}

		// ---- Consume the attack ----
		//entity.Remove<Attacking>();
	}
}
