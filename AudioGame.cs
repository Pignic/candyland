using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace EldmeresTale;

public class AudioGame : Game {
	GraphicsDeviceManager graphics;

	// === AUDIO CONFIG ===
	const int SampleRate = 44100;
	const int Channels = 2;
	const int SamplesPerBuffer = 1024;
	const double HatInterval = 2.0;
	const double HatLength = 2.5;

	DynamicSoundEffectInstance sound;
	byte[] buffer;

	long globalSample = 0;
	long lastHatSample = -999999;

	Random rng = new Random();

	// Metallic partials (inharmonic on purpose)
	readonly double[] partials =
	{
		4217,  5623,  7349,
		9031, 11297, 13331
	};

	double[] partialPhase;

	public AudioGame() {
		graphics = new GraphicsDeviceManager(this);
		Content.RootDirectory = "Content";
		IsMouseVisible = true;

		graphics.PreferredBackBufferWidth = 640;
		graphics.PreferredBackBufferHeight = 360;
	}

	protected override void Initialize() {
		base.Initialize();

		buffer = new byte[SamplesPerBuffer * Channels * sizeof(short)];
		partialPhase = new double[partials.Length];

		sound = new DynamicSoundEffectInstance(
			SampleRate,
			AudioChannels.Stereo);

		sound.BufferNeeded += OnBufferNeeded;
		sound.Play();
	}

	void OnBufferNeeded(object sender, EventArgs e) {
		GenerateAudio(buffer);
		sound.SubmitBuffer(buffer);
	}

	// Exponential open-hat envelope
	double HatEnvelope(double t) {
		if(t < 0.002) return t / 0.002;      // tiny attack
		return Math.Exp(-t * 1.3);            // long decay
	}

	void GenerateAudio(byte[] buffer) {
		int index = 0;

		for(int i = 0; i < SamplesPerBuffer; i++) {
			double time = globalSample / (double)SampleRate;

			// Trigger hat
			if(globalSample - lastHatSample >
				HatInterval * SampleRate) {
				lastHatSample = globalSample;

				// Randomize phases slightly per hit
				for(int p = 0; p < partialPhase.Length; p++)
					partialPhase[p] = rng.NextDouble() * Math.PI * 2;
			}

			double localTime =
				(globalSample - lastHatSample) / (double)SampleRate;

			double sample = 0.0;

			if(localTime >= 0 && localTime < HatLength) {
				double env = HatEnvelope(localTime);

				// High-passed noise (difference filter)
				double noise =
					(rng.NextDouble() * 2.0 - 1.0) -
					(rng.NextDouble() * 2.0 - 1.0) * 0.95;

				// Metallic partial cluster
				double metal = 0.0;
				for(int p = 0; p < partials.Length; p++) {
					partialPhase[p] +=
						partials[p] * 2.0 * Math.PI / SampleRate;
					metal += Math.Sin(partialPhase[p]);
				}

				metal /= partials.Length;

				sample =
					(noise * 0.65 + metal * 0.35) * env;
			}

			sample = Math.Clamp(sample, -1.0, 1.0);
			short pcm = (short)(sample * short.MaxValue);

			// Stereo interleaved
			buffer[index++] = (byte)(pcm & 0xFF);
			buffer[index++] = (byte)((pcm >> 8) & 0xFF);
			buffer[index++] = (byte)(pcm & 0xFF);
			buffer[index++] = (byte)((pcm >> 8) & 0xFF);

			globalSample++;
		}
	}

	protected override void Draw(GameTime gameTime) {
		GraphicsDevice.Clear(Color.Black);
		base.Draw(gameTime);
	}
}