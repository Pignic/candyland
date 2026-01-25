using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Result;
using EldmeresTale.Events;

namespace EldmeresTale.ECS.Systems;

public sealed class DamageSystem : AEntitySetSystem<float> {

	public DamageSystem(World world)
		: base(world.GetEntities()
			  .With<Damaged>()
			  .AsSet()) {
	}

	protected override void Update(float deltaTime, in Entity entity) {
		Damaged damaged = entity.Get<Damaged>();
		if (entity.Has<Health>()) {
			ref Health health = ref entity.Get<Health>();
			health.TakeDamage((int)damaged.DamageAmount);
			if (entity.Has<Position>()) {
				World.CreateEntity().Set(new ECSEvent(new AttackEvent {
					Damage = (int)damaged.DamageAmount,
					Position = entity.Get<Position>().Value,
					Crit = damaged.WasCrit
				}, true));
			}
		}
		if (entity.Has<Velocity>()) {
			ref Velocity velocity = ref entity.Get<Velocity>();
			velocity.Impulse += damaged.Direction * damaged.KnockbackStrength;
		}
	}

	protected override void PostUpdate(float state) {
		foreach (Entity entity in Set.GetEntities()) {
			entity.Remove<Damaged>();
		}
		base.PostUpdate(state);
	}
}