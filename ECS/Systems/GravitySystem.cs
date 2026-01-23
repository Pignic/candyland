using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using System;
using System.Collections.Generic;

namespace EldmeresTale.ECS.Systems;

public sealed class GravitySystem : AEntitySetSystem<float> {

	private readonly List<Entity> _stableEntities;

	public GravitySystem(World world)
		: base(world.GetEntities()
			.With<ZPosition>()
			.With<Gravity>()
			.AsSet()) {
		_stableEntities = [];
	}

	protected override void Update(float deltaTime, in Entity entity) {
		Gravity gravity = entity.Get<Gravity>();
		ref ZPosition zPosition = ref entity.Get<ZPosition>();
		zPosition.ZSpeed += gravity.Value * deltaTime * -1f;
		zPosition.Z += zPosition.ZSpeed;
		if (zPosition.Z < 0) {
			zPosition.ZSpeed *= -1 * zPosition.Absorption;
			zPosition.Z *= -1;
		}
		if (Math.Abs(zPosition.ZSpeed) < 1 && zPosition.Z < 1) {
			_stableEntities.Add(entity);
		}
		base.Update(deltaTime, entity);
	}

	protected override void PostUpdate(float state) {
		foreach (Entity entity in _stableEntities) {
			entity.Remove<Gravity>();
			entity.Remove<ZPosition>();
		}
		_stableEntities.Clear();
		base.PostUpdate(state);
	}
}