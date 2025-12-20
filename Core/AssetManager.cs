using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;

namespace Candyland.Core;

public class AssetManager {
	private GraphicsDevice _graphicsDevice;
	private Dictionary<string, Texture2D> _textureCache;

	public AssetManager(GraphicsDevice graphicsDevice) {
		_graphicsDevice = graphicsDevice;
		_textureCache = new Dictionary<string, Texture2D>();
	}

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

	public Texture2D LoadTextureOrFallback(string path, System.Func<Texture2D> fallbackGenerator) {
		var texture = LoadTexture(path);
		if(texture != null)
			return texture;

		// Generate fallback and cache it
		texture = fallbackGenerator();
		_textureCache[path] = texture;
		return texture;
	}

	public void PreloadTextures(Dictionary<string, string> textureManifest) {
		foreach(var kvp in textureManifest) {
			LoadTexture(kvp.Value);
		}
	}

	public Texture2D GetCachedTexture(string key) {
		return _textureCache.ContainsKey(key) ? _textureCache[key] : null;
	}

	public void ClearCache() {
		foreach(var texture in _textureCache.Values) {
			texture?.Dispose();
		}
		_textureCache.Clear();
	}
}