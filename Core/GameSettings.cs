namespace EldmeresTale.Core;

/// <summary>
/// Game settings and configuration
/// </summary>
public class GameSettings {
	private static GameSettings _instance;
	public static GameSettings Instance => _instance ??= new GameSettings();

	// Audio
	public float MusicVolume { get; set; } = 0.7f; // 0.0 to 1.0
	public float SfxVolume { get; set; } = 0.8f;   // 0.0 to 1.0

	// Video
	public int WindowScale { get; set; } = 2;
	public bool IsFullscreen { get; set; } = false;

	private GameSettings() { }

	/// <summary>
	/// Load settings from file (TODO: implement file IO)
	/// </summary>
	public void Load() {
		// TODO: Load from JSON file
	}

	/// <summary>
	/// Save settings to file (TODO: implement file IO)
	/// </summary>
	public void Save() {
		// TODO: Save to JSON file
	}
}