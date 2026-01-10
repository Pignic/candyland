using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace EldmeresTale.Core.UI;

public abstract class UIElement {

	public static void SetGraphicContext(GraphicsDevice graphicsDevice) {
		FONT = new BitmapFont(graphicsDevice);
		DEFAULT_TEXTURE = new Texture2D(graphicsDevice, 1, 1);
		DEFAULT_TEXTURE.SetData([Color.White]);
	}

	// === HIERARCHY ===
	public UIElement Parent { get; private set; }
	public List<UIElement> Children { get; } = [];
	public bool IsNavigable { get; set; } = false;
	public Color BackgroundColor { get; set; } = Color.Transparent;
	public Color BorderColor { get; set; } = Color.Gold;
	public int BorderWidth { get; set; } = 0;

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
	public Point GlobalPosition {
		get {
			if (Parent == null) {
				return new Point(X, Y);
			}

			Point parentPos = Parent.GlobalPosition;
			return new Point(parentPos.X + X, parentPos.Y + Y);
		}
	}

	public Rectangle GlobalBounds {
		get {
			Point pos = GlobalPosition;
			return new Rectangle(pos.X, pos.Y, Width, Height);
		}
	}

	public Rectangle LocalBounds => new Rectangle(0, 0, Width, Height);

	public Rectangle ContentBounds => new Rectangle(
		PaddingLeft + BorderWidth,
		PaddingTop + BorderWidth,
		Width - PaddingLeft - PaddingRight - (BorderWidth * 2),
		Height - PaddingTop - PaddingBottom - (BorderWidth * 2)
	);

	public Rectangle GlobalContentBounds => new Rectangle(
		GlobalPosition.X + PaddingLeft + BorderWidth,
		GlobalPosition.Y + PaddingTop + BorderWidth,
		Width - PaddingLeft - PaddingRight - (BorderWidth * 2),
		Height - PaddingTop - PaddingBottom - (BorderWidth * 2)
	);

	private static BitmapFont FONT;
	private static Texture2D DEFAULT_TEXTURE;
	protected BitmapFont Font => FONT;
	protected Texture2D DefaultTexture => DEFAULT_TEXTURE;

	public void SetPadding(int padding) {
		SetPadding(padding, padding);
	}

	public void SetPadding(int paddingX, int paddingY) {
		SetPadding(paddingX, paddingX, paddingY, paddingY);
	}

	public void SetPadding(int paddingL, int paddingR, int paddingT, int paddingB) {
		PaddingLeft = paddingL;
		PaddingRight = paddingR;
		PaddingTop = paddingT;
		PaddingBottom = paddingB;
	}

	public void AddChild(UIElement child) {
		child.Parent?.RemoveChild(child);

		Children.Add(child);
		child.Parent = this;
		OnChildAdded(child);
	}

	public void RemoveChild(UIElement child) {
		if (Children.Remove(child)) {
			child.Parent = null;
			OnChildRemoved(child);
		}
	}

	public void ClearChildren() {
		foreach (UIElement child in Children) {
			child.Parent = null;
		}

		Children.Clear();
	}

	// === UPDATE / DRAW ===
	public virtual void Update(GameTime gameTime) {
		if (!Visible || !Enabled) {
			return;
		}

		// Update this element
		OnUpdate(gameTime);

		// Update children
		foreach (UIElement child in Children) {
			child.Update(gameTime);
		}
	}

	public virtual void Draw(SpriteBatch spriteBatch) {
		if (!Visible) {
			return;
		}
		// Draw the border
		DrawBorder(spriteBatch, GlobalBounds, BorderColor, BorderWidth);

		// Draw the background
		spriteBatch.Draw(DefaultTexture, GlobalContentBounds, BackgroundColor);

		// Draw this element
		OnDraw(spriteBatch);

		// Draw children
		foreach (UIElement child in Children) {
			child.Draw(spriteBatch);
		}
	}

	// === INPUT HANDLING ===
	public virtual bool HandleMouse(MouseState mouse, MouseState previousMouse) {
		if (!Visible || !Enabled) {
			return false;
		}

		bool anyChildHandled = false;

		// Check ALL children for hover state (don't stop on first true)
		for (int i = Children.Count - 1; i >= 0; i--) {
			if (Children[i].HandleMouse(mouse, previousMouse)) {
				anyChildHandled = true;
			}
		}

		// Check if mouse is over this element
		Point mousePos = mouse.Position;
		if (GlobalBounds.Contains(mousePos)) {
			bool handled = OnMouseInput(mouse, previousMouse);
			return handled || anyChildHandled;
		} else {
			// Mouse is NOT over this element
			// Still need to call OnMouseInput so elements can clear hover state
			OnMouseInput(mouse, previousMouse);
			return anyChildHandled;
		}
	}

	public bool ContainsPoint(Point point) => GlobalBounds.Contains(point);

	// === VIRTUAL METHODS (override in derived classes) ===

	protected virtual void OnUpdate(GameTime gameTime) { }
	protected virtual void OnDraw(SpriteBatch spriteBatch) { }
	protected virtual bool OnMouseInput(MouseState mouse, MouseState previousMouse) => false;
	protected virtual void OnChildAdded(UIElement child) { }
	protected virtual void OnChildRemoved(UIElement child) { }

	// === HELPER METHODS ===

	public void SetSize(int width, int height) {
		Width = width;
		Height = height;
	}

	public void SetPosition(int x, int y) {
		X = x;
		Y = y;
	}

	private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color, int width) {
		if (width <= 0) {
			return;
		}
		// Top
		spriteBatch.Draw(DefaultTexture,
			new Rectangle(bounds.X, bounds.Y, bounds.Width, width), color);
		// Bottom
		spriteBatch.Draw(DefaultTexture,
			new Rectangle(bounds.X, bounds.Bottom - width, bounds.Width, width), color);
		// Left
		spriteBatch.Draw(DefaultTexture,
			new Rectangle(bounds.X, bounds.Y, width, bounds.Height), color);
		// Right
		spriteBatch.Draw(DefaultTexture,
			new Rectangle(bounds.Right - width, bounds.Y, width, bounds.Height), color);
	}
}