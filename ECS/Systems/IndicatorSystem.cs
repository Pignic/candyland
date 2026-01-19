using DefaultEcs;
using DefaultEcs.System;
using EldmeresTale.Core.UI;
using EldmeresTale.ECS.Components;
using EldmeresTale.Quests;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace EldmeresTale.ECS.Systems;

public class IndicatorSystem : AEntitySetSystem<SpriteBatch> {

	public float Frequency = 3;
	public float Amplitude = 2;
	public float BaseY = 0;
	public float Scale = 2;

	private readonly BitmapFont _font;
	readonly QuestManager _questManager;

	readonly RoomManager _roomManager;

	public IndicatorSystem(World world, BitmapFont font, QuestManager questManager, RoomManager roomManager)
		: base(world.GetEntities()
			.With<Position>()
			.With((in Faction f) => f.Name == FactionName.NPC)
			.With<InteractionZone>()
			.With<RoomId>()
			.AsSet()) {
		_font = font;
		_questManager = questManager;
		_roomManager = roomManager;
	}


	protected override void Update(SpriteBatch spriteBatch, in Entity entity) {
		InteractionZone interactionZone = entity.Get<InteractionZone>();
		RoomId room = entity.Get<RoomId>();
		if (room.Name != _roomManager.CurrentRoom.Id) {
			return;
		}

		string indicatorSign = null;
		Color? color = null;

		if (interactionZone.IsPlayerNearby) {
			indicatorSign = "E";
			color = Color.Yellow;
		} else {
			if (HasQuestObjective(interactionZone.InteractionId)) {
				indicatorSign = "!";
				color = Color.Yellow;
			} else if (HasQuestAvailable(interactionZone.InteractionId)) {
				indicatorSign = "?";
				color = new Color(180, 180, 255);
			}
		}
		if (indicatorSign != null && color.HasValue) {
			Position position = entity.Get<Position>();
			Vector2 indicatorPos = new Vector2(position.Value.X, position.Value.Y - BaseY);
			if (entity.Has<Collider>()) {
				Collider collider = entity.Get<Collider>();
				indicatorPos += new Vector2(collider.Width / 2f, 0);
			}
			DrawIndicator(spriteBatch, indicatorPos, indicatorSign, color.Value);
		}
	}

	private void DrawIndicator(SpriteBatch spriteBatch, Vector2 position, string sign, Color color) {
		if (_font == null) {
			return;
		}

		float bobOffset = (float)Math.Sin(DateTime.Now.TimeOfDay.TotalSeconds * 3f) * 3f;
		Vector2 indicatorPos = new Vector2(
			position.X - (_font.MeasureString(sign, Scale) / 2),
			position.Y - _font.GetHeight(1, Scale) + bobOffset
		);

		_font.DrawText(spriteBatch, sign, indicatorPos, color, Color.DarkGray, null, Scale);
	}

	public bool HasQuestObjective(string npcId) {
		if (_questManager == null) {
			return false;
		}
		foreach (QuestInstance instance in _questManager.GetActiveQuests()) {
			QuestNode currentNode = instance.GetCurrentNode();
			if (currentNode == null) {
				continue;
			}

			foreach (QuestObjective objective in currentNode.Objectives) {
				// Check if incomplete
				int current = instance.ObjectiveProgress.TryGetValue(objective, out int value)
					? value : 0;
				if (current >= objective.RequiredCount) {
					continue;
				}

				// Check if this NPC is involved
				bool isForThisNPC = false;

				if (objective.Type == "talk_to_npc" && objective.Target == npcId) {
					isForThisNPC = true;
				} else if (objective.Type == "choose_dialog_response" &&
						  objective.Target.StartsWith(npcId + "_")) {
					isForThisNPC = true;
				}

				if (isForThisNPC) {
					return true;
				}
			}
		}
		return false;
	}

	public bool HasQuestAvailable(string npcId) {
		if (_questManager == null) {
			return false;
		}

		foreach (Quest quest in _questManager.GetAllQuests()) {
			// Skip already active/completed
			if (_questManager.IsQuestActive(quest.Id)) {
				continue;
			}
			if (_questManager.IsQuestCompleted(quest.Id)) {
				continue;
			}
			if (!_questManager.CanAcceptQuest(quest.Id)) {
				continue;
			}
			// Check if first objective involves this NPC
			QuestNode startNode = quest.Nodes.TryGetValue(quest.StartNodeId, out QuestNode value)
				? value : null;

			if (startNode?.Objectives.Count > 0) {
				QuestObjective firstObj = startNode.Objectives[0];
				if (firstObj.Type == "talk_to_npc" && firstObj.Target == npcId) {
					return true;
				}
			}
			// Check questGiver field
			if (!string.IsNullOrEmpty(quest.QuestGiver) && quest.QuestGiver == npcId) {
				return true;
			}
		}
		return false;
	}
}
