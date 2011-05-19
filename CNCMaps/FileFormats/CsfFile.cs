using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.FileFormats {

	class CsfFile : VirtualFile {

		public CsfFile(Stream baseStream, string filename, bool isBuffered = true) : this(baseStream, filename, 0, baseStream.Length, isBuffered) { }

		public CsfFile(Stream baseStream, string filename, int baseOffset, long fileSize, bool isBuffered = true)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
			Parse();
		}

		Dictionary<string, CsfEntry> LabelMap = new Dictionary<string, CsfEntry>();

		[StructLayout(LayoutKind.Sequential, Size = 24, Pack = 1)]
		struct CsfHeader {
			public int id;
			public int flags1;
			public int numlabels;
			public int numextravalues;
			public int zero;
			public int language;
		}

		class CsfEntry {

			public string Value { get; set; }

			public string ExtraValue { get; set; }

			public CsfEntry(string value, string extraValue) {
				this.Value = value;
				this.ExtraValue = extraValue;
			}
		}

		enum LANGUAGE : byte {
			US, ZERO1, GERMAN, FRENCH, ZERO2, ZERO3,
			ZERO4, ZERO5, KOREAN, CHINESE
		}

		static int CSF_File_id = BitConverter.ToInt32(Encoding.ASCII.GetBytes("CSF ").Reverse().ToArray(), 0);
		static int csf_label_id = BitConverter.ToInt32(Encoding.ASCII.GetBytes("LBL ").Reverse().ToArray(), 0);
		static int csf_string_id = BitConverter.ToInt32(Encoding.ASCII.GetBytes("STR ").Reverse().ToArray(), 0);
		static int csf_string_w_id = BitConverter.ToInt32(Encoding.ASCII.GetBytes("STRW").Reverse().ToArray(), 0);

		int Parse() {
			CNCMaps.Utility.Logger.WriteLine("Parsing {0}", this.FileName);
			var header = CNCMaps.Utility.EzMarshal.ByteArrayToStructure<CsfHeader>(Read(Marshal.SizeOf(typeof(CsfHeader))));
			for (int i = 0; i < header.numlabels; i++) {
				ReadInt32();
				int flags = ReadInt32();
				string name = ReadString();
				if ((flags & 1) != 0) {
					bool has_extra_value = ReadInt32() == csf_string_w_id;
					string value = ReadWstring();
					string extraValue = "";
					if (has_extra_value)
						extraValue = ReadString();
					SetValue(name, value, extraValue);
				}
				else
					SetValue(name, "", "");
			}
			return 0;
		}

		private void SetValue(string name, string value, string extraValue) {
			this.LabelMap[name.ToLower()] = new CsfEntry(value, extraValue);
		}

		public string GetValue(string name) {
			CsfEntry csfEntry;
			if (this.LabelMap.TryGetValue(name, out csfEntry))
				return csfEntry.Value;
			return "";
		}

		string ConvertToString(string s) {
			StringBuilder r = new StringBuilder();
			for (int i = 0; i < s.Length; i++)
				r.Append((char)~s[i]);
			return r.ToString();
		}

		string ReadString() {
			return Encoding.ASCII.GetString(Read(ReadInt32()));
		}

		string ReadWstring() {
			return ConvertToString(Encoding.Unicode.GetString(Read(ReadInt32() * 2)));
		}
	}
}