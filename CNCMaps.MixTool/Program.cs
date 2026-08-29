using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.Map;
using CNCMaps.FileFormats.VirtualFileSystem;

namespace CNCMaps.MixTool {

	/// <summary>
	/// Command-line inspection of .mix archives, on top of the same MixFile
	/// reader the renderer uses (so encrypted Westwood mixes work too).
	///
	/// Mix archives store name hashes rather than names, so entries can only be
	/// named when the name is known up front: a built-in list of game files, an
	/// XCC "local mix database.dat" when the archive carries one, filenames
	/// harvested from text entries (--harvest), plus anything passed with
	/// --names. Unnamed entries are typed by content sniff.
	/// </summary>
	static class Program {

		static int Main(string[] args) {
			if (args.Length == 0 || args[0] is "-h" or "--help" or "help") {
				Usage();
				return args.Length == 0 ? 1 : 0;
			}

			try {
				switch (args[0].ToLowerInvariant()) {
					case "list": return List(args.Skip(1).ToArray());
					case "extract": return Extract(args.Skip(1).ToArray());
					case "hash": return Hash(args.Skip(1).ToArray());
					case "tiles": return Tiles(args.Skip(1).ToArray());
					case "maps": return Maps(args.Skip(1).ToArray());
					case "csf": return Csf(args.Skip(1).ToArray());
					default:
						Console.Error.WriteLine("Unknown command '" + args[0] + "'");
						Usage();
						return 1;
				}
			}
			catch (Exception exc) {
				Console.Error.WriteLine("error: " + exc.Message);
				if (Array.IndexOf(args, "--debug") >= 0) Console.Error.WriteLine(exc);
				return 2;
			}
		}

