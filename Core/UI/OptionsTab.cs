using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Core.UI.Tabs;

public class OptionsTab : IMenuTab {
	private readonly GraphicsDevice _graphicsDevice;
	private readonly BitmapFont _font;
	private readonly int _initialScale;

	public UIPanel RootPanel { get; private set; }

	// Settings controls
	private UISlider _musicVolumeSlider;
	private UISlider _sfxVolumeSlider;
	private UISlider _scaleSlider;
	private UICheckbox _fullscreenCheckbox;
	private UICheckbox _cameraShakeCheckbox;

	// Navigable elements list
	private readonly List<UIElement> _navigableElements;

	public bool IsVisible {
		get => RootPanel.Visible;
		set => RootPanel.Visible = value;
	}

	// Events for settings changes
	public event Action<float> OnMusicVolumeChanged;
	public event Action<float> OnSfxVolumeChanged;
	public event Action<int> OnScaleChanged;
	public event Action<bool> OnFullscreenChanged;
	public event Action<bool> OnCameraShakeChanged;

	public OptionsTab(GraphicsDevice graphicsDevice, BitmapFont font, int initialScale) {
		_graphicsDevice = graphicsDevice;
		_font = font;
		_initialScale = initialScale;
		_navigableElements = [];
	}

	public void Initialize() {
		RootPanel = new UIPanel(_graphicsDevice) {
			X = 10,
			Y = 32,
			Width = 600,
			Height = 253,
			EnableScrolling = true,
			Layout = UIPanel.LayoutMode.Vertical,
			Spacing = 10,
			Visible = false
		};
		RootPanel.SetPadding(10);

		// Title
		UILabel title = new UILabel(_font, "OPTIONS") {
			TextColor = Color.Yellow
		};
		title.UpdateSize();
		RootPanel.AddChild(title);

		AddSpacer(10);

		// === AUDIO SECTION ===
		AddSectionHeader("-- AUDIO --", Color.LightGreen);
		AddSpacer(5);

		// Music Volume Slider
		_musicVolumeSlider = new UISlider(
			_graphicsDevice,
			_font,
			"Music Volume",
			0, 10,
			(int)(GameSettings.Instance.MusicVolume * 10)
		) {
			Width = 300,
			IsNavigable = true
		};
		_musicVolumeSlider.OnValueChanged += (value) => {
			float volume = value / 10f;
			GameSettings.Instance.MusicVolume = volume;
			GameSettings.Instance.Save();
			System.Diagnostics.Debug.WriteLine($"[OPTIONS TAB] Music volume: {volume:F1}");
			OnMusicVolumeChanged?.Invoke(volume);
		};
		RootPanel.AddChild(_musicVolumeSlider);
		_navigableElements.Add(_musicVolumeSlider);

		AddSpacer(5);

		// SFX Volume Slider
		_sfxVolumeSlider = new UISlider(
			_graphicsDevice,
			_font,
			"SFX Volume",
			0, 10,
			(int)(GameSettings.Instance.SfxVolume * 10)
		) {
			Width = 300,
			IsNavigable = true
		};
		_sfxVolumeSlider.OnValueChanged += (value) => {
			float volume = value / 10f;
			GameSettings.Instance.SfxVolume = volume;
			GameSettings.Instance.Save();
			System.Diagnostics.Debug.WriteLine($"[OPTIONS TAB] SFX volume: {volume:F1}");
			OnSfxVolumeChanged?.Invoke(volume);
		};
		RootPanel.AddChild(_sfxVolumeSlider);
		_navigableElements.Add(_sfxVolumeSlider);

		AddSpacer(15);

		// === VIDEO SECTION ===
		AddSectionHeader("-- VIDEO --", Color.Cyan);
		AddSpacer(5);

		// Scale Slider
		_scaleSlider = new UISlider(
			_graphicsDevice,
			_font,
			"Window Scale",
			1, 3,
			_initialScale
		) {
			Width = 300,
			IsNavigable = true
		};
		_scaleSlider.OnValueChanged += (value) => {
			GameSettings.Instance.WindowScale = value;
			GameSettings.Instance.Save();
			System.Diagnostics.Debug.WriteLine($"[OPTIONS TAB] Scale: {value}");
			OnScaleChanged?.Invoke(value);
		};
		RootPanel.AddChild(_scaleSlider);
		_navigableElements.Add(_scaleSlider);

		AddSpacer(5);

		// Fullscreen Checkbox
		_fullscreenCheckbox = new UICheckbox(
			_graphicsDevice,
			_font,
			"Fullscreen",
			_graphicsDevice.PresentationParameters.IsFullScreen
		) {
			Width = 300,
			IsNavigable = true
		};
		_fullscreenCheckbox.OnValueChanged += (value) => {
			GameSettings.Instance.IsFullscreen = value;
			GameSettings.Instance.Save();
			System.Diagnostics.Debug.WriteLine($"[OPTIONS TAB] Fullscreen: {value}");
			OnFullscreenChanged?.Invoke(value);
		};
		RootPanel.AddChild(_fullscreenCheckbox);
		_navigableElements.Add(_fullscreenCheckbox);

		AddSpacer(5);

		// Camera Shake Checkbox
		_cameraShakeCheckbox = new UICheckbox(
			_graphicsDevice,
			_font,
			"Camera Shake",
			GameSettings.Instance.CameraShake
		) {
			Width = 300,
			IsNavigable = true
		};
		_cameraShakeCheckbox.OnValueChanged += (value) => {
			GameSettings.Instance.CameraShake = value;
			GameSettings.Instance.Save();
			System.Diagnostics.Debug.WriteLine($"[OPTIONS TAB] Camera Shake: {value}");
			OnCameraShakeChanged?.Invoke(value);
		};
		RootPanel.AddChild(_cameraShakeCheckbox);
		_navigableElements.Add(_cameraShakeCheckbox);

		AddSpacer(20);

		// === CONTROLS SECTION ===
		AddSectionHeader("-- CONTROLS --", Color.Orange);
		AddSpacer(5);

		AddInfoLine("WASD / Arrows - Move");
		AddInfoLine("Space - Attack");
		AddInfoLine("E - Interact / Talk");
		AddInfoLine("Tab - Menu");
		AddInfoLine("M - Map Editor");
		AddInfoLine("Esc - Quit");

		AddSpacer(10);

		// === DEBUG INFO ===
		AddSectionHeader("-- DEBUG --", Color.Red);
		AddSpacer(5);
		AddInfoLine("F4 - Toggle Debug Info");
		AddInfoLine("F5 - Quick Save");
		AddInfoLine("F9 - Quick Load");

		System.Diagnostics.Debug.WriteLine("[OPTIONS TAB] Initialized");
	}

