using EldmeresTale.Audio;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Factories;
using EldmeresTale.ECS.Systems;
using EldmeresTale.Entities;
using EldmeresTale.Events;
using EldmeresTale.Quests;
using EldmeresTale.Systems;
using Microsoft.Xna.Framework;
using System;

namespace EldmeresTale.Core.Coordination;

public class EventCoordinator : IDisposable {
	private readonly GameEventBus _eventBus;
	private readonly EventSubscriptions _subscriptions = new();

	// System references
	private readonly QuestManager _questManager;
	private readonly Player _player;
	private readonly SoundEffectPlayer _soundPlayer;
	private readonly NotificationSystem _notificationSystem;

	// ECS
	private readonly MovementSystem _movementSystem;

	private readonly VFXFactory _vfxFactory;

	public EventCoordinator(
		GameEventBus eventBus,
		QuestManager questManager,
		Player player,
		SoundEffectPlayer soundPlayer,
		NotificationSystem notificationSystem,
		MovementSystem movementSystem,
		 VFXFactory vfxFactory
	) {
		_eventBus = eventBus;
		_questManager = questManager;
		_player = player;
		_soundPlayer = soundPlayer;
		_notificationSystem = notificationSystem;
		_movementSystem = movementSystem;
		_vfxFactory = vfxFactory;
	}

	public void Initialize() {

		// Physics events
		_subscriptions.Add(_eventBus.Subscribe<PropCollectedEvent>(OnPropCollected));
		_subscriptions.Add(_eventBus.Subscribe<PropPushedEvent>(OnPropPushed));

		// Loot events
		//_subscriptions.Add(_eventBus.Subscribe<PickupSpawnedEvent>(OnPickupSpawned));
		_subscriptions.Add(_eventBus.Subscribe<PickupCollectedEvent>(OnPickupCollected));

		// Quest events
		_subscriptions.Add(_eventBus.Subscribe<QuestStartedEvent>(OnQuestStarted));
		_subscriptions.Add(_eventBus.Subscribe<QuestNodeAdvancedEvent>(OnQuestNodeAdvanced));
		_subscriptions.Add(_eventBus.Subscribe<QuestCompletedEvent>(OnQuestCompleted));
		_subscriptions.Add(_eventBus.Subscribe<QuestObjectiveUpdatedEvent>(OnQuestObjectiveUpdated));

		// Player events
		_subscriptions.Add(_eventBus.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp));
		_subscriptions.Add(_eventBus.Subscribe<PlayerAttackEvent>(OnPlayerAttack));

		_subscriptions.Add(_eventBus.Subscribe<AttackEvent>(OnAttack));

		// Room change event
		_subscriptions.Add(_eventBus.Subscribe<RoomChangedEvent>(OnRoomChanged));


		System.Diagnostics.Debug.WriteLine($"[EVENT COORDINATOR] Initialized with {_subscriptions.Count} event subscriptions");
	}

	private void OnPropCollected(PropCollectedEvent e) {
		//System.Diagnostics.Debug.WriteLine($"Collected prop: {e.Prop.Type}");
		_soundPlayer.Play("buy_item", 0.7f);
		//_questManager.UpdateObjectiveProgress("collect_item", e.Prop.Type.ToString(), 1);
	}

	private void OnPropPushed(PropPushedEvent e) {
		_soundPlayer.Play("equip_armor", 0.3f);
		//System.Diagnostics.Debug.WriteLine($"Pushed prop: {e.Prop.Type}");
	}


	private void OnPickupCollected(PickupCollectedEvent e) {
		_player.CollectPickup(e.Type, e.Value);
		string sound = e.Type switch {
			PickupType.Health => "use_potion",
			_ => "buy_item"
		};
		_soundPlayer.Play(sound, 0.8f);
		_questManager.UpdateObjectiveProgress("collect_item", e.Type.ToString(), 1);
		System.Diagnostics.Debug.WriteLine($"[LOOT] Collected {e.Type}");
	}

	private void OnQuestStarted(QuestStartedEvent e) {
		System.Diagnostics.Debug.WriteLine($"[QUEST STARTED] {e.QuestName}");
		_soundPlayer.Play("menu_accept", 0.9f);
		_notificationSystem.ShowQuestStarted(e.QuestName);
	}

	private void OnQuestCompleted(QuestCompletedEvent e) {
		System.Diagnostics.Debug.WriteLine($"[QUEST COMPLETED] {e.QuestName}");
		_soundPlayer.Play("level_up", 0.9f);
		_player.GiveRewards(e.LastNode.Rewards);
		_notificationSystem.ShowQuestCompleted(e.QuestName, e.LastNode.Rewards.Xp, e.LastNode.Rewards.Gold);
	}

	private void OnQuestObjectiveUpdated(QuestObjectiveUpdatedEvent e) {
		System.Diagnostics.Debug.WriteLine("[QUEST PROGRESS] Objective updated");
		// Notification shown by NotificationSystem
	}

	private void OnQuestNodeAdvanced(QuestNodeAdvancedEvent e) {
		_player.GiveRewards(e.LastNode.Rewards);
	}

	private void OnPlayerLevelUp(PlayerLevelUpEvent e) {
		_vfxFactory.CreateLevelUp(_player.Position);
		_soundPlayer.Play("level_up", 1.0f);
		System.Diagnostics.Debug.WriteLine($"[LEVEL UP] Player reached level {e.NewLevel}");
	}

	private void OnPlayerAttack(PlayerAttackEvent e) {
		e.Player.TriggerAttackEffect();
		_soundPlayer.Play("sword_woosh");
	}

	private void OnAttack(AttackEvent e) {
		_vfxFactory.CreateDamageNumber(e.Position.Value, e.Damage.ToString(), Color.Red, 1);
	}

	public void OnRoomChanged(RoomChangedEvent e) {
		_movementSystem.SetCurrentMap(e.NewRoom.Map);
	}

	public void Dispose() {
		_subscriptions?.Dispose();
		System.Diagnostics.Debug.WriteLine("[EVENT COORDINATOR] Disposed");
	}
}