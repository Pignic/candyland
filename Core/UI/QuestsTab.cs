using EldmeresTale.Quests;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace EldmeresTale.Core.UI.Tabs;

public class QuestsTab : IMenuTab {
	private readonly GraphicsDevice _graphicsDevice;
	private readonly BitmapFont _font;
	private readonly QuestManager _questManager;

	public UIPanel RootPanel { get; private set; }

	// Quest entry containers (created once, updated with data)
	private UIPanel _activeQuestsContainer;
	private UIPanel _completedQuestsContainer;

	// Cache for quest entries
	private readonly Dictionary<string, UIPanel> _questEntryCache;

	public bool IsVisible {
		get => RootPanel.Visible;
		set => RootPanel.Visible = value;
	}

	public QuestsTab(GraphicsDevice graphicsDevice, BitmapFont font, QuestManager questManager) {
		_graphicsDevice = graphicsDevice;
		_font = font;
		_questManager = questManager;
		_questEntryCache = [];

		// Wire up quest events to refresh
		_questManager.OnQuestStarted += (_) => RefreshContent();
		_questManager.OnQuestCompleted += (_, __) => RefreshContent();
		_questManager.OnObjectiveUpdated += (_, __) => RefreshContent();
		_questManager.OnNodeAdvanced += (_) => RefreshContent();
	}

	public void Initialize() {
		RootPanel = new UIPanel(_graphicsDevice) {
			X = 10,
			Y = 32,
			Width = 600,
			Height = 253,
			EnableScrolling = true,
			Layout = UIPanel.LayoutMode.Vertical,
			Spacing = 5,
			Visible = false
		};
		RootPanel.SetPadding(10);

		// Active quests section
		AddSectionHeader("-- ACTIVE QUESTS --", Color.Cyan);
		AddSpacer(5);

		_activeQuestsContainer = new UIPanel(_graphicsDevice) {
			Width = RootPanel.Width - 20,
			Height = -1,
			Layout = UIPanel.LayoutMode.Vertical,
			Spacing = 10,
			BackgroundColor = Color.Transparent
		};
		RootPanel.AddChild(_activeQuestsContainer);

		AddSpacer(15);

		// Completed quests section
		AddSectionHeader("-- COMPLETED QUESTS --", Color.Green);
		AddSpacer(5);

		_completedQuestsContainer = new UIPanel(_graphicsDevice) {
			Width = RootPanel.Width - 20,
			Height = -1,
			Layout = UIPanel.LayoutMode.Vertical,
			Spacing = 5,
			BackgroundColor = Color.Transparent
		};
		RootPanel.AddChild(_completedQuestsContainer);

		System.Diagnostics.Debug.WriteLine("[QUESTS TAB] Initialized");
	}

	public void RefreshContent() {
		// Clear containers (we'll rebuild quest entries efficiently)
		_activeQuestsContainer.ClearChildren();
		_completedQuestsContainer.ClearChildren();

		// Add active quests
		List<QuestInstance> activeQuests = _questManager.GetActiveQuests();

		if (activeQuests.Count == 0) {
			UILabel noQuestsLabel = new UILabel(_font, "  No active quests") {
				TextColor = Color.Gray
			};
			noQuestsLabel.UpdateSize();
			_activeQuestsContainer.AddChild(noQuestsLabel);
		} else {
			foreach (QuestInstance instance in activeQuests) {
				UIPanel questEntry = CreateQuestEntry(instance);
				_activeQuestsContainer.AddChild(questEntry);
			}
		}

		// Add completed quests
		HashSet<string> completedQuests = _questManager.GetCompletedQuests();

		if (completedQuests.Count == 0) {
			UILabel noCompletedLabel = new UILabel(_font, "  No completed quests yet") {
				TextColor = Color.Gray
			};
			noCompletedLabel.UpdateSize();
			_completedQuestsContainer.AddChild(noCompletedLabel);
		} else {
			foreach (string questId in completedQuests) {
				// Get quest definition to show name
				Quest quest = _questManager.GetQuestInstance(questId)?.Quest;
				if (quest == null) {
					// Try to get from all quests
					foreach (Quest q in _questManager.GetAllQuests()) {
						if (q.Id == questId) {
							quest = q;
							break;
						}
					}
				}

				if (quest != null) {
					UILabel completedLabel = new UILabel(_font, $"  ✓ {_questManager.GetQuestName(quest)}") {
						TextColor = Color.LimeGreen
					};
					completedLabel.UpdateSize();
					_completedQuestsContainer.AddChild(completedLabel);
				}
			}
		}

		System.Diagnostics.Debug.WriteLine($"[QUESTS TAB] Refreshed - {activeQuests.Count} active, {completedQuests.Count} completed");
	}

