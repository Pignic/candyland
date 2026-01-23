using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Command;
using EldmeresTale.ECS.Components.Tag;
using EldmeresTale.Entities;
using EldmeresTale.Systems;
using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Systems;

public sealed class InteractionSystem : AEntitySetSystem<InputCommands> {
	private readonly Entity _player;

	public InteractionSystem(World world, Player player)
		: base(world.GetEntities()
			.With<RoomActive>()
			.With<Position>()
			.With<InteractionZone>()
			.AsSet()) {
		_player = player.Entity;
	}

	protected override void Update(InputCommands input, in Entity entity) {
		Position pos = entity.Get<Position>();
		ref InteractionZone zone = ref entity.Get<InteractionZone>();

		Vector2 playerCenter = _player.Get<Position>().Value;
		Vector2 targetCenter = pos.Value;

		// Check if player is in range
		bool wasNearby = zone.IsPlayerNearby;
		zone.IsPlayerNearby = zone.IsInRange(targetCenter, playerCenter);

		// Handle interaction key press
		if (zone.IsPlayerNearby && input.InteractPressed) {
			entity.Set(new InteractionRequest(_player, zone.InteractionId));
		}
	}
}