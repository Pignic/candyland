using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.Core.UI;

public class TabConfig {
	public string Name { get; set; }
	public UIElement Content { get; set; }
	public Action OnShow { get; set; }
	public Action OnHide { get; set; }
}

public class UITabContainer : UIElement {
	private readonly TabConfig[] _tabs;
	private readonly UIButton[] _tabButtons;
	private readonly UIPanel _buttonPanel;

	private int _selectedTabIndex = -1;

	// Static field to remember last selected tab across menu sessions
	private static int _lastSelectedTabIndex = 0;

	public event Action<int> OnTabChanged;

	public int SelectedTabIndex => _selectedTabIndex;

	public UITabContainer(TabConfig[] tabs) : base() {
		_tabs = tabs;

		if (tabs == null || tabs.Length == 0) {
			throw new ArgumentException("UITabContainer requires at least one tab");
		}

		// Create button panel at top
		_buttonPanel = new UIPanel() {
			X = 0,
			Y = 0,
			Height = 22,
			Layout = UIPanel.LayoutMode.Horizontal,
			Spacing = 0,
			BackgroundColor = Color.Transparent
		};
		AddChild(_buttonPanel);

		// Create tab buttons
		_tabButtons = new UIButton[tabs.Length];
		int buttonWidth = Width > 0 ? Width / tabs.Length : 100; // Default if width not set yet

		for (int i = 0; i < tabs.Length; i++) {
			int tabIndex = i; // Capture for lambda
			_tabButtons[i] = new UIButton(tabs[i].Name) {
				Width = buttonWidth,
				Height = 22,
				BorderWidth = 0, // No borders to prevent overlap
				BackgroundColor = new Color(60, 60, 60),
				HoverColor = new Color(80, 80, 80),
				TextColor = Color.LightGray,
				OnClick = () => SelectTab(tabIndex)
			};
			_buttonPanel.AddChild(_tabButtons[i]);
		}

		// Add all content panels as children (they'll manage their own visibility)
		foreach (TabConfig tab in tabs) {
			if (tab.Content != null) {
				// Position content below buttons
				tab.Content.Y = 22;
				tab.Content.Visible = false;
				AddChild(tab.Content);
			}
		}

		// Show last selected tab (or first tab if never opened)
		int initialTab = _lastSelectedTabIndex;
		if (initialTab < 0 || initialTab >= _tabs.Length) {
			initialTab = 0; // Fallback to first tab if invalid
		}
		SelectTab(initialTab);
	}

	public void SelectTab(int tabIndex) {
		if (tabIndex < 0 || tabIndex >= _tabs.Length) {
			return;
		}

		if (tabIndex == _selectedTabIndex) {
			return; // Already selected
		}

		if (_selectedTabIndex >= 0) {
			// Hide current tab
			TabConfig oldTab = _tabs[_selectedTabIndex];
			if (oldTab.Content != null) {
				oldTab.Content.Visible = false;
			}
			oldTab.OnHide?.Invoke();
		}

		// Update selection
		_selectedTabIndex = tabIndex;
		_lastSelectedTabIndex = tabIndex; // Remember for next time menu opens

		// Show new tab
		TabConfig newTab = _tabs[tabIndex];
		if (newTab.Content != null) {
			newTab.Content.Visible = true;
		}
		newTab.OnShow?.Invoke();

		// Update button styles
		for (int i = 0; i < _tabButtons.Length; i++) {
			if (i == tabIndex) {
				// Selected tab - brighter with bottom border
				_tabButtons[i].BackgroundColor = Color.SlateGray;
				_tabButtons[i].TextColor = Color.Yellow;
				_tabButtons[i].BorderWidth = 0;
				_tabButtons[i].BorderColor = Color.Yellow;
			} else {
				// Unselected tabs
				_tabButtons[i].BackgroundColor = new Color(60, 60, 60);
				_tabButtons[i].TextColor = Color.LightGray;
				_tabButtons[i].BorderWidth = 0;
			}
		}

		// Fire event
		OnTabChanged?.Invoke(tabIndex);

		System.Diagnostics.Debug.WriteLine($"[UITabContainer] Switched to tab {tabIndex}: {newTab.Name}");
	}

	public UIElement GetCurrentContent() {
		return _tabs[_selectedTabIndex].Content;
	}

	public void UpdateButtonWidths() {
		if (Width <= 0) {
			return;
		}

		_buttonPanel.Width = Width;
		int buttonWidth = Width / _tabs.Length;

		foreach (UIButton button in _tabButtons) {
			button.Width = buttonWidth;
		}

		// Force layout recalculation after width changes
		_buttonPanel.UpdateLayout();
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		// UIElement base class handles drawing children
	}
}