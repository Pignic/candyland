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

	private double _currentTime = 0d;
	private bool _isPlaying = false;
	private bool _isInitialized = false;

	// For noise generation
	private Random _noiseRandom = new Random();
	private long _samplePosition = 0;

	// Active notes currently playing
	private class ActiveNote {
		public Note Note;
		public double TimeStarted;
		public double Phase;
		public bool IsStopping;
		public double TimeStoppedAt;
		public double LastNoiseSample;
	}
	private List<ActiveNote> _activeNotes = new List<ActiveNote>();

	public bool IsPlaying => _isPlaying;
	public double CurrentTime => _currentTime;

	public MusicPlayer() {
		// DynamicSoundEffectInstance for real-time audio
		_soundEffect = new DynamicSoundEffectInstance(SAMPLE_RATE, AudioChannels.Stereo);
		_soundEffect.BufferNeeded += OnBufferNeeded;
	}

	public void LoadSong(Song song) {
		Stop();
		_currentSong = song;
		_currentTime = 0d;
		_samplePosition = 0;
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
		_currentTime = 0d;
		_samplePosition = 0;
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
		// Generate samples
		for(int i = 0; i < BUFFER_SIZE; i++) {
			// Calculate time from sample position (PRECISE timing!)
			double sampleTime = _samplePosition / (double)SAMPLE_RATE;
			double currentBeat = sampleTime * _currentSong.BeatsPerSecond;

			// Update active notes for this exact sample time
			UpdateActiveNotes(currentBeat);

			// Mix all active notes
			double sampleL = 0d;
			double sampleR = 0d;

			foreach(var activeNote in _activeNotes) {
				Channel channel = _currentSong.Channels.FirstOrDefault(c => c.Id == activeNote.Note.ChannelId);
				if(channel == null) continue;

				double noteTime = sampleTime - activeNote.TimeStarted;
				double noteDuration = activeNote.Note.DurationBeats * _currentSong.SecondsPerBeat;

				// Generate waveform with effects
				double sample = GenerateWaveform(
					channel.Type,
					activeNote.Note.Frequency,
					ref activeNote.Phase,
					activeNote.Note,
					noteTime,
					noteDuration,
					ref activeNote.LastNoiseSample
				);

				// Apply ADSR envelope
				sample *= ApplyEnvelope(
						channel.Envelope,
						noteTime,
						noteDuration,
						activeNote.IsStopping,
						activeNote.TimeStoppedAt,
						sampleTime
					);

				// Apply velocity (note accent)
				sample *= activeNote.Note.Velocity;

				// Apply volume and panning
				sampleL += sample * channel.VolumeL;
				sampleR += sample * channel.VolumeR;
			}

			// Clamp and convert to 16-bit PCM
			sampleL = Math.Clamp(sampleL, -1d, 1d);
			sampleR = Math.Clamp(sampleR, -1d, 1d);

			short pcmL = (short)(sampleL * 32767);
			short pcmR = (short)(sampleR * 32767);

			// Write to buffer (stereo interleaved: LRLRLR...)
			int offset = i * 4;
			buffer[offset + 0] = (byte)(pcmL & 0xFF);
			buffer[offset + 1] = (byte)((pcmL >> 8) & 0xFF);
			buffer[offset + 2] = (byte)(pcmR & 0xFF);
			buffer[offset + 3] = (byte)((pcmR >> 8) & 0xFF);

			// Increment sample position AFTER generating the sample
			_samplePosition++;
		}

		// Update _currentTime to match sample position (for UI/looping)
		_currentTime = _samplePosition / (double)SAMPLE_RATE;

		// Check if song finished (loop handling)
		if(_currentTime >= _currentSong.TotalDurationSeconds) {
			if(_currentSong.Loop) {
				double loopTime = _currentTime;
				foreach(var an in _activeNotes) {
					if(!an.IsStopping) {
						an.IsStopping = true;
						an.TimeStoppedAt = loopTime;
					}
				}

				// Reset time for loop
				_samplePosition = 0;
				_currentTime = 0d;
				_activeNotes.Clear();
			} else {
				Stop();
			}
		}
	}

	/// <summary>
	/// Update which notes are currently playing
	/// </summary>
	private void UpdateActiveNotes(double currentBeat) {
		// Remove notes that have finished their release
		_activeNotes.RemoveAll(an => {
			if(an.IsStopping) {
				double timeSinceStopped = (currentBeat * _currentSong.SecondsPerBeat) - an.TimeStoppedAt;
				Channel ch = _currentSong.Channels.FirstOrDefault(c => c.Id == an.Note.ChannelId);
				if(ch != null && timeSinceStopped > ch.Envelope.Release) {
					return true; // Release finished, remove it
				}
			}
			return false;
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
					foreach(var an in _activeNotes) {
						if(an.Note.ChannelId == note.ChannelId &&
						   an.Note.StartBeat != note.StartBeat &&
						   !an.IsStopping) {
							an.IsStopping = true;
							an.TimeStoppedAt = currentBeat * _currentSong.SecondsPerBeat;
						}
					}
					_activeNotes.Add(new ActiveNote {
						Note = note,
						TimeStarted = note.StartBeat * _currentSong.SecondsPerBeat,
						Phase = 0d,
						IsStopping = false,
						TimeStoppedAt = 0d,
						LastNoiseSample = 0d
					});

					System.Diagnostics.Debug.WriteLine($"✅ ADDED Note at beat {note.StartBeat}, _activeNotes.Count = {_activeNotes.Count}");
				}
			}
		}

		double currentTime = currentBeat * _currentSong.SecondsPerBeat;
		foreach(var an in _activeNotes) {
			if(!an.IsStopping) {
				double noteEndTime = an.TimeStarted + (an.Note.DurationBeats * _currentSong.SecondsPerBeat);
				if(currentTime >= noteEndTime) {
					an.IsStopping = true;
					an.TimeStoppedAt = noteEndTime;
				}
			}
		}
	}

	/// <summary>
	/// Generate a single sample for a given waveform with effects
	/// </summary>
	private double GenerateWaveform(Waveform type, double baseFrequency, ref double phase,
									Note note, double noteTime, double noteDuration,
									ref double lastNoiseSample) {
		double actualFrequency = baseFrequency;

		// Apply portamento (pitch slide to next note)
		if(note.HasPortamento && note.TargetFrequency > 0) {
			double progress = noteTime / noteDuration;
			progress = Math.Clamp(progress, 0d, 1d);

			// Smooth interpolation from current to target frequency
			actualFrequency = baseFrequency + (note.TargetFrequency - baseFrequency) * progress;
		}

		// Apply vibrato (pitch wobble)
		if(note.HasVibrato && noteTime < noteDuration) {
			const double VIBRATO_RATE = 4.5d;  // Hz (oscillations per second)
			const double VIBRATO_DEPTH = 0.015d;  // ±3% frequency variation (~0.5 semitones)

			double vibratoOffset = (double)Math.Sin(noteTime * 2.0 * Math.PI * VIBRATO_RATE);
			actualFrequency *= (1.0d + vibratoOffset * VIBRATO_DEPTH);
		}

		double sample = 0d;

		switch(type) {
			case Waveform.Sine:
				sample = (double)Math.Sin(phase);
				break;

			case Waveform.Square:
				sample = phase < Math.PI ? 1d : -1d;
				break;

			case Waveform.Triangle:
				sample = (double)(2.0 / Math.PI * Math.Asin(Math.Sin(phase)));
				break;

			case Waveform.Sawtooth:
				sample = (double)(2.0 * (phase / (2.0 * Math.PI) - Math.Floor(phase / (2.0 * Math.PI) + 0.5)));
				break;

			case Waveform.Noise:
				// Generate white noise
				double rawNoise = (double)(_noiseRandom.NextDouble() * 2.0 - 1.0);

				// Apply AGGRESSIVE filtering and character based on frequency (drum type)

				if(baseFrequency < 80d) {
					// KICK DRUM - Deep punch with pitch envelope
					rawNoise = lastNoiseSample * 0.97d + rawNoise * 0.03d;
					lastNoiseSample = rawNoise;

					// Add pitched "thump" - use noteTime, not phase!
					double kickFreq = 65d * (double)Math.Exp(-noteTime * 20d);
					double kickTone = (double)Math.Sin(noteTime * 2.0 * Math.PI * kickFreq);

					// Mix: 20% filtered noise + 80% tone for that "BOOM"
					sample = rawNoise * 0.2d + kickTone * 0.8d;

				} else if(baseFrequency < 250d) {
					// SNARE - Crisp crack with body
					rawNoise = lastNoiseSample * 0.65d + rawNoise * 0.35d;
					lastNoiseSample = rawNoise;

					// Add slight tone for snare "buzz" - use noteTime, not phase!
					double snareFreq = 200d * (double)Math.Exp(-noteTime * 8d); // Pitch drops slightly
					double snareTone = (double)Math.Sin(noteTime * 2.0 * Math.PI * snareFreq);

					// Mix: 85% noise + 15% tone
					sample = rawNoise * 0.85d + snareTone * 0.15d;

				} else if(baseFrequency < 400d) {
					// TOM - Mid punch, tonal
					rawNoise = lastNoiseSample * 0.60d + rawNoise * 0.40d;
					lastNoiseSample = rawNoise;

					// Add decaying tone - use noteTime, not phase!
					double tomFreq = 180d * (double)Math.Exp(-noteTime * 12d);
					double tomTone = (double)Math.Sin(noteTime * 2.0 * Math.PI * tomFreq);

					// Mix: 40% noise + 60% tone
					sample = rawNoise * 0.4d + tomTone * 0.6d;

				} else if(baseFrequency < 2000d) {
					// RIDE/LOWER CYMBAL - Metallic ping
					// Very light filtering
					rawNoise = lastNoiseSample * 0.20d + rawNoise * 0.80d;
					lastNoiseSample = rawNoise;

					sample = rawNoise;

				} else if(baseFrequency < 7000d) {
					// CRASH - Shimmery sustain
					// Minimal filtering, bright
					rawNoise = lastNoiseSample * 0.12d + rawNoise * 0.88d;
					lastNoiseSample = rawNoise;

					sample = rawNoise;

				} else {
					// HI-HAT - Very bright, crisp
					// Almost no filtering, pure white noise
					rawNoise = lastNoiseSample * 0.03d + rawNoise * 0.97d;
					lastNoiseSample = rawNoise;

					sample = rawNoise;
				}
				break;
		}

		// Advance phase (with actual frequency for pitch effects)
		if(type != Waveform.Noise) {
			phase += (double)(2.0 * Math.PI * actualFrequency / SAMPLE_RATE);
			if(phase >= 2.0 * Math.PI) {
				phase -= (double)(2.0 * Math.PI);
			}
		}

		return sample; // Reduce volume to prevent clipping
	}

	/// <summary>
	/// Apply ADSR envelope to sample
	/// </summary>
	private double ApplyEnvelope(ADSREnvelope env, double noteTime, double noteDuration, bool isStopping, double timeStoppedAt, double currentSampleTime) {
		if(noteTime < 0d) return 0d;

		// If note is stopping, use release envelope from when it stopped
		if(isStopping) {
			double releaseTime = currentSampleTime - timeStoppedAt;
			if(releaseTime < env.Release) {
				// Get the envelope value at the moment it stopped
				double stoppedValue;
				double timeAtStop = timeStoppedAt - (currentSampleTime - noteTime);

				if(timeAtStop < env.Attack) {
					stoppedValue = timeAtStop / env.Attack;
				} else if(timeAtStop < env.Attack + env.Decay) {
					double decayProgress = (timeAtStop - env.Attack) / env.Decay;
					stoppedValue = 1d + (env.Sustain - 1d) * decayProgress;
				} else {
					stoppedValue = env.Sustain;
				}

				// Release from that value
				return stoppedValue * (1d - releaseTime / env.Release);
			}
			return 0d;
		}

		// Normal ADSR (not stopping)
		// Attack phase
		if(noteTime < env.Attack) {
			return noteTime / env.Attack;
		}

		// Decay phase
		if(noteTime < env.Attack + env.Decay) {
			double decayProgress = (noteTime - env.Attack) / env.Decay;
			return 1d + (env.Sustain - 1d) * decayProgress;
		}

		// Sustain phase
		return env.Sustain;
	}

	public void Dispose() {
		Stop();
		_soundEffect?.Dispose();
	}
}