		static void Usage() {
			Console.WriteLine(@"CNCMaps.MixTool - inspect and extract .mix archives

  list <mix> [--names <file>] [--filter <substr>] [--unnamed] [--harvest]
      List entries: hash, size, resolved name (when known) and sniffed type.
      --names    newline-separated extra filenames to try resolving
      --filter   only show entries whose name or type contains <substr>
      --unnamed  only show entries whose name could not be resolved
      --harvest  also try filenames referenced by the archive's text entries

  extract <mix> [name...] [-o <dir>] [--names <file>] [--all] [--harvest]
      Extract named files; -r descends into nested archives. With --all,
      extracts this archive's entries, using the resolved
      name where known and <hash>.<type> otherwise.

  maps <mix|dir>... [--names <file>] [-o <dir>]
      Find map files (by content) inside mix archives, descending into nested
      ones. Reports name, theater, size and the in-game title as TSV. Names
      are resolved via the local mix database and harvested references.
      With -o, each found map is also written to <dir>/<archive>/<name>.

  csf <csf|mix>... [label...] [--filter <substr>]
      Dump stringtable labels as TSV. Mix arguments are searched for .csf
      entries by name and content. With labels, print only those; --filter
      matches label or value.

  hash <name>...
      Print the mix index hash of a filename.

Names stored in an XCC 'local mix database.dat' are picked up automatically.
Encrypted archives (ra2md.mix and friends) are decrypted transparently; index
entries pointing outside the archive (XCC-breaking protection) are reported
as 'bogus' and skipped.");
		}

		// ---- shared helpers -------------------------------------------------

		static MixFile Open(string path) {
			if (!File.Exists(path))
				throw new FileNotFoundException("no such file: " + path);
			var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
			return new MixFile(stream, Path.GetFileName(path), true);
		}

		static string Arg(string[] args, string name, string fallback = null) {
			int i = Array.IndexOf(args, name);
			return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
		}

		static bool Flag(string[] args, string name) {
			return Array.IndexOf(args, name) >= 0;
		}

		static IEnumerable<string> Positional(string[] args) {
			var withValue = new[] { "-o", "--names", "--filter" };
			for (int i = 0; i < args.Length; i++) {
				if (withValue.Contains(args[i])) { i++; continue; }
				if (args[i].StartsWith("-")) continue;
				yield return args[i];
			}
		}

		/// <summary>
		/// Entry that cannot lie inside the archive. Some mods ship such entries on purpose (a 4GB
		/// "local mix database.dat" at offset -1) to break XCC Mixer; reading one throws, so every
		/// read path must skip them. Offsets are body-relative and the header only shrinks the usable
		/// size, so comparing against the full archive length is the safe side.
		/// </summary>
		static bool IsBogus(MixFile mix, MixFile.MixEntry e) {
			return (long)e.Offset + e.Length > mix.Length;
		}

		const uint LmdHash = 0x366e051f;   // "local mix database.dat"
		const string XccId = "XCC by Olaf van der Spek";

		/// <summary>Filenames from the archive's XCC local mix database, if any.</summary>
		static List<string> LmdNames(MixFile mix) {
			var names = new List<string>();
			if (!mix.Index.TryGetValue(LmdHash, out var e) || IsBogus(mix, e))
				return names;
			byte[] data = ReadEntry(mix, LmdHash);
			// t_xcc_header: id[32], size, type, version; then game, count; then
			// NUL-separated lowercase filenames
			if (data.Length < 52 || Encoding.ASCII.GetString(data, 0, XccId.Length) != XccId)
				return names;
			int pos = 52;
			while (pos < data.Length) {
				int end = Array.IndexOf(data, (byte)0, pos);
				if (end < 0) end = data.Length;
				if (end > pos)
					names.Add(Encoding.ASCII.GetString(data, pos, end - pos));
				pos = end + 1;
			}
			return names;
		}

		/// <summary>
		/// Filenames mentioned inside the archive's own text entries (mission
		/// lists, rules INIs). This recovers names in archives whose local mix
		/// database was stripped or replaced by a decoy.
		/// </summary>
		static IEnumerable<string> HarvestNames(MixFile mix) {
			var token = new System.Text.RegularExpressions.Regex(
				@"[A-Za-z0-9!$_\-]{1,60}\.[A-Za-z0-9]{1,4}(?![A-Za-z0-9.])");
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var e in mix.Index.Values) {
				if (IsBogus(mix, e) || e.Length > 32 * 1024 * 1024) continue;
				byte[] head = ReadEntry(mix, e.Hash, 64);
				if (SniffType(head, e.Length) is not ("ini" or "txt")) continue;
				string text = Encoding.ASCII.GetString(ReadEntry(mix, e.Hash));
				foreach (System.Text.RegularExpressions.Match m in token.Matches(text))
					if (seen.Add(m.Value))
						yield return m.Value;
			}
		}

		/// <summary>Hash -> name, for every candidate name that is actually present.</summary>
		static Dictionary<uint, string> ResolveNames(MixFile mix, string namesFile, bool harvest = false) {
			var candidates = new List<string>(KnownNames.All());
			if (namesFile != null) {
				if (!File.Exists(namesFile))
					throw new FileNotFoundException("no such names file: " + namesFile);
				candidates.AddRange(File.ReadAllLines(namesFile)
					.Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("#")));
			}
			candidates.AddRange(LmdNames(mix));
			if (harvest)
				candidates.AddRange(HarvestNames(mix));
			var found = new Dictionary<uint, string>();
			foreach (string name in candidates) {
				uint hash = MixFile.MixEntry.HashFilename(name);
				if (mix.Index.ContainsKey(hash))
					found[hash] = name;   // last candidate wins; duplicates are harmless
			}
			return found;
		}

		/// <summary>
		/// Mix headers are either "flags then count" (new) or "count" (C&C1 style). Nested archives
		/// are common inside the game mixes; recognising them is what lets --recurse find theater files.
		/// </summary>
		static bool LooksLikeMix(byte[] head, long size) {
			if (head.Length < 8 || size < 20) return false;
			uint signature = BitConverter.ToUInt32(head, 0);
			if ((signature & 0xFFFF) == 0) {
				const uint known = 0x10000 | 0x20000;               // checksum | encrypted
				if ((signature & ~known) != 0) return false;
				if ((signature & 0x20000) != 0) return true;        // encrypted: header unreadable here
				ushort count = BitConverter.ToUInt16(head, 4);
				return count > 0 && 4 + 6 + (long)count * 12 <= size;
			}
			ushort plain = BitConverter.ToUInt16(head, 0);
			return plain > 0 && 6 + (long)plain * 12 <= size;
		}

