using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace EldmeresTale.Tools;

public static class TilesetRemapper {
	private const int TILE_SIZE = 16;  // Adjust if needed
	private const int OUTPUT_WIDTH = 4;  // 4x4 grid
	private const int OUTPUT_HEIGHT = 4;

	public class RemapConfig {
		public Point SourceOrigin;  // Where to start reading in source image
		public Point[] LookupTable;  // lookupTable[maskID] = source tile position
	}

	public static void RemapTileset() {
		// [13][10][ 4][12]  Row 0
		// [ 6][ 8][ 0][ 1]  Row 1
		// [11][ 3][ 2][ 5]  Row 2
		// [15][14][ 9][ 7]  Row 3
		Point[] LookupTable = [
			new Point(2, 0), new Point(0, 1), new Point(0, 5), new Point(1, 0),
			new Point(0, 6), new Point(2, 5), new Point(1, 1), new Point(0, 3),
			new Point(0, 2), new Point(1, 2), new Point(2, 3), new Point(2, 1),
			new Point(1, 4), new Point(0, 0), new Point(2, 6), new Point(2, 2)
		];
		// Example: Remap a purchased tileset
		Dictionary<string, RemapConfig> configs = new Dictionary<string, RemapConfig> {
			["Assets/SOURCES/Remapped/fancy_grass.png"] = new RemapConfig {
				SourceOrigin = new Point(12, 1),
				LookupTable = LookupTable
			},
			["Assets/SOURCES/Remapped/clay_rock.png"] = new RemapConfig {
				SourceOrigin = new Point(32, 1),
				LookupTable = LookupTable
			}
		};
		RemapTileset("Assets/SOURCES/FD_Ground_Tiles.png", configs);
	}

	public static void RemapTileset(string sourcePath, Dictionary<string, RemapConfig> configs) {
		// Load source image
		using Bitmap sourceImage = new Bitmap(sourcePath);

		// Process each config
		foreach (KeyValuePair<string, RemapConfig> kvp in configs) {
			string outputPath = kvp.Key;
			RemapConfig config = kvp.Value;

			Console.WriteLine($"Processing {outputPath}...");
			RemapSingle(sourceImage, config, outputPath);
		}

		Console.WriteLine("Done!");
	}

	private static void RemapSingle(Bitmap sourceImage, RemapConfig config, string outputPath) {
		// Create output image (4x4 tiles)
		int outputWidth = OUTPUT_WIDTH * TILE_SIZE;
		int outputHeight = OUTPUT_HEIGHT * TILE_SIZE;
		Bitmap outputImage = new Bitmap(outputWidth, outputHeight, PixelFormat.Format32bppArgb);

		using (Graphics g = Graphics.FromImage(outputImage)) {
			// For each mask value (0-15)
			for (int maskID = 0; maskID < 16; maskID++) {
				// Get source tile position from lookup table
				Point sourceTilePos = config.LookupTable[maskID];

				// Calculate positions
				int sourceTileX = (config.SourceOrigin.X + sourceTilePos.X) * TILE_SIZE;
				int sourceTileY = (config.SourceOrigin.Y + sourceTilePos.Y) * TILE_SIZE;

				// Calculate output position (mask ID determines position in 4x4 grid)
				int outputRow = maskID / OUTPUT_WIDTH;
				int outputCol = maskID % OUTPUT_WIDTH;
				int destX = outputCol * TILE_SIZE;
				int destY = outputRow * TILE_SIZE;

				// Copy tile
				Rectangle sourceRect = new Rectangle(sourceTileX, sourceTileY, TILE_SIZE, TILE_SIZE);
				Rectangle destRect = new Rectangle(destX, destY, TILE_SIZE, TILE_SIZE);

				g.DrawImage(sourceImage, destRect, sourceRect, GraphicsUnit.Pixel);
			}
		}

		string directory = Path.GetDirectoryName(outputPath);
		if (!string.IsNullOrEmpty(directory)) {
			Directory.CreateDirectory(directory);
		}
		// Save output
		outputImage.Save(outputPath, ImageFormat.Png);
		Console.WriteLine($"  ✓ Saved {outputPath}");
	}
}