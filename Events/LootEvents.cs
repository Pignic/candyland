using DefaultEcs;
using EldmeresTale.Entities;
using Microsoft.Xna.Framework;

namespace EldmeresTale.Events;

public class PickupSpawnedEvent : GameEvent {
	public Entity Pickup { get; set; }
	public Vector2 SpawnPosition { get; set; }
}

public class PickupCollectedEvent : GameEvent {
	public ActorEntity Collector { get; set; }
	public Entity Pickup { get; set; }
}
