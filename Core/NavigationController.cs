using Microsoft.Xna.Framework;
using System;

namespace EldmeresTale.Core;

public enum NavigationMode {
	None, Index, Spatial
}

public class NavigationController {

	public NavigationMode Mode { get; set; }

	public int SelectedIndex { get; private set; }

	public int ItemCount { get; set; }

	public bool WrapAround { get; set; } = true;

	public Point SelectedGridPosition { get; private set; }

	public Point GridSize { get; set; }

	public event Action<int> OnSelectionChanged;

	public event Action<Point> OnGridSelectionChanged;

	public NavigationController() {
		Mode = NavigationMode.Index;
		SelectedIndex = 0;
		ItemCount = 0;
		GridSize = new Point(1, 1);
		SelectedGridPosition = Point.Zero;
	}

	public void Update(Systems.InputCommands input) {
		if (Mode == NavigationMode.Index) {
			UpdateIndexMode(input);
		} else {
			UpdateSpatialMode(input);
		}
	}

	private void UpdateIndexMode(Systems.InputCommands input) {
		if (ItemCount == 0) {
			return;
		}

		int previousIndex = SelectedIndex;

		// Navigate down
		if (input.MoveDownPressed) {
			SelectedIndex++;
		}
		// Navigate up
		else if (input.MoveUpPressed) {
			SelectedIndex--;
		}

		// Apply wrapping or clamping
		if (WrapAround) {
			if (SelectedIndex < 0) {
				SelectedIndex = ItemCount - 1;
			} else if (SelectedIndex >= ItemCount) {
				SelectedIndex = 0;
			}
		} else {
			SelectedIndex = Math.Clamp(SelectedIndex, 0, ItemCount - 1);
		}

		// Fire event if changed
		if (SelectedIndex != previousIndex) {
			OnSelectionChanged?.Invoke(SelectedIndex);
		}
	}

	private void UpdateSpatialMode(Systems.InputCommands input) {
		if (GridSize.X == 0 || GridSize.Y == 0) {
			return;
		}

		Point previousPosition = SelectedGridPosition;

		// Navigate horizontally
		if (input.MoveLeftPressed) {
			SelectedGridPosition += new Point(-1, 0);
		} else if (input.MoveRightPressed) {
			SelectedGridPosition += new Point(1, 0);
		}

		// Navigate vertically
		if (input.MoveUpPressed) {
			SelectedGridPosition += new Point(0, -1);
		} else if (input.MoveDownPressed) {
			SelectedGridPosition += new Point(0, 1);
		}

		// Apply wrapping or clamping
		if (WrapAround) {
			if (SelectedGridPosition.X < 0) {
				SelectedGridPosition = new Point(GridSize.X - 1, SelectedGridPosition.Y);
			} else if (SelectedGridPosition.X >= GridSize.X) {
				SelectedGridPosition = new Point(0, SelectedGridPosition.Y);
			}

			if (SelectedGridPosition.Y < 0) {
				SelectedGridPosition = new Point(SelectedGridPosition.X, GridSize.Y - 1);
			} else if (SelectedGridPosition.Y >= GridSize.Y) {
				SelectedGridPosition = new Point(SelectedGridPosition.X, 0);
			}
		} else {
			SelectedGridPosition = new Point(
				Math.Clamp(SelectedGridPosition.X, 0, GridSize.X - 1),
				Math.Clamp(SelectedGridPosition.Y, 0, GridSize.Y - 1)
			);
		}

		// Fire event if changed
		if (SelectedGridPosition != previousPosition) {
			OnGridSelectionChanged?.Invoke(SelectedGridPosition);
		}
	}

	// SELECTION QUERIES
	public bool IsSelected(int index) {
		return Mode == NavigationMode.Index && SelectedIndex == index;
	}

	public bool IsSelected(int gridX, int gridY) {
		return Mode == NavigationMode.Spatial &&
			   SelectedGridPosition.X == gridX &&
			   SelectedGridPosition.Y == gridY;
	}

	public bool IsSelected(Point gridPosition) {
		return Mode == NavigationMode.Spatial && SelectedGridPosition == gridPosition;
	}

	// SELECTION CONTROL
	public void SetSelectedIndex(int index) {
		if (Mode != NavigationMode.Index) {
			return;
		}

		int previousIndex = SelectedIndex;
		SelectedIndex = Math.Clamp(index, 0, ItemCount - 1);

		if (SelectedIndex != previousIndex) {
			OnSelectionChanged?.Invoke(SelectedIndex);
		}
	}

	public void SetSelectedGridPosition(Point position) {
		if (Mode != NavigationMode.Spatial) {
			return;
		}

		Point previousPosition = SelectedGridPosition;

		SelectedGridPosition = new Point(
			Math.Clamp(position.X, 0, GridSize.X - 1),
			Math.Clamp(position.Y, 0, GridSize.Y - 1)
		);

		if (SelectedGridPosition != previousPosition) {
			OnGridSelectionChanged?.Invoke(SelectedGridPosition);
		}
	}

	public void Reset() {
		SelectedIndex = 0;
		SelectedGridPosition = Point.Zero;
	}

	public int GridPositionToIndex(Point position) {
		return (position.Y * GridSize.X) + position.X;
	}

	public int GetCurrentIndexFromGrid() {
		return GridPositionToIndex(SelectedGridPosition);
	}

	public Point IndexToGridPosition(int index) {
		return new Point(
			index % GridSize.X,
			index / GridSize.X
		);
	}
}