		/// <summary>Open a nested archive held inside another mix.</summary>
		static MixFile OpenNested(MixFile parent, uint hash, string name, List<IDisposable> keep) {
			try {
				byte[] raw = ReadEntry(parent, hash);
				if (raw.Length == 0) return null;
				var ms = new MemoryStream(raw);
				var nested = new MixFile(ms, name, true);
				// A non-archive can still parse into a plausible header, so only accept one whose entries
				// fit inside the data.
				if (nested.Index.Count == 0 ||
					nested.Index.Values.Count(e => e.Offset + (long)e.Length <= raw.Length)
						< nested.Index.Count * 0.9) {
					nested.Dispose();
					ms.Dispose();
					return null;
				}
				keep.Add(ms);
				keep.Add(nested);
				return nested;
			}
			catch {
				return null;   // not actually a mix, or a flavour we cannot read
			}
		}

		/// <summary>
		/// Read an entry's bytes. When recursing, entry lengths can come from data that is not an
		/// archive at all, so a nonsense size means "no content" rather than an allocation.
		/// </summary>
		static byte[] ReadEntry(MixFile mix, uint hash, int limit = int.MaxValue) {
			if (mix.Index.TryGetValue(hash, out var entry) && IsBogus(mix, entry))
				return Array.Empty<byte>();
			var vf = mix.OpenFile(hash);
			if (vf == null) return Array.Empty<byte>();
			long available = vf.Length;
			if (available <= 0 || available > int.MaxValue) return Array.Empty<byte>();
			int n = (int)Math.Min(limit, available);
			vf.Position = 0;
			return vf.Read(n);
		}

		/// <summary>Best-effort file type from the first bytes, for unnamed entries.</summary>
		static string SniffType(byte[] head, long size) {
			if (size == 768) return "pal";
			if (head.Length >= 4 && Encoding.ASCII.GetString(head, 0, 4) == "Voxe") return "vxl";
			// text before mix: a large INI whose first bytes parse as a plausible
			// entry count would otherwise pass the old-style mix header check
			int printable = head.Count(c => c >= 32 && c < 127 || c == 9 || c == 10 || c == 13);
			if (head.Length > 0 && printable / (double)head.Length > 0.92)
				return head[0] == '[' || head[0] == ';' ? "ini" : "txt";
			if (LooksLikeMix(head, size)) return "mix";
			if (head.Length >= 16) {
				// TMP (theater tile): cx/cy are the block dimensions the games use
				int cx = BitConverter.ToInt32(head, 0), cy = BitConverter.ToInt32(head, 4);
				if ((cx == 60 && cy == 30) || (cx == 48 && cy == 24)) return "tmp";
			}
			if (head.Length >= 4 && head[0] == 0 && head[1] == 0) return "shp";
			return "bin";
		}

		// ---- commands -------------------------------------------------------

