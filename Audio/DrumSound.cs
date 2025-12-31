using System;

namespace EldmeresTale.Audio;

/// <summary>
/// Base class for drum sounds with built-in envelopes and synthesis
/// </summary>
public abstract class DrumSound {
	public ADSREnvelope Envelope { get; protected set; }
	protected Random Random { get; private set; }

	public DrumSound(Random random) {
		Random = random;
	}

	/// <summary>
	/// Generate a single sample for this drum sound
	/// </summary>
	/// <param name="noteTime">Time since the note started (in seconds)</param>
	/// <param name="lastNoiseSample">Filter state (passed by ref)</param>
	/// <returns>Audio sample value (-1.0 to 1.0)</returns>
	public abstract double GenerateSample(double noteTime, ref double lastNoiseSample);

	/// <summary>
	/// Apply the drum's envelope to the given sample
	/// </summary>
	public double ApplyEnvelope(double sample, double noteTime, double noteDuration, bool isStopping, double timeStoppedAt, double currentTime) {
		if(noteTime < 0.0) return 0.0;

		// If note is stopping, use release envelope
		if(isStopping) {
			double releaseTime = currentTime - timeStoppedAt;
			if(releaseTime < Envelope.Release) {
				// Get the envelope value at the moment it stopped
				double stoppedValue;
				double timeAtStop = timeStoppedAt - (currentTime - noteTime);

				if(timeAtStop < Envelope.Attack) {
					stoppedValue = timeAtStop / Envelope.Attack;
				} else if(timeAtStop < Envelope.Attack + Envelope.Decay) {
					double decayProgress = (timeAtStop - Envelope.Attack) / Envelope.Decay;
					stoppedValue = 1.0 + (Envelope.Sustain - 1.0) * decayProgress;
				} else {
					stoppedValue = Envelope.Sustain;
				}

				// Release from that value
				return sample * stoppedValue * (1.0 - releaseTime / Envelope.Release);
			}
			return 0.0;
		}

		// Normal ADSR (not stopping)
		// Attack phase
		if(noteTime < Envelope.Attack) {
			return sample * (noteTime / Envelope.Attack);
		}

		// Decay phase
		if(noteTime < Envelope.Attack + Envelope.Decay) {
			double decayProgress = (noteTime - Envelope.Attack) / Envelope.Decay;
			return sample * (1.0 + (Envelope.Sustain - 1.0) * decayProgress);
		}

		// Sustain phase
		return sample * Envelope.Sustain;
	}
}

/// <summary>
/// Kick drum - deep bass thump
/// </summary>
public class KickDrum : DrumSound {
	public KickDrum(Random random) : base(random) {
		// Very short attack, medium decay, no sustain, short release
		Envelope = new ADSREnvelope(0.001f, 0.15f, 0.0f, 0.05f);
	}

	public override double GenerateSample(double noteTime, ref double lastNoiseSample) {
		// Generate white noise
		double rawNoise = Random.NextDouble() * 2.0 - 1.0;

		// Heavy filtering (97% old + 3% new) for deep bass character
		rawNoise = lastNoiseSample * 0.97 + rawNoise * 0.03;
		lastNoiseSample = rawNoise;

		// Add pitched "thump" that decays from 65Hz down
		double kickFreq = 65.0 * Math.Exp(-noteTime * 15.0);
		double kickTone = Math.Sin(noteTime * 2.0 * Math.PI * kickFreq);

		// Mix: 20% noise + 80% tone for deep punch
		return rawNoise * 0.2 + kickTone * 0.8;
	}
}

/// <summary>
/// Snare drum - crisp crack with body
/// </summary>
public class SnareDrum : DrumSound {
	public SnareDrum(Random random) : base(random) {
		// Instant attack, short decay, low sustain, medium release
		Envelope = new ADSREnvelope(0.001f, 0.05f, 0.2f, 0.08f);
	}

	public override double GenerateSample(double noteTime, ref double lastNoiseSample) {
		// Generate white noise
		double rawNoise = Random.NextDouble() * 2.0 - 1.0;

		// Medium filtering (65% old + 35% new) for snare character
		rawNoise = lastNoiseSample * 0.65 + rawNoise * 0.35;
		lastNoiseSample = rawNoise;

		// Add slight tone for snare "buzz" - pitch drops slightly
		double snareFreq = 200.0 * Math.Exp(-noteTime * 8.0);
		double snareTone = Math.Sin(noteTime * 2.0 * Math.PI * snareFreq);

		// Mix: 85% noise + 15% tone
		return rawNoise * 0.85 + snareTone * 0.15;
	}
}

/// <summary>
/// Tom drum - mid-range thud
/// </summary>
public class TomDrum : DrumSound {
	public TomDrum(Random random) : base(random) {
		// Short attack, medium decay, low sustain, medium release
		Envelope = new ADSREnvelope(0.001f, 0.1f, 0.15f, 0.1f);
	}

