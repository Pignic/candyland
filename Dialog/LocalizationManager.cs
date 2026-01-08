using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EldmeresTale.Dialog;

public class LocalizationManager {
	private readonly Dictionary<string, string> strings;
	private string currentLanguage;

	public LocalizationManager() {
		strings = [];
		currentLanguage = "en";
	}

	public void LoadLanguage(string languageCode, string filepath) {
		if (!File.Exists(filepath)) {
			System.Diagnostics.Debug.WriteLine($"Language file not found: {filepath}");
			return;
		}

		try {
			string json = File.ReadAllText(filepath);
			JsonDocument doc = JsonDocument.Parse(json);
			JsonElement root = doc.RootElement;

			// Check if the file has a language code wrapper
			if (root.TryGetProperty(languageCode, out JsonElement langElement)) {
				ParseLocalizationObject(langElement, "");
			} else {
				// Assume root is the translation object
				ParseLocalizationObject(root, "");
			}
			currentLanguage = languageCode;
			System.Diagnostics.Debug.WriteLine($"Loaded language: {languageCode}");
		} catch (System.Exception ex) {
			System.Diagnostics.Debug.WriteLine($"Error loading language file: {ex.Message}");
		}
	}

	private void ParseLocalizationObject(JsonElement element, string prefix) {
		foreach (JsonProperty property in element.EnumerateObject()) {
			string key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

			if (property.Value.ValueKind == JsonValueKind.Object) {
				// Recurse into nested objects
				ParseLocalizationObject(property.Value, key);
			} else if (property.Value.ValueKind == JsonValueKind.String) {
				// Store string value
				strings[key] = property.Value.GetString();
			}
		}
	}

	public string GetString(string key, Dictionary<string, string> replacements = null) {
		if (string.IsNullOrEmpty(key)) {
			return "";
		}

		string text = strings.TryGetValue(key, out string value) ? value : key;

		// Apply replacements
		if (replacements != null) {
			foreach (KeyValuePair<string, string> kvp in replacements) {
				text = text.Replace($"{{{kvp.Key}}}", kvp.Value);
			}
		}

		return text;
	}

	public bool HasKey(string key) {
		return strings.ContainsKey(key);
	}

	public string GetCurrentLanguage() {
		return currentLanguage;
	}

	public void Clear() {
		strings.Clear();
	}
}