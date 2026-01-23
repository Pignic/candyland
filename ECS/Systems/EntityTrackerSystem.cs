using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components.Tag;
using System;
using System.Collections.Generic;

namespace EldmeresTale.ECS.Systems;

public sealed class EntityTrackerSystem : AEntitySetSystem<float> {

	private readonly float _logFrequency;
	private float _logTimer;
	private readonly List<Type> _excludes;


	public EntityTrackerSystem(World world, float logFrequency, List<Type> excludes)
		: base(world.GetEntities()
			  .With<EntityTracker>()
			  .AsSet()) {
		_logFrequency = logFrequency;
		_logTimer = _logFrequency;
		_excludes = excludes;
	}

	protected override void Update(float deltaTime, in Entity entity) {
		_logTimer -= deltaTime;
		if (_logTimer < 0) {
			_logTimer = _logFrequency;
			EntityInspector.DumpEntity(entity, entity.Get<EntityTracker>().Label, _excludes);
		}
		base.Update(deltaTime, entity);
	}
}