	public void RefreshContent() {
		// Update slider/checkbox values if needed
		// (Usually not needed as they track GameSettings directly)
	}

	public void Update(GameTime gameTime) {
		if (!IsVisible) {
			return;
		}

		RootPanel.Update(gameTime);
	}

	public void HandleMouse(MouseState mouseState, MouseState previousMouseState) {
		if (!IsVisible) {
			return;
		}

		RootPanel.HandleMouse(mouseState, previousMouseState);
	}

	public void Draw(SpriteBatch spriteBatch) {
		if (!IsVisible) {
			return;
		}

		RootPanel.Draw(spriteBatch);
	}

	public int GetNavigableCount() => _navigableElements.Count;

	public UIElement GetNavigableElement(int index) {
		if (index >= 0 && index < _navigableElements.Count) {
			return _navigableElements[index];
		}
		return null;
	}

	private void AddSectionHeader(string text, Color color) {
		UILabel label = new UILabel(_font, text) {
			TextColor = color
		};
		label.UpdateSize();
		RootPanel.AddChild(label);
	}

	private void AddInfoLine(string text) {
		UILabel label = new UILabel(_font, "  " + text) {
			TextColor = Color.White
		};
		label.UpdateSize();
		RootPanel.AddChild(label);
	}

	private void AddSpacer(int height) {
		UIPanel spacer = new UIPanel(_graphicsDevice) {
			Height = height,
			Width = RootPanel.Width
		};
		RootPanel.AddChild(spacer);
	}

	public void Dispose() {
		System.Diagnostics.Debug.WriteLine("[OPTIONS TAB] Disposed");
	}
}