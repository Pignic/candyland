using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace EldmeresTale.Core;

public class AssetManager {
	private GraphicsDevice _graphicsDevice;
	private Dictionary<string, Texture2D> _textureCache;
	private Dictionary<string, Effect> _shaderCache;
	private ContentManager content;

	public AssetManager(GraphicsDevice graphicsDevice, ContentManager content) {
		_graphicsDevice = graphicsDevice;
		_textureCache = new Dictionary<string, Texture2D>();
		_shaderCache = new Dictionary<string, Effect>();
		this.content = content;
	}

	public Texture2D LoadTexture(string path) {
		// Check cache first
		if(_textureCache.ContainsKey(path)){
			return _textureCache[path];
		}

		// Try to load from file
		if(!File.Exists(path)){
			return null;
		}

		using var fileStream = new FileStream(path, FileMode.Open);
		var texture = Texture2D.FromStream(_graphicsDevice, fileStream);

		// Cache it
		_textureCache[path] = texture;
		return texture;
	}

	public Effect LoadShader(string name) {
		if(_shaderCache.ContainsKey(name)) {
			return _shaderCache[name];
		}

		// Load shader
		Effect shader = null;
		try {
			shader = content.Load<Effect>("VariationMask");
			_shaderCache[name] = shader;
		} catch(Exception ex) {
			System.Diagnostics.Debug.WriteLine($"Shader load error: {ex.Message}");
		}

		return shader;
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