	public override double GenerateSample(double noteTime, ref double lastNoiseSample) {
		// Generate white noise
		double rawNoise = Random.NextDouble() * 2.0 - 1.0;

		// Moderate filtering (60% old + 40% new)
		rawNoise = lastNoiseSample * 0.6 + rawNoise * 0.4;
		lastNoiseSample = rawNoise;

		// Add decaying tone
		double tomFreq = 180.0 * Math.Exp(-noteTime * 12.0);
		double tomTone = Math.Sin(noteTime * 2.0 * Math.PI * tomFreq);

		// Mix: 40% noise + 60% tone
		return rawNoise * 0.4 + tomTone * 0.6;
	}
}

/// <summary>
/// Closed hi-hat - short, crisp metallic sound
/// </summary>
public class ClosedHiHat : DrumSound {
	private readonly double[] partials = { 4217, 5623, 7349, 9031, 11297, 13331 };
	private double[] partialPhase;

	public ClosedHiHat(Random random) : base(random) {
		// Instant attack, very short decay, no sustain, short release
		Envelope = new ADSREnvelope(0.001f, 0.05f, 0.0f, 0.05f);
		partialPhase = new double[partials.Length];

		// Initialize with random phases
		for(int i = 0; i < partialPhase.Length; i++) {
			partialPhase[i] = random.NextDouble() * Math.PI * 2.0;
		}
	}

	public override double GenerateSample(double noteTime, ref double lastNoiseSample) {
		// High-passed noise (difference filter)
		double noise1 = Random.NextDouble() * 2.0 - 1.0;
		double noise2 = Random.NextDouble() * 2.0 - 1.0;
		double noise = noise1 - noise2 * 0.95;

		// Metallic partial cluster (inharmonic)
		double metal = 0.0;
		for(int p = 0; p < partials.Length; p++) {
			partialPhase[p] += partials[p] * 2.0 * Math.PI / 44100.0;
			metal += Math.Sin(partialPhase[p]);
		}
		metal /= partials.Length;

		// Mix: 65% noise + 35% metallic partials
		return noise * 0.65 + metal * 0.35;
	}
}

/// <summary>
/// Open hi-hat - longer, sustained metallic ring
/// </summary>
public class OpenHiHat : DrumSound {
	private readonly double[] partials = { 4217, 5623, 7349, 9031, 11297, 13331 };
	private double[] partialPhase;

	public OpenHiHat(Random random) : base(random) {
		// Instant attack, short decay, low sustain, LONG release for ring
		Envelope = new ADSREnvelope(0.001f, 0.1f, 0.3f, 1.5f);
		partialPhase = new double[partials.Length];

		// Initialize with random phases
		for(int i = 0; i < partialPhase.Length; i++) {
			partialPhase[i] = random.NextDouble() * Math.PI * 2.0;
		}
	}

	public override double GenerateSample(double noteTime, ref double lastNoiseSample) {
		// High-passed noise (difference filter)
		double noise1 = Random.NextDouble() * 2.0 - 1.0;
		double noise2 = Random.NextDouble() * 2.0 - 1.0;
		double noise = noise1 - noise2 * 0.95;

		// Metallic partial cluster (inharmonic)
		double metal = 0.0;
		for(int p = 0; p < partials.Length; p++) {
			partialPhase[p] += partials[p] * 2.0 * Math.PI / 44100.0;
			metal += Math.Sin(partialPhase[p]);
		}
		metal /= partials.Length;

		// Mix: 65% noise + 35% metallic partials
		return noise * 0.65 + metal * 0.35;
	}
}

/// <summary>
/// Crash cymbal - bright, shimmery sustain
/// </summary>
public class CrashCymbal : DrumSound {
	public CrashCymbal(Random random) : base(random) {
		// Instant attack, medium decay, medium sustain, long release
		Envelope = new ADSREnvelope(0.001f, 0.2f, 0.4f, 1.0f);
	}

	public override double GenerateSample(double noteTime, ref double lastNoiseSample) {
		// Generate white noise
		double rawNoise = Random.NextDouble() * 2.0 - 1.0;

		// Minimal filtering (12% old + 88% new) - very bright
		rawNoise = lastNoiseSample * 0.12 + rawNoise * 0.88;
		lastNoiseSample = rawNoise;

		return rawNoise;
	}
}

/// <summary>
/// Ride cymbal - metallic ping
/// </summary>
public class RideCymbal : DrumSound {
	public RideCymbal(Random random) : base(random) {
		// Instant attack, short decay, low sustain, medium release
		Envelope = new ADSREnvelope(0.001f, 0.1f, 0.25f, 0.4f);
	}

	public override double GenerateSample(double noteTime, ref double lastNoiseSample) {
		// Generate white noise
		double rawNoise = Random.NextDouble() * 2.0 - 1.0;

		// Light filtering (20% old + 80% new)
		rawNoise = lastNoiseSample * 0.2 + rawNoise * 0.8;
		lastNoiseSample = rawNoise;

		return rawNoise;
	}
}