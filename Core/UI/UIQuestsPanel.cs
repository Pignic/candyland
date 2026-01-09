using EldmeresTale.Quests;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace EldmeresTale.Core.UI;

public class UIQuestsPanel : UIPanel {
	private readonly QuestManager _questManager;

	public UIQuestsPanel(QuestManager questManager) : base() {
		_questManager = questManager;

		// Configure panel
		X = 0;
		Y = 0;
		Width = 600;
		Height = 253;
		EnableScrolling = true;
		Layout = LayoutMode.Vertical;
		Spacing = 5;
		SetPadding(10);

		// Subscribe to quest events for live updates
		if (_questManager != null) {
			_questManager.OnQuestStarted += OnQuestChanged;
			_questManager.OnQuestCompleted += OnQuestCompleted;
			_questManager.OnObjectiveUpdated += OnObjectiveChanged;
			_questManager.OnNodeAdvanced += OnQuestChanged;
		}
		RefreshContent();
	}

	public void RefreshContent() {
		ClearChildren();

		// Title
		UILabel title = new UILabel("QUESTS") {
			TextColor = Color.Yellow
		};
		title.UpdateSize();
		AddChild(title);

		AddSpacer(10);

		// Active Quests Section
		List<QuestInstance> activeQuests = _questManager.GetActiveQuests();

		AddSectionHeader("-- ACTIVE --", Color.Cyan);

		if (activeQuests.Count == 0) {
			UILabel noQuestsLabel = new UILabel("  No active quests") {
				TextColor = Color.Gray
			};
			noQuestsLabel.UpdateSize();
			AddChild(noQuestsLabel);
		} else {
			AddSpacer(5);

			foreach (QuestInstance instance in activeQuests) {
				AddQuestEntry(instance);
			}
		}

		// Completed Quests Section (placeholder for now)
		AddSpacer(15);
		AddSectionHeader("-- COMPLETED --", Color.Green);
		UILabel completedLabel = new UILabel("  Coming soon...") {
			TextColor = Color.Gray
		};
		completedLabel.UpdateSize();
		AddChild(completedLabel);
	}

	private void AddQuestEntry(QuestInstance instance) {
		// Quest Name
		string questName = _questManager.GetQuestName(instance.Quest);
		UILabel nameLabel = new UILabel(questName) {
			TextColor = Color.Yellow
		};
		nameLabel.UpdateSize();
		AddChild(nameLabel);

		// Current Node's Objectives
		QuestNode currentNode = instance.GetCurrentNode();
		if (currentNode != null) {
			AddSpacer(3);

			// Show each objective with progress
			foreach (QuestObjective objective in currentNode.Objectives) {
				AddObjectiveEntry(instance, objective);
			}
		}

		AddSpacer(10);
	}

	private void AddObjectiveEntry(QuestInstance instance, QuestObjective objective) {
		// Get current progress
		int current = instance.ObjectiveProgress.TryGetValue(objective, out int value)
			? value : 0;
		int required = objective.RequiredCount;

		// Get objective text
		string objectiveText = _questManager.GetObjectiveDescription(instance, objective);

		// Remove the progress part if it's there (we'll show it in progress bar)
		if (objectiveText.Contains(" (")) {
			objectiveText = objectiveText.Substring(0, objectiveText.IndexOf(" ("));
		}

		// Objective text with bullet
		UILabel textLabel = new UILabel("  • " + objectiveText) {
			TextColor = current >= required ? Color.LimeGreen : Color.White
		};
		textLabel.UpdateSize();
		AddChild(textLabel);

		// Progress bar (only if count > 1)
		if (required > 1) {
			UIPanel progressBarContainer = new UIPanel() {
				Width = Width - 40,
				Height = 12,
				BackgroundColor = Color.Transparent
			};

			UIProgressBar progressBar = new UIProgressBar(
				() => $"{current}/{required}",
				() => (float)current / required
			) {
				X = 20,
				Y = 0,
				Width = Width - 60,
				Height = 10,
				BackgroundColor = new Color(40, 40, 40),
				ForegroundColor = current >= required ? Color.LimeGreen : Color.Gold,
				BorderColor = Color.Gray,
				BorderWidth = 1,
				TextColor = Color.White
			};

			progressBarContainer.AddChild(progressBar);
			AddChild(progressBarContainer);

			AddSpacer(3);
		}
	}

	private void AddSectionHeader(string text, Color color) {
		UILabel label = new UILabel(text) {
			TextColor = color
		};
		label.UpdateSize();
		AddChild(label);
	}

	private void AddSpacer(int height) {
		UIPanel spacer = new UIPanel() {
			Height = height,
			Width = Width
		};
		AddChild(spacer);
	}

	// Event handlers - refresh display when quests change
	private void OnQuestChanged(Quest quest) {
		RefreshContent();
	}

	private void OnQuestCompleted(Quest quest, QuestNode lastNode) {
		RefreshContent();
	}

	private void OnObjectiveChanged(Quest quest, QuestObjective objective) {
		RefreshContent();
	}
}