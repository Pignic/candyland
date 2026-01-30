using DefaultEcs;
using EldmeresTale.ECS.Components;
using Microsoft.Xna.Framework;

namespace EldmeresTale.Events;

public class PickupSpawnedEvent : GameEvent {
	public Entity Pickup { get; set; }
	public Vector2 SpawnPosition { get; set; }
}

public class PickupCollectedEvent : GameEvent {
	public Entity Collector { get; set; }
	public PickupType Type { get; set; }
	public int Value { get; set; }
	public string Name { get; set; }
}
