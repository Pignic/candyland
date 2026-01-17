using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Command;
using EldmeresTale.ECS.Components.Result;
using Microsoft.Xna.Framework;
using System;

namespace EldmeresTale.ECS.Systems;

public sealed class AttackSystem : AEntitySetSystem<float> {

	private readonly Random _critRandom;

	public AttackSystem(World world)
		: base(world.GetEntities()
			  .With<Attacking>()
			  .AsSet()) {
		_critRandom = new Random();
	}
	protected override void PreUpdate(float state) {
		foreach (Entity e in World.GetEntities().With<Damaged>().AsEnumerable()) {
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

			// ---- AABB closest-point calculation ----
			Vector2 halfSize = new Vector2(col.Width * 0.5f, col.Height * 0.5f);
			Vector2 boxCenter = pos.Value + col.Offset;

			Vector2 boxMin = boxCenter - halfSize;
			Vector2 boxMax = boxCenter + halfSize;

			Vector2 closestPoint = new Vector2(
				MathF.Max(boxMin.X, MathF.Min(attacking.Origin.X, boxMax.X)),
				MathF.Max(boxMin.Y, MathF.Min(attacking.Origin.Y, boxMax.Y))
			);

			// ---- Range check ----
			Vector2 toTarget = closestPoint - attacking.Origin;
			float distSq = toTarget.LengthSquared();

			if (distSq > rangeSq || distSq == 0f) {
				continue;
			}

			// ---- Angle (cone) check ----
			Vector2 toTargetDir = Vector2.Normalize(toTarget);
			float dot = Vector2.Dot(attacking.Direction, toTargetDir);

			if (dot < cosThreshold) {
				continue;
			}

			// ---- Apply damage (radial knockback) ----
			Vector2 knockbackDir = toTargetDir;
			bool isCrit = _critRandom.NextSingle() <= attacking.CritChance;
			float damage = attacking.AttackDamage * (isCrit ? attacking.CritMultiplier : 1f);
			if (target.Has<Damaged>()) {
				ref Damaged d = ref target.Get<Damaged>();
				d.DamageAmount += damage;
			} else {
				target.Set(new Damaged {
					DamageAmount = damage,
					Direction = knockbackDir,
					// TODO: get that from the attacker stats
					KnockbackStrength = 1000,
					WasCrit = isCrit
				});
			}
			Vector2 soundLocation = boxCenter;
			target.Set(new PlaySound("monster_hurt_mid", soundLocation));
		}

		// ---- Consume the attack ----
		entity.Remove<Attacking>();
	}
}
