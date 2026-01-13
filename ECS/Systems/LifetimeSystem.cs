using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using System.Collections.Generic;

namespace EldmeresTale.ECS.Systems;

public class LifetimeSystem : AEntitySetSystem<float> {

	private readonly List<Entity> _entitiesToDispose = new(256);

	public LifetimeSystem(World world)
		: base(world.GetEntities()
			.With<Lifetime>()
			.AsSet()) {
	}

	protected override void PreUpdate(float state) {
		base.PreUpdate(state);
		_entitiesToDispose.Clear();
	}

	protected override void Update(float deltaTime, in Entity entity) {
		ref Lifetime lifetime = ref entity.Get<Lifetime>();
		lifetime.Remaining -= deltaTime;

		if (lifetime.Remaining <= 0) {
			_entitiesToDispose.Add(entity);
		}
	}

	protected override void PostUpdate(float state) {
		base.PostUpdate(state);
		foreach (Entity entity in _entitiesToDispose) {
			entity.Dispose();
		}
	}
}