using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EldmeresTale.Audio;

/// <summary>
/// Parses .music text files into Song objects
/// </summary>
public static class MusicParser {

	// Note name to semitone offset (C = 0)
	private static readonly Dictionary<string, int> NoteOffsets = new Dictionary<string, int> {
		{ "C", 0 }, { "C#", 1 }, { "Db", 1 },
		{ "D", 2 }, { "D#", 3 }, { "Eb", 3 },
		{ "E", 4 },
		{ "F", 5 }, { "F#", 6 }, { "Gb", 6 },
		{ "G", 7 }, { "G#", 8 }, { "Ab", 8 },
		{ "A", 9 }, { "A#", 10 }, { "Bb", 10 },
		{ "B", 11 }
	};

	public static Song ParseFile(string filepath) {
		if(!File.Exists(filepath)) {
			System.Diagnostics.Debug.WriteLine($"[MUSIC] File not found: {filepath}");
			return null;
		}

		string[] lines = File.ReadAllLines(filepath);
		return Parse(lines);
	}

	public static Song Parse(string[] lines) {
		Song song = new Song();

		// Current pattern being built
		Dictionary<int, List<string[]>> channelPatterns = new Dictionary<int, List<string[]>>();
		int currentChannel = -1;  // Track which channel we're currently filling

		for(int i = 0; i < lines.Length; i++) {
			string line = lines[i];

			// Skip empty lines
			if(string.IsNullOrWhiteSpace(line)) {
				continue;
			}

			string trimmedLine = line.Trim();

			// Skip comments
			if(trimmedLine.StartsWith("#") || trimmedLine.StartsWith("//")) {
				continue;
			}

			// Parse header commands
			if(trimmedLine.StartsWith("tempo ")) {
				song.Tempo = int.Parse(trimmedLine.Substring(6).Trim());
			} else if(trimmedLine == "loop") {
				song.Loop = true;
			} else if(trimmedLine.StartsWith("scale ")) {
				song.Scale = trimmedLine.Substring(6).Trim();
			} else if(trimmedLine.StartsWith("range ")) {
				ParseChannelRange(trimmedLine, song);
			} else if(trimmedLine.StartsWith("volume ")) {
				ParseVolume(trimmedLine, song);
			} else if(trimmedLine.StartsWith("pan ")) {
				ParsePan(trimmedLine, song);
			} else if(trimmedLine.StartsWith("envelope ")) {
				ParseEnvelope(trimmedLine, song);
			}
			  // Parse note patterns
			  else if(trimmedLine.StartsWith("ch")) {
				int colonIndex = trimmedLine.IndexOf(':');
				if(colonIndex > 0) {
					// Extract channel number
					string chPart = trimmedLine.Substring(0, colonIndex).Trim();
					int chNum = int.Parse(chPart.Substring(2)); // "ch0" -> 0

					// Set as current channel
					currentChannel = chNum;

					// Extract note pattern (if any on same line)
					string pattern = trimmedLine.Substring(colonIndex + 1).Trim();

					// Initialize channel pattern list if needed
					if(!channelPatterns.ContainsKey(chNum)) {
						channelPatterns[chNum] = new List<string[]>();
					}

					// Only add notes if pattern is not empty
					if(!string.IsNullOrWhiteSpace(pattern)) {
						string[] notes = pattern.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
						if(notes.Length > 0) {
							channelPatterns[chNum].Add(notes);
						}
					}
				}
			} else if(currentChannel >= 0) {
				string lineWithoutComments = trimmedLine;

				// Method 1: Split first, then check each token
				string[] tokens = trimmedLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				List<string> validTokens = new List<string>();

				foreach(string token in tokens) {
					// Stop if we hit a comment token
					if(token.StartsWith("#") || token.StartsWith("//")) {
						break; // Everything after this is a comment
					}
					validTokens.Add(token);
				}

				// Skip if no valid tokens
				if(validTokens.Count == 0) {
					continue;
				}

				// Rebuild line from valid tokens
				string[] notes = validTokens.ToArray();

				if(channelPatterns.ContainsKey(currentChannel)) {
					channelPatterns[currentChannel].Add(notes);
				}
			}
		}

		// Convert patterns to notes
		ConvertPatternsToNotes(channelPatterns, song);

		System.Diagnostics.Debug.WriteLine($"[MUSIC] Parsed: {song.Tempo} BPM, {song.Channels.Count} channels, {song.Notes.Count} notes");

		return song;
	}

