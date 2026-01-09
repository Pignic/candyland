using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EldmeresTale.Core.UI;

public class UIPanel : UIElement {

	// === SCROLLING ===
	public bool EnableScrolling { get; set; } = false;
	public float ScrollOffset { get; private set; } = 0f;
	public float MaxScrollOffset { get; private set; } = 0f;

	// Scrollbar
	private const int SCROLLBAR_WIDTH = 10;
	private bool _isScrollbarDragging = false;
	private int _scrollbarDragStartY = 0;
	private float _scrollbarDragStartOffset = 0f;

	// === LAYOUT ===
	public enum LayoutMode {
		None,           // Manual positioning
		Vertical,       // Stack children vertically
		Horizontal,     // Stack children horizontally
		Grid            // Grid layout
	}

	public enum AllignMode {
		Left,
		Right,
		Center
	}

	public LayoutMode Layout { get; set; } = LayoutMode.None;
	public AllignMode Allign { get; set; } = AllignMode.Left;
	public int Spacing { get; set; } = 5; // Space between children

	public UIPanel() : base() {

	}

	protected override void OnUpdate(GameTime gameTime) {
		// Update scroll limits based on content height
		if (EnableScrolling) {
			float contentHeight = CalculateContentHeight();
			MaxScrollOffset = Math.Max(0, contentHeight - ContentBounds.Height);
		}
	}

	protected override void OnDraw(SpriteBatch spriteBatch) {
		Point globalPos = GlobalPosition;
		spriteBatch.Draw(_defaultTexture, GlobalBounds, BackgroundColor);

		// If scrolling, set up scissor test for clipping
		if (EnableScrolling) {
			spriteBatch.End();

			RasterizerState rasterizerState = new RasterizerState { ScissorTestEnable = true };
			spriteBatch.Begin(
				samplerState: SamplerState.PointClamp,
				rasterizerState: rasterizerState
			);

			// Set scissor rectangle to content area
			Rectangle contentGlobal = new Rectangle(
				globalPos.X + ContentBounds.X,
				globalPos.Y + ContentBounds.Y,
				ContentBounds.Width,
				ContentBounds.Height
			);
			spriteBatch.GraphicsDevice.ScissorRectangle = contentGlobal;

			// Draw children with scroll offset
			DrawChildrenWithScroll(spriteBatch);

			// End scissor test
			spriteBatch.End();
			spriteBatch.Begin(samplerState: SamplerState.PointClamp);

			// Draw scrollbar
			DrawScrollbar(spriteBatch);
		}
		// No scrolling - children draw normally in base Draw()
	}

	protected override bool OnMouseInput(MouseState mouse, MouseState previousMouse) {
		if (!EnableScrolling) {
			return false;
		}

		Point mousePos = mouse.Position;
		Rectangle scrollbarBounds = GetScrollbarBounds();

		// Handle scrollbar dragging
		if (_isScrollbarDragging) {
			if (mouse.LeftButton == ButtonState.Released) {
				_isScrollbarDragging = false;
			} else {
				int deltaY = mousePos.Y - _scrollbarDragStartY;
				float scrollbarHeight = scrollbarBounds.Height;
				float thumbHeight = GetScrollbarThumbHeight();
				float scrollableHeight = scrollbarHeight - thumbHeight;

				if (scrollableHeight > 0) {
					float offsetDelta = deltaY / scrollableHeight * MaxScrollOffset;
					ScrollOffset = MathHelper.Clamp(
						_scrollbarDragStartOffset + offsetDelta,
						0,
						MaxScrollOffset
					);
				}
			}
			return true;
		}

		// Start scrollbar drag
		if (scrollbarBounds.Contains(mousePos) &&
			mouse.LeftButton == ButtonState.Pressed &&
			previousMouse.LeftButton == ButtonState.Released) {
			_isScrollbarDragging = true;
			_scrollbarDragStartY = mousePos.Y;
			_scrollbarDragStartOffset = ScrollOffset;
			return true;
		}

		// Mouse wheel scrolling
		int scrollDelta = mouse.ScrollWheelValue - previousMouse.ScrollWheelValue;
		if (scrollDelta != 0 && GlobalBounds.Contains(mousePos)) {
			ScrollOffset = MathHelper.Clamp(
				ScrollOffset - (scrollDelta / 10f),
				0,
				MaxScrollOffset
			);
			return true;
		}

		return false;
	}

	public override void Draw(SpriteBatch spriteBatch) {
		if (!Visible) {
			return;
		}

		OnDraw(spriteBatch);

		// Only draw children if NOT scrolling (scrolling draws them specially)
		if (!EnableScrolling) {
			foreach (UIElement child in Children) {
				child.Draw(spriteBatch);
			}
		}
	}

	protected override void OnChildAdded(UIElement child) {
		UpdateLayout();
	}

	protected override void OnChildRemoved(UIElement child) {
		UpdateLayout();
	}

	// === LAYOUT MANAGEMENT ===

