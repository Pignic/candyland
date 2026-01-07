
namespace EldmeresTale.Audio;

public enum MoodType {
	Normal,      // Baseline - no changes
	Hurry,       // Fast, urgent, high-pitched
	Tense,       // Slow, dark, ominous
	Happy,       // Upbeat, bright, energetic
	Sad,         // Slow, quiet, mellow
	Triumphant,  // Victory, powerful, bright
	Mysterious   // Subtle, filtered, curious
}

public class MoodConfig {
	public double TempoMultiplier { get; set; } = 1.0;
	public int PitchShift { get; set; } = 0; // Semitones
	public double VolumeMultiplier { get; set; } = 1.0;
	public double WaveformMorph { get; set; } = 0.0; // 0.0 = original, 1.0 = morphed

	public static MoodConfig GetConfig(MoodType mood) {
		return mood switch {
			MoodType.Normal => new MoodConfig {
				TempoMultiplier = 1.0,
				PitchShift = 0,
				VolumeMultiplier = 1.0,
				WaveformMorph = 0.0
			},

			MoodType.Hurry => new MoodConfig {
				TempoMultiplier = 1.5,      // 50% faster
				PitchShift = 2,             // Up 2 semitones (brighter)
				VolumeMultiplier = 1.1,     // Slightly louder
				WaveformMorph = 0.3         // Add some harshness
			},

			MoodType.Tense => new MoodConfig {
				TempoMultiplier = 0.85,     // 15% slower
				PitchShift = -3,            // Down 3 semitones (darker)
				VolumeMultiplier = 0.8,     // Quieter
				WaveformMorph = 0.5         // Morph to darker waveforms
			},

			MoodType.Happy => new MoodConfig {
				TempoMultiplier = 1.2,      // 20% faster
				PitchShift = 1,             // Up 1 semitone
				VolumeMultiplier = 1.15,    // Louder, more energetic
				WaveformMorph = -0.2        // Slightly softer (negative = toward sine)
			},

			MoodType.Sad => new MoodConfig {
				TempoMultiplier = 0.75,     // 25% slower
				PitchShift = -2,            // Down 2 semitones
				VolumeMultiplier = 0.7,     // Quieter
				WaveformMorph = -0.4        // Morph to softer waveforms
			},

			MoodType.Triumphant => new MoodConfig {
				TempoMultiplier = 1.15,     // Slightly faster
				PitchShift = 3,             // Up 3 semitones (bright!)
				VolumeMultiplier = 1.3,     // Much louder
				WaveformMorph = 0.4         // Brighter, more aggressive
			},

			MoodType.Mysterious => new MoodConfig {
				TempoMultiplier = 0.9,      // Slightly slower
				PitchShift = -1,            // Down 1 semitone
				VolumeMultiplier = 0.85,    // Quieter
				WaveformMorph = -0.3        // Softer waveforms
			},

			_ => new MoodConfig()
		};
	}
}
