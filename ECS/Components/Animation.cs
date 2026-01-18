using Microsoft.Xna.Framework;

namespace EldmeresTale.ECS.Components;

public enum Direction {
	Down = 0,
	Left = 1,
	Right = 2,
	Up = 3
}

public struct Animation {
	public int FrameCount;
	public int FrameWidth;
	public int FrameHeight;
	public int CurrentFrame;
	public float FrameTime;      // Time per frame
	public float Timer;          // Current timer
	public bool Loop;
	public bool IsPlaying;
	public bool PingPong;        // Play forward then backward
	private bool _pingPongReverse;
	private Direction _currentDirection;

	public Animation(int frameCount, int frameWidth, int frameHeight, float frameTime, bool loop = true, bool pingPong = false) {
		FrameCount = frameCount;
		FrameWidth = frameWidth;
		FrameHeight = frameHeight;
		CurrentFrame = 0;
		FrameTime = frameTime;
		Timer = 0f;
		Loop = loop;
		IsPlaying = true;
		PingPong = pingPong;
		_pingPongReverse = false;
	}

	public void Update(float deltaTime) {
		if (!IsPlaying) {
			return;
		}

		Timer += deltaTime;

		if (Timer >= FrameTime) {
			Timer -= FrameTime;

			if (PingPong) {
				if (_pingPongReverse) {
					CurrentFrame--;
					if (CurrentFrame < 0) {
						CurrentFrame = 1;
						_pingPongReverse = false;
					}
				} else {
					CurrentFrame++;
					if (CurrentFrame >= FrameCount) {
						CurrentFrame = FrameCount - 2;
						_pingPongReverse = true;
						if (CurrentFrame < 0) {
							CurrentFrame = 0;
						}
					}
				}
			} else {
				CurrentFrame++;
				if (CurrentFrame >= FrameCount) {
					if (Loop) {
						CurrentFrame = 0;
					} else {
						CurrentFrame = FrameCount - 1;
						IsPlaying = false;
					}
				}
			}
		}
	}

	public void UpdateDirection(Direction direction) {
		if (direction != _currentDirection) {
			_currentDirection = direction;
		}
		IsPlaying = true;
	}

	public Rectangle GetSourceRect() {
		return new Rectangle(
			CurrentFrame * FrameWidth,
			(int)_currentDirection * FrameHeight,
			FrameWidth,
			FrameHeight
		);
	}
}