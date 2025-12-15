using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;

namespace Candyland.Core {
	/// <summary>
	/// Centralized asset management with caching
	/// Handles loading textures from files or creating fallbacks
	/// </summary>
	public class AssetManager {
		private GraphicsDevice _graphicsDevice;
		private Dictionary<string, Texture2D> _textureCache;

		public AssetManager(GraphicsDevice graphicsDevice) {
			_graphicsDevice = graphicsDevice;
			_textureCache = new Dictionary<string, Texture2D>();
		}

		/// <summary>
		/// Load texture from file with caching. Returns null if file doesn't exist.
		/// </summary>
		public Texture2D LoadTexture(string path) {
			// Check cache first
			if(_textureCache.ContainsKey(path))
				return _textureCache[path];

			// Try to load from file
			if(!File.Exists(path))
				return null;

			using var fileStream = new FileStream(path, FileMode.Open);
			var texture = Texture2D.FromStream(_graphicsDevice, fileStream);

			// Cache it
			_textureCache[path] = texture;
			return texture;
		}

		/// <summary>
		/// Load texture with fallback to generated texture
		/// </summary>
		public Texture2D LoadTextureOrFallback(string path, System.Func<Texture2D> fallbackGenerator) {
			var texture = LoadTexture(path);
			if(texture != null)
				return texture;

			// Generate fallback and cache it
			texture = fallbackGenerator();
			_textureCache[path] = texture;
			return texture;
		}

		/// <summary>
		/// Preload common textures based on a manifest
		/// </summary>
		public void PreloadTextures(Dictionary<string, string> textureManifest) {
			foreach(var kvp in textureManifest) {
				LoadTexture(kvp.Value);
			}
		}

		/// <summary>
		/// Get cached texture by key
		/// </summary>
		public Texture2D GetCachedTexture(string key) {
			return _textureCache.ContainsKey(key) ? _textureCache[key] : null;
		}

		/// <summary>
		/// Clear all cached textures (use sparingly - for level transitions etc)
		/// </summary>
		public void ClearCache() {
			foreach(var texture in _textureCache.Values) {
				texture?.Dispose();
			}
			_textureCache.Clear();
		}
	}
}