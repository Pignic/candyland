using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EldmeresTale.Audio;

/// <summary>
/// Real-time music synthesizer and player
/// Generates audio samples on-the-fly and plays them
/// </summary>
public class MusicPlayer {
	private Song _currentSong;
	private DynamicSoundEffectInstance _soundEffect;

	private const int SAMPLE_RATE = 44100;
	private const int BUFFER_SIZE = 4410; // ~0.1 seconds at 44.1kHz

	private float _currentTime = 0f;
	private bool _isPlaying = false;
	private bool _isInitialized = false;

	// For noise generation
	private Random _noiseRandom = new Random();
	private float _lastNoiseSample = 0f;

	// Active notes currently playing
	private class ActiveNote {
		public Note Note;
		public float TimeStarted;
		public float Phase;  // For waveform generation
	}
	private List<ActiveNote> _activeNotes = new List<ActiveNote>();

	public bool IsPlaying => _isPlaying;
	public float CurrentTime => _currentTime;

	public MusicPlayer() {
		// DynamicSoundEffectInstance for real-time audio
		_soundEffect = new DynamicSoundEffectInstance(SAMPLE_RATE, AudioChannels.Stereo);
		_soundEffect.BufferNeeded += OnBufferNeeded;
	}

	public void LoadSong(Song song) {
		Stop();
		_currentSong = song;
		_currentTime = 0f;
		_activeNotes.Clear();
		_isInitialized = true;

		System.Diagnostics.Debug.WriteLine($"[MUSIC] Loaded song: {song.Tempo} BPM, {song.TotalDurationSeconds:F2}s");
	}

	public void Play() {
		if(!_isInitialized || _currentSong == null) {
			System.Diagnostics.Debug.WriteLine("[MUSIC] Cannot play: no song loaded");
			return;
		}

		_isPlaying = true;
		_soundEffect.Play();

		System.Diagnostics.Debug.WriteLine("[MUSIC] Playing...");
	}

	public void Stop() {
		_isPlaying = false;
		_currentTime = 0f;
		_activeNotes.Clear();

		if(_soundEffect != null) {
			_soundEffect.Stop();
		}
	}

	public void Pause() {
		_isPlaying = false;
		if(_soundEffect != null) {
			_soundEffect.Pause();
		}
	}

	public void Resume() {
		if(_isInitialized && _currentSong != null) {
			_isPlaying = true;
			_soundEffect.Resume();
		}
	}

	public void Update(float deltaTime) {
		if(!_isPlaying || _currentSong == null) return;

		_currentTime += deltaTime;

		// Check if song finished
		if(_currentTime >= _currentSong.TotalDurationSeconds) {
			if(_currentSong.Loop) {
				_currentTime = 0f;
				_activeNotes.Clear();
			} else {
				Stop();
			}
		}
	}

	/// <summary>
	/// Called when the sound effect needs more audio data
	/// </summary>
	private void OnBufferNeeded(object sender, EventArgs e) {
		if(!_isPlaying || _currentSong == null) return;

		byte[] buffer = new byte[BUFFER_SIZE * 4]; // 2 channels * 2 bytes per sample
		GenerateAudioBuffer(buffer);

		_soundEffect.SubmitBuffer(buffer);
	}

	/// <summary>
	/// Generate audio samples for the buffer
	/// </summary>
	private void GenerateAudioBuffer(byte[] buffer) {
		float currentBeat = _currentTime * _currentSong.BeatsPerSecond;

		// Update active notes
		UpdateActiveNotes(currentBeat);

		// Generate samples
		for(int i = 0; i < BUFFER_SIZE; i++) {
			float sampleTime = _currentTime + (i / (float)SAMPLE_RATE);

			// Mix all active notes
			float sampleL = 0f;
			float sampleR = 0f;

			foreach(var activeNote in _activeNotes) {
				Channel channel = _currentSong.Channels.FirstOrDefault(c => c.Id == activeNote.Note.ChannelId);
				if(channel == null) continue;

				float noteTime = sampleTime - activeNote.TimeStarted;
				float noteDuration = activeNote.Note.DurationBeats * _currentSong.SecondsPerBeat;

				// Generate waveform with effects
				float sample = GenerateWaveform(
					channel.Type,
					activeNote.Note.Frequency,
					ref activeNote.Phase,
					activeNote.Note,
					noteTime,
					noteDuration
				);

				// Apply ADSR envelope
				sample *= ApplyEnvelope(channel.Envelope, noteTime, noteDuration);

				// Apply velocity (note accent)
				sample *= activeNote.Note.Velocity;

				// Apply volume and panning
				sampleL += sample * channel.VolumeL;
				sampleR += sample * channel.VolumeR;
			}

			// Clamp and convert to 16-bit PCM
			sampleL = Math.Clamp(sampleL, -1f, 1f);
			sampleR = Math.Clamp(sampleR, -1f, 1f);

			short pcmL = (short)(sampleL * 32767);
			short pcmR = (short)(sampleR * 32767);

			// Write to buffer (stereo interleaved: LRLRLR...)
			int offset = i * 4;
			buffer[offset + 0] = (byte)(pcmL & 0xFF);
			buffer[offset + 1] = (byte)((pcmL >> 8) & 0xFF);
			buffer[offset + 2] = (byte)(pcmR & 0xFF);
			buffer[offset + 3] = (byte)((pcmR >> 8) & 0xFF);
		}
	}

