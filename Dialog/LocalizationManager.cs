using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Candyland.Dialog;

public class LocalizationManager {
	private readonly Dictionary<string, string> strings;
	private string currentLanguage;

	public LocalizationManager() {
		strings = new Dictionary<string, string>();
		currentLanguage = "en";
	}

	public void loadLanguage(string languageCode, string filepath) {
		if(!File.Exists(filepath)) {
			System.Diagnostics.Debug.WriteLine($"Language file not found: {filepath}");
			return;
		}

		try {
			string json = File.ReadAllText(filepath);
			JsonDocument doc = JsonDocument.Parse(json);
			JsonElement root = doc.RootElement;

			// Check if the file has a language code wrapper
			JsonElement langElement;
			if(root.TryGetProperty(languageCode, out langElement)) {
				parseLocalizationObject(langElement, "");
			} else {
				// Assume root is the translation object
				parseLocalizationObject(root, "");
			}
			currentLanguage = languageCode;
			System.Diagnostics.Debug.WriteLine($"Loaded language: {languageCode}");
		} catch(System.Exception ex) {
			System.Diagnostics.Debug.WriteLine($"Error loading language file: {ex.Message}");
		}
	}

	private void parseLocalizationObject(JsonElement element, string prefix) {
		foreach(JsonProperty property in element.EnumerateObject()) {
			string key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

			if(property.Value.ValueKind == JsonValueKind.Object) {
				// Recurse into nested objects
				parseLocalizationObject(property.Value, key);
			} else if(property.Value.ValueKind == JsonValueKind.String) {
				// Store string value
				this.strings[key] = property.Value.GetString();
			}
		}
	}

	public string getString(string key, Dictionary<string, string> replacements = null) {
		if(string.IsNullOrEmpty(key))
			return "";

		string text = this.strings.ContainsKey(key) ? strings[key] : key;

		// Apply replacements
		if(replacements != null) {
			foreach(KeyValuePair<string, string> kvp in replacements) {
				text = text.Replace($"{{{kvp.Key}}}", kvp.Value);
			}
		}

		return text;
	}

	public bool HasKey(string key) {
		return strings.ContainsKey(key);
	}

	public string getCurrentLanguage() {
		return currentLanguage;
	}

	public void Clear() {
		strings.Clear();
	}
}