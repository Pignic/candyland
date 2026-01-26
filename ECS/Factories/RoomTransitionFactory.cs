using DefaultEcs;
using EldmeresTale.Core;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Components.Tag;
using EldmeresTale.Worlds;
using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Factories;

public class RoomTransitionFactory {

	private readonly World _world;

	private readonly AssetManager _assetManager;

	public RoomTransitionFactory(World world, AssetManager assetManager) {
		_world = world;
		_assetManager = assetManager;
	}

	public Entity CreateDoor(string doorId, string targetDoorId, string fromRoomId, string toRoomId, Rectangle doorShape, Direction direction) {
		Entity door = _world.CreateEntity();
		door.Set(new RoomTransition(doorId, direction, targetDoorId, toRoomId));
		door.Set(new RoomId(fromRoomId));
		door.Set(new Collider(doorShape.Width, doorShape.Height, new Vector2(doorShape.Width / 2, doorShape.Height)));
		door.Set(new Position(doorShape.X, doorShape.Y));
		Sprite sprite = new Sprite(_assetManager.DefaultTexture) {
			Scale = new Vector2(doorShape.Width, doorShape.Height)
		};
		door.Set(sprite);
		return door;
	}

	public Entity CreateDoor(string roomId, DoorData doorData) {
		return CreateDoor(doorData.Id, doorData.TargetDoorId, roomId, doorData.TargetRoomId, new Rectangle(doorData.X, doorData.Y, doorData.Width, doorData.Height), doorData.Direction);
	}
}