	/// <summary>
	/// Update which notes are currently playing
	/// </summary>
	private void UpdateActiveNotes(float currentBeat) {
		// Remove finished notes
		_activeNotes.RemoveAll(an => {
			float endBeat = an.Note.StartBeat + an.Note.DurationBeats;
			return currentBeat > endBeat + 0.5f; // Small buffer for release
		});

		// Add new notes that should start
		foreach(var note in _currentSong.Notes) {
			if(note.StartBeat <= currentBeat && note.StartBeat + note.DurationBeats >= currentBeat) {
				// Check if already playing
				bool alreadyActive = _activeNotes.Any(an =>
					an.Note.ChannelId == note.ChannelId &&
					an.Note.StartBeat == note.StartBeat
				);

				if(!alreadyActive) {
					_activeNotes.Add(new ActiveNote {
						Note = note,
						TimeStarted = note.StartBeat * _currentSong.SecondsPerBeat,
						Phase = 0f
					});
				}
			}
		}
	}

	/// <summary>
	/// Generate a single sample for a given waveform with effects
	/// </summary>
	private float GenerateWaveform(Waveform type, float baseFrequency, ref float phase,
									Note note, float noteTime, float noteDuration) {
		float actualFrequency = baseFrequency;

		// Apply portamento (pitch slide to next note)
		if(note.HasPortamento && note.TargetFrequency > 0) {
			float progress = noteTime / noteDuration;
			progress = Math.Clamp(progress, 0f, 1f);

			// Smooth interpolation from current to target frequency
			actualFrequency = baseFrequency + (note.TargetFrequency - baseFrequency) * progress;
		}

		// Apply vibrato (pitch wobble)
		if(note.HasVibrato) {
			const float VIBRATO_RATE = 4.5f;  // Hz (oscillations per second)
			const float VIBRATO_DEPTH = 0.015f;  // ±3% frequency variation (~0.5 semitones)

			float vibratoOffset = (float)Math.Sin(noteTime * 2.0 * Math.PI * VIBRATO_RATE);
			actualFrequency *= (1.0f + vibratoOffset * VIBRATO_DEPTH);
		}

		float sample = 0f;

		switch(type) {
			case Waveform.Sine:
				sample = (float)Math.Sin(phase);
				break;

			case Waveform.Square:
				sample = phase < Math.PI ? 1f : -1f;
				break;

			case Waveform.Triangle:
				sample = (float)(2.0 / Math.PI * Math.Asin(Math.Sin(phase)));
				break;

			case Waveform.Sawtooth:
				sample = (float)(2.0 * (phase / (2.0 * Math.PI) - Math.Floor(phase / (2.0 * Math.PI) + 0.5)));
				break;

			case Waveform.Noise:
				// White noise (using persistent random for proper noise)
				sample = (float)(_noiseRandom.NextDouble() * 2.0 - 1.0);

				// Filter based on frequency for drum character
				if(baseFrequency < 100f) {
					// Low frequency = more filtered (kick drum)
					sample = _lastNoiseSample * 0.9f + sample * 0.1f;
					_lastNoiseSample = sample;
				} else if(baseFrequency < 300f) {
					// Mid frequency = moderate filtering (snare)
					sample = _lastNoiseSample * 0.6f + sample * 0.4f;
					_lastNoiseSample = sample;
				} else {
					// High frequency = minimal filtering (hi-hat)
					sample = _lastNoiseSample * 0.1f + sample * 0.9f;
					_lastNoiseSample = sample;
				}
				break;
		}

		// Advance phase (with actual frequency for pitch effects)
		if(type != Waveform.Noise) {
			phase += (float)(2.0 * Math.PI * actualFrequency / SAMPLE_RATE);
			if(phase >= 2.0 * Math.PI) {
				phase -= (float)(2.0 * Math.PI);
			}
		}

		return sample * 0.3f; // Reduce volume to prevent clipping
	}

	/// <summary>
	/// Apply ADSR envelope to sample
	/// </summary>
	private float ApplyEnvelope(ADSREnvelope env, float noteTime, float noteDuration) {
		if(noteTime < 0f) return 0f;

		// Attack phase
		if(noteTime < env.Attack) {
			return noteTime / env.Attack;
		}

		// Decay phase
		if(noteTime < env.Attack + env.Decay) {
			float decayProgress = (noteTime - env.Attack) / env.Decay;
			return 1f + (env.Sustain - 1f) * decayProgress;
		}

		// Sustain phase
		if(noteTime < noteDuration) {
			return env.Sustain;
		}

		// Release phase
		float releaseTime = noteTime - noteDuration;
		if(releaseTime < env.Release) {
			return env.Sustain * (1f - releaseTime / env.Release);
		}

		return 0f;
	}

	public void Dispose() {
		Stop();
		_soundEffect?.Dispose();
	}
}