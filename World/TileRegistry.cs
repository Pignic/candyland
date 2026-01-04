using EldmeresTale.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EldmoresTale.World;

public class TileRegistry {

	private static TileRegistry _instance;

	public static TileRegistry Instance {
		get {
			_instance ??= new TileRegistry();
			return _instance;
		}
	}

	private Dictionary<string, TileDefinition> _tiles;
	private Dictionary<TileType, TileDefinition> _tilesByEnum;

	private TileRegistry() {
		_tiles = new Dictionary<string, TileDefinition>();
		_tilesByEnum = new Dictionary<TileType, TileDefinition>();
	}

	public void LoadFromFile(string path = "Assets/Data/tiles.json") {
		try {
			if (!File.Exists(path)) {
				System.Diagnostics.Debug.WriteLine($"[TILE REGISTRY] File not found: {path}");
				LoadDefaults();
				return;
			}

			string json = File.ReadAllText(path);
			TileDataFile data = JsonSerializer.Deserialize<TileDataFile>(json);

			if (data?.tiles == null) {
				System.Diagnostics.Debug.WriteLine("[TILE REGISTRY] Invalid JSON format");
				LoadDefaults();
				return;
			}

			_tiles.Clear();
			_tilesByEnum.Clear();

			foreach (TileDefinition tile in data.tiles) {
				// Parse colors
				tile.ParseColors();

				// Store by ID
				_tiles[tile.Id] = tile;

				// Map to enum if possible
				if (Enum.TryParse<TileType>(tile.Id, true, out TileType tileType)) {
					_tilesByEnum[tileType] = tile;
				}

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

	public TileDefinition GetTile(TileType type) {
		return _tilesByEnum.GetValueOrDefault(type);
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
		TileDefinition[] defaultTiles = new[] {
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
		};

		foreach (TileDefinition tile in defaultTiles) {
			tile.ParseColors();
			_tiles[tile.Id] = tile;

			if (Enum.TryParse<TileType>(tile.Id, true, out TileType tileType)) {
				_tilesByEnum[tileType] = tile;
			}
		}
	}

	// Helper class for JSON deserialization
	private class TileDataFile {
		public List<TileDefinition> tiles { get; set; }
	}
}