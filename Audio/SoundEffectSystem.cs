using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EldmeresTale.Audio;

public class SoundEffectNote {
	[JsonPropertyName("frequency")]
	public double Frequency { get; set; }

	[JsonPropertyName("duration")]
	public double Duration { get; set; }

	[JsonPropertyName("volume")]
	public double Volume { get; set; } = 1.0;
}

public class PitchSweep {
	[JsonPropertyName("start")]
	public double Start { get; set; }

	[JsonPropertyName("end")]
	public double End { get; set; }

	[JsonPropertyName("duration")]
	public double Duration { get; set; }
}

public class Vibrato {
	[JsonPropertyName("rate")]
	public double Rate { get; set; } = 5.0;

	[JsonPropertyName("depth")]
	public double Depth { get; set; } = 0.1;
}

public class Randomization {
	[JsonPropertyName("pitch_variance")]
	public double PitchVariance { get; set; } = 0.0;

	[JsonPropertyName("timing_variance")]
	public double TimingVariance { get; set; } = 0.0;

	[JsonPropertyName("volume_variance")]
	public double VolumeVariance { get; set; } = 0.0;
}

public class ADSREnvelopeJson {
	[JsonPropertyName("attack")]
	public float Attack { get; set; }

	[JsonPropertyName("decay")]
	public float Decay { get; set; }

	[JsonPropertyName("sustain")]
	public float Sustain { get; set; }

	[JsonPropertyName("release")]
	public float Release { get; set; }

	// Implicit conversion to ADSREnvelope struct
	public static implicit operator ADSREnvelope(ADSREnvelopeJson json) {
		return new ADSREnvelope(json.Attack, json.Decay, json.Sustain, json.Release);
	}
}

public class SoundEffectLayer {
	[JsonPropertyName("waveform")]
	public string WaveformStr { get; set; } = "sine";

	[JsonIgnore]
	public Waveform Waveform => WaveformStr.ToLower() switch {
		"sine" => Waveform.Sine,
		"square" => Waveform.Square,
		"triangle" => Waveform.Triangle,
		"sawtooth" => Waveform.Sawtooth,
		"noise" => Waveform.Noise,
		_ => Waveform.Sine
	};

	[JsonPropertyName("frequency")]
	public double? Frequency { get; set; }

	[JsonPropertyName("filter")]
	public double? Filter { get; set; }

	[JsonPropertyName("pitch_sweep")]
	public PitchSweep PitchSweep { get; set; }

	[JsonPropertyName("vibrato")]
	public Vibrato Vibrato { get; set; }

	[JsonPropertyName("notes")]
	public List<SoundEffectNote> Notes { get; set; }

	[JsonPropertyName("envelope")]
	public ADSREnvelopeJson EnvelopeJson { get; set; }

	[JsonIgnore]
	public ADSREnvelope Envelope => EnvelopeJson ?? new ADSREnvelope(0.01f, 0.05f, 0.8f, 0.1f);

	[JsonPropertyName("volume")]
	public double Volume { get; set; } = 1.0;
}

public class SoundEffectDefinition {
	[JsonPropertyName("loop")]
	public bool Loop { get; set; } = false;

	[JsonPropertyName("loop_duration")]
	public double LoopDuration { get; set; } = 1.0;

	[JsonPropertyName("layers")]
	public List<SoundEffectLayer> Layers { get; set; } = new List<SoundEffectLayer>();

	[JsonPropertyName("randomization")]
	public Randomization Randomization { get; set; } = new Randomization();
}

public class SoundEffectGenerator {
	private const int SAMPLE_RATE = 44100;
	private Random _random = new Random();

	/// <summary>
	/// Generate audio samples for a sound effect
	/// </summary>
	public float[] Generate(SoundEffectDefinition sfx) {
		// Calculate total duration
		double totalDuration = CalculateDuration(sfx);

		// Apply timing variance
		if (sfx.Randomization.TimingVariance > 0) {
			double variance = ((_random.NextDouble() * 2.0) - 1.0) * sfx.Randomization.TimingVariance;
			totalDuration *= 1.0 + variance;
		}

		int sampleCount = (int)(totalDuration * SAMPLE_RATE);
		float[] samples = new float[sampleCount];

		// Generate each layer and mix
		foreach (SoundEffectLayer layer in sfx.Layers) {
			float[] layerSamples = GenerateLayer(layer, totalDuration, sfx.Randomization);

			// Mix layer into output
			for (int i = 0; i < Math.Min(samples.Length, layerSamples.Length); i++) {
				samples[i] += layerSamples[i];
			}
		}

		// Normalize and boost
		float max = 0f;
		foreach (float s in samples) {
			if (Math.Abs(s) > max) {
				max = Math.Abs(s);
			}
		}

		// Normalize to prevent clipping, then boost overall volume
		if (max > 0.001f) {
			float normalizeTarget = 0.9f; // Leave headroom
			float boost = 3.0f; // 3x amplification

			for (int i = 0; i < samples.Length; i++) {
				samples[i] = samples[i] / max * normalizeTarget * boost;
				samples[i] = Math.Clamp(samples[i], -1f, 1f); // Hard limit
			}
		}

		return samples;
	}

