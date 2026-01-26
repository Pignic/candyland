using DefaultEcs;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace EldmeresTale.Worlds;

public enum DoorDirection {
	North,
	South,
	East,
	West
}

public class Room {
	public string Id { get; set; }
	public MapData MapData { get; }
	public TileMap Map { get; set; }
	public List<Entity> Enemies { get; set; }
	public List<Entity> Pickups { get; set; }
	public Dictionary<string, Entity> Doors { get; set; }
	public List<Entity> NPCs { get; }
	public List<Entity> Props { get; }

	public Vector2 PlayerSpawnPosition { get; set; }

	public Room(string id, TileMap map, MapData mapData) {
		Id = id;
		Map = map;
		Enemies = [];
		Pickups = [];
		Doors = [];
		NPCs = [];
		Props = [];
		PlayerSpawnPosition = new Vector2(map.PixelWidth / 2, map.PixelHeight / 2);
		MapData = mapData;
	}

	// Create a room from MapData
	public static Room FromMapData(string roomId, MapData mapData) {
		TileMap tileMap = mapData.ToTileMap();
		return new Room(roomId, tileMap, mapData) {
			PlayerSpawnPosition = new Vector2(mapData.PlayerSpawnX, mapData.PlayerSpawnY)
		};
	}
}