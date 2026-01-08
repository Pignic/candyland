using EldmeresTale.Core;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.IO;

namespace EldmeresTale.Audio;

public class SoundEffectPlayer : IDisposable {
	private const int SAMPLE_RATE = 44100;
	private readonly SoundEffectLibrary _library;
	private readonly List<PlayingSound> _playingSounds = [];
	private float _masterVolume = 1.0f;
	public float MasterVolume {
		get => _masterVolume;
		set => _masterVolume = Math.Clamp(value, 0f, 1f);
	}

	private class PlayingSound {
		public DynamicSoundEffectInstance Instance;
		public float[] Samples;
		public int Position;
		public bool Loop;
		public bool Finished;
	}

	public SoundEffectPlayer() {
		_library = new SoundEffectLibrary();
		_masterVolume = GameSettings.Instance.SfxVolume;
	}

	public void LoadLibrary(string filepath) {
		_library.LoadFromFile(filepath);
	}

	public void Play(string effectName, float volume = 1.0f) {
		if (!_library.HasEffect(effectName)) {
			System.Diagnostics.Debug.WriteLine($"[SFX] Effect not found: {effectName}");
			return;
		}

		// Generate samples
		float[] samples = _library.Generate(effectName);
		System.Diagnostics.Debug.WriteLine($"[SFX] Generated {samples.Length} samples");

		if (samples.Length == 0) {
			System.Diagnostics.Debug.WriteLine("[SFX] ERROR: No samples generated!");
			return;
		}

		float maxSample = 0f;
		for (int i = 0; i < Math.Min(100, samples.Length); i++) {
			if (Math.Abs(samples[i]) > maxSample) {
				maxSample = Math.Abs(samples[i]);
			}
		}

		// Create sound instance - STEREO to match MusicPlayer
		DynamicSoundEffectInstance instance = new DynamicSoundEffectInstance(SAMPLE_RATE, AudioChannels.Stereo) {
			Volume = Math.Clamp(volume * _masterVolume, 0f, 1f)
		};
		instance.BufferNeeded += (sender, e) => OnBufferNeeded((DynamicSoundEffectInstance)sender);


		// Add to playing list
		PlayingSound playingSound = new PlayingSound {
			Instance = instance,
			Samples = samples,
			Position = 0,
			Loop = false,
			Finished = false
		};

		_playingSounds.Add(playingSound);

		// Submit initial buffers and start playing
		SubmitBuffers(playingSound);

		instance.Play();
	}

	private void OnBufferNeeded(DynamicSoundEffectInstance instance) {
		// Find the playing sound for this instance
		PlayingSound sound = _playingSounds.Find(s => s.Instance == instance);
		if (sound == null) {
			return;
		}

		SubmitBuffers(sound);
	}

	private static void SubmitBuffers(PlayingSound sound) {
		const int BUFFER_SIZE = 4410; // ~0.1 seconds


		// Submit up to 2 buffers
		for (int buffersSubmitted = 0; sound.Instance.PendingBufferCount < 2 && !sound.Finished; buffersSubmitted++) {
			int remainingSamples = sound.Samples.Length - sound.Position;
			if (remainingSamples <= 0) {
				if (sound.Loop) {
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
			for (int i = 0; i < samplesToSubmit; i++) {
				float sample = sound.Samples[sound.Position + i];
				sample = Math.Clamp(sample, -1f, 1f);
				short pcm = (short)(sample * 32767);

				// Left channel
				buffer[(i * 4) + 0] = (byte)(pcm & 0xFF);
				buffer[(i * 4) + 1] = (byte)((pcm >> 8) & 0xFF);
				// Right channel (same as left for mono sources)
				buffer[(i * 4) + 2] = (byte)(pcm & 0xFF);
				buffer[(i * 4) + 3] = (byte)((pcm >> 8) & 0xFF);
			}

			sound.Instance.SubmitBuffer(buffer);
			sound.Position += samplesToSubmit;
		}
	}

	public void Update() {
		// Remove finished sounds
		for (int i = _playingSounds.Count - 1; i >= 0; i--) {
			PlayingSound sound = _playingSounds[i];

			if (sound.Finished && sound.Instance.PendingBufferCount == 0) {
				sound.Instance.Stop();
				sound.Instance.Dispose();
				_playingSounds.RemoveAt(i);
			}
		}
	}

	public void StopAll() {
		foreach (PlayingSound sound in _playingSounds) {
			sound.Instance.Stop();
			sound.Instance.Dispose();
		}
		_playingSounds.Clear();
	}

	public void Dispose() {
		StopAll();
	}

	// Use for debug
	public void ExportToWav(string effectName, string outputPath) {
		if (!_library.HasEffect(effectName)) {
			System.Diagnostics.Debug.WriteLine($"[SFX] Effect not found: {effectName}");
			return;
		}

		// Generate samples
		float[] samples = _library.Generate(effectName);
		if (samples.Length == 0) {
			System.Diagnostics.Debug.WriteLine($"[SFX] No samples generated for {effectName}");
			return;
		}

		// Convert float samples to 16-bit PCM stereo
		byte[] pcmData = new byte[samples.Length * 4]; // Stereo: 2 channels * 2 bytes
		for (int i = 0; i < samples.Length; i++) {
			float sample = Math.Clamp(samples[i], -1f, 1f);
			short pcm = (short)(sample * 32767);

			// Left channel
			pcmData[(i * 4) + 0] = (byte)(pcm & 0xFF);
			pcmData[(i * 4) + 1] = (byte)((pcm >> 8) & 0xFF);
			// Right channel
			pcmData[(i * 4) + 2] = (byte)(pcm & 0xFF);
			pcmData[(i * 4) + 3] = (byte)((pcm >> 8) & 0xFF);
		}

		// Write WAV file
		using FileStream fs = new System.IO.FileStream(outputPath, System.IO.FileMode.Create);
		using BinaryWriter writer = new System.IO.BinaryWriter(fs);
		// RIFF header
		writer.Write(['R', 'I', 'F', 'F']);
		writer.Write(36 + pcmData.Length); // File size - 8
		writer.Write(['W', 'A', 'V', 'E']);

		// fmt chunk
		writer.Write(['f', 'm', 't', ' ']);
		writer.Write(16); // fmt chunk size
		writer.Write((short)1); // Audio format (1 = PCM)
		writer.Write((short)2); // Channels (2 = stereo)
		writer.Write(SAMPLE_RATE); // Sample rate
		writer.Write(SAMPLE_RATE * 4); // Byte rate (sample rate * channels * bytes per sample)
		writer.Write((short)4); // Block align (channels * bytes per sample)
		writer.Write((short)16); // Bits per sample

		// data chunk
		writer.Write(['d', 'a', 't', 'a']);
		writer.Write(pcmData.Length);
		writer.Write(pcmData);

	}
}