	private double CalculateDuration(SoundEffectDefinition sfx) {
		if (sfx.Loop) {
			return sfx.LoopDuration;
		}

		double maxDuration = 0;
		foreach (SoundEffectLayer layer in sfx.Layers) {
			double layerDuration = 0;

			// Check pitch sweep
			if (layer.PitchSweep != null) {
				layerDuration = layer.PitchSweep.Duration;
			}

			// Check notes
			if (layer.Notes != null) {
				foreach (SoundEffectNote note in layer.Notes) {
					layerDuration += note.Duration;
				}
			}

			// Add envelope release
			layerDuration += layer.Envelope.Release;

			if (layerDuration > maxDuration) {
				maxDuration = layerDuration;
			}
		}

		return maxDuration > 0 ? maxDuration : 1.0;
	}

	private float[] GenerateLayer(SoundEffectLayer layer, double duration, Randomization randomization) {
		int sampleCount = (int)(duration * SAMPLE_RATE);
		float[] samples = new float[sampleCount];


		double phase = 0;
		double lastNoiseSample = 0;
		double currentTime = 0;
		double noteStartTime = 0;
		int currentNoteIndex = 0;

		// Calculate when to start release phase (duration minus release time)
		double releaseStartTime = Math.Max(0, duration - layer.Envelope.Release);

		// Apply pitch variance
		double pitchMultiplier = 1.0;
		if (randomization.PitchVariance > 0) {
			double variance = ((_random.NextDouble() * 2.0) - 1.0) * randomization.PitchVariance;
			pitchMultiplier = 1.0 + variance;
		}

		// Apply volume variance
		double volumeMultiplier = 1.0;
		if (randomization.VolumeVariance > 0) {
			double variance = ((_random.NextDouble() * 2.0) - 1.0) * randomization.VolumeVariance;
			volumeMultiplier = Math.Max(0.1, 1.0 + variance);
		}


		for (int i = 0; i < sampleCount; i++) {
			currentTime = i / (double)SAMPLE_RATE;
			double noteTime = currentTime - noteStartTime;

			// Determine frequency
			double frequency = 440; // Default

			if (layer.PitchSweep != null) {
				// Pitch sweep
				double progress = Math.Clamp(currentTime / layer.PitchSweep.Duration, 0, 1);
				frequency = layer.PitchSweep.Start + ((layer.PitchSweep.End - layer.PitchSweep.Start) * progress);
			} else if (layer.Notes != null && layer.Notes.Count > 0) {
				// Note sequence
				if (currentNoteIndex < layer.Notes.Count) {
					SoundEffectNote note = layer.Notes[currentNoteIndex];
					frequency = note.Frequency;

					// Check if note finished
					if (noteTime >= note.Duration && currentNoteIndex < layer.Notes.Count - 1) {
						currentNoteIndex++;
						noteStartTime = currentTime;
						noteTime = 0;
					}
				}
			} else if (layer.Frequency.HasValue) {
				frequency = layer.Frequency.Value;
			}

			// Apply pitch variance
			frequency *= pitchMultiplier;

			// Apply vibrato
			if (layer.Vibrato != null) {
				double vibratoOffset = Math.Sin(currentTime * 2.0 * Math.PI * layer.Vibrato.Rate);
				frequency *= 1.0 + (vibratoOffset * layer.Vibrato.Depth);
			}

			// Generate waveform
			float sample = GenerateWaveform(layer.Waveform, frequency, ref phase, ref lastNoiseSample, layer.Filter);

			// Apply envelope - use currentTime for overall envelope, noteTime for note sequences
			double envelope;
			if (layer.PitchSweep != null || layer.Frequency.HasValue) {
				// For pitch sweeps and continuous tones, use currentTime
				envelope = ApplyEnvelopeWithRelease(layer.Envelope, currentTime, releaseStartTime);
			} else {
				// For note sequences, use noteTime
				envelope = ApplyEnvelope(layer.Envelope, noteTime);
			}

			sample *= (float)envelope;

			// Apply layer volume
			sample *= (float)(layer.Volume * volumeMultiplier);

			// Get note volume if using notes
			if (layer.Notes != null && currentNoteIndex < layer.Notes.Count) {
				sample *= (float)layer.Notes[currentNoteIndex].Volume;
			}

			samples[i] = sample;

			// Advance phase
			if (layer.Waveform != Waveform.Noise) {
				phase += 2.0 * Math.PI * frequency / SAMPLE_RATE;
				if (phase >= 2.0 * Math.PI) {
					phase -= 2.0 * Math.PI;
				}
			}
		}

		// Check final sample values
		float maxSample = 0f;
		for (int i = 0; i < samples.Length; i++) {
			if (Math.Abs(samples[i]) > maxSample) {
				maxSample = Math.Abs(samples[i]);
			}
		}

		return samples;
	}