		static int List(string[] args) {
			string path = Positional(args).FirstOrDefault();
			if (path == null) { Usage(); return 1; }
			string filter = Arg(args, "--filter");
			bool unnamedOnly = Flag(args, "--unnamed");

			using var mix = Open(path);
			var names = ResolveNames(mix, Arg(args, "--names"), Flag(args, "--harvest"));

			Console.WriteLine("{0}: {1} entries, {2:n0} bytes, {3} named",
				Path.GetFileName(path), mix.Index.Count, new FileInfo(path).Length, names.Count);
			Console.WriteLine("{0,-10} {1,12} {2,-5} {3}", "hash", "size", "type", "name");

			var rows = new List<(uint Hash, uint Length, string Type, string Name)>();
			foreach (var e in mix.Index.Values) {
				names.TryGetValue(e.Hash, out string name);
				if (unnamedOnly && name != null) continue;
				string type = IsBogus(mix, e) ? "bogus"
					: name != null
					? Path.GetExtension(name).TrimStart('.').ToLowerInvariant()
					: SniffType(ReadEntry(mix, e.Hash, 64), e.Length);
				if (filter != null &&
					(name ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
					type.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
				rows.Add((e.Hash, e.Length, type, name));
			}

			foreach (var r in rows.OrderBy(r => r.Name ?? "￿").ThenByDescending(r => r.Length))
				Console.WriteLine("0x{0:x8} {1,12:n0} {2,-5} {3}", r.Hash, r.Length, r.Type, r.Name ?? "?");
			Console.WriteLine("({0} shown)", rows.Count);
			return 0;
		}

		static int Extract(string[] args) {
			var positional = Positional(args).ToList();
			if (positional.Count == 0) { Usage(); return 1; }
			string path = positional[0];
			var wanted = positional.Skip(1).ToList();
			string outDir = Arg(args, "-o", ".");
			Directory.CreateDirectory(outDir);

			using var mix = Open(path);
			int written = 0;

			if (Flag(args, "--all")) {
				var names = ResolveNames(mix, Arg(args, "--names"), Flag(args, "--harvest"));
				foreach (var e in mix.Index.Values) {
					if (IsBogus(mix, e)) continue;
					names.TryGetValue(e.Hash, out string name);
					byte[] data = ReadEntry(mix, e.Hash);
					name ??= string.Format("{0:x8}.{1}", e.Hash, SniffType(data.Take(64).ToArray(), e.Length));
					File.WriteAllBytes(Path.Combine(outDir, Path.GetFileName(name)), data);
					written++;
				}
			}
			else {
				if (wanted.Count == 0) {
					Console.Error.WriteLine("nothing to extract: pass filenames or --all");
					return 1;
				}
				bool recurse = Flag(args, "--recurse") || Flag(args, "-r");
				var keep = new List<IDisposable>();
				try {
					foreach (string name in wanted) {
						var hit = Find(mix, name, Path.GetFileName(path), recurse, keep);
						if (hit == null) {
							Console.Error.WriteLine("not found: " + name);
							continue;
						}
						byte[] data = ReadEntry(hit.Value.Mix, MixFile.MixEntry.HashFilename(name));
						File.WriteAllBytes(Path.Combine(outDir, Path.GetFileName(name)), data);
						Console.WriteLine("{0,12:n0}  {1}  (from {2})", data.Length, name, hit.Value.Where);
						written++;
					}
				}
				finally {
					foreach (var d in keep) d.Dispose();
				}
			}
			Console.WriteLine("extracted {0} file(s) to {1}", written, Path.GetFullPath(outDir));
			return written > 0 ? 0 : 1;
		}

		/// <summary>Locate a file in this mix, optionally descending into nested archives.</summary>
		static (MixFile Mix, string Where)? Find(MixFile mix, string name, string where,
				bool recurse, List<IDisposable> keep, int depth = 0) {
			if (mix.Index.ContainsKey(MixFile.MixEntry.HashFilename(name)))
				return (mix, where);
			if (!recurse || depth >= 4)
				return null;
			foreach (var e in mix.Index.Values.OrderByDescending(e => e.Length)) {
				if (!LooksLikeMix(ReadEntry(mix, e.Hash, 16), e.Length)) continue;
				var nested = OpenNested(mix, e.Hash, "nested", keep);
				if (nested == null) continue;
				var hit = Find(nested, name, where + "/" + string.Format("{0:x8}", e.Hash), recurse, keep, depth + 1);
				if (hit != null) return hit;
			}
			return null;
		}

		/// <summary>
		/// Report the highest tile index each map references. Tile numbers are
		/// global across a theater's tilesets in order, so a map whose highest
		/// index exceeds the vanilla tile count needs extra tilesets (a terrain
		/// expansion) to draw correctly.
		/// </summary>
		static int Tiles(string[] args) {
			var targets = Positional(args).ToList();
			if (targets.Count == 0) { Usage(); return 1; }

			var files = new List<string>();
			foreach (string target in targets) {
				if (Directory.Exists(target))
					files.AddRange(Directory.EnumerateFiles(target, "*", SearchOption.AllDirectories)
						.Where(f => new[] { ".map", ".mpr", ".yrm" }.Contains(Path.GetExtension(f).ToLowerInvariant())));
				else if (File.Exists(target))
					files.Add(target);
			}

			Console.WriteLine("path\ttheater\twidth\theight\tmaxtile\tdistinct\tstatus");
			foreach (string f in files.OrderBy(f => f)) {
				string theater = "?", status = "ok";
				int max = -1, distinct = 0, w = 0, h = 0;
				try {
					using var fs = new FileStream(f, FileMode.Open, FileAccess.Read);
					var map = new MapFile(fs, Path.GetFileName(f));
					theater = (map.GetSection("Map")?.ReadString("Theater") ?? "?").ToUpperInvariant();
					w = map.FullSize.Width;
					h = map.FullSize.Height;
					var seen = new HashSet<int>();
					foreach (var tile in map.Tiles) {
						if (tile == null) continue;
						seen.Add(tile.TileNum);
						if (tile.TileNum > max) max = tile.TileNum;
					}
					distinct = seen.Count;
				}
				catch (Exception exc) {
					status = "error: " + exc.GetType().Name;
				}
				Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
					f.Replace('\\', '/'), theater, w, h, max, distinct, status);
			}
			return 0;
		}

		/// <summary>
		/// Content-based map discovery inside mix archives: campaign maps often
		/// ship packed into the game's mixes under hashed names rather than as
		/// loose files, invisible to a directory scan.
		/// </summary>
		static int Maps(string[] args) {
			var targets = Positional(args).ToList();
			if (targets.Count == 0) { Usage(); return 1; }
			string namesFile = Arg(args, "--names");
			string outDir = Arg(args, "-o");

			var files = new List<string>();
			foreach (string t in targets) {
				if (Directory.Exists(t))
					files.AddRange(Directory.EnumerateFiles(t, "*.mix", SearchOption.AllDirectories));
				else if (File.Exists(t))
					files.Add(t);
				else
					Console.Error.WriteLine("no such file: " + t);
			}

			Console.WriteLine("archive\tname\tsize\ttheater\twidth\theight\ttitle");
			int total = 0;
			foreach (string f in files.OrderBy(f => f)) {
				try {
					using var mix = Open(f);
					total += MapsIn(mix, f.Replace('\\', '/'), namesFile, 0, outDir);
				}
				catch (Exception exc) {
					Console.Error.WriteLine(f + ": " + exc.Message);
				}
			}
			Console.Error.WriteLine(total + " map(s) found");
			return 0;
		}

		static readonly string[] MapExtensions = { ".map", ".mpr", ".yrm" };

		static int MapsIn(MixFile mix, string where, string namesFile, int depth, string outDir = null) {
			var names = ResolveNames(mix, namesFile, harvest: true);
			int found = 0;
			var keep = new List<IDisposable>();
			try {
				foreach (var e in mix.Index.Values) {
					if (IsBogus(mix, e)) continue;
					names.TryGetValue(e.Hash, out string name);
					string type = SniffType(ReadEntry(mix, e.Hash, 64), e.Length);
					bool nameSaysMap = name != null &&
						MapExtensions.Contains(Path.GetExtension(name).ToLowerInvariant());
					if (!nameSaysMap && type == "mix" && depth < 4) {
						var nested = OpenNested(mix, e.Hash, name ?? e.Hash.ToString("x8"), keep);
						if (nested != null) {
							found += MapsIn(nested, where + "/" + (name ?? e.Hash.ToString("x8")), namesFile, depth + 1, outDir);
							continue;
						}
						// not actually an archive; fall through to the text check
					}
					if (!nameSaysMap && type is not ("ini" or "txt" or "mix")) continue;
					if (e.Length < 4096) continue;   // a map with terrain data is far larger
					string text = Encoding.ASCII.GetString(ReadEntry(mix, e.Hash));
					if (!text.Contains("[IsoMapPack5]") && !text.Contains("[MapPack]")) continue;
					string size = IniValue(text, "Map", "Size") ?? "";
					var dims = size.Split(',');
					string outName = name ?? e.Hash.ToString("x8") + ".map";
					Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
						where, outName, e.Length,
						IniValue(text, "Map", "Theater") ?? "?",
						dims.Length == 4 ? dims[2] : "?", dims.Length == 4 ? dims[3] : "?",
						IniValue(text, "Basic", "Name") ?? "");
					if (outDir != null) {
						string sub = Path.Combine(outDir, string.Join("_",
							where.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)));
						Directory.CreateDirectory(sub);
						File.WriteAllBytes(Path.Combine(sub, Path.GetFileName(outName)), ReadEntry(mix, e.Hash));
					}
					found++;
				}
			}
			finally {
				foreach (var d in keep) d.Dispose();
			}
			return found;
		}

		/// <summary>
		/// Merge every stringtable found in the given files: loose .csf files
		/// plus csf entries inside mix archives (found by name or content).
		/// </summary>
		static int Csf(string[] args) {
			var positional = Positional(args).ToList();
			var files = positional.Where(File.Exists).ToList();
			var labels = positional.Where(p => !File.Exists(p)).ToList();
			if (files.Count == 0) { Usage(); return 1; }
			string filter = Arg(args, "--filter");

			var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (string path in files) {
				byte[] head = new byte[4];
				using (var fs = File.OpenRead(path))
					fs.Read(head, 0, 4);
				if (CsfReader.LooksLikeCsf(head)) {
					foreach (var kv in new CsfReader(File.ReadAllBytes(path)).Labels)
						merged[kv.Key] = kv.Value;
					continue;
				}
				using var mix = Open(path);
				var names = ResolveNames(mix, null);
				foreach (var e in mix.Index.Values) {
					if (IsBogus(mix, e)) continue;
					names.TryGetValue(e.Hash, out string name);
					bool nameSaysCsf = name != null && name.EndsWith(".csf", StringComparison.OrdinalIgnoreCase);
					if (!nameSaysCsf && !CsfReader.LooksLikeCsf(ReadEntry(mix, e.Hash, 4))) continue;
					try {
						foreach (var kv in new CsfReader(ReadEntry(mix, e.Hash)).Labels)
							merged[kv.Key] = kv.Value;
					}
					catch {
						// sniffing can hit a non-csf that happens to start with the magic
					}
				}
			}

			int shown = 0;
			foreach (var kv in labels.Count > 0
					? labels.Select(l => new KeyValuePair<string, string>(l,
						merged.TryGetValue(l, out string v) ? v : ""))
					: merged.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)) {
				if (filter != null &&
					kv.Key.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
					kv.Value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
				Console.WriteLine(kv.Key + "\t" + kv.Value.Replace("\r", " ").Replace("\n", " "));
				shown++;
			}
			Console.Error.WriteLine($"{merged.Count} labels loaded, {shown} shown");
			return 0;
		}

		/// <summary>Cheap single-value INI scrape; avoids a full map parse.</summary>
		static string IniValue(string text, string section, string key) {
			int s = text.IndexOf("[" + section + "]", StringComparison.OrdinalIgnoreCase);
			if (s < 0) return null;
			int end = text.IndexOf("\n[", s + 1, StringComparison.Ordinal);
			string body = end < 0 ? text.Substring(s) : text.Substring(s, end - s);
			foreach (string line in body.Split('\n')) {
				int eq = line.IndexOf('=');
				if (eq < 0) continue;
				if (line.Substring(0, eq).Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
					return line.Substring(eq + 1).Trim();
			}
			return null;
		}

		static int Hash(string[] args) {
			if (args.Length == 0) { Usage(); return 1; }
			foreach (string name in args)
				Console.WriteLine("0x{0:x8}  {1}", MixFile.MixEntry.HashFilename(name), name);
			return 0;
		}
	}
}
