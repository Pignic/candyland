using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Candyland.Core {
	public class Animation {
		public Texture2D Texture { get; private set; }
		public int FrameCount { get; private set; }
		public int FrameWidth { get; private set; }
		public int FrameHeight { get; private set; }
		public float FrameTime { get; private set; } // Time per frame in seconds

		private int _currentFrame;
		private float _timer;
		private int _row; // Which row in the sprite sheet
		private bool _pingpong;
		private bool _animateForward = true;

		public Animation(Texture2D texture, int frameCount, int frameWidth, int frameHeight, float frameTime, int row = 0, bool pingpong = false) {
			Texture = texture;
			FrameCount = frameCount;
			FrameWidth = frameWidth;
			FrameHeight = frameHeight;
			FrameTime = frameTime;
			_row = row;
			_currentFrame = 0;
			_timer = 0;
			_pingpong = pingpong;

		}

		public void Update(GameTime gameTime) {
			_timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

			if(_timer >= FrameTime) {
				_timer -= FrameTime;
				if(_pingpong) {
					var step = _animateForward ? 1 : -1;
					_currentFrame += step;
					if(_currentFrame >= FrameCount) {
						_currentFrame = FrameCount - 2;
						_animateForward = false;
					} else if(_currentFrame < 0) {
						_currentFrame = 1;
						_animateForward = true;
					}
				} else {
					_currentFrame = (_currentFrame + (_animateForward ? 1 : -1) + FrameCount) % FrameCount;
				}
			}
		}

		public void Reset() {
			_currentFrame = 0;
			_timer = 0;
		}

		public Rectangle GetSourceRectangle() {
			return new Rectangle(
				_currentFrame * FrameWidth,
				_row * FrameHeight,
				FrameWidth,
				FrameHeight
			);
		}
	}

	public enum Direction {
		Down = 0,
		Left = 1,
		Right = 2,
		Up = 3
	}

	public class AnimationController {
		private Animation[] _animations; // One animation per direction
		private Direction _currentDirection;
		private bool _isMoving;

		public AnimationController(Texture2D spriteSheet, int frameCount, int frameWidth, int frameHeight, float frameTime, bool pingpong = false) {
			_animations = new Animation[4];

			// Create animations for each direction (assuming rows: down, left, right, up)
			for(int i = 0; i < 4; i++) {
				_animations[i] = new Animation(spriteSheet, frameCount, frameWidth, frameHeight, frameTime, i, pingpong);
			}

			_currentDirection = Direction.Down;
			_isMoving = false;
		}

		public void Update(GameTime gameTime, Vector2 velocity) {
			// Determine direction based on velocity
			if(velocity.Length() > 0) {
				_isMoving = true;

				// Prioritize horizontal movement for direction
				if(Math.Abs(velocity.X) > Math.Abs(velocity.Y)) {
					_currentDirection = velocity.X > 0 ? Direction.Right : Direction.Left;
				} else {
					_currentDirection = velocity.Y > 0 ? Direction.Down : Direction.Up;
				}

				// Update current animation
				_animations[(int)_currentDirection].Update(gameTime);
			} else {
				_isMoving = false;
				// Reset to first frame when idle
				_animations[(int)_currentDirection].Reset();
			}
		}

		public Rectangle GetSourceRectangle() {
			return _animations[(int)_currentDirection].GetSourceRectangle();
		}

		public Texture2D GetTexture() {
			return _animations[(int)_currentDirection].Texture;
		}
	}
}