using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.Entities;
using EldmeresTale.Systems;
using Microsoft.Xna.Framework;
using System;

namespace EldmeresTale.ECS.Systems;

public sealed class InteractionSystem : AEntitySetSystem<InputCommands> {
	private readonly Player _player;

	public event Action<Entity, string> OnInteraction;

	public InteractionSystem(DefaultEcs.World world, Player player)
		: base(world.GetEntities()
			.With<Position>()
			.With<InteractionZone>()
			.AsSet()) {
		_player = player;
	}

	protected override void Update(InputCommands input, in Entity entity) {
		Position pos = entity.Get<Position>();
		ref InteractionZone zone = ref entity.Get<InteractionZone>();

		// Get player center position
		Vector2 playerCenter = _player.Position + new Vector2(_player.Width / 2f, _player.Height / 2f);
		Vector2 targetCenter = pos.Value;
		if (entity.Has<Collider>()) {
			ref Collider collider = ref entity.Get<Collider>();
			targetCenter += new Vector2(collider.Width / 2, collider.Height / 2);
		}

		// Check if player is in range
		bool wasNearby = zone.IsPlayerNearby;
		zone.IsPlayerNearby = zone.IsInRange(targetCenter, playerCenter);

		// Handle interaction key press
		if (zone.IsPlayerNearby && input.InteractPressed) {
			OnInteraction?.Invoke(entity, zone.InteractionId);
		}
	}
}