	private float GenerateWaveform(Waveform type, double frequency, ref double phase, ref double lastNoiseSample, double? filterAmount) {
		double sample = 0;

		switch (type) {
			case Waveform.Sine:
				sample = Math.Sin(phase);
				break;

			case Waveform.Square:
				sample = phase < Math.PI ? 1.0 : -1.0;
				break;

			case Waveform.Triangle:
				sample = 2.0 / Math.PI * Math.Asin(Math.Sin(phase));
				break;

			case Waveform.Sawtooth:
				sample = 2.0 * ((phase / (2.0 * Math.PI)) - Math.Floor((phase / (2.0 * Math.PI)) + 0.5));
				break;

			case Waveform.Noise:
				double rawNoise = (_random.NextDouble() * 2.0) - 1.0;

				// Apply filter if specified
				if (filterAmount.HasValue) {
					double filter = filterAmount.Value;
					rawNoise = (lastNoiseSample * filter) + (rawNoise * (1.0 - filter));
					lastNoiseSample = rawNoise;
				}

				sample = rawNoise;
				break;
		}

		return (float)sample;
	}

	private double ApplyEnvelope(ADSREnvelope env, double noteTime) {
		if (noteTime < 0) {
			return 0;
		}

		// Attack
		if (noteTime < env.Attack) {
			return noteTime / env.Attack;
		}

		// Decay
		if (noteTime < env.Attack + env.Decay) {
			double decayProgress = (noteTime - env.Attack) / env.Decay;
			return 1.0 + ((env.Sustain - 1.0) * decayProgress);
		}

		// Sustain
		return env.Sustain;
	}

	/// <summary>
	/// Apply envelope with automatic release at the end of the sound
	/// Used for pitch sweeps and continuous tones
	/// </summary>
	private double ApplyEnvelopeWithRelease(ADSREnvelope env, double currentTime, double releaseStartTime) {
		if (currentTime < 0) {
			return 0;
		}

		// Release phase (at the end)
		if (currentTime >= releaseStartTime) {
			double releaseTime = currentTime - releaseStartTime;
			if (releaseTime < env.Release) {
				// Get sustain value and fade from it
				double sustainLevel = env.Sustain;
				if (sustainLevel <= 0.01) {
					sustainLevel = 0.01; // Minimum to avoid silence
				}

				return sustainLevel * (1.0 - (releaseTime / env.Release));
			}
			return 0;
		}

		// Attack
		if (currentTime < env.Attack) {
			return currentTime / env.Attack;
		}

		// Decay
		if (currentTime < env.Attack + env.Decay) {
			double decayProgress = (currentTime - env.Attack) / env.Decay;
			return 1.0 + ((env.Sustain - 1.0) * decayProgress);
		}

		// Sustain (hold until release starts)
		double sustainLevel2 = env.Sustain;
		if (sustainLevel2 <= 0.01) {
			sustainLevel2 = 0.01; // Minimum to avoid premature silence
		}

		return sustainLevel2;
	}
}

/// <summary>
/// Manages loading and caching sound effect definitions
/// </summary>
public class SoundEffectLibrary {
	private Dictionary<string, SoundEffectDefinition> _definitions = new Dictionary<string, SoundEffectDefinition>();
	private SoundEffectGenerator _generator = new SoundEffectGenerator();

	public void LoadFromFile(string filepath) {
		string json = File.ReadAllText(filepath);
		Dictionary<string, SoundEffectDefinition> library = JsonSerializer.Deserialize<Dictionary<string, SoundEffectDefinition>>(json);

		if (library != null) {
			foreach (KeyValuePair<string, SoundEffectDefinition> kvp in library) {
				_definitions[kvp.Key] = kvp.Value;
			}
		}

		System.Diagnostics.Debug.WriteLine($"[SFX] Loaded {_definitions.Count} sound effects");
	}

	public float[] Generate(string effectName) {
		if (_definitions.TryGetValue(effectName, out SoundEffectDefinition definition)) {
			return _generator.Generate(definition);
		}

		System.Diagnostics.Debug.WriteLine($"[SFX] Sound effect not found: {effectName}");
		return new float[0];
	}

	public bool HasEffect(string effectName) {
		return _definitions.ContainsKey(effectName);
	}
}