	private static void ParseChannelRange(string line, Song song) {
		// Format: range ch0:square C4 A5
		string[] parts = line.Substring(6).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if(parts.Length < 3) return;

		// Parse "ch0:square"
		string[] chParts = parts[0].Split(':');
		int chNum = int.Parse(chParts[0].Substring(2)); // "ch0" -> 0

		Waveform waveform = Waveform.Square;
		if(chParts.Length > 1) {
			Enum.TryParse(chParts[1], true, out waveform);
		}

		Channel channel = new Channel(chNum, waveform) {
			MinNote = parts[1],
			MaxNote = parts[2]
		};

		song.Channels.Add(channel);

		System.Diagnostics.Debug.WriteLine($"[MUSIC] Channel {chNum}: {waveform}, range {parts[1]}-{parts[2]}");
	}

	private static void ParseVolume(string line, Song song) {
		// Format: volume ch0 0.8
		string[] parts = line.Substring(7).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if(parts.Length < 2) return;

		int chNum = int.Parse(parts[0].Substring(2));
		float volume = float.Parse(parts[1]);

		Channel ch = song.Channels.FirstOrDefault(c => c.Id == chNum);
		if(ch != null) {
			ch.Volume = volume;
		}
	}

	private static void ParsePan(string line, Song song) {
		// Format: pan ch0 0.3 0.7
		string[] parts = line.Substring(4).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if(parts.Length < 3) return;

		int chNum = int.Parse(parts[0].Substring(2));
		float left = float.Parse(parts[1]);
		float right = float.Parse(parts[2]);

		Channel ch = song.Channels.FirstOrDefault(c => c.Id == chNum);
		if(ch != null) {
			ch.PanL = left;
			ch.PanR = right;
		}
	}

	private static void ParseEnvelope(string line, Song song) {
		// Format: envelope ch0 0.01 0.1 0.7 0.2
		string[] parts = line.Substring(9).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if(parts.Length < 5) return;

		int chNum = int.Parse(parts[0].Substring(2));
		float attack = float.Parse(parts[1]);
		float decay = float.Parse(parts[2]);
		float sustain = float.Parse(parts[3]);
		float release = float.Parse(parts[4]);

		Channel ch = song.Channels.FirstOrDefault(c => c.Id == chNum);
		if(ch != null) {
			ch.Envelope = new ADSREnvelope(attack, decay, sustain, release);
		}
	}

	private static void ConvertPatternsToNotes(Dictionary<int, List<string[]>> patterns, Song song) {
		float maxBeat = 0f;

		foreach(var kvp in patterns) {
			int channelId = kvp.Key;
			List<string[]> lines = kvp.Value;

			float currentBeat = 0f;

			foreach(string[] noteLine in lines) {
				for(int i = 0; i < noteLine.Length; i++) {
					string noteStr = noteLine[i];
					float beatPosition = currentBeat + i;

					// Skip rests
					if(noteStr == ".") {
						continue;
					}

					// Handle holds (-)
					if(noteStr == "-") {
						// Extend previous note duration
						Note prevNote = song.Notes.LastOrDefault(n => n.ChannelId == channelId);
						if(prevNote != null) {
							prevNote.DurationBeats += 1f;
						}
						continue;
					}

					// Parse velocity and effects from note string
					float velocity = 1.0f;
					bool hasVibrato = false;
					bool hasPortamento = false;
					string pitchStr = noteStr;

					// For "C5!^~" the order is: ~(outermost) -> ^(middle) -> !(innermost)

					// Step 1: Parse vibrato (OUTERMOST - furthest from note)
					if(pitchStr.EndsWith("~")) {
						hasVibrato = true;
						pitchStr = pitchStr.Substring(0, pitchStr.Length - 1);
					}

					// Step 2: Parse portamento (MIDDLE)
					if(pitchStr.EndsWith("^")) {
						hasPortamento = true;
						pitchStr = pitchStr.Substring(0, pitchStr.Length - 1);
					}

					// Step 3: Parse velocity modifiers (INNERMOST - closest to note)
					if(pitchStr.EndsWith("''")) {
						velocity = 0.4f;
						pitchStr = pitchStr.Substring(0, pitchStr.Length - 2);
					} else if(pitchStr.EndsWith("'")) {
						velocity = 0.6f;
						pitchStr = pitchStr.Substring(0, pitchStr.Length - 1);
					} else if(pitchStr.EndsWith("!!")) {
						velocity = 2.0f;
						pitchStr = pitchStr.Substring(0, pitchStr.Length - 2);
					} else if(pitchStr.EndsWith("!")) {
						velocity = 1.5f;
						pitchStr = pitchStr.Substring(0, pitchStr.Length - 1);
					}

					// Create note
					Note note = new Note(channelId, pitchStr, beatPosition, 1f);
					note.Velocity = velocity;
					note.HasVibrato = hasVibrato;
					note.HasPortamento = hasPortamento;

					System.Diagnostics.Debug.WriteLine($"[PARSER] Channel {channelId}, Beat {beatPosition}: '{noteStr}' -> pitch '{pitchStr}', vel {velocity}");

					// Calculate frequency
					note.Frequency = NoteToFrequency(pitchStr);

					if(note.Frequency == 0f) {
						System.Diagnostics.Debug.WriteLine($"❌ [PARSER] INVALID NOTE: '{pitchStr}' at channel {channelId}, beat {beatPosition}");
						System.Diagnostics.Debug.WriteLine($"   Original: '{noteStr}'");
						System.Diagnostics.Debug.WriteLine($"   Frequency returned: {note.Frequency} Hz");
						continue; // Skip this note entirely
					}

					// If portamento, find target frequency (next note)
					if(hasPortamento) {
						// Look ahead for next note (skip rests and holds)
						for(int j = i + 1; j < noteLine.Length; j++) {
							string nextNoteStr = noteLine[j];
							if(nextNoteStr != "." && nextNoteStr != "-") {
								// Extract pitch from next note (remove all effects)
								string nextPitch = nextNoteStr
									.Replace("~", "")
									.Replace("^", "")
									.Replace("!", "")
									.Replace("'", "");
								note.TargetFrequency = NoteToFrequency(nextPitch);
								break;
							}
						}
					}

					song.Notes.Add(note);

					maxBeat = Math.Max(maxBeat, beatPosition + 1f);
				}

				// Move to next line (each column = 1 beat)
				currentBeat += noteLine.Length;
			}
		}

		song.TotalDurationBeats = maxBeat;

		System.Diagnostics.Debug.WriteLine($"[PARSER] Total notes created: {song.Notes.Count}");
	}

	/// <summary>
	/// Convert note name to frequency in Hz
	/// A4 = 440 Hz
	/// </summary>
	public static float NoteToFrequency(string note) {
		// Handle drum sounds (single char)
		if(note.Length == 1 && DrumMap.IsDrumSound(note[0])) {
			return DrumMap.Frequencies[note[0]];
		}

		// Parse musical note (e.g., "D4", "F#3", "Bb5")
		string noteName;
		int octave;

		if(note.Length == 2) {
			// "D4"
			noteName = note.Substring(0, 1);
			octave = int.Parse(note.Substring(1, 1));
		} else if(note.Length == 3) {
			// "D#4" or "Bb4"
			noteName = note.Substring(0, 2);
			octave = int.Parse(note.Substring(2, 1));
		} else {
			// ✅ ADD THIS DEBUG:
			System.Diagnostics.Debug.WriteLine($"❌ [FREQ] Invalid note length: '{note}' (len={note.Length})");
			return 0f; // Invalid - will be caught above
		}

		if(!NoteOffsets.ContainsKey(noteName)) {
			// ✅ ADD THIS DEBUG:
			System.Diagnostics.Debug.WriteLine($"❌ [FREQ] Unknown note name: '{noteName}' in '{note}'");
			return 0f;
		}

		// Calculate semitones from A4
		int semitonesFromA4 = (octave - 4) * 12 + (NoteOffsets[noteName] - 9); // A = 9

		// Frequency = 440 * 2^(semitones/12)
		float freq = 440f * (float)Math.Pow(2.0, semitonesFromA4 / 12.0);

		// ✅ ADD THIS DEBUG for valid notes:
		System.Diagnostics.Debug.WriteLine($"✅ [FREQ] '{note}' -> {freq:F2} Hz");

		return freq;
	}
}