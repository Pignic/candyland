using Microsoft.Xna.Framework;

namespace EldmeresTale.Events;

public abstract class GameEvent {

	public GameTime Timestamp { get; set; }

	public Vector2? Position { get; set; }
}
