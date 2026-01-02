using EldmeresTale.Core;
using EldmeresTale.Entities;
using Microsoft.Xna.Framework;
using System;

namespace EldmeresTale.Dialog;

/// <summary>
/// Provides access to game state for cutscene commands
/// Acts as a bridge between cutscene system and game world
/// </summary>
public class CutsceneContext {
	private ApplicationContext _appContext;
	private Camera _camera;

	// Fade state
	public bool isFading { get; set; }
	public string fadeDirection { get; set; }
	public float fadeDuration { get; set; }
	public float fadeAlpha { get; set; }

	public CutsceneContext(ApplicationContext appContext, Camera camera) {
		_appContext = appContext;
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
		_appContext.gameState.QuestManager.startQuest(questId);
		System.Diagnostics.Debug.WriteLine($"[CUTSCENE] StartQuest: {questId}");
	}

	// Flags
	public void SetFlag(string flagId, string value) {
		bool boolValue = value.Equals("true", StringComparison.CurrentCultureIgnoreCase) || value == "1";
		_appContext.gameState.GameState.setFlag(flagId, boolValue);
		System.Diagnostics.Debug.WriteLine($"[CUTSCENE] SetFlag: {flagId} = {value}");
	}

	public string GetFlag(string flagId) {
		return _appContext.gameState.GameState.getFlag(flagId).ToString().ToLower();
	}
}