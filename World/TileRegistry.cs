using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EldmeresTale.World;

public class TileRegistry {

	private static TileRegistry _instance;

	public static TileRegistry Instance {
		get {
			_instance ??= new TileRegistry();
			return _instance;
		}
	}

	private readonly Dictionary<string, TileDefinition> _tiles;

	private TileRegistry() {
		_tiles = [];
	}

	public void LoadFromFile(string path) {
		try {
			if (!File.Exists(path)) {
				System.Diagnostics.Debug.WriteLine($"[TILE REGISTRY] File not found: {path}");
				LoadDefaults();
				return;
			}

			string json = File.ReadAllText(path);
			TileDataFile data = JsonSerializer.Deserialize<TileDataFile>(json);

			if (data?.Tiles == null) {
				System.Diagnostics.Debug.WriteLine("[TILE REGISTRY] Invalid JSON format");
				LoadDefaults();
				return;
			}
			_tiles.Clear();

			foreach (TileDefinition tile in data.Tiles) {
				tile.ParseColors();
				_tiles[tile.Id] = tile;
				System.Diagnostics.Debug.WriteLine($"[TILE REGISTRY] Loaded tile: {tile.Id} ({tile.Name})");
			}
			System.Diagnostics.Debug.WriteLine($"[TILE REGISTRY] Loaded {_tiles.Count} tiles from {path}");
		} catch (Exception ex) {
			System.Diagnostics.Debug.WriteLine($"[TILE REGISTRY] Error loading tiles: {ex.Message}");
			LoadDefaults();
		}
	}

	public TileDefinition GetTile(string id) {
		return _tiles.GetValueOrDefault(id);
	}

	public IEnumerable<TileDefinition> GetAllTiles() {
		return _tiles.Values;
	}

	public IEnumerable<string> GetTileIds() {
		return _tiles.Keys;
	}

	private void LoadDefaults() {
		System.Diagnostics.Debug.WriteLine("[TILE REGISTRY] Loading default tiles (hardcoded)");

		// Fallback defaults if JSON fails
		TileDefinition[] defaultTiles = [
			new TileDefinition {
				Id = "grass",
				Name = "Grass",
				IsWalkable = true,
				ColorHex = "#32B432",
			},
			new TileDefinition {
				Id = "water",
				Name = "Water",
				IsWalkable = false,
				ColorHex = "#2896FF"
			},
			new TileDefinition {
				Id = "stone",
				Name = "Stone",
				IsWalkable = true,
				ColorHex = "#8C8C8C"
			},
			new TileDefinition {
				Id = "tree",
				Name = "Tree",
				IsWalkable = false,
				ColorHex = "#147814"
			}
		];

		foreach (TileDefinition tile in defaultTiles) {
			tile.ParseColors();
			_tiles[tile.Id] = tile;
		}
	}

	// Helper class for JSON deserialization
	private class TileDataFile {
		public List<TileDefinition> Tiles { get; set; }
	}
}