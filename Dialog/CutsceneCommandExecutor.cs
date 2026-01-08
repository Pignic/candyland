using Microsoft.Xna.Framework;
using System;

namespace EldmeresTale.Dialog;

public class CutsceneCommandExecutor {

	public CutsceneContext Context { get; }

	private CutsceneCommand _currentCommand;
	private bool _isExecuting = false;

	public event Action<string> OnCommandComplete; // Fired when command completes with nextNodeId

	public CutsceneCommandExecutor(CutsceneContext context) {
		Context = context;
	}

	public void ExecuteCommand(CutsceneCommand command) {
		_currentCommand = command;
		_isExecuting = true;

		System.Diagnostics.Debug.WriteLine($"[CUTSCENE] Executing command: {command.GetType().Name}");
		command.Execute(Context);

		// If command is instant and doesn't need to wait, complete immediately
		if (!command.Wait && command.IsComplete()) {
			CompleteCommand();
		}
	}

	public void Update(GameTime gameTime) {
		if (!_isExecuting || _currentCommand == null) {
			return;
		}

		bool complete = _currentCommand.Update(gameTime, Context);

		if (complete && _currentCommand.Wait) {
			CompleteCommand();
		} else if (complete && !_currentCommand.Wait) {
			// Command finished but we're not waiting - already completed
		}
	}

	private void CompleteCommand() {
		System.Diagnostics.Debug.WriteLine($"[CUTSCENE] Command complete: {_currentCommand.GetType().Name}");
		string nextNode = _currentCommand.NextNodeId;
		_currentCommand = null;
		_isExecuting = false;

		// Notify that command is complete and provide next node
		OnCommandComplete?.Invoke(nextNode);
	}

	public bool IsExecuting => _isExecuting;
}