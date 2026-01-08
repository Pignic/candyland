using EldmeresTale.Entities;
using Microsoft.Xna.Framework;

namespace EldmeresTale.Events;

public class PickupSpawnedEvent : GameEvent {
	public Pickup Pickup { get; set; }
	public Vector2 SpawnPosition { get; set; }
}

public class PickupCollectedEvent : GameEvent {
	public ActorEntity Collector { get; set; }
	public Pickup Pickup { get; set; }
}