	public void UpdateLayout() {
		if (Layout == LayoutMode.None) {
			return;
		}

		int currentX = ContentBounds.X;
		int currentY = ContentBounds.Y;
		int nextX = currentX;

		foreach (UIElement child in Children) {
			if (!child.Visible) {
				continue;
			}

			switch (Layout) {
				case LayoutMode.Vertical:
					if (Allign == AllignMode.Left) {
						child.X = currentX;
					} else if (Allign == AllignMode.Center) {
						child.X = currentX + (ContentBounds.Width / 2) - (child.Width / 2);
					} else {
						child.X = currentX + ContentBounds.Width - child.Width;
					}
					child.Y = currentY;
					currentY += child.Height + Spacing;
					if (child == Children[Children.Count - 1] && child.Height < 0) {
						child.Height = ContentBounds.Y - currentY;
					}
					break;

				case LayoutMode.Horizontal:
					child.X = currentX;
					child.Y = currentY;
					currentX += child.Width + Spacing;
					if (child == Children[Children.Count - 1] && child.Width < 0) {
						child.Width = ContentBounds.X - currentX;
					}
					break;

				case LayoutMode.Grid:
					if (nextX + child.Width > ContentBounds.Width) {
						currentX = ContentBounds.X;
						currentY += child.Height + Spacing;
					} else {
						currentX += nextX;
					}
					child.X = currentX;
					child.Y = currentY;
					nextX = currentX + child.Width + Spacing;
					break;
			}
		}
	}

	// === SCROLLING HELPERS ===

	private void DrawChildrenWithScroll(SpriteBatch spriteBatch) {
		// Temporarily offset children for scrolling
		foreach (UIElement child in Children) {
			int originalY = child.Y;
			child.Y -= (int)ScrollOffset;
			child.Draw(spriteBatch);
			child.Y = originalY; // Restore position
		}
	}

	private float CalculateContentHeight() {
		if (Children.Count == 0) {
			return 0;
		}

		float maxBottom = 0;
		foreach (UIElement child in Children) {
			if (child.Visible) {
				maxBottom = Math.Max(maxBottom, child.Y + child.Height);
			}
		}

		return maxBottom + PaddingBottom;
	}

	private void DrawScrollbar(SpriteBatch spriteBatch) {
		if (MaxScrollOffset <= 0) {
			return;
		}

		Rectangle scrollbarBounds = GetScrollbarBounds();

		// Track
		spriteBatch.Draw(_defaultTexture, scrollbarBounds, Color.DarkGray * 0.5f);

		// Thumb
		float thumbHeight = GetScrollbarThumbHeight();
		float scrollRatio = MaxScrollOffset > 0 ? ScrollOffset / MaxScrollOffset : 0;
		int thumbY = scrollbarBounds.Y + (int)((scrollbarBounds.Height - thumbHeight) * scrollRatio);

		Rectangle thumbBounds = new Rectangle(
			scrollbarBounds.X,
			thumbY,
			scrollbarBounds.Width,
			(int)thumbHeight
		);

		spriteBatch.Draw(_defaultTexture, thumbBounds, Color.Gray);
	}

	private Rectangle GetScrollbarBounds() {
		Point globalPos = GlobalPosition;
		return new Rectangle(
			globalPos.X + Width - SCROLLBAR_WIDTH - PaddingRight,
			globalPos.Y + PaddingTop,
			SCROLLBAR_WIDTH,
			Height - PaddingTop - PaddingBottom
		);
	}

	private float GetScrollbarThumbHeight() {
		Rectangle scrollbarBounds = GetScrollbarBounds();
		float contentHeight = CalculateContentHeight();
		float viewportRatio = ContentBounds.Height / contentHeight;
		return Math.Max(20, scrollbarBounds.Height * viewportRatio);
	}

	public override bool HandleMouse(MouseState mouse, MouseState previousMouse) {
		if (!Visible || !Enabled) {
			return false;
		}

		// Handle scrollbar and mouse wheel first
		bool scrollHandled = OnMouseInput(mouse, previousMouse);

		if (EnableScrolling) {
			bool anyChildHandled = false;

			bool wasClick = mouse.LeftButton == ButtonState.Pressed &&
						   previousMouse.LeftButton == ButtonState.Released;

			// Apply scroll offset to children for mouse input (same as drawing)
			List<UIElement> childrenCopy = [.. Children];
			foreach (UIElement child in childrenCopy) {
				int originalY = child.Y;
				child.Y -= (int)ScrollOffset;

				bool handled = child.HandleMouse(mouse, previousMouse);

				child.Y = originalY;

				if (handled) {
					anyChildHandled = true;

					if (wasClick) {
						break;
					}
				}
			}

			return scrollHandled || anyChildHandled;
		} else {
			// No scrolling - use default behavior
			return base.HandleMouse(mouse, previousMouse);
		}
	}
	public int GetNavigableChildCount() {
		return Children.Count(c => c.IsNavigable);
	}

	public UIElement GetNavigableChild(int index) {
		return Children.Where(c => c.IsNavigable).ElementAtOrDefault(index);
	}
}