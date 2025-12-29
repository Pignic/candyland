using System;
using System.Collections.Generic;

namespace EldmeresTale.Audio;

/// <summary>
/// Waveform types for synthesis
/// </summary>
public enum Waveform {
	Square,
	Triangle,
	Sawtooth,
	Sine,
	Noise
}

/// <summary>
/// ADSR Envelope for shaping sound
/// Attack, Decay, Sustain, Release
/// </summary>
public struct ADSREnvelope {
	public float Attack;   // Time to reach peak (seconds)
	public float Decay;    // Time to reach sustain level (seconds)
	public float Sustain;  // Sustain level (0.0 to 1.0)
	public float Release;  // Time to fade out (seconds)

	public ADSREnvelope(float attack, float decay, float sustain, float release) {
		Attack = attack;
		Decay = decay;
		Sustain = sustain;
		Release = release;
	}

	// Default envelope (instant attack, no decay, full sustain, quick release)
	public static ADSREnvelope Default => new ADSREnvelope(0.01f, 0.05f, 0.8f, 0.1f);
}

/// <summary>
/// Musical note with pitch and timing
/// </summary>
public class Note {
	public int ChannelId { get; set; }
	public string Pitch { get; set; }  // "D4", "F#3", "k" (for drums)
	public float StartBeat { get; set; }  // When note starts (in beats)
	public float DurationBeats { get; set; }  // How long it plays (in beats)

	// Calculated frequency (Hz) - set by parser
	public float Frequency { get; set; }

	// NEW: Expression and effects
	public float Velocity { get; set; } = 1.0f;  // Volume multiplier (0.0 to 2.0)
	public bool HasVibrato { get; set; } = false;
	public bool HasPortamento { get; set; } = false;
	public float TargetFrequency { get; set; } = 0f;  // For portamento

	public Note(int channelId, string pitch, float startBeat, float durationBeats) {
		ChannelId = channelId;
		Pitch = pitch;
		StartBeat = startBeat;
		DurationBeats = durationBeats;
		Frequency = 0f;
	}
}

/// <summary>
/// Audio channel configuration
/// </summary>
public class Channel {
	public int Id { get; set; }
	public Waveform Type { get; set; }

	// Note range (for melodic channels)
	public string MinNote { get; set; }  // "C4"
	public string MaxNote { get; set; }  // "A5"

	// Volume and panning
	public float VolumeL { get; set; } = 0.5f;
	public float VolumeR { get; set; } = 0.5f;

	// Envelope
	public ADSREnvelope Envelope { get; set; } = ADSREnvelope.Default;

	public Channel(int id, Waveform type) {
		Id = id;
		Type = type;
	}
}

/// <summary>
/// Complete song with all data
/// </summary>
public class Song {
	public int Tempo { get; set; } = 120;  // BPM
	public bool Loop { get; set; } = false;
	public string Scale { get; set; } = "C major";

	public List<Channel> Channels { get; set; } = new List<Channel>();
	public List<Note> Notes { get; set; } = new List<Note>();

	// Calculated values
	public float BeatsPerSecond => Tempo / 60f;
	public float SecondsPerBeat => 60f / Tempo;
	public float TotalDurationBeats { get; set; }
	public float TotalDurationSeconds => TotalDurationBeats * SecondsPerBeat;

	public Song() { }
}

/// <summary>
/// Drum sound mappings
/// </summary>
public static class DrumMap {
	// Frequency/noise patterns for drums
	public static readonly Dictionary<char, float> Frequencies = new Dictionary<char, float> {
		{ 'k', 60f },    // Kick - low frequency
		{ 's', 200f },   // Snare - mid frequency
		{ 'h', 8000f },  // Hi-hat - high frequency
		{ 'o', 10000f }, // Open hi-hat - very high
		{ 'c', 12000f }, // Crash - very high
		{ 'r', 6000f },  // Ride - high
		{ 't', 150f },   // Tom - mid-low
	};

	public static bool IsDrumSound(char c) {
		return Frequencies.ContainsKey(c);
	}
}