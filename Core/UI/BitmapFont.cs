namespace Candyland.Core.UI;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

public class BitmapFont {
	private readonly Dictionary<char, bool[,]> characters;
	private readonly int charWidth = 5;
	private readonly int charHeight = 8;
	private readonly int scale = 1;
	private readonly Texture2D pixelTexture;
	private Dictionary<char, Texture2D> _characterTextures;
	private GraphicsDevice _graphicsDevice;

	public BitmapFont(GraphicsDevice graphicsDevice) {
		_graphicsDevice = graphicsDevice;
		pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
		pixelTexture.SetData([Color.White]);
		characters = new Dictionary<char, bool[,]>();
		_characterTextures = new Dictionary<char, Texture2D>();
		InitializeCharacters();
		GenerateCharacterTextures();
	}

	private void GenerateCharacterTextures() {
		foreach(var kvp in characters) {
			char c = kvp.Key;
			bool[,] charData = kvp.Value;

			// Create texture at base resolution
			Texture2D charTex = new Texture2D(_graphicsDevice, charWidth, charHeight);
			Color[] pixels = new Color[charWidth * charHeight];

			for(int y = 0; y < charHeight; y++) {
				for(int x = 0; x < charWidth; x++) {
					int index = y * charWidth + x;
					pixels[index] = charData[y, x] ? Color.White : Color.Transparent;
				}
			}

			charTex.SetData(pixels);
			_characterTextures[c] = charTex;
		}
	}

	private void InitializeCharacters() {
		// @formatter:off
		// UPPERCASE LETTERS
		characters['A'] = new bool[,] {
				{ false, false, true,  false, false },
				{ false, true,  false, true,  false },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, false, false, false, false }
			};

		characters['B'] = new bool[,] {
				{ true,  true,  true,  true,  false },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  true,  true,  true,  false },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  true,  true,  true,  false },
				{ false, false, false, false, false }
			};

		characters['C'] = new bool[,] {
				{ false, true,  true,  true,  true  },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ false, true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		characters['D'] = new bool[,] {
				{ true,  true,  true,  true,  false },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  true,  true,  true,  false },
				{ false, false, false, false, false }
			};

		characters['E'] = new bool[,] {
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		characters['F'] = new bool[,] {
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ false, false, false, false, false }
			};

		characters['G'] = new bool[,] {
				{ false, true,  true,  true,  true  },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  false, true,  true,  true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, true,  true,  true,  false },
				{ false, false, false, false, false }
			};

		characters['H'] = new bool[,] {
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, false, false, false, false }
			};

		characters['I'] = new bool[,] {
				{ true,  true,  true,  true,  true  },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		characters['J'] = new bool[,] {
				{ false, false, false, false, true  },
				{ false, false, false, false, true  },
				{ false, false, false, false, true  },
				{ false, false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, true,  true,  true,  false },
				{ false, false, false, false, false }
			};

		characters['K'] = new bool[,] {
				{ true,  false, false, false, true  },
				{ true,  false, false, true,  false },
				{ true,  false, true,  false, false },
				{ true,  true,  false, false, false },
				{ true,  false, true,  false, false },
				{ true,  false, false, true,  false },
				{ true,  false, false, false, true  },
				{ false, false, false, false, false }
			};

		characters['L'] = new bool[,] {
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		characters['M'] = new bool[,] {
				{ true,  false, false, false, true  },
				{ true,  true,  false, true,  true  },
				{ true,  false, true,  false, true  },
				{ true,  false, true,  false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, false, false, false, false }
			};

		characters['N'] = new bool[,] {
				{ true,  false, false, false, true  },
				{ true,  true,  false, false, true  },
				{ true,  true,  false, false, true  },
				{ true,  false, true,  false, true  },
				{ true,  false, false, true,  true  },
				{ true,  false, false, true,  true  },
				{ true,  false, false, false, true  },
				{ false, false, false, false, false }
			};

		characters['O'] = new bool[,] {
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		characters['P'] = new bool[,] {
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ false, false, false, false, false }
			};

		characters['Q'] = new bool[,] {
				{ false, true,  true,  true,  false },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, true,  false, true  },
				{ true,  false, false, true,  false },
				{ false, true,  true,  false, true  },
				{ false, false, false, false, false }
			};

		characters['R'] = new bool[,] {
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, true,  false },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, false, false, false, false }
			};

		characters['S'] = new bool[,] {
				{ false, true,  true,  true,  false},
				{ true,  false, false, false, true},
				{ true,  false, false, false, false},
				{ false, true,  true,  true,  false},
				{ false, false, false, false, true },
				{ true,  false, false, false, true },
				{ false, true,  true,  true,  false},
				{ false, false, false, false, false}
			};

		characters['T'] = new bool[,] {
				{ true,  true,  true,  true,  true  },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, false, false, false }
			};

		characters['U'] = new bool[,] {
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		characters['V'] = new bool[,] {
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, true,  false, true,  false },
				{ false, true,  false, true,  false },
				{ false, false, true,  false, false },
				{ false, false, false, false, false }
			};

		characters['W'] = new bool[,] {
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, true,  false, true  },
				{ true,  false, true,  false, true  },
				{ true,  true,  false, true,  true  },
				{ true,  false, false, false, true  },
				{ false, false, false, false, false }
			};

		characters['X'] = new bool[,] {
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, true,  false, true,  false },
				{ false, false, true,  false, false },
				{ false, true,  false, true,  false },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, false, false, false, false }
			};

		characters['Y'] = new bool[,] {
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, true,  false, true,  false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, false, false, false }
			};

		characters['Z'] = new bool[,] {
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, true  },
				{ false, false, false, true,  false },
				{ false, false, true,  false, false },
				{ false, true,  false, false, false },
				{ true,  false, false, false, false },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		characters['a'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, true,  true,  true,  false },
				{ false, false, false, false, true  },
				{ false, true,  true,  true,  true  },
				{ true,  false, false, false, true  },
				{ false, true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		characters['b'] = new bool[,] {
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  true,  true,  true,  false },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  true,  true,  true,  false },
				{ false, false, false, false, false }
			};

		characters['c'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, true,  true,  true,  false },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ false, true,  true,  true,  false },
				{ false, false, false, false, false }
			};

		characters['d'] = new bool[,] {
				{ false, false, false, false, true  },
				{ false, false, false, false, true  },
				{ false, true,  true,  true,  true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		characters['e'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, true,  true,  true,  false },
				{ true,  false, false, false, true  },
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, false },
				{ false, true,  true,  true,  false },
				{ false, false, false, false, false }
			};

		characters['f'] = new bool[,] {
				{ false, false, true,  true,  false },
				{ false, true,  false, false, false },
				{ false, true,  false, false, false },
				{ true,  true,  true,  true,  false },
				{ false, true,  false, false, false },
				{ false, true,  false, false, false },
				{ false, true,  false, false, false },
				{ false, false, false, false, false }
			};

		characters['g'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, true,  true,  true,  true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, true,  true,  true,  true  },
				{ false, false, false, false, true  },
				{ true,  true,  true,  true,  false }  // descender
            };

		characters['h'] = new bool[,] {
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  true,  true,  true,  false },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, false, false, false, false }
			};

		characters['i'] = new bool[,] {
				{ false, false, true,  false, false },
				{ false, false, false, false, false },
				{ false, true,  true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, true,  true,  true,  false },
				{ false, false, false, false, false }
			};

		characters['j'] = new bool[,] {
				{ false, false, false, true,  false },
				{ false, false, false, false, false },
				{ false, false, true,  true,  false },
				{ false, false, false, true,  false },
				{ false, false, false, true,  false },
				{ false, false, false, true,  false },
				{ true,  false, false, true,  false },
				{ false, true,  true,  false, false }  // descender
            };

		characters['k'] = new bool[,] {
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  false, false, true,  false },
				{ true,  false, true,  false, false },
				{ true,  true,  false, false, false },
				{ true,  false, true,  false, false },
				{ true,  false, false, true,  false },
				{ false, false, false, false, false }
			};

		characters['l'] = new bool[,] {
				{ false, true,  true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, true,  true,  true,  false },
				{ false, false, false, false, false }
			};

		characters['m'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ true,  true,  false, true,  false },
				{ true,  false, true,  false, true  },
				{ true,  false, true,  false, true  },
				{ true,  false, true,  false, true  },
				{ true,  false, true,  false, true  },
				{ false, false, false, false, false }
			};

		characters['n'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ true,  true,  true,  true,  false },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, false, false, false, false }
			};

		characters['o'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, true,  true,  true,  false },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, true,  true,  true,  false },
				{ false, false, false, false, false }
			};

		characters['p'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ true,  true,  true,  true,  false },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  true,  true,  true,  false },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false }  // descender
            };

		characters['q'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, true,  true,  true,  true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, true,  true,  true,  true  },
				{ false, false, false, false, true  },
				{ false, false, false, false, true  }  // descender
            };

		characters['r'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ true,  false, true,  true,  false },
				{ true,  true,  false, false, true  },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ false, false, false, false, false }
			};

		characters['s'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, true,  true,  true,  false },
				{ true,  false, false, false, false },
				{ false, true,  true,  true,  false },
				{ false, false, false, false, true  },
				{ true,  true,  true,  true,  false },
				{ false, false, false, false, false }
			};

		characters['t'] = new bool[,] {
				{ false, true,  false, false, false },
				{ false, true,  false, false, false },
				{ true,  true,  true,  true,  false },
				{ false, true,  false, false, false },
				{ false, true,  false, false, false },
				{ false, true,  false, false, false },
				{ false, false, true,  true,  false },
				{ false, false, false, false, false }
			};

		characters['u'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		characters['v'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, true,  false, true,  false },
				{ false, false, true,  false, false },
				{ false, false, false, false, false }
			};

		characters['w'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, true,  false, true  },
				{ true,  false, true,  false, true  },
				{ false, true,  false, true,  false },
				{ false, false, false, false, false }
			};

		characters['x'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ true,  false, false, false, true  },
				{ false, true,  false, true,  false },
				{ false, false, true,  false, false },
				{ false, true,  false, true,  false },
				{ true,  false, false, false, true  },
				{ false, false, false, false, false }
			};

		characters['y'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ false, true,  true,  true,  true  },
				{ false, false, false, false, true  },
				{ true,  true,  true,  true,  false }  // descender
            };

		characters['z'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, true,  false },
				{ false, false, true,  false, false },
				{ false, true,  false, false, false },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		// NUMBERS
		characters['0'] = new bool[,] {
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		characters['1'] = new bool[,] {
				{ false, false, true,  false, false },
				{ false, true,  true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, true,  true,  true,  false },
				{ false, false, false, false, false }
			};

		characters['2'] = new bool[,] {
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, true  },
				{ false, false, false, false, true  },
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		characters['3'] = new bool[,] {
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, true  },
				{ false, false, false, false, true  },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, true  },
				{ false, false, false, false, true  },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		characters['4'] = new bool[,] {
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, true  },
				{ false, false, false, false, true  },
				{ false, false, false, false, true  },
				{ false, false, false, false, false }
			};

		characters['5'] = new bool[,] {
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, true  },
				{ false, false, false, false, true  },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		characters['6'] = new bool[,] {
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, false },
				{ true,  false, false, false, false },
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		characters['7'] = new bool[,] {
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, true  },
				{ false, false, false, false, true  },
				{ false, false, false, true,  false },
				{ false, false, true,  false, false },
				{ false, true,  false, false, false },
				{ false, true,  false, false, false },
				{ false, false, false, false, false }
			};

		characters['8'] = new bool[,] {
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		characters['9'] = new bool[,] {
				{ true,  true,  true,  true,  true  },
				{ true,  false, false, false, true  },
				{ true,  false, false, false, true  },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, true  },
				{ false, false, false, false, true  },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		// PUNCTUATION & SYMBOLS
		characters[' '] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false }
			};

		characters['.'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, false, false, false }
			};

		characters[','] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, true,  false, false },
				{ false, true,  false, false, false },
				{ false, false, false, false, false }
			};

		characters['!'] = new bool[,] {
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, false, false, false },
				{ false, false, true,  false, false },
				{ false, false, false, false, false }
			};

		characters['?'] = new bool[,] {
				{ false, true,  true,  true,  false },
				{ true,  false, false, false, true  },
				{ false, false, false, false, true  },
				{ false, false, false, true,  false },
				{ false, false, true,  false, false },
				{ false, false, false, false, false },
				{ false, false, true,  false, false },
				{ false, false, false, false, false }
			};

		characters[':'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, false, false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false }
			};

		characters[';'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, false, false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, true,  false, false, false },
				{ false, false, false, false, false }
			};

		characters['-'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false }
			};

		characters['_'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ true,  true,  true,  true,  true  }
			};

		characters['+'] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ true,  true,  true,  true,  true  },
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false }
			};

		characters['='] = new bool[,] {
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, false },
				{ true,  true,  true,  true,  true  },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false }
			};

		characters['/'] = new bool[,] {
				{ false, false, false, false, true  },
				{ false, false, false, true,  false },
				{ false, false, false, true,  false },
				{ false, false, true,  false, false },
				{ false, true,  false, false, false },
				{ false, true,  false, false, false },
				{ true,  false, false, false, false },
				{ false, false, false, false, false }
			};

		characters['\\'] = new bool[,] {
				{ true,  false, false, false, false },
				{ false, true,  false, false, false },
				{ false, true,  false, false, false },
				{ false, false, true,  false, false },
				{ false, false, false, true,  false },
				{ false, false, false, true,  false },
				{ false, false, false, false, true  },
				{ false, false, false, false, false }
			};

		characters['('] = new bool[,] {
				{ false, false, true,  false, false },
				{ false, true,  false, false, false },
				{ false, true,  false, false, false },
				{ false, true,  false, false, false },
				{ false, true,  false, false, false },
				{ false, true,  false, false, false },
				{ false, false, true,  false, false },
				{ false, false, false, false, false }
			};

		characters[')'] = new bool[,] {
				{ false, false, true,  false, false },
				{ false, false, false, true,  false },
				{ false, false, false, true,  false },
				{ false, false, false, true,  false },
				{ false, false, false, true,  false },
				{ false, false, false, true,  false },
				{ false, false, true,  false, false },
				{ false, false, false, false, false }
			};

		characters['['] = new bool[,] {
				{ false, true,  true,  false, false },
				{ false, true,  false, false, false },
				{ false, true,  false, false, false },
				{ false, true,  false, false, false },
				{ false, true,  false, false, false },
				{ false, true,  false, false, false },
				{ false, true,  true,  false, false },
				{ false, false, false, false, false }
			};

		characters[']'] = new bool[,] {
				{ false, false, true,  true,  false },
				{ false, false, false, true,  false },
				{ false, false, false, true,  false },
				{ false, false, false, true,  false },
				{ false, false, false, true,  false },
				{ false, false, false, true,  false },
				{ false, false, true,  true,  false },
				{ false, false, false, false, false }
			};

		characters['\''] = new bool[,] {
				{ false, false, true,  false, false },
				{ false, false, true,  false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false }
			};

		characters['"'] = new bool[,] {
				{ false, true,  false, true,  false },
				{ false, true,  false, true,  false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false },
				{ false, false, false, false, false }
			};

		characters['<'] = new bool[,] {
				{ false, false, false, false, true  },
				{ false, false, false, true,  false },
				{ false, false, true,  false, false },
				{ false, true,  false, false, false },
				{ false, false, true,  false, false },
				{ false, false, false, true,  false },
				{ false, false, false, false, true  },
				{ false, false, false, false, false }
			};

		characters['>'] = new bool[,] {
				{ true,  false, false, false, false },
				{ false, true,  false, false, false },
				{ false, false, true,  false, false },
				{ false, false, false, true,  false },
				{ false, false, true,  false, false },
				{ false, true,  false, false, false },
				{ true,  false, false, false, false },
				{ false, false, false, false, false }
			};

		characters['*'] = new bool[,] {
				{ false, false, true,  false, false },
				{ true,  false, true,  false, true  },
				{ false, true,  true,  true,  false },
				{ false, false, true,  false, false },
				{ false, true,  true,  true,  false },
				{ true,  false, true,  false, true  },
				{ false, false, true,  false, false },
				{ false, false, false, false, false }
			};

		characters['%'] = new bool[,] {
				{ true,  true,  false, false, true  },
				{ true,  true,  false, true,  false },
				{ false, false, true,  false, false },
				{ false, true,  false, false, false },
				{ false, true,  false, false, false },
				{ true,  false, true,  true,  false },
				{ true,  false, false, true,  true  },
				{ false, false, false, false, false }
			};

		characters['#'] = new bool[,] {
				{ false, true,  false, true,  false },
				{ false, true,  false, true,  false },
				{ true,  true,  true,  true,  true  },
				{ false, true,  false, true,  false },
				{ true,  true,  true,  true,  true  },
				{ false, true,  false, true,  false },
				{ false, true,  false, true,  false },
				{ false, false, false, false, false }
			};

		characters['@'] = new bool[,] {
				{ false, true,  true,  true,  false },
				{ true,  false, false, false, true  },
				{ true,  false, true,  true,  true  },
				{ true,  false, true,  false, true  },
				{ true,  false, true,  true,  true  },
				{ true,  false, false, false, false },
				{ false, true,  true,  true,  true  },
				{ false, false, false, false, false }
			};

		characters['$'] = new bool[,] {
				{ false, false, true,  false, false },
				{ false, true,  true,  true,  true  },
				{ true,  false, true,  false, false },
				{ false, true,  true,  true,  false },
				{ false, false, true,  false, true  },
				{ true,  true,  true,  true,  false },
				{ false, false, true,  false, false },
				{ false, false, false, false, false }
			};

		characters['&'] = new bool[,] {
				{ false, true,  true,  false, false },
				{ true,  false, false, true,  false },
				{ true,  false, false, true,  false },
				{ false, true,  true,  false, false },
				{ true,  false, false, true,  false },
				{ true,  false, false, false, true  },
				{ false, true,  true,  false, true  },
				{ false, false, false, false, false }
			};
	}
	// @formatter:on

	public void drawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color) {
		drawText(spriteBatch, text, position, color, null, null);
	}

	public void drawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color, Color? shadowColor, Point? shadowOffset = null, float textScale = 1) {
		int xOffset = 0;
		foreach(char c in text) {
			if(this.characters.ContainsKey(c)) {
				if(shadowColor.HasValue) {
					drawCharacter(spriteBatch, c, position + new Vector2(xOffset + (shadowOffset.HasValue ? shadowOffset.Value.X : 1), (shadowOffset.HasValue ? shadowOffset.Value.Y : 1)), shadowColor.Value, textScale);
				}
				drawCharacter(spriteBatch, c, position + new Vector2(xOffset, 0), color, textScale);
				xOffset += (charWidth + 1) * scale; // 1 pixel spacing between characters
			}
		}
	}

	private void drawCharacter(SpriteBatch spriteBatch, char c, Vector2 position, Color color, float textScale = 1) {
		if(_characterTextures.ContainsKey(c)) {
			Texture2D charTex = _characterTextures[c];

			Rectangle destRect = new Rectangle(
				(int)(position.X),
				(int)position.Y,
				(int)(charWidth * scale * textScale),
				(int)(charHeight * scale * textScale)
			);
			spriteBatch.Draw(charTex, destRect, color);

		}
		// Draw bit by bit
		//bool[,] charData = this.characters[c];
		//for(int y = 0; y < charHeight; y++) {
		//	for(int x = 0; x < charWidth; x++) {
		//		if(charData[y, x]) {
		//			Rectangle destRect = new Rectangle(
		//				(int)position.X + x * scale,
		//				(int)position.Y + y * scale,
		//				scale,
		//				scale
		//			);
		//			spriteBatch.Draw(pixelTexture, destRect, color);
		//		}
		//	}
		//}
	}

	public int measureString(string text) {
		return text.Length * (charWidth + 1) * scale;
	}

	public int getHeight(int margin = 1) {
		return (charHeight + (margin * 2 - 1)) * scale;
	}
}