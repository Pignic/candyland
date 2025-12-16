using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Candyland.Core.UI {
	/// <summary>
	/// Base class for all UI elements. Supports hierarchy, layout, and input handling.
	/// </summary>
	public abstract class UIElement {
		// === HIERARCHY ===
		public UIElement Parent { get; private set; }
		public List<UIElement> Children { get; private set; } = new List<UIElement>();


		public Color BackgroundColor { get; set; } = Color.Transparent;
		public Color BorderColor { get; set; } = Color.Gold;
		public int BorderWidth { get; set; } = 2;

		// === LAYOUT ===
		// Position relative to parent (or screen if no parent)
		public int X { get; set; }
		public int Y { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }

		// Padding inside the element (affects child layout)
		public int PaddingLeft { get; set; }
		public int PaddingRight { get; set; }
		public int PaddingTop { get; set; }
		public int PaddingBottom { get; set; }

		// === STATE ===
		public bool Visible { get; set; } = true;
		public bool Enabled { get; set; } = true;

		// === COMPUTED PROPERTIES ===

		/// <summary>
		/// Absolute position on screen (calculated from parent chain)
		/// </summary>
		public Point GlobalPosition {
			get {
				if(Parent == null)
					return new Point(X, Y);

				var parentPos = Parent.GlobalPosition;
				return new Point(parentPos.X + X, parentPos.Y + Y);
			}
		}

		/// <summary>
		/// Global bounds (absolute screen coordinates)
		/// </summary>
		public Rectangle GlobalBounds {
			get {
				var pos = GlobalPosition;
				return new Rectangle(pos.X, pos.Y, Width, Height);
			}
		}

		/// <summary>
		/// Local bounds (relative to this element)
		/// </summary>
		public Rectangle LocalBounds => new Rectangle(0, 0, Width, Height);

		/// <summary>
		/// Content area (inside padding)
		/// </summary>
		public Rectangle ContentBounds => new Rectangle(
			PaddingLeft,
			PaddingTop,
			Width - PaddingLeft - PaddingRight,
			Height - PaddingTop - PaddingBottom
		);

		// === HIERARCHY MANAGEMENT ===

		public void AddChild(UIElement child) {
			if(child.Parent != null)
				child.Parent.RemoveChild(child);

			Children.Add(child);
			child.Parent = this;
			OnChildAdded(child);
		}

		public void RemoveChild(UIElement child) {
			if(Children.Remove(child)) {
				child.Parent = null;
				OnChildRemoved(child);
			}
		}

		public void ClearChildren() {
			foreach(var child in Children)
				child.Parent = null;

			Children.Clear();
		}

		// === UPDATE / DRAW ===

		/// <summary>
		/// Update this element and all children
		/// </summary>
		public virtual void Update(GameTime gameTime) {
			if(!Visible || !Enabled) return;

			// Update this element
			OnUpdate(gameTime);

			// Update children
			foreach(var child in Children)
				child.Update(gameTime);
		}

		/// <summary>
		/// Draw this element and all children
		/// </summary>
		public virtual void Draw(SpriteBatch spriteBatch) {
			if(!Visible) return;

			// Draw this element
			OnDraw(spriteBatch);

			// Draw children
			foreach(var child in Children)
				child.Draw(spriteBatch);
		}

		// === INPUT HANDLING ===

		/// <summary>
		/// Handle mouse input. Returns true if input was handled.
		/// </summary>
		public virtual bool HandleMouse(MouseState mouse, MouseState previousMouse) {
			if(!Visible || !Enabled) return false;

			bool anyChildHandled = false;

			// Check ALL children for hover state (don't stop on first true)
			for(int i = Children.Count - 1; i >= 0; i--) {
				if(Children[i].HandleMouse(mouse, previousMouse))
					anyChildHandled = true;
			}

			// Check if mouse is over this element
			Point mousePos = mouse.Position;
			if(GlobalBounds.Contains(mousePos)) {
				bool handled = OnMouseInput(mouse, previousMouse);
				return handled || anyChildHandled;
			} else {
				// Mouse is NOT over this element
				// Still need to call OnMouseInput so elements can clear hover state
				OnMouseInput(mouse, previousMouse);
				return anyChildHandled;
			}
		}

		/// <summary>
		/// Check if point is inside this element (global coordinates)
		/// </summary>
		public bool ContainsPoint(Point point) => GlobalBounds.Contains(point);

		// === VIRTUAL METHODS (override in derived classes) ===

		protected virtual void OnUpdate(GameTime gameTime) { }
		protected virtual void OnDraw(SpriteBatch spriteBatch) { }
		protected virtual bool OnMouseInput(MouseState mouse, MouseState previousMouse) => false;
		protected virtual void OnChildAdded(UIElement child) { }
		protected virtual void OnChildRemoved(UIElement child) { }

		// === HELPER METHODS ===

		/// <summary>
		/// Set padding for all sides
		/// </summary>
		public void SetPadding(int padding) {
			PaddingLeft = PaddingRight = PaddingTop = PaddingBottom = padding;
		}

		/// <summary>
		/// Set size
		/// </summary>
		public void SetSize(int width, int height) {
			Width = width;
			Height = height;
		}

		/// <summary>
		/// Set position
		/// </summary>
		public void SetPosition(int x, int y) {
			X = x;
			Y = y;
		}
	}
}