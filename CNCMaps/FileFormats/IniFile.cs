using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CNCMaps.VirtualFileSystem;
using System.Globalization;

namespace CNCMaps.FileFormats {

	public class IniFile : VirtualTextFile {

		public List<IniSection> Sections { get; set; }

		public IniSection CurrentSection {
			get;
			set;
		}

		public IniFile(Stream baseStream, string filename, int baseOffset, long fileSize, bool isBuffered = true)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
			Sections = new List<IniSection>();
			Parse();
		}

		public IniSection GetSection(string SectionName) {
			return Sections.Find(x => x.Name == SectionName);
		}

		void Parse() {
			CNCMaps.Utility.Logger.WriteLine("Parsing {0}", Path.GetFileName(this.FileName));
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
				IniSection iniSection = new IniSection(sectionName);
				Sections.Add(iniSection);
				CurrentSection = iniSection;
			}
			else if (CurrentSection != null) {
				return CurrentSection.ParseLine(line);
			}
			return 0;
		}

		void SetCurrentSection(string sectionName) {
			CurrentSection = Sections.Find(x => x.Name == sectionName);
		}

		public void SetCurrentSection(IniSection section) {
			if (this.Sections.Contains(section))
				this.CurrentSection = section;
			else
				throw new InvalidOperationException("Invalid section");
		}

		public string ReadString(string key) {
			return CurrentSection.ReadString(key);
		}

		public string ReadString(string section, string key) {
			if (this.CurrentSection.Name != section)
				SetCurrentSection(section);
			return ReadString(key);
		}

		public bool ReadBool(string key) {
			return CurrentSection.ReadBool(key);
		}

		public bool ReadBool(string section, string key) {
			if (this.CurrentSection.Name != section)
				SetCurrentSection(section);
			return ReadBool(key);
		}

		public class IniSection {

			public string Name { get; set; }

			public Dictionary<string, string> SortedEntries { get; set; }

			public List<KeyValuePair<string, string>> OrderedEntries { get; set; }

			static NumberFormatInfo culture = CultureInfo.InvariantCulture.NumberFormat;

			public IniSection(string name) {
				this.SortedEntries = new Dictionary<string, string>();
				this.OrderedEntries = new List<KeyValuePair<string, string>>();
				this.Name = name;
			}

			public override string ToString() {
				StringBuilder sb = new StringBuilder();
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
				string key, value;
				int pos = line.IndexOf("=");
				if (pos != -1) {
					key = line.Substring(0, pos);
					value = line.Substring(pos + 1);
					FixLine(ref key);
					FixLine(ref value);
					SetValue(key, value);
					return 1;
				}
				return 0;
			}

			public void SetValue(string key, string value) {
				if (!SortedEntries.ContainsKey(key)) {
					OrderedEntries.Add(new KeyValuePair<string, string>(key, value));
				}
				SortedEntries[key] = value;
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

			static string[] TrueValues = { "yes", "1", "true", "on" };
			static string[] FalseValues = { "no", "0", "false", "off" };

			public bool ReadBool(string key, bool defaultValue = false) {
				string entry = ReadString(key);
				if (TrueValues.Contains(entry, StringComparer.InvariantCultureIgnoreCase))
					return true;
				else if (FalseValues.Contains(entry, StringComparer.InvariantCultureIgnoreCase))
					return false;
				else return defaultValue;
			}

			public string ReadString(string key, string defaultValue = "") {
				string ret;
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

			public double ReadDouble(string key, double defaultValue = 0.0) {
				double ret;
				if (double.TryParse(ReadString(key), NumberStyles.Any, culture, out ret))
					return ret;
				else
					return defaultValue;
			}

			public string ConcatenatedValues() {
				StringBuilder sb = new StringBuilder();
				foreach (var v in this.OrderedEntries)
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

		}
	}
}