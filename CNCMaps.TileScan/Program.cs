// Reports each map's theater and highest referenced tile index. An index at or above the
// theater's stock tile count means the map needs expansion tiles (e.g. Terrain Expansion).
//
//   CNCMaps.TileScan <root-dir>
//   -> TSV lines: relative_path <tab> theater <tab> max_tile_index <tab> tile_count
using System;
using System.IO;
using CNCMaps.FileFormats.Map;

class Program {
	static int Main(string[] args) {
		if (args.Length < 1) {
			Console.Error.WriteLine("usage: CNCMaps.TileScan <root-dir>");
			return 2;
		}
		if (args[0] == "--dump") {
			var map = new MapFile(File.OpenRead(args[1]), Path.GetFileName(args[1]));
			foreach (var t in map.Tiles)
				Console.Out.WriteLine($"{t.Rx}	{t.Ry}	{t.TileNum}	{t.SubTile}	{t.Z}");
			return 0;
		}
		string root = args[0];
		int ok = 0, fail = 0;
		foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) {
			string ext = Path.GetExtension(path).ToLowerInvariant();
			if (ext != ".map" && ext != ".mpr" && ext != ".yrm")
				continue;
			try {
				var map = new MapFile(File.OpenRead(path), Path.GetFileName(path));
				string theater = map.GetSection("Map")?.ReadString("Theater") ?? "";
				int maxTile = 0, count = 0;
				foreach (var t in map.Tiles) {
					if (t.TileNum >= 65535) continue;   // 0xFFFF = empty/clear
					count++;
					if (t.TileNum > maxTile) maxTile = t.TileNum;
				}
				string rel = Path.GetRelativePath(root, path);
				Console.Out.WriteLine($"{rel}\t{theater}\t{maxTile}\t{count}");
				ok++;
			}
			catch (Exception e) {
				Console.Error.WriteLine($"FAIL\t{Path.GetRelativePath(root, path)}\t{e.Message}");
				fail++;
			}
		}
		Console.Error.WriteLine($"scanned {ok} maps, {fail} failed");
		return 0;
	}
}
