using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Tag;
using EldmeresTale.Entities;
using EldmeresTale.Events;
using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Systems;

public class RoomTransitionSystem : AEntitySetSystem<float> {

	private readonly Player _player;

	public RoomTransitionSystem(World world, Player player) : base(world.GetEntities()
			.With<RoomId>()
			.With<RoomActive>()
			.With<RoomTransition>()
			.With<Position>()
			.With<Collider>()
			.AsSet()) {
		_player = player;
	}

	protected override void Update(float deltaTime, in Entity entity) {
		Rectangle playerRectangle = _player.Bounds;
		if (entity.Get<Collider>().GetBounds(entity.Get<Position>()).Intersects(playerRectangle)) {
			RoomTransition transition = entity.Get<RoomTransition>();
			World.CreateEntity().Set(new ECSEvent(new RoomChangingEvent {
				NewRoomId = transition.TargetRoomId,
				Position = _player.Position,
				PreviousRoomId = _player.Entity.Get<RoomId>().Name,
				DoorId = transition.DoorId,
				TargetDoorId = transition.TargetDoorID
			}));
		}
		base.Update(deltaTime, entity);
	}
}
