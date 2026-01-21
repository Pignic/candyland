namespace EldmeresTale;

static class Program {
	static void Main(string[] args) {
		//TilesetRemapper.RemapTileset();
		MainGame game = new MainGame();
		game.Run();
	}
}