	private UIPanel CreateQuestEntry(QuestInstance instance) {
		UIPanel entry = new UIPanel(_graphicsDevice) {
			Width = _activeQuestsContainer.Width - 10,
			Height = -1,
			Layout = UIPanel.LayoutMode.Vertical,
			Spacing = 3,
			BackgroundColor = new Color(40, 40, 40, 150)
		};
		entry.SetPadding(5);

		// Quest name
		string questName = _questManager.GetQuestName(instance.Quest);
		UILabel nameLabel = new UILabel(_font, questName) {
			TextColor = Color.Yellow
		};
		nameLabel.UpdateSize();
		entry.AddChild(nameLabel);

		// Current node objectives
		QuestNode currentNode = instance.GetCurrentNode();
		if (currentNode != null) {
			foreach (QuestObjective objective in currentNode.Objectives) {
				UIPanel objectivePanel = CreateObjectiveEntry(instance, objective);
				entry.AddChild(objectivePanel);
			}
		}

		return entry;
	}

	private UIPanel CreateObjectiveEntry(QuestInstance instance, QuestObjective objective) {
		UIPanel objectivePanel = new UIPanel(_graphicsDevice) {
			Width = _activeQuestsContainer.Width - 20,
			Height = -1,
			Layout = UIPanel.LayoutMode.Vertical,
			Spacing = 2,
			BackgroundColor = Color.Transparent
		};

		// Get progress
		int current = instance.ObjectiveProgress.TryGetValue(objective, out int value) ? value : 0;
		int required = objective.RequiredCount;
		bool isComplete = current >= required;

		// Get objective text
		string objectiveText = _questManager.GetObjectiveDescription(instance, objective);

		// Remove progress suffix if present (we'll add it back with progress bar)
		if (objectiveText.Contains(" (")) {
			objectiveText = objectiveText.Substring(0, objectiveText.IndexOf(" ("));
		}

		// Objective text
		UILabel textLabel = new UILabel(_font, "  • " + objectiveText) {
			TextColor = isComplete ? Color.LimeGreen : Color.White
		};
		textLabel.UpdateSize();
		objectivePanel.AddChild(textLabel);

		// Progress bar (if count > 1)
		if (required > 1) {
			UIPanel progressBarContainer = new UIPanel(_graphicsDevice) {
				Width = objectivePanel.Width - 20,
				Height = 12,
				BackgroundColor = Color.Transparent
			};

			UIProgressBar progressBar = new UIProgressBar(
				_graphicsDevice,
				_font,
				() => $"{current}/{required}",
				() => (float)current / required
			) {
				X = 20,
				Y = 0,
				Width = objectivePanel.Width - 40,
				Height = 10,
				BackgroundColor = new Color(40, 40, 40),
				ForegroundColor = isComplete ? Color.LimeGreen : Color.Gold,
				BorderColor = Color.Gray,
				BorderWidth = 1,
				TextColor = Color.White
			};

			progressBarContainer.AddChild(progressBar);
			objectivePanel.AddChild(progressBarContainer);
		}

		return objectivePanel;
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

	public int GetNavigableCount() => 0; // Quests tab has no navigable elements

	public UIElement GetNavigableElement(int index) => null;

	private void AddSectionHeader(string text, Color color) {
		UILabel label = new UILabel(_font, text) {
			TextColor = color
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
		_questEntryCache.Clear();
		System.Diagnostics.Debug.WriteLine("[QUESTS TAB] Disposed");
	}
}