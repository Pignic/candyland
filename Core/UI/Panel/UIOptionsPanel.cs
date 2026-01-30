using EldmeresTale.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EldmeresTale.Core.UI;

public class UIOptionsPanel : UIPanel {
	private readonly int _currentScale;

	private readonly NavigationController _navController;

	private readonly ApplicationContext _appContext;

	// Controls
	private UISlider _musicVolumeSlider;
	private UISlider _sfxVolumeSlider;
	private UISlider _scaleSlider;
	private UICheckbox _fullscreenCheckbox;
	private UICheckbox _cameraShakeCheckbox;

	public UIOptionsPanel(ApplicationContext appContext, int currentScale, bool isFullscreen) : base() {
		_appContext = appContext;
		_currentScale = currentScale;

		_navController = new NavigationController {
			Mode = NavigationMode.Index,
			WrapAround = true
		};
		Width = -1;
		Height = -1;
		EnableScrolling = true;
		Layout = LayoutMode.Vertical;
		Spacing = 10;
		SetPadding(10);
		BuildContent(isFullscreen);
	}

	private void BuildContent(bool isFullscreen) {
		// Title
		UILabel title = new UILabel("OPTIONS") {
			TextColor = Color.Yellow
		};
		title.UpdateSize();
		AddChild(title);

		AddSpacer(10);

		// === AUDIO SECTION ===
		AddSectionHeader("-- AUDIO --", Color.LightGreen);
		AddSpacer(5);

		// Music Volume Slider
		_musicVolumeSlider = new UISlider("Music Volume", 0, 10,
			(int)(GameSettings.Instance.MusicVolume * 10)) {
			Width = 300,
			IsNavigable = true
		};
		_musicVolumeSlider.OnValueChanged += (value) => {
			float volume = value / 10f;
			GameSettings.Instance.MusicVolume = volume;
			GameSettings.Instance.Save();
			System.Diagnostics.Debug.WriteLine($"[OPTIONS] Music volume: {volume:F1}");
			OnMusicVolumeChanged(volume);
		};
		AddChild(_musicVolumeSlider);

		AddSpacer(5);

		// SFX Volume Slider
		_sfxVolumeSlider = new UISlider("SFX Volume", 0, 10,
			(int)(GameSettings.Instance.SfxVolume * 10)) {
			Width = 300,
			IsNavigable = true
		};
		_sfxVolumeSlider.OnValueChanged += (value) => {
			float volume = value / 10f;
			GameSettings.Instance.SfxVolume = volume;
			GameSettings.Instance.Save();
			System.Diagnostics.Debug.WriteLine($"[OPTIONS] SFX volume: {volume:F1}");
			OnSfxVolumeChanged(volume);
		};
		AddChild(_sfxVolumeSlider);

		AddSpacer(15);

		// === VIDEO SECTION ===
		AddSectionHeader("-- VIDEO --", Color.Cyan);
		AddSpacer(5);

		// Window Scale Slider
		_scaleSlider = new UISlider("Window Scale", 1, 3, _currentScale) {
			Width = 300,
			IsNavigable = true
		};
		_scaleSlider.OnValueChanged += (value) => {
			GameSettings.Instance.WindowScale = value;
			GameSettings.Instance.Save();
			System.Diagnostics.Debug.WriteLine($"[OPTIONS] Scale: {value}");
			OnScaleChanged(value);
		};
		AddChild(_scaleSlider);

		AddSpacer(5);

		// Fullscreen Checkbox
		_fullscreenCheckbox = new UICheckbox("Fullscreen", isFullscreen) {
			Width = 300,
			IsNavigable = true
		};
		_fullscreenCheckbox.OnValueChanged += (value) => {
			GameSettings.Instance.IsFullscreen = value;
			GameSettings.Instance.Save();
			System.Diagnostics.Debug.WriteLine($"[OPTIONS] Fullscreen: {value}");
			OnFullscreenChanged(value);
		};
		AddChild(_fullscreenCheckbox);

		AddSpacer(5);

		// Camera Shake Checkbox
		_cameraShakeCheckbox = new UICheckbox("Camera Shake",
			GameSettings.Instance.CameraShake) {
			Width = 300,
			IsNavigable = true
		};
		_cameraShakeCheckbox.OnValueChanged += (value) => {
			GameSettings.Instance.CameraShake = value;
			GameSettings.Instance.Save();
			System.Diagnostics.Debug.WriteLine($"[OPTIONS] Camera Shake: {value}");
			OnCameraShakeChanged(value);
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
		UILabel label = new UILabel(text) {
			TextColor = color
		};
		label.UpdateSize();
		AddChild(label);
	}

	private void AddInfoLine(string text) {
		UILabel label = new UILabel("  " + text) {
			TextColor = Color.White
		};
		label.UpdateSize();
		AddChild(label);
	}

	private void AddSpacer(int height) {
		UIPanel spacer = new UIPanel() {
			Height = height
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


	// Event handlers
	private void OnMusicVolumeChanged(float volume) {
		_appContext.MusicPlayer.Volume = volume;
		System.Diagnostics.Debug.WriteLine($"[SCENE] Music volume: {volume:F2}");
	}

	private void OnSfxVolumeChanged(float volume) {
		_appContext.SoundEffects.MasterVolume = volume;
		System.Diagnostics.Debug.WriteLine($"[SCENE] SFX volume: {volume:F2}");
	}

	private void OnScaleChanged(int newScale) {
		int newWidth = _appContext.Display.VirtualWidth * newScale;
		int newHeight = _appContext.Display.VirtualHeight * newScale;
		_appContext.RequestResolutionChange(newWidth, newHeight);
		System.Diagnostics.Debug.WriteLine($"[SCENE] Scale: {newScale}");
	}

	private void OnFullscreenChanged(bool isFullscreen) {
		_appContext.RequestFullscreenChange(isFullscreen);
		System.Diagnostics.Debug.WriteLine($"[SCENE] Fullscreen: {isFullscreen}");
	}

	private void OnCameraShakeChanged(bool enabled) {
		// Camera shake is handled via GameSettings, no additional action needed
		System.Diagnostics.Debug.WriteLine($"[SCENE] Camera Shake: {enabled}");
	}


	public override bool HandleMouse(MouseState mouse, MouseState previousMouse) {
		if (!Visible || !Enabled) {
			return false;
		}

		InputCommands input = _appContext.Input.GetCommands();
		UpdateOptionsNavigation(input);
		return base.HandleMouse(mouse, previousMouse);
	}

	public void UpdateOptionsNavigation(InputCommands input) {
		_navController.Update(input);
		int selectedIndex = _navController.SelectedIndex;
		int navigableCount = GetNavigableCount();

		// Update hover state for all navigable elements
		for (int i = 0; i < navigableCount; i++) {
			UIElement element = GetNavigableElement(i);
			bool isSelected = i == selectedIndex;
			if (element is UINavigableElement nav) {
				nav.ForceHoverState(isSelected);
			}
		}

		// Handle input for selected element
		UIElement selectedElement = GetNavigableElement(selectedIndex);

		if (selectedElement is UISlider slider) {
			// Adjust slider with left/right
			if (input.MoveLeftPressed) {
				slider.Value--;
			}
			if (input.MoveRightPressed) {
				slider.Value++;
			}
		} else if (selectedElement is UICheckbox checkbox) {
			// Toggle checkbox with space/attack
			if (input.AttackPressed) {
				checkbox.IsChecked = !checkbox.IsChecked;
			}
		}
	}

	public void OnShow() {
		_navController.Mode = NavigationMode.Index;
		_navController.ItemCount = GetNavigableCount();
		_navController.Reset();
		System.Diagnostics.Debug.WriteLine($"[MENU] Options tab shown (navigable: {_navController.ItemCount})");
	}
}