using Candyland.Core;
using Candyland.Core.UI;
using Candyland.Quests;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Candyland.Entities;

public class NPC : ActorEntity {

	public string DialogId { get; set; }
	private QuestManager _questManager;
	private BitmapFont _font;
	private float markerScale = 3;

	// Interaction
	public float InteractionRange { get; set; } = 50f;
	public bool CanInteract { get; set; } = true;

	// Visual feedback
	private bool _isPlayerNearby = false;
	private float _indicatorTimer = 0f;

	// Static sprite constructor
	public NPC(Texture2D texture, Vector2 position, string dialogId, QuestManager questManager, int width = 24, int height = 24)
		: base(texture, position, width, height, 0f) // NPCs don't move (speed = 0)
	{
		DialogId = dialogId;

		// NPCs don't take damage or attack
		MaxHealth = 999999;
		health = MaxHealth;
		AttackDamage = 0;
		_questManager = questManager;

	}

	// Animated sprite constructor
	public NPC(Texture2D spriteSheet, Vector2 position, string dialogId, QuestManager questManager, int frameCount, int frameWidth, int frameHeight, float frameTime, int width = 24, int height = 24)
		: base(spriteSheet, position, frameCount, frameWidth, frameHeight, frameTime, width, height, 0f) {
		DialogId = dialogId;

		// NPCs don't take damage or attack
		MaxHealth = 999999;
		health = MaxHealth;
		AttackDamage = 0;
		_questManager = questManager;
	}

	public override void Update(GameTime gameTime) {
		base.Update(gameTime);
		if(!IsAlive) return;

		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

		// Update indicator animation
		_indicatorTimer += deltaTime;

		// Update animation if using one (idle animation)
		if(_useAnimation && _animationController != null) {
			_animationController.Update(gameTime, Vector2.Zero); // No movement
		}
	}

	public bool IsPlayerInRange(Vector2 playerPosition) {
		Vector2 npcCenter = Position + new Vector2(Width / 2f, Height / 2f);
		float distance = Vector2.Distance(playerPosition, npcCenter);

		bool inRange = distance <= InteractionRange;
		_isPlayerNearby = inRange;

		return inRange && CanInteract;
	}

	public override void Draw(SpriteBatch spriteBatch) {
		base.Draw(spriteBatch);
		if(HasQuestObjective()) {
			DrawQuestObjectiveIndicator(spriteBatch);  // "!"
		} else if(HasQuestAvailable()) {
			DrawQuestAvailableIndicator(spriteBatch);  // "?"
		} else if(_isPlayerNearby && CanInteract) {
			DrawInteractionIndicator(spriteBatch);     // "E"
		}
	}

	private void DrawQuestObjectiveIndicator(SpriteBatch spriteBatch) {
		if(_font == null) return;

		float bobOffset = (float)System.Math.Sin(_indicatorTimer * 3f) * 3f;
		Vector2 indicatorPos = new Vector2(
			Position.X + Width / 2f - (2.5f * markerScale),
			Position.Y - (10 * markerScale) + bobOffset
		);

		_font.drawText(spriteBatch, "!", indicatorPos, Color.Yellow, Color.DarkGray, null, markerScale);
	}

	private void DrawQuestAvailableIndicator(SpriteBatch spriteBatch) {
		if(_font == null) return;
		float bobOffset = (float)System.Math.Sin(_indicatorTimer * 3f) * 3f;
		Vector2 indicatorPos = new Vector2(
			Position.X + Width / 2f - (2.5f * markerScale),
			Position.Y - (10 * markerScale) + bobOffset
		);
		_font.drawText(spriteBatch, "?", indicatorPos, new Color(180, 180, 255), Color.DarkGray, null, markerScale);
	}

	private void DrawInteractionIndicator(SpriteBatch spriteBatch) {
		if(_font == null) return;

		float bobOffset = (float)System.Math.Sin(_indicatorTimer * 3f) * 3f;
		Vector2 indicatorPos = new Vector2(
			Position.X + Width / 2f - (2.5f * markerScale),
			Position.Y - (10 * markerScale) + bobOffset
		);

		_font.drawText(spriteBatch, "E", indicatorPos, Color.White, Color.DarkGray, null, markerScale);
	}

	public Vector2 GetCenterPosition() {
		return Position + new Vector2(Width / 2f, Height / 2f);
	}
	public void SetQuestManager(QuestManager questManager) {
		_questManager = questManager;
	}

	public void SetFont(BitmapFont font) {
		_font = font;
	}

	public bool HasQuestObjective() {
		if(_questManager == null) return false;

		var activeQuests = _questManager.getActiveQuests();
		foreach(var instance in activeQuests) {
			var currentNode = instance.getCurrentNode();
			if(currentNode == null) continue;

			foreach(var objective in currentNode.objectives) {
				// Check if incomplete
				int current = instance.objectiveProgress.ContainsKey(objective)
					? instance.objectiveProgress[objective] : 0;
				if(current >= objective.requiredCount) continue;

				// Check if this NPC is involved
				bool isForThisNPC = false;

				if(objective.type == "talk_to_npc" && objective.target == DialogId) {
					isForThisNPC = true;
				} else if(objective.type == "choose_dialog_response" &&
						  objective.target.StartsWith(DialogId + "_")) {
					// Example: "shepherd_wolves_dead" → belongs to "shepherd"
					isForThisNPC = true;
				}

				if(isForThisNPC) return true;
			}
		}
		return false;
	}

	public bool HasQuestAvailable() {
		if(_questManager == null) return false;

		foreach(Quest quest in _questManager.getAllQuests()) {
			// Skip already active/completed
			if(_questManager.isQuestActive(quest.id)) continue;
			if(_questManager.isQuestCompleted(quest.id)) continue;
			if(!_questManager.canAcceptQuest(quest.id)) continue;

			// Check if first objective involves this NPC
			var startNode = quest.nodes.ContainsKey(quest.startNodeId)
				? quest.nodes[quest.startNodeId] : null;

			if(startNode?.objectives.Count > 0) {
				var firstObj = startNode.objectives[0];
				if(firstObj.type == "talk_to_npc" && firstObj.target == DialogId) {
					return true;
				}
			}

			// Check questGiver field
			if(!string.IsNullOrEmpty(quest.questGiver) && quest.questGiver == DialogId) {
				return true;
			}
		}
		return false;
	}

}