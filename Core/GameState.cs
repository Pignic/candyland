using Candyland.Entities;
using Candyland.Quests;
using System;

namespace Candyland.Core;

public sealed class GameState : IDisposable {
	public Player Player { get; set; }

	// Quests
	public QuestManager _questManager { get; set; }

	public GameState() { }

	public void Dispose() {

	}
}