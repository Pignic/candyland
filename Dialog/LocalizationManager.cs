using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Candyland.Dialog
{
    /// <summary>
    /// Manages localized text strings
    /// </summary>
    public class LocalizationManager
    {
        private Dictionary<string, string> _strings;
        private string _currentLanguage;

        public LocalizationManager()
        {
            _strings = new Dictionary<string, string>();
            _currentLanguage = "en";
        }

        /// <summary>
        /// Load a language file
        /// </summary>
        public void LoadLanguage(string languageCode, string filepath)
        {
            if (!File.Exists(filepath))
            {
                System.Diagnostics.Debug.WriteLine($"Language file not found: {filepath}");
                return;
            }

            try
            {
                string json = File.ReadAllText(filepath);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Check if the file has a language code wrapper
                JsonElement langElement;
                if (root.TryGetProperty(languageCode, out langElement))
                {
                    ParseLocalizationObject(langElement, "");
                }
                else
                {
                    // Assume root is the translation object
                    ParseLocalizationObject(root, "");
                }

                _currentLanguage = languageCode;
                System.Diagnostics.Debug.WriteLine($"Loaded language: {languageCode}");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading language file: {ex.Message}");
            }
        }

        /// <summary>
        /// Recursively parse localization JSON object
        /// </summary>
        private void ParseLocalizationObject(JsonElement element, string prefix)
        {
            foreach (var property in element.EnumerateObject())
            {
                string key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    // Recurse into nested objects
                    ParseLocalizationObject(property.Value, key);
                }
                else if (property.Value.ValueKind == JsonValueKind.String)
                {
                    // Store string value
                    _strings[key] = property.Value.GetString();
                }
            }
        }

        /// <summary>
        /// Get localized string by key
        /// </summary>
        public string GetString(string key, Dictionary<string, string> replacements = null)
        {
            if (string.IsNullOrEmpty(key))
                return "";

            string text = _strings.ContainsKey(key) ? _strings[key] : key;

            // Apply replacements
            if (replacements != null)
            {
                foreach (var kvp in replacements)
                {
                    text = text.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
            }

            return text;
        }

        /// <summary>
        /// Check if a key exists
        /// </summary>
        public bool HasKey(string key)
        {
            return _strings.ContainsKey(key);
        }

        /// <summary>
        /// Get current language code
        /// </summary>
        public string GetCurrentLanguage()
        {
            return _currentLanguage;
        }

        /// <summary>
        /// Clear all loaded strings
        /// </summary>
        public void Clear()
        {
            _strings.Clear();
        }
    }
}