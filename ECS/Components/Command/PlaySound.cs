using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Components.Command;

public struct PlaySound {
	public string SoundName;
	public Vector2 Location;

	public PlaySound(string soundName, Vector2 location) {
		SoundName = soundName;
		Location = location;
	}
}