using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.FileFormats {

	public class IniFile : VirtualTextFile {

		public List<IniSection> Sections { get; set; }
		public IniSection CurrentSection { get; set; }

		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public IniFile(Stream baseStream, string filename, int baseOffset, long fileSize, bool isBuffered = true)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
			Sections = new List<IniSection>();
			Parse();
		}

		public IniSection GetSection(string sectionName) {
			return Sections.Find(x => x.Name == sectionName);
		}

		public IniSection GetOrCreateSection(string sectionName, string insertAfter = null) {
			var ret = Sections.Find(x => x.Name == sectionName);
			if (ret == null) {
				int insertIdx = (insertAfter != null) ? Sections.FindIndex(section => section.Name == insertAfter) : -1;

				ret = new IniSection(sectionName);
				if (insertIdx != -1) {
					Sections.Insert(insertIdx, ret);
					ret.Index = insertIdx;
					// move up all section indices
					for (int i = insertIdx + 1; i < Sections.Count; i++)
						Sections[i].Index++;
				}
				else {
					Sections.Add(ret);
					ret.Index = Sections.Count;
				}
			}
			return ret;
		}

		void Parse() {
			logger.Info("Parsing {0}", Path.GetFileName(FileName));
			while (CanRead) {
				ProcessLine(ReadLine());
			}
		}

		int ProcessLine(string line) {
			IniSection.FixLine(ref line);
			if (line.Length == 0) return 0;

			// Test if this line contains start of new section i.e. matches [*]
			if ((line[0] == '[') && (line[line.Length - 1] == ']')) {
				string sectionName = line.Substring(1, line.Length - 2);
				var iniSection = new IniSection(sectionName, Sections.Count);
				logger.Trace("Loading ini section {0}", sectionName);
				Sections.Add(iniSection);
				CurrentSection = iniSection;
			}
			else if (CurrentSection != null) {
				return CurrentSection.ParseLine(line);
			}
			return 0;
		}

		void SetCurrentSection(string sectionName) {
			logger.Trace("Changing current section to {0}", sectionName);
			CurrentSection = Sections.Find(x => x.Name == sectionName);
		}

		public void SetCurrentSection(IniSection section) {
			if (Sections.Contains(section))
				CurrentSection = section;
			else
				throw new InvalidOperationException("Invalid section");
		}

		public string ReadString(string section, string key, string @default = "") {
			if (CurrentSection == null || CurrentSection.Name != section)
				SetCurrentSection(section);
			return CurrentSection.ReadString(key, @default);
		}

		public bool ReadBool(string key) {
			return CurrentSection.ReadBool(key);
		}

		public bool ReadBool(string section, string key) {
			if (CurrentSection.Name != section)
				SetCurrentSection(section);
			return ReadBool(key);
		}

		public class IniSection {
			public int Index { get; set; }
			public string Name { get; set; }

			public class IniValue {
				private string value;
				public IniValue(string value) {
					this.value = value;
				}

				public override string ToString() {
					return value;
				}
				public static implicit operator IniValue(string value) {
					return new IniValue(value);
				}
				public static implicit operator string(IniValue val) {
					return val.value;
				}
				public void Set(string value) {
					this.value = value;
				}
				public override bool Equals(object obj) {
					return value.Equals(obj.ToString());
				}
			}

			public Dictionary<string, IniValue> SortedEntries { get; set; }
			public List<KeyValuePair<string, IniValue>> OrderedEntries { get; set; }

			static NumberFormatInfo culture = CultureInfo.InvariantCulture.NumberFormat;

			public IniSection(string name, int index = -1) {
				SortedEntries = new Dictionary<string, IniValue>();
				OrderedEntries = new List<KeyValuePair<string, IniValue>>();
				Name = name;
				Index = index;
			}

			public override string ToString() {
				var sb = new StringBuilder();
				sb.Append('[');
				sb.Append(Name);
				sb.AppendLine("]");
				foreach (var v in OrderedEntries) {
					sb.Append(v.Key);
					sb.Append('=');
					sb.AppendLine(v.Value);
				}
				return sb.ToString();
			}

			public void Clear() {
				SortedEntries.Clear();
				OrderedEntries.Clear();
			}

			public int ParseLines(IEnumerable<string> lines) {
				return lines.Sum(line => ParseLine(line));
			}

			public int ParseLine(string line) {
				// ignore comments
				if (line[0] == ';') return 0;
				string key;
				int pos = line.IndexOf("=", StringComparison.Ordinal);
				if (pos != -1) {
					key = line.Substring(0, pos);
					string value = line.Substring(pos + 1);
					FixLine(ref key);
					FixLine(ref value);
					SetValue(key, value);
					return 1;
				}
				return 0;
			}

			public void SetValue(string key, string value) {
				if (!SortedEntries.ContainsKey(key)) {
					IniValue val = value;
					OrderedEntries.Add(new KeyValuePair<string, IniValue>(key, val));
					SortedEntries[key] = val;
				}
				else
					SortedEntries[key].Set(value);
			}

			public static void FixLine(ref string line) {
				int start = 0;

				while (start < line.Length && (line[start] == ' ' || line[start] == '\t'))
					start++;

				int end = line.IndexOf(';', start);
				if (end == -1) end = line.Length;

				while (end > 1 && (line[end - 1] == ' ' || line[end - 1] == '\t'))
					end--;

				line = line.Substring(start, Math.Max(end - start, 0));
			}

			public static string FixLine(string line) {
				string copy = line;
				FixLine(ref copy);
				return copy;
			}

			public bool HasKey(string keyName) {
				return SortedEntries.ContainsKey(keyName);
			}

			static readonly string[] TrueValues = { "yes", "1", "true", "on" };
			static readonly string[] FalseValues = { "no", "0", "false", "off" };

			public bool ReadBool(string key, bool defaultValue = false) {
				string entry = ReadString(key);
				if (TrueValues.Contains(entry, StringComparer.InvariantCultureIgnoreCase))
					return true;
				else if (FalseValues.Contains(entry, StringComparer.InvariantCultureIgnoreCase))
					return false;
				else return defaultValue;
			}

			public string ReadString(string key, string defaultValue = "") {
				IniValue ret;
				if (SortedEntries.TryGetValue(key, out ret))
					return ret;
				else
					return defaultValue;
			}

			public int ReadInt(string key, int defaultValue = 0) {
				int ret;
				if (int.TryParse(ReadString(key), out ret))
					return ret;
				else
					return defaultValue;
			}

			public short ReadShort(string key, short defaultValue = 0) {
				short ret;
				if (short.TryParse(ReadString(key), out ret))
					return ret;
				else
					return defaultValue;
			}

			public float ReadFloat(string key, float defaultValue = 0.0f) {
				float ret;
				if (float.TryParse(ReadString(key).Replace(',', '.'), NumberStyles.Any, culture, out ret))
					return ret;
				else
					return defaultValue;
			}

			public double ReadDouble(string key, double defaultValue = 0.0) {
				double ret;
				if (double.TryParse(ReadString(key).Replace(',', '.'), NumberStyles.Any, culture, out ret))
					return ret;
				else
					return defaultValue;
			}

			public string ConcatenatedValues() {
				var sb = new StringBuilder();
				foreach (var v in OrderedEntries)
					sb.Append(v.Value);
				return sb.ToString();
			}

			/// <summary>
			///  returns index of key:value
			/// </summary>
			/// <param name="p"></param>
			/// <returns></returns>
			public int FindValueIndex(string p) {
				for (int i = 0; i < OrderedEntries.Count; i++)
					if (OrderedEntries[i].Value == p)
						return i;
				return -1;
			}


			public void WriteTo(StreamWriter sw) {
				sw.Write('[');
				sw.Write(Name);
				sw.WriteLine(']');
				foreach (var kvp in OrderedEntries) {
					sw.Write(kvp.Key);
					sw.Write('=');
					sw.WriteLine(kvp.Value);
				}
			}

		}

		public void Save(string filename) {
			var sw = new StreamWriter(filename, false, Encoding.Default, 64 * 1024);
			foreach (var section in Sections) {
				section.WriteTo(sw);
				if (section != Sections.Last())
					sw.WriteLine();
			}
			sw.Flush();
			sw.Dispose();
		}

		/// <summary>
		/// Merges (and overrides) the entries from given ini files with this
		/// </summary>
		/// <param name="ini"></param>
		public void MergeWith(IniFile ini) {
			if (ini == null) return;

			foreach (var v in ini.Sections) {
				var ownSection = GetOrCreateSection(v.Name);
				// numbered arrays are 'appended' instead of overwritten
				if (IsObjectArray(v.Name)) {
					int number = 1 + int.Parse(ownSection.OrderedEntries.Last().Key);
					foreach (var kvp in v.OrderedEntries)
						ownSection.SetValue(number++.ToString(), kvp.Value);
				}
				else
					foreach (var kvp in v.OrderedEntries)
						ownSection.SetValue(kvp.Key, kvp.Value);
			}
		}

		private bool IsObjectArray(string p) {
			return new[] {
				"BuildingTypes",
				"AircraftTypes",
				"InfantryTypes",
				"OverlayTypes",
				"TerrainTypes",
				"SmudgeTypes",
				"VehicleTypes",
			}.Contains(p);
		}

	}
}