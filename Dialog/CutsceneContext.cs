using EldmeresTale.Core;
using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using System;

namespace EldmeresTale.Dialog;

public class CutsceneContext {

	private readonly ApplicationContext _appContext;
	private readonly GameServices _gameServices;
	private readonly Camera _camera;

	// Fade state
	public bool IsFading { get; set; }
	public string FadeDirection { get; set; }
	public float FadeDuration { get; set; }
	public float FadeAlpha { get; set; }

	public CutsceneContext(ApplicationContext appContext, GameServices gameServices, Camera camera) {
		_appContext = appContext;
		_gameServices = gameServices;
		_camera = camera;
	}

	// NPC access
	public NPC GetNPC(string npcId) {
		// TODO: Get NPC from game world
		// For now, return null - you'll need to hook this up to your NPC system
		System.Diagnostics.Debug.WriteLine($"[CUTSCENE] GetNPC: {npcId}");
		return null;
	}

	// Camera control
	public Vector2 GetCameraPosition() {
		return _camera.Position;
	}

	public void SetCameraPosition(Vector2 position) {
		_camera.Position = position;
	}

	// Audio
	public void PlaySound(string soundId, float volume) {
		_appContext.SoundEffects.Play(soundId, volume);
		System.Diagnostics.Debug.WriteLine($"[CUTSCENE] PlaySound: {soundId} at {volume}");
	}

	public void ChangeMusic(string musicId) {
		// TODO: Load and play music by ID
		System.Diagnostics.Debug.WriteLine($"[CUTSCENE] ChangeMusic: {musicId}");
	}

	// Inventory
	public void GiveItem(string itemId, int quantity) {
		// TODO: Add item to player inventory
		System.Diagnostics.Debug.WriteLine($"[CUTSCENE] GiveItem: {itemId} x{quantity}");
	}

	// Quests
	public void StartQuest(string questId) {
		_gameServices.QuestManager.StartQuest(questId);
		System.Diagnostics.Debug.WriteLine($"[CUTSCENE] StartQuest: {questId}");
	}

	// Flags
	public void SetFlag(string flagId, string value) {
		bool boolValue = value.Equals("true", StringComparison.CurrentCultureIgnoreCase) || value == "1";
		_gameServices.GameState.SetFlag(flagId, boolValue);
		System.Diagnostics.Debug.WriteLine($"[CUTSCENE] SetFlag: {flagId} = {value}");
	}

	public string GetFlag(string flagId) {
		return _gameServices.GameState.GetFlag(flagId).ToString().ToLower();
	}
}