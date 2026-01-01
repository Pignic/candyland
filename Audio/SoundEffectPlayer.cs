using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;

namespace EldmeresTale.Audio;

/// <summary>
/// Plays procedurally generated sound effects
/// </summary>
public class SoundEffectPlayer : IDisposable {
	private const int SAMPLE_RATE = 44100;
	private SoundEffectLibrary _library;
	private List<PlayingSound> _playingSounds = new List<PlayingSound>();

	private class PlayingSound {
		public DynamicSoundEffectInstance Instance;
		public float[] Samples;
		public int Position;
		public bool Loop;
		public bool Finished;
	}

	public SoundEffectPlayer() {
		_library = new SoundEffectLibrary();
	}

	public void LoadLibrary(string filepath) {
		_library.LoadFromFile(filepath);
	}

	/// <summary>
	/// Play a sound effect by name
	/// </summary>
	public void Play(string effectName, float volume = 1.0f) {
		if(!_library.HasEffect(effectName)) {
			System.Diagnostics.Debug.WriteLine($"[SFX] Effect not found: {effectName}");
			return;
		}

		// Generate samples
		float[] samples = _library.Generate(effectName);
		System.Diagnostics.Debug.WriteLine($"[SFX] Generated {samples.Length} samples");

		if(samples.Length == 0) {
			System.Diagnostics.Debug.WriteLine($"[SFX] ERROR: No samples generated!");
			return;
		}

		// Debug: Check sample values
		float maxSample = 0f;
		for(int i = 0; i < Math.Min(100, samples.Length); i++) {
			if(Math.Abs(samples[i]) > maxSample) maxSample = Math.Abs(samples[i]);
		}
		System.Diagnostics.Debug.WriteLine($"[SFX] Max sample value in first 100: {maxSample}");

		// Create sound instance - STEREO to match MusicPlayer
		var instance = new DynamicSoundEffectInstance(SAMPLE_RATE, AudioChannels.Stereo);
		instance.Volume = Math.Clamp(volume, 0f, 1f);
		instance.BufferNeeded += (sender, e) => OnBufferNeeded((DynamicSoundEffectInstance)sender);

		System.Diagnostics.Debug.WriteLine($"[SFX] Created instance, volume={instance.Volume}, state={instance.State}");

		// Add to playing list
		var playingSound = new PlayingSound {
			Instance = instance,
			Samples = samples,
			Position = 0,
			Loop = false,
			Finished = false
		};

		_playingSounds.Add(playingSound);

		// Submit initial buffers and start playing
		SubmitBuffers(playingSound);
		System.Diagnostics.Debug.WriteLine($"[SFX] After SubmitBuffers: PendingBufferCount={instance.PendingBufferCount}");

		instance.Play();
		System.Diagnostics.Debug.WriteLine($"[SFX] After Play(): State={instance.State}");

		System.Diagnostics.Debug.WriteLine($"[SFX] Playing: {effectName} ({samples.Length} samples, {samples.Length / (float)SAMPLE_RATE:F2}s)");
	}

	private void OnBufferNeeded(DynamicSoundEffectInstance instance) {
		// Find the playing sound for this instance
		PlayingSound sound = _playingSounds.Find(s => s.Instance == instance);
		if(sound == null) return;

		SubmitBuffers(sound);
	}

	private void SubmitBuffers(PlayingSound sound) {
		const int BUFFER_SIZE = 4410; // ~0.1 seconds

		int buffersSubmitted = 0;

		// Submit up to 2 buffers
		while(sound.Instance.PendingBufferCount < 2 && !sound.Finished) {
			int remainingSamples = sound.Samples.Length - sound.Position;
			if(remainingSamples <= 0) {
				if(sound.Loop) {
					sound.Position = 0;
					remainingSamples = sound.Samples.Length;
				} else {
					sound.Finished = true;
					break;
				}
			}

			int samplesToSubmit = Math.Min(BUFFER_SIZE, remainingSamples);
			byte[] buffer = new byte[samplesToSubmit * 4]; // 4 bytes per frame (stereo: L+R * 2 bytes each)

			// Convert float samples to 16-bit PCM stereo
			for(int i = 0; i < samplesToSubmit; i++) {
				float sample = sound.Samples[sound.Position + i];
				sample = Math.Clamp(sample, -1f, 1f);
				short pcm = (short)(sample * 32767);

				// Left channel
				buffer[i * 4 + 0] = (byte)(pcm & 0xFF);
				buffer[i * 4 + 1] = (byte)((pcm >> 8) & 0xFF);
				// Right channel (same as left for mono sources)
				buffer[i * 4 + 2] = (byte)(pcm & 0xFF);
				buffer[i * 4 + 3] = (byte)((pcm >> 8) & 0xFF);
			}

			sound.Instance.SubmitBuffer(buffer);
			sound.Position += samplesToSubmit;
			buffersSubmitted++;
		}

		if(buffersSubmitted > 0) {
			System.Diagnostics.Debug.WriteLine($"[SFX] SubmitBuffers: Submitted {buffersSubmitted} buffers, Position={sound.Position}/{sound.Samples.Length}");
		}
	}

	/// <summary>
	/// Update and clean up finished sounds
	/// </summary>
	public void Update() {
		// Remove finished sounds
		for(int i = _playingSounds.Count - 1; i >= 0; i--) {
			var sound = _playingSounds[i];

			if(sound.Finished && sound.Instance.PendingBufferCount == 0) {
				sound.Instance.Stop();
				sound.Instance.Dispose();
				_playingSounds.RemoveAt(i);
			}
		}
	}

	/// <summary>
	/// Stop all playing sounds
	/// </summary>
	public void StopAll() {
		foreach(var sound in _playingSounds) {
			sound.Instance.Stop();
			sound.Instance.Dispose();
		}
		_playingSounds.Clear();
	}

	public void Dispose() {
		StopAll();
	}
}