using EldmeresTale.Audio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace EldmeresTale.Core;

public class AssetManager {

	private readonly GraphicsDevice _graphicsDevice;
	private readonly Dictionary<string, Texture2D> _textureCache;
	private readonly Dictionary<string, Song> _musicCache;
	private readonly Dictionary<string, Effect> _shaderCache;
	private readonly ContentManager content;

	public Texture2D DefaultTexture { get; }

	public AssetManager(GraphicsDevice graphicsDevice, ContentManager content) {
		_graphicsDevice = graphicsDevice;
		_textureCache = [];
		_shaderCache = [];
		_musicCache = [];
		this.content = content;

		DefaultTexture = new Texture2D(graphicsDevice, 1, 1);
		DefaultTexture.SetData([Color.White]);
	}

	public Texture2D LoadTexture(string path) {
		// Check cache first
		if (_textureCache.TryGetValue(path, out Texture2D value)) {
			return value;
		}

		// Try to load from file
		if (!File.Exists(path)) {
			return null;
		}

		using FileStream fileStream = new FileStream(path, FileMode.Open);
		Texture2D texture = Texture2D.FromStream(_graphicsDevice, fileStream);

		// Cache it
		_textureCache[path] = texture;
		return texture;
	}

	public Effect LoadShader(string name) {
		if (_shaderCache.TryGetValue(name, out Effect value)) {
			return value;
		}

		// Load shader
		Effect shader = null;
		try {
			shader = content.Load<Effect>("VariationMask");
			_shaderCache[name] = shader;
		} catch (Exception ex) {
			System.Diagnostics.Debug.WriteLine($"Shader load error: {ex.Message}");
		}

		return shader;
	}

	public Song LoadMusic(string path) {
		// Check cache first
		if (_musicCache.TryGetValue(path, out Song value)) {
			return value;
		}

		// Try to load from file
		if (!File.Exists(path)) {
			return null;
		}

		Song song = MusicParser.ParseFile(path);

		// Cache it
		_musicCache[path] = song;
		return song;
	}

	public Texture2D LoadTextureOrFallback(string path, System.Func<Texture2D> fallbackGenerator) {
		Texture2D texture = LoadTexture(path);
		if (texture != null) {
			return texture;
		}

		// Generate fallback and cache it
		texture = fallbackGenerator();
		_textureCache[path] = texture;
		return texture;
	}

	public void PreloadTextures(Dictionary<string, string> textureManifest) {
		foreach (KeyValuePair<string, string> kvp in textureManifest) {
			LoadTexture(kvp.Value);
		}
	}

	public Texture2D GetCachedTexture(string key) {
		return _textureCache.TryGetValue(key, out Texture2D value) ? value : null;
	}

	public void ClearCache() {
		foreach (Texture2D texture in _textureCache.Values) {
			texture?.Dispose();
		}
		_textureCache.Clear();
	}
}