using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CNCMaps.MixTool {

	/// <summary>
	/// Minimal Westwood stringtable (.csf) reader, extracted from
	/// CNCMaps.FileFormats.CsfFile so the tool needs no VirtualFile plumbing.
	/// Files start with " FSC"; wide strings are stored bitwise-inverted.
	/// </summary>
	class CsfReader {
		public readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase);

		public static bool LooksLikeCsf(byte[] head) {
			return head.Length >= 4 && Encoding.ASCII.GetString(head, 0, 4) == " FSC";
		}

		public CsfReader(byte[] data) {
			using var r = new BinaryReader(new MemoryStream(data));
			r.ReadInt32();                     // " FSC"
			r.ReadInt32();                     // version
			int numLabels = r.ReadInt32();
			r.ReadInt32();                     // string count
			r.ReadInt32();
			r.ReadInt32();                     // language
			for (int i = 0; i < numLabels; i++) {
				r.ReadInt32();                 // " LBL"
				int flags = r.ReadInt32();
				string name = Encoding.ASCII.GetString(r.ReadBytes(r.ReadInt32()));
				if ((flags & 1) == 0) {
					Labels[name] = "";
					continue;
				}
				bool hasExtra = r.ReadInt32() == 0x53545257;   // "STRW" reversed
				var value = Encoding.Unicode.GetString(r.ReadBytes(r.ReadInt32() * 2));
				var sb = new StringBuilder(value.Length);
				foreach (char c in value)
					sb.Append((char)~c);
				if (hasExtra)
					r.ReadBytes(r.ReadInt32());
				Labels[name] = sb.ToString();
			}
		}

		public string Get(string label) {
			return Labels.TryGetValue(label, out string v) ? v : "";
		}
	}
}
