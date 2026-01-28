using EldmeresTale.Entities.Definitions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EldmeresTale.Entities.Factories;

public static class MaterialFactory {

	private static Dictionary<string, MaterialDefinition> _catalog;
	private static bool _initialized = false;

	public static Dictionary<string, MaterialDefinition> Catalog {
		get {
			if (!_initialized) {
				Initialize();
			}

			return _catalog;
		}
	}
	public static void Initialize(string path = "Assets/Data/material.json") {
		_catalog = [];

		try {
			if (!File.Exists(path)) {
				System.Diagnostics.Debug.WriteLine($"[MATERIAL FACTORY] File not found: {path}");
				return;
			}

			string json = File.ReadAllText(path);
			MaterialCatalogData data = JsonSerializer.Deserialize<MaterialCatalogData>(json);

			if (data?.Material == null) {
				System.Diagnostics.Debug.WriteLine("[MATERIAL FACTORY] Invalid JSON format");
				return;
			}

			foreach (MaterialDefinition item in data.Material) {
				_catalog[item.Id] = item;
			}

			System.Diagnostics.Debug.WriteLine($"[MATERIAL FACTORY] Loaded {_catalog.Count} items from {path}");

		} catch (Exception ex) {
			System.Diagnostics.Debug.WriteLine($"[MATERIAL FACTORY] Error: {ex.Message}");
		}

		_initialized = true;
	}

	// JSON container class
	private class MaterialCatalogData {
		public List<MaterialDefinition> Material { get; set; }
	}
}
