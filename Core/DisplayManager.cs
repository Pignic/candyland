using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Candyland.Dialog;

public sealed class DisplayManager {

	public readonly int VirtualWidth;
	public readonly int VirtualHeight;

	public int Scale { get; private set; }
	public Rectangle Letterbox { get; private set; }
	public Viewport Viewport { get; private set; }

	public event Action DisplayChanged;

	public DisplayManager(int virtualWidth, int virtualHeight) {
		VirtualWidth = virtualWidth;
		VirtualHeight = virtualHeight;
	}

	public void Update(GraphicsDevice device) {
		var vp = device.Viewport;

		int scaleX = vp.Width / VirtualWidth;
		int scaleY = vp.Height / VirtualHeight;
		int newScale = Math.Max(1, Math.Min(scaleX, scaleY));

		if(newScale == Scale && vp.Equals(Viewport)){
			return;
		}

		Scale = newScale;
		Viewport = vp;

		int width = VirtualWidth * Scale;
		int height = VirtualHeight * Scale;

		int x = (vp.Width - width) / 2;
		int y = (vp.Height - height) / 2;

		Letterbox = new Rectangle(x, y, width, height);

		DisplayChanged?.Invoke();
	}

	public MouseState ScaleMouseState(MouseState original) {
		Point scaledPosition = new Point(
			original.Position.X / Scale,
			original.Position.Y / Scale
		);

		return new MouseState(
			scaledPosition.X,
			scaledPosition.Y,
			original.ScrollWheelValue,
			original.LeftButton,
			original.MiddleButton,
			original.RightButton,
			original.XButton1,
			original.XButton2
		);
	}
}