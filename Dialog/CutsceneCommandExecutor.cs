using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Dialog;

/// <summary>
/// Executes and manages cutscene commands
/// </summary>
public class CutsceneCommandExecutor {
	private CutsceneContext _context;
	private CutsceneCommand _currentCommand;
	private bool _isExecuting = false;

	public event Action<string> OnCommandComplete; // Fired when command completes with nextNodeId

	public CutsceneCommandExecutor(CutsceneContext context) {
		_context = context;
	}

	/// <summary>
	/// Start executing a command
	/// </summary>
	public void ExecuteCommand(CutsceneCommand command) {
		_currentCommand = command;
		_isExecuting = true;

		System.Diagnostics.Debug.WriteLine($"[CUTSCENE] Executing command: {command.GetType().Name}");
		command.Execute(_context);

		// If command is instant and doesn't need to wait, complete immediately
		if(!command.wait && command.IsComplete()) {
			CompleteCommand();
		}
	}

	/// <summary>
	/// Update the current command
	/// </summary>
	public void Update(GameTime gameTime) {
		if(!_isExecuting || _currentCommand == null) return;

		bool complete = _currentCommand.Update(gameTime, _context);

		if(complete && _currentCommand.wait) {
			CompleteCommand();
		} else if(complete && !_currentCommand.wait) {
			// Command finished but we're not waiting - already completed
		}
	}

	private void CompleteCommand() {
		System.Diagnostics.Debug.WriteLine($"[CUTSCENE] Command complete: {_currentCommand.GetType().Name}");
		string nextNode = _currentCommand.nextNodeId;
		_currentCommand = null;
		_isExecuting = false;

		// Notify that command is complete and provide next node
		OnCommandComplete?.Invoke(nextNode);
	}

	public bool IsExecuting => _isExecuting;

	public CutsceneContext Context => _context;
}