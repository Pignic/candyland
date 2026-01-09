using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Core.UI;

public class UIOptionsPanel : UIPanel {
	private readonly BitmapFont _font;
	private readonly int _currentScale;

	// Controls
	private UISlider _musicVolumeSlider;
	private UISlider _sfxVolumeSlider;
	private UISlider _scaleSlider;
	private UICheckbox _fullscreenCheckbox;
	private UICheckbox _cameraShakeCheckbox;

	// Events
	public event Action<float> OnMusicVolumeChanged;
	public event Action<float> OnSfxVolumeChanged;
	public event Action<int> OnScaleChanged;
	public event Action<bool> OnFullscreenChanged;
	public event Action<bool> OnCameraShakeChanged;

	public UIOptionsPanel(GraphicsDevice graphicsDevice, BitmapFont font, int currentScale, bool isFullscreen)
		: base(graphicsDevice) {
		_font = font;
		_currentScale = currentScale;

		// Configure panel
		X = 0;
		Y = 0;
		Width = 600;
		Height = 253;
		EnableScrolling = true;
		Layout = LayoutMode.Vertical;
		Spacing = 10;
		SetPadding(10);

		BuildContent(isFullscreen);
	}

	private void BuildContent(bool isFullscreen) {
		// Title
		UILabel title = new UILabel(_font, "OPTIONS") {
			TextColor = Color.Yellow
		};
		title.UpdateSize();
		AddChild(title);

		AddSpacer(10);

		// === AUDIO SECTION ===
		AddSectionHeader("-- AUDIO --", Color.LightGreen);
		AddSpacer(5);

		// Music Volume Slider
		_musicVolumeSlider = new UISlider(GraphicsDevice, _font, "Music Volume", 0, 10,
			(int)(GameSettings.Instance.MusicVolume * 10)) {
			Width = 300,
			IsNavigable = true
		};
		_musicVolumeSlider.OnValueChanged += (value) => {
			float volume = value / 10f;
			GameSettings.Instance.MusicVolume = volume;
			GameSettings.Instance.Save();
			System.Diagnostics.Debug.WriteLine($"[OPTIONS] Music volume: {volume:F1}");
			OnMusicVolumeChanged?.Invoke(volume);
		};
		AddChild(_musicVolumeSlider);

		AddSpacer(5);

		// SFX Volume Slider
		_sfxVolumeSlider = new UISlider(GraphicsDevice, _font, "SFX Volume", 0, 10,
			(int)(GameSettings.Instance.SfxVolume * 10)) {
			Width = 300,
			IsNavigable = true
		};
		_sfxVolumeSlider.OnValueChanged += (value) => {
			float volume = value / 10f;
			GameSettings.Instance.SfxVolume = volume;
			GameSettings.Instance.Save();
			System.Diagnostics.Debug.WriteLine($"[OPTIONS] SFX volume: {volume:F1}");
			OnSfxVolumeChanged?.Invoke(volume);
		};
		AddChild(_sfxVolumeSlider);

		AddSpacer(15);

		// === VIDEO SECTION ===
		AddSectionHeader("-- VIDEO --", Color.Cyan);
		AddSpacer(5);

		// Window Scale Slider
		_scaleSlider = new UISlider(GraphicsDevice, _font, "Window Scale", 1, 3, _currentScale) {
			Width = 300,
			IsNavigable = true
		};
		_scaleSlider.OnValueChanged += (value) => {
			GameSettings.Instance.WindowScale = value;
			GameSettings.Instance.Save();
			System.Diagnostics.Debug.WriteLine($"[OPTIONS] Scale: {value}");
			OnScaleChanged?.Invoke(value);
		};
		AddChild(_scaleSlider);

		AddSpacer(5);

		// Fullscreen Checkbox
		_fullscreenCheckbox = new UICheckbox(GraphicsDevice, _font, "Fullscreen", isFullscreen) {
			Width = 300,
			IsNavigable = true
		};
		_fullscreenCheckbox.OnValueChanged += (value) => {
			GameSettings.Instance.IsFullscreen = value;
			GameSettings.Instance.Save();
			System.Diagnostics.Debug.WriteLine($"[OPTIONS] Fullscreen: {value}");
			OnFullscreenChanged?.Invoke(value);
		};
		AddChild(_fullscreenCheckbox);

		AddSpacer(5);

		// Camera Shake Checkbox
		_cameraShakeCheckbox = new UICheckbox(GraphicsDevice, _font, "Camera Shake",
			GameSettings.Instance.CameraShake) {
			Width = 300,
			IsNavigable = true
		};
		_cameraShakeCheckbox.OnValueChanged += (value) => {
			GameSettings.Instance.CameraShake = value;
			GameSettings.Instance.Save();
			System.Diagnostics.Debug.WriteLine($"[OPTIONS] Camera Shake: {value}");
			OnCameraShakeChanged?.Invoke(value);
		};
		AddChild(_cameraShakeCheckbox);

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

		// === DEBUG SECTION ===
		AddSectionHeader("-- DEBUG --", Color.Red);
		AddSpacer(5);

		AddInfoLine("F5 - Quick Save");
		AddInfoLine("F9 - Quick Load");
	}

	private void AddSectionHeader(string text, Color color) {
		UILabel label = new UILabel(_font, text) {
			TextColor = color
		};
		label.UpdateSize();
		AddChild(label);
	}

	private void AddInfoLine(string text) {
		UILabel label = new UILabel(_font, "  " + text) {
			TextColor = Color.White
		};
		label.UpdateSize();
		AddChild(label);
	}

	private void AddSpacer(int height) {
		UIPanel spacer = new UIPanel(GraphicsDevice) {
			Height = height,
			Width = Width
		};
		AddChild(spacer);
	}

	public int GetNavigableCount() {
		int count = 0;
		foreach (UIElement child in Children) {
			if (child.IsNavigable) {
				count++;
			}
		}
		return count;
	}

	public UIElement GetNavigableElement(int index) {
		int currentIndex = 0;
		foreach (UIElement child in Children) {
			if (child.IsNavigable) {
				if (currentIndex == index) {
					return child;
				}
				currentIndex++;
			}
		}
		return null;
	}
}