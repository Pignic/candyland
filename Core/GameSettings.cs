using System;
using System.IO;
using System.Text.Json;

namespace EldmeresTale.Core;

public class GameSettings {
	private static GameSettings _instance;
	public static GameSettings Instance => _instance ??= new GameSettings(true);

	// Audio
	public float MusicVolume { get; set; } = 0.7f; // 0.0 to 1.0
	public float SfxVolume { get; set; } = 0.8f;   // 0.0 to 1.0

	// Video
	public int WindowScale { get; set; } = 2;
	public bool IsFullscreen { get; set; } = false;
	public bool DebugMode { get; set; } = false;
	public bool CameraShake { get; set; } = true;

	public GameSettings() {

	}

	public GameSettings(bool load) {
		Load();
	}

	public void Load() {
		try {
			if (!File.Exists("settings.json")) {
				System.Diagnostics.Debug.WriteLine("[SETTINGS] No settings file, using defaults");
				return;
			}

			string json = File.ReadAllText("settings.json");
			GameSettings loaded = JsonSerializer.Deserialize<GameSettings>(json);

			if (loaded != null) {
				MusicVolume = loaded.MusicVolume;
				SfxVolume = loaded.SfxVolume;
				WindowScale = loaded.WindowScale;
				IsFullscreen = loaded.IsFullscreen;
				DebugMode = loaded.DebugMode;
				CameraShake = loaded.CameraShake;
				System.Diagnostics.Debug.WriteLine($"[SETTINGS] Loaded successfully");
			}
		} catch (Exception ex) {
			System.Diagnostics.Debug.WriteLine($"[SETTINGS] Load error: {ex.Message}");
		}
	}

	public void Save() {
		try {
			JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
			string json = JsonSerializer.Serialize(this, options);
			File.WriteAllText("settings.json", json);
			System.Diagnostics.Debug.WriteLine($"[SETTINGS] Saved");
		} catch (Exception ex) {
			System.Diagnostics.Debug.WriteLine($"[SETTINGS] Save error: {ex.Message}");
		}
	}
}