using EldmeresTale.Audio;
using EldmeresTale.ECS.Components;
using EldmeresTale.ECS.Systems;
using EldmeresTale.Entities;
using EldmeresTale.Events;
using EldmeresTale.Quests;
using EldmeresTale.Systems;
using System;

namespace EldmeresTale.Core.Coordination;

public class EventCoordinator : IDisposable {
	private readonly GameEventBus _eventBus;
	private readonly EventSubscriptions _subscriptions = new();

	// System references
	private readonly VFXSystem _vfxSystem;
	private readonly QuestManager _questManager;
	private readonly Player _player;
	private readonly Camera _camera;
	private readonly SoundEffectPlayer _soundPlayer;
	private readonly NotificationSystem _notificationSystem;

	// ECS
	private readonly MovementSystem _movementSystem;

	public EventCoordinator(
		GameEventBus eventBus,
		VFXSystem vfxSystem,
		QuestManager questManager,
		Player player,
		Camera camera,
		SoundEffectPlayer soundPlayer,
		NotificationSystem notificationSystem,
		MovementSystem movementSystem
	) {
		_eventBus = eventBus;
		_vfxSystem = vfxSystem;
		_questManager = questManager;
		_player = player;
		_camera = camera;
		_soundPlayer = soundPlayer;
		_notificationSystem = notificationSystem;
		_movementSystem = movementSystem;
	}

	public void Initialize() {

		// Physics events
		_subscriptions.Add(_eventBus.Subscribe<PropCollectedEvent>(OnPropCollected));
		_subscriptions.Add(_eventBus.Subscribe<PropPushedEvent>(OnPropPushed));

		// Loot events
		//_subscriptions.Add(_eventBus.Subscribe<PickupSpawnedEvent>(OnPickupSpawned));
		_subscriptions.Add(_eventBus.Subscribe<Events.PickupCollectedEvent>(OnPickupCollected));

		// Quest events
		_subscriptions.Add(_eventBus.Subscribe<QuestStartedEvent>(OnQuestStarted));
		_subscriptions.Add(_eventBus.Subscribe<QuestCompletedEvent>(OnQuestCompleted));
		_subscriptions.Add(_eventBus.Subscribe<QuestObjectiveUpdatedEvent>(OnQuestObjectiveUpdated));

		// Player events
		_subscriptions.Add(_eventBus.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp));
		_subscriptions.Add(_eventBus.Subscribe<PlayerAttackEvent>(OnPlayerAttack));

		// Room change event
		_subscriptions.Add(_eventBus.Subscribe<RoomChangedEvent>(OnRoomChange));


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


	private void OnPickupCollected(Events.PickupCollectedEvent e) {
		if (e.Collector is Player player) {
			player.CollectPickup(e.Pickup);
		}
		Pickup pickup = e.Pickup.Get<Pickup>();
		string sound = pickup.Type switch {
			PickupType.Health => "use_potion",
			_ => "buy_item"
		};
		_soundPlayer.Play(sound, 0.8f);
		_questManager.UpdateObjectiveProgress("collect_item", pickup.Type.ToString(), 1);
		System.Diagnostics.Debug.WriteLine($"[LOOT] Collected {pickup.Type}");
	}

	private void OnQuestStarted(QuestStartedEvent e) {
		System.Diagnostics.Debug.WriteLine($"[QUEST STARTED] {e.QuestName}");
		_soundPlayer.Play("menu_accept", 0.9f);
		_notificationSystem.ShowQuestStarted(e.QuestName);
	}

	private void OnQuestCompleted(QuestCompletedEvent e) {
		System.Diagnostics.Debug.WriteLine($"[QUEST COMPLETED] {e.QuestName}");
		_soundPlayer.Play("level_up", 0.9f);
		_notificationSystem.ShowQuestCompleted(e.QuestName, e.LastNode.Rewards.Xp, e.LastNode.Rewards.Gold);
	}

	private void OnQuestObjectiveUpdated(QuestObjectiveUpdatedEvent e) {
		System.Diagnostics.Debug.WriteLine("[QUEST PROGRESS] Objective updated");
		// Notification shown by NotificationSystem
	}

	private void OnPlayerLevelUp(PlayerLevelUpEvent e) {
		_vfxSystem.ShowLevelUp(_player.Position);
		_soundPlayer.Play("level_up", 1.0f);
		System.Diagnostics.Debug.WriteLine($"[LEVEL UP] Player reached level {e.NewLevel}");
	}

	private void OnPlayerAttack(PlayerAttackEvent e) {
		e.Player.TriggerAttackEffect();
		_soundPlayer.Play("sword_woosh");
	}

	public void OnRoomChange(RoomChangedEvent e) {
		_movementSystem.SetCurrentMap(e.NewRoom.Map);
	}

	public void Dispose() {
		_subscriptions?.Dispose();
		System.Diagnostics.Debug.WriteLine("[EVENT COORDINATOR] Disposed");
	}
}