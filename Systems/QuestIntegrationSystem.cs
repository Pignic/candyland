using EldmeresTale.Entities;
using EldmeresTale.Quests;

namespace EldmeresTale.Systems;

public class QuestIntegrationSystem : GameSystem {
	private QuestManager _questManager;

	public void OnEnemyKilled(Enemy enemy) {
		_questManager.updateObjectiveProgress("kill_enemy", enemy.EnemyType, 1);
	}

	public void OnPickupCollected(Pickup pickup) {
		_questManager.updateObjectiveProgress("collect_item", pickup.ItemId, 1);
	}

	public void OnNPCTalked(string npcDialogId) {
		_questManager.updateObjectiveProgress("talk_to_npc", npcDialogId, 1);
	}
}