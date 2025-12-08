using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Candyland.Core
{
    public class BitmapFont
    {
        private Dictionary<char, bool[,]> _characters;
        private int _charWidth = 5;
        private int _charHeight = 7;
        private int _scale = 2;
        private Texture2D _pixelTexture;

        public BitmapFont(GraphicsDevice graphicsDevice)
        {
            _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            _characters = new Dictionary<char, bool[,]>();
            InitializeCharacters();
        }

        private void InitializeCharacters()
        {
            // UPPERCASE LETTERS
            _characters['A'] = new bool[,] {
                { false, false, true,  false, false },
                { false, true,  false, true,  false },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  }
            };

            _characters['B'] = new bool[,] {
                { true,  true,  true,  true,  false },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  true,  true,  true,  false },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  true,  true,  true,  false }
            };

            _characters['C'] = new bool[,] {
                { false, true,  true,  true,  true  },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { false, true,  true,  true,  true  }
            };

            _characters['D'] = new bool[,] {
                { true,  true,  true,  true,  false },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  true,  true,  true,  false }
            };

            _characters['E'] = new bool[,] {
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  true,  true,  true,  true  }
            };

            _characters['F'] = new bool[,] {
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  false, false, false, false }
            };

            _characters['G'] = new bool[,] {
                { false, true,  true,  true,  true  },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  false, true,  true,  true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { false, true,  true,  true,  false }
            };

            _characters['H'] = new bool[,] {
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  }
            };

            _characters['I'] = new bool[,] {
                { true,  true,  true,  true,  true  },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { true,  true,  true,  true,  true  }
            };

            _characters['J'] = new bool[,] {
                { false, false, false, false, true  },
                { false, false, false, false, true  },
                { false, false, false, false, true  },
                { false, false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { false, true,  true,  true,  false }
            };

            _characters['K'] = new bool[,] {
                { true,  false, false, false, true  },
                { true,  false, false, true,  false },
                { true,  false, true,  false, false },
                { true,  true,  false, false, false },
                { true,  false, true,  false, false },
                { true,  false, false, true,  false },
                { true,  false, false, false, true  }
            };

            _characters['L'] = new bool[,] {
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  true,  true,  true,  true  }
            };

            _characters['M'] = new bool[,] {
                { true,  false, false, false, true  },
                { true,  true,  false, true,  true  },
                { true,  false, true,  false, true  },
                { true,  false, true,  false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  }
            };

            _characters['N'] = new bool[,] {
                { true,  false, false, false, true  },
                { true,  true,  false, false, true  },
                { true,  true,  false, false, true  },
                { true,  false, true,  false, true  },
                { true,  false, false, true,  true  },
                { true,  false, false, true,  true  },
                { true,  false, false, false, true  }
            };

            _characters['O'] = new bool[,] {
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  true,  true,  true,  true  }
            };

            _characters['P'] = new bool[,] {
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  false, false, false, false }
            };

            _characters['Q'] = new bool[,] {
                { false, true,  true,  true,  false },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, true,  false, true  },
                { true,  false, false, true,  false },
                { false, true,  true,  false, true  }
            };

            _characters['R'] = new bool[,] {
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  true,  true,  true,  true  },
                { true,  false, false, true,  false },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  }
            };

            _characters['S'] = new bool[,] {
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  true,  true,  true,  true  },
                { false, false, false, false, true  },
                { false, false, false, false, true  },
                { true,  true,  true,  true,  true  }
            };

            _characters['T'] = new bool[,] {
                { true,  true,  true,  true,  true  },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false }
            };

            _characters['U'] = new bool[,] {
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  true,  true,  true,  true  }
            };

            _characters['V'] = new bool[,] {
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { false, true,  false, true,  false },
                { false, true,  false, true,  false },
                { false, false, true,  false, false }
            };

            _characters['W'] = new bool[,] {
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, true,  false, true  },
                { true,  false, true,  false, true  },
                { true,  true,  false, true,  true  },
                { true,  false, false, false, true  }
            };

            _characters['X'] = new bool[,] {
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { false, true,  false, true,  false },
                { false, false, true,  false, false },
                { false, true,  false, true,  false },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  }
            };

            _characters['Y'] = new bool[,] {
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { false, true,  false, true,  false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false }
            };

            _characters['Z'] = new bool[,] {
                { true,  true,  true,  true,  true  },
                { false, false, false, false, true  },
                { false, false, false, true,  false },
                { false, false, true,  false, false },
                { false, true,  false, false, false },
                { true,  false, false, false, false },
                { true,  true,  true,  true,  true  }
            };

            // LOWERCASE LETTERS (use uppercase for simplicity - you can make them unique later)
            for (char c = 'a'; c <= 'z'; c++)
            {
                _characters[c] = _characters[(char)(c - 32)]; // Map to uppercase
            }

            // NUMBERS
            _characters['0'] = new bool[,] {
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  true,  true,  true,  true  }
            };

            _characters['1'] = new bool[,] {
                { false, false, true,  false, false },
                { false, true,  true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, true,  true,  true,  false }
            };

            _characters['2'] = new bool[,] {
                { true,  true,  true,  true,  true  },
                { false, false, false, false, true  },
                { false, false, false, false, true  },
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  true,  true,  true,  true  }
            };

            _characters['3'] = new bool[,] {
                { true,  true,  true,  true,  true  },
                { false, false, false, false, true  },
                { false, false, false, false, true  },
                { true,  true,  true,  true,  true  },
                { false, false, false, false, true  },
                { false, false, false, false, true  },
                { true,  true,  true,  true,  true  }
            };

            _characters['4'] = new bool[,] {
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  true,  true,  true,  true  },
                { false, false, false, false, true  },
                { false, false, false, false, true  },
                { false, false, false, false, true  }
            };

            _characters['5'] = new bool[,] {
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  true,  true,  true,  true  },
                { false, false, false, false, true  },
                { false, false, false, false, true  },
                { true,  true,  true,  true,  true  }
            };

            _characters['6'] = new bool[,] {
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, false },
                { true,  false, false, false, false },
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  true,  true,  true,  true  }
            };

            _characters['7'] = new bool[,] {
                { true,  true,  true,  true,  true  },
                { false, false, false, false, true  },
                { false, false, false, false, true  },
                { false, false, false, true,  false },
                { false, false, true,  false, false },
                { false, true,  false, false, false },
                { false, true,  false, false, false }
            };

            _characters['8'] = new bool[,] {
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  true,  true,  true,  true  }
            };

            _characters['9'] = new bool[,] {
                { true,  true,  true,  true,  true  },
                { true,  false, false, false, true  },
                { true,  false, false, false, true  },
                { true,  true,  true,  true,  true  },
                { false, false, false, false, true  },
                { false, false, false, false, true  },
                { true,  true,  true,  true,  true  }
            };

            // PUNCTUATION & SYMBOLS
            _characters[' '] = new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false }
            };

            _characters['.'] = new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false }
            };

            _characters[','] = new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, true,  false, false },
                { false, true,  false, false, false }
            };

            _characters['!'] = new bool[,] {
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, false, false, false },
                { false, false, true,  false, false }
            };

            _characters['?'] = new bool[,] {
                { false, true,  true,  true,  false },
                { true,  false, false, false, true  },
                { false, false, false, false, true  },
                { false, false, false, true,  false },
                { false, false, true,  false, false },
                { false, false, false, false, false },
                { false, false, true,  false, false }
            };

            _characters[':'] = new bool[,] {
                { false, false, false, false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, false, false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, false, false, false }
            };

            _characters[';'] = new bool[,] {
                { false, false, false, false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, false, false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, true,  false, false, false }
            };

            _characters['-'] = new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { true,  true,  true,  true,  true  },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false }
            };

            _characters['_'] = new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { true,  true,  true,  true,  true  }
            };

            _characters['+'] = new bool[,] {
                { false, false, false, false, false },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { true,  true,  true,  true,  true  },
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, false, false, false }
            };

            _characters['='] = new bool[,] {
                { false, false, false, false, false },
                { false, false, false, false, false },
                { true,  true,  true,  true,  true  },
                { false, false, false, false, false },
                { true,  true,  true,  true,  true  },
                { false, false, false, false, false },
                { false, false, false, false, false }
            };

            _characters['/'] = new bool[,] {
                { false, false, false, false, true  },
                { false, false, false, true,  false },
                { false, false, false, true,  false },
                { false, false, true,  false, false },
                { false, true,  false, false, false },
                { false, true,  false, false, false },
                { true,  false, false, false, false }
            };

            _characters['\\'] = new bool[,] {
                { true,  false, false, false, false },
                { false, true,  false, false, false },
                { false, true,  false, false, false },
                { false, false, true,  false, false },
                { false, false, false, true,  false },
                { false, false, false, true,  false },
                { false, false, false, false, true  }
            };

            _characters['('] = new bool[,] {
                { false, false, true,  false, false },
                { false, true,  false, false, false },
                { false, true,  false, false, false },
                { false, true,  false, false, false },
                { false, true,  false, false, false },
                { false, true,  false, false, false },
                { false, false, true,  false, false }
            };

            _characters[')'] = new bool[,] {
                { false, false, true,  false, false },
                { false, false, false, true,  false },
                { false, false, false, true,  false },
                { false, false, false, true,  false },
                { false, false, false, true,  false },
                { false, false, false, true,  false },
                { false, false, true,  false, false }
            };

            _characters['['] = new bool[,] {
                { false, true,  true,  false, false },
                { false, true,  false, false, false },
                { false, true,  false, false, false },
                { false, true,  false, false, false },
                { false, true,  false, false, false },
                { false, true,  false, false, false },
                { false, true,  true,  false, false }
            };

            _characters[']'] = new bool[,] {
                { false, false, true,  true,  false },
                { false, false, false, true,  false },
                { false, false, false, true,  false },
                { false, false, false, true,  false },
                { false, false, false, true,  false },
                { false, false, false, true,  false },
                { false, false, true,  true,  false }
            };

            _characters['\''] = new bool[,] {
                { false, false, true,  false, false },
                { false, false, true,  false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false }
            };

            _characters['"'] = new bool[,] {
                { false, true,  false, true,  false },
                { false, true,  false, true,  false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false }
            };

            _characters['<'] = new bool[,] {
                { false, false, false, false, true  },
                { false, false, false, true,  false },
                { false, false, true,  false, false },
                { false, true,  false, false, false },
                { false, false, true,  false, false },
                { false, false, false, true,  false },
                { false, false, false, false, true  }
            };

            _characters['>'] = new bool[,] {
                { true,  false, false, false, false },
                { false, true,  false, false, false },
                { false, false, true,  false, false },
                { false, false, false, true,  false },
                { false, false, true,  false, false },
                { false, true,  false, false, false },
                { true,  false, false, false, false }
            };

            _characters['*'] = new bool[,] {
                { false, false, true,  false, false },
                { true,  false, true,  false, true  },
                { false, true,  true,  true,  false },
                { false, false, true,  false, false },
                { false, true,  true,  true,  false },
                { true,  false, true,  false, true  },
                { false, false, true,  false, false }
            };

            _characters['%'] = new bool[,] {
                { true,  true,  false, false, true  },
                { true,  true,  false, true,  false },
                { false, false, true,  false, false },
                { false, true,  false, false, false },
                { false, true,  false, false, false },
                { true,  false, true,  true,  false },
                { true,  false, false, true,  true  }
            };

            _characters['#'] = new bool[,] {
                { false, true,  false, true,  false },
                { false, true,  false, true,  false },
                { true,  true,  true,  true,  true  },
                { false, true,  false, true,  false },
                { true,  true,  true,  true,  true  },
                { false, true,  false, true,  false },
                { false, true,  false, true,  false }
            };

            _characters['@'] = new bool[,] {
                { false, true,  true,  true,  false },
                { true,  false, false, false, true  },
                { true,  false, true,  true,  true  },
                { true,  false, true,  false, true  },
                { true,  false, true,  true,  true  },
                { true,  false, false, false, false },
                { false, true,  true,  true,  true  }
            };

            _characters['$'] = new bool[,] {
                { false, false, true,  false, false },
                { false, true,  true,  true,  true  },
                { true,  false, true,  false, false },
                { false, true,  true,  true,  false },
                { false, false, true,  false, true  },
                { true,  true,  true,  true,  false },
                { false, false, true,  false, false }
            };

            _characters['&'] = new bool[,] {
                { false, true,  true,  false, false },
                { true,  false, false, true,  false },
                { true,  false, false, true,  false },
                { false, true,  true,  false, false },
                { true,  false, false, true,  false },
                { true,  false, false, false, true  },
                { false, true,  true,  false, true  }
            };
        }

        public void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color)
        {
            int xOffset = 0;

            foreach (char c in text)
            {
                if (_characters.ContainsKey(c))
                {
                    DrawCharacter(spriteBatch, c, position + new Vector2(xOffset, 0), color);
                    xOffset += (_charWidth + 1) * _scale; // 1 pixel spacing between characters
                }
            }
        }

        private void DrawCharacter(SpriteBatch spriteBatch, char c, Vector2 position, Color color)
        {
            bool[,] charData = _characters[c];

            for (int y = 0; y < _charHeight; y++)
            {
                for (int x = 0; x < _charWidth; x++)
                {
                    if (charData[y, x])
                    {
                        Rectangle destRect = new Rectangle(
                            (int)position.X + x * _scale,
                            (int)position.Y + y * _scale,
                            _scale,
                            _scale
                        );
                        spriteBatch.Draw(_pixelTexture, destRect, color);
                    }
                }
            }
        }

        public int MeasureString(string text)
        {
            return text.Length * (_charWidth + 1) * _scale;